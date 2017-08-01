using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Display
{
    public class DisplayDevice
    {
        public string DisplayID
        {
            internal set;
            get;
        }

        public string PlugAndPlayID
        {
            internal set;
            get;
        }
        
        public string SerialNumber
        {
            internal set;
            get;
        }

        public string Model
        {
            internal set;
            get;
        }

        public string DisplayPath
        {
            internal set;
            get;
        }

        public string SHA256Hash
        {
            internal set;
            get;
        }

        public Rectangle Bounds
        {
            internal set;
            get;
        }

        public Rectangle VirtualBounds
        {
            internal set;
            get;
        }

        internal DisplayDevice(string displayid, string plugandplayid, string serialnumber, string model, string devicepath)
        {
            this.DisplayID = displayid;
            this.PlugAndPlayID = plugandplayid;
            this.SerialNumber = serialnumber;
            this.Model = model;
            this.DisplayPath = devicepath;

            this.SHA256Hash = SHA256Hash(displayid + plugandplayid + serialnumber + model);

            string SHA256Hash(string input)
            {
                SHA256 sha256 = SHA256Managed.Create();
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                byte[] hash = sha256.ComputeHash(bytes);

                StringBuilder result = new StringBuilder();

                for (int i = 0; i < hash.Length; i++)
                {
                    result.Append(hash[i].ToString("X2"));
                }

                return result.ToString();
            }
        }

        public static DisplayDevice[] AllDisplays()
        {
            DisplayDevice[] displaydevices = SetupAPI();

            (NativeStructures.DISPLAY_DEVICE, NativeStructures.DISPLAY_DEVICE)[] enumdisplaydevices = EnumDisplayDevicesWrapper();

            NativeStructures.MONITORINFOEX[] enummonitorinfoexs = EnumDisplayMonitorsWrapper();

            foreach (DisplayDevice item in displaydevices)
            {
                NativeStructures.DISPLAY_DEVICE enumdevice = enumdisplaydevices.FirstOrDefault(x => x.Item2.DeviceID.ToLower().Equals(item.DisplayPath)).Item1;

                NativeStructures.MONITORINFOEX enummonitor = enummonitorinfoexs.FirstOrDefault(x => new string(x.szDevice).TrimEnd((char)0).Equals(enumdevice.DeviceName));

                Rectangle bounds = Rectangle.FromLTRB(enummonitor.rcMonitor.left, enummonitor.rcMonitor.top, enummonitor.rcMonitor.right, enummonitor.rcMonitor.bottom);            
                item.Bounds = bounds;
                item.VirtualBounds = ToVirtual(bounds);
            }

            return displaydevices;

            Rectangle ToVirtual(Rectangle bounds)
            {
                Rectangle virtualscreen = new Rectangle(NativeFunctions.GetSystemMetrics(NativeConstants.SM_XVIRTUALSCREEN), 
                    NativeFunctions.GetSystemMetrics(NativeConstants.SM_YVIRTUALSCREEN),
                    NativeFunctions.GetSystemMetrics(NativeConstants.SM_CXVIRTUALSCREEN),
                    NativeFunctions.GetSystemMetrics(NativeConstants.SM_CYVIRTUALSCREEN));

                if (bounds.X >= 0 && bounds.Y < 0)
                {
                    return new Rectangle(bounds.X, bounds.Y + virtualscreen.Height, bounds.Width, bounds.Height);
                }
                else if (bounds.X < 0 && bounds.Y >= 0)
                {
                    return new Rectangle(bounds.X + virtualscreen.Width, bounds.Y, bounds.Width, bounds.Height);
                }
                else if (bounds.X < 0 && bounds.Y < 0)
                {
                    return new Rectangle(bounds.X + virtualscreen.Width, bounds.Y + virtualscreen.Height, bounds.Width, bounds.Height);
                }
                else
                {
                    return bounds;
                }
            }
        }

        private static (NativeStructures.DISPLAY_DEVICE, NativeStructures.DISPLAY_DEVICE)[] EnumDisplayDevicesWrapper()
        {
            List<(NativeStructures.DISPLAY_DEVICE, NativeStructures.DISPLAY_DEVICE)> returnvalue = new List<(NativeStructures.DISPLAY_DEVICE, NativeStructures.DISPLAY_DEVICE)>();

            NativeStructures.DISPLAY_DEVICE display = new NativeStructures.DISPLAY_DEVICE();
            display.cb = Marshal.SizeOf(display);

            for (uint id = 0; NativeFunctions.EnumDisplayDevices(null, id, ref display, NativeConstants.EDD_GET_DEVICE_INTERFACE_NAME); id++)
            {
                if (display.StateFlags.HasFlag(NativeStructures.DisplayDeviceStateFlags.AttachedToDesktop))
                {
                    NativeStructures.DISPLAY_DEVICE general = display;

                    display.cb = Marshal.SizeOf(display);

                    NativeFunctions.EnumDisplayDevices(display.DeviceName, 0, ref display, NativeConstants.EDD_GET_DEVICE_INTERFACE_NAME);

                    NativeStructures.DISPLAY_DEVICE specific = display;

                    returnvalue.Add((general, specific));

                    display.cb = Marshal.SizeOf(display);
                }
            }

            return returnvalue.ToArray();
        }

        private static NativeStructures.MONITORINFOEX[] EnumDisplayMonitorsWrapper()
        {
            MonitorInfoExCallback callback = new MonitorInfoExCallback();
            NativeStructures.MonitorEnumProc proc = new NativeStructures.MonitorEnumProc(callback.Callback);
            NativeFunctions.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, proc, IntPtr.Zero);

            return callback.MonitorInfoExList.ToArray();
        }

        private static DisplayDevice[] SetupAPI()
        {
            List<DisplayDevice> returnvalue = new List<DisplayDevice>();

            Guid displayguid = new Guid(NativeConstants.GUID_DEVINTERFACE_MONITOR);

            IntPtr displayshandle = NativeFunctions.SetupDiGetClassDevs(ref displayguid, null, IntPtr.Zero, (uint)(NativeStructures.DiGetClassFlags.DIGCF_PRESENT | NativeStructures.DiGetClassFlags.DIGCF_DEVICEINTERFACE));

            NativeStructures.SP_DEVICE_INTERFACE_DATA data = new NativeStructures.SP_DEVICE_INTERFACE_DATA();
            data.cbSize = Marshal.SizeOf(data);

            for (uint id = 0; NativeFunctions.SetupDiEnumDeviceInterfaces(displayshandle, IntPtr.Zero, ref displayguid, id, ref data); id++)
            {
                NativeStructures.SP_DEVINFO_DATA spdid = new NativeStructures.SP_DEVINFO_DATA();
                spdid.cbSize = (uint)Marshal.SizeOf(spdid);

                NativeStructures.SP_DEVICE_INTERFACE_DETAIL_DATA ndidd = new NativeStructures.SP_DEVICE_INTERFACE_DETAIL_DATA();

                if (IntPtr.Size == 8) //64 bit
                    ndidd.cbSize = 8;
                else //32 bit
                    ndidd.cbSize = 4 + Marshal.SystemDefaultCharSize;

                uint requiredsize = 0;
                uint buffer = NativeConstants.BUFFER_SIZE;

                if (NativeFunctions.SetupDiGetDeviceInterfaceDetail(displayshandle, ref data, ref ndidd, buffer, ref requiredsize, ref spdid))
                {
                    StringBuilder idbuffer = new StringBuilder((int)buffer);
                    NativeFunctions.CM_Get_Device_ID(spdid.DevInst, idbuffer, (int)buffer);

                    string[] temp = idbuffer.ToString().Split('\\');

                    (string serialnumber, string model) = EDIDReader(displayshandle, spdid);

                    returnvalue.Add(new DisplayDevice(temp[1], temp[2], serialnumber, model, ndidd.DevicePath));
                }
            }

            NativeFunctions.SetupDiDestroyDeviceInfoList(displayshandle);

            return returnvalue.ToArray();

            (string, string) EDIDReader(IntPtr pdevinfoset, NativeStructures.SP_DEVINFO_DATA deviceinfodata)
            {
                IntPtr deviceregistrykey = NativeFunctions.SetupDiOpenDevRegKey(pdevinfoset, ref deviceinfodata, NativeConstants.DICS_FLAG_GLOBAL, 0, NativeConstants.DIREG_DEV, NativeConstants.KEY_QUERY_VALUE);

                IntPtr buffer = Marshal.AllocHGlobal((int)256);

                RegistryValueKind regtype = RegistryValueKind.Binary;
                int length = 256;

                uint result = NativeFunctions.RegQueryValueEx(deviceregistrykey, "EDID", 0, ref regtype, buffer, ref length);

                NativeFunctions.RegCloseKey(deviceregistrykey);

                byte[] edid = new byte[256];
                Marshal.Copy(buffer, edid, 0, 256);
                Marshal.FreeHGlobal(buffer);

                int[] startbytes = { 0x36, 0x48, 0x5A, 0x6C };

                string serialfilter = new string(new char[] { (char)00, (char)00, (char)00, (char)0xff });
                string modelfilter = new string(new char[] { (char)00, (char)00, (char)00, (char)0xfc });

                string serialnumber = string.Empty;
                string model = string.Empty;

                foreach (int startbtye in startbytes)
                {
                    string possibleinfo = Encoding.Default.GetString(edid, startbtye, 18);

                    if (possibleinfo.Contains(serialfilter))
                    {
                        serialnumber = possibleinfo.Substring(4).Replace("\0", "").Trim();
                    }

                    if (possibleinfo.Contains(modelfilter))
                    {
                        model = possibleinfo.Substring(4).Replace("\0", "").Trim();
                    }
                }

                return (serialnumber, model);
            }
        }

        private class MonitorInfoExCallback
        {
            public List<NativeStructures.MONITORINFOEX> MonitorInfoExList = new List<NativeStructures.MONITORINFOEX>();

            public virtual bool Callback(IntPtr monitor, IntPtr hdc, IntPtr lprcMonitor, IntPtr lparam)
            {
                NativeStructures.MONITORINFOEX info = new NativeStructures.MONITORINFOEX();

                NativeFunctions.GetMonitorInfo(new HandleRef(null, monitor), info);

                this.MonitorInfoExList.Add(info);

                return true;
            }
        }
    }
}