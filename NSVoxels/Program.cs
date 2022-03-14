using NSVoxels.GUI.Boot;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NSVoxels
{
    public static class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();
        static void Main()
        {
            //AllocConsole();

            using (var frm  = new BootScreen())
            {
                Application.EnableVisualStyles();
                Application.Run(frm);
            }


        }
    }
}
