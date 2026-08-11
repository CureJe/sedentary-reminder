using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace StandUpBuddy
{
    internal static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        private static void Main(string[] args)
        {
            try { SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            bool startHidden = Array.Exists(args, delegate(string argument)
            {
                return string.Equals(argument, "--startup", StringComparison.OrdinalIgnoreCase);
            });
            Application.Run(new MainForm(startHidden));
        }
    }
}
