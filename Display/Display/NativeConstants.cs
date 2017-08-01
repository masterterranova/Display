namespace Display
{
    internal class NativeConstants
    {
        public const uint EDD_GET_DEVICE_INTERFACE_NAME = 1;

        public const int DICS_FLAG_GLOBAL = 0x00000001;
        public const int DIREG_DEV = 0x00000001;
        public const int KEY_QUERY_VALUE = 0x1;

        public const int BUFFER_SIZE = 168; //guess

        public const string GUID_DEVINTERFACE_MONITOR = "{E6F07B5F-EE97-4a90-B076-33F57BF4EAA7}";

        public const int SM_XVIRTUALSCREEN = 76;
        public const int SM_YVIRTUALSCREEN = 77;
        public const int SM_CXVIRTUALSCREEN = 78;
        public const int SM_CYVIRTUALSCREEN = 79;
    }
}
