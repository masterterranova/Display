using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Display;
using System.IO;

namespace DisplayTest
{
    class Program
    {
        static void Main(string[] args)
        {
            using (StreamWriter writer = new StreamWriter("log.txt"))
            {
                foreach (DisplayDevice x in DisplayDevice.AllDisplays())
                {
                    foreach (var prop in x.GetType().GetProperties())
                    {
                        string message = $"{prop.Name}: {prop.GetValue(x)}";

                        Console.WriteLine(message);
                        writer.WriteLine(message);
                    }

                    Console.WriteLine("\n");
                    writer.WriteLine();
                }
            }

            Console.ReadLine();
        }
    }
}
