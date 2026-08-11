using System;
using Microsoft.Win32;

namespace StandUpBuddy
{
    internal static class StartupManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "起身啦";

        public static bool IsEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                {
                    if (key == null) return false;
                    string storedCommand = Convert.ToString(key.GetValue(ValueName));
                    return string.Equals(storedCommand, CurrentCommand, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch { return false; }
        }

        public static void SetEnabled(bool enabled)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
            {
                if (key == null) throw new InvalidOperationException("无法打开 Windows 启动项设置。");
                if (enabled)
                    key.SetValue(ValueName, CurrentCommand, RegistryValueKind.String);
                else
                    key.DeleteValue(ValueName, false);
            }
        }

        private static string CurrentCommand
        {
            get { return "\"" + System.Windows.Forms.Application.ExecutablePath + "\" --startup"; }
        }
    }
}
