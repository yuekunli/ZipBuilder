using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZipBuilder
{
    internal static class Program
    {

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int processId);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);


        private const int ATTACH_PARENT_PROCESS = -1;
        const int SW_HIDE = 0;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                //var handle = GetConsoleWindow();
                //if (handle != IntPtr.Zero)
                //{
                //    ShowWindow(handle, SW_HIDE);
                //}

                //ShowWindow(handle, SW_HIDE);

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
            else
            {
                AttachConsole(ATTACH_PARENT_PROCESS);

                new CommandLineMode().Run(args);
            }
        }
    }
}
