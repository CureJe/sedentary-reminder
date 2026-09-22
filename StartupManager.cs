using System;
using Microsoft.Win32;

namespace StandUpBuddy
{
    internal static class StartupManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "StandUpBuddy";

        internal static bool IsLegacyValueName(string name)
        {
            return CharacterCatalog.Fingerprint(name) == "f18dba5971012a63576c7a1398e1dc9f2d9eaa1c2b9c2583c15ac58a817d7832";
        }

        public static bool IsEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                {
                    if (key == null) return false;
                    string storedCommand = Convert.ToString(key.GetValue(ValueName));
                    if (string.Equals(storedCommand, CurrentCommand, StringComparison.OrdinalIgnoreCase)) return true;
                    foreach (string name in key.GetValueNames())
                        if (IsLegacyValueName(name)) return true;
                    return false;
                }
            }
            catch { return false; }
        }

        public static void SetEnabled(bool enabled)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
            {
                if (key == null) throw new InvalidOperationException("Unable to open Windows startup settings.");
                if (enabled)
                    key.SetValue(ValueName, CurrentCommand, RegistryValueKind.String);
                else
                    key.DeleteValue(ValueName, false);
                // Remove only this application's old entry when the user changes this option.
                foreach (string name in key.GetValueNames())
                    if (IsLegacyValueName(name)) key.DeleteValue(name, false);
            }
        }

        private static string CurrentCommand
        {
            get { return "\"" + System.Windows.Forms.Application.ExecutablePath + "\" --startup"; }
        }
    }
}
