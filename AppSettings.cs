using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace StandUpBuddy
{
    internal sealed class AppSettings
    {
        public int IntervalMinutes = 45;
        public int PopupSeconds = 20;
        public string CharacterMode = "Rotate characters";
        public bool IsRunning = true;
        public bool SoundEnabled = true;

        private static string SettingsPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "StandUpBuddy",
                    "settings.ini");
            }
        }

        public static AppSettings Load()
        {
            AppSettings result = new AppSettings();
            try
            {
                if (!File.Exists(SettingsPath)) return result;
                Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (string rawLine in File.ReadAllLines(SettingsPath))
                {
                    int split = rawLine.IndexOf('=');
                    if (split <= 0) continue;
                    values[rawLine.Substring(0, split).Trim()] = rawLine.Substring(split + 1).Trim();
                }

                int number;
                bool flag;
                string text;
                if (values.TryGetValue("IntervalMinutes", out text) && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
                    result.IntervalMinutes = Math.Max(1, Math.Min(240, number));
                if (values.TryGetValue("PopupSeconds", out text) && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
                    result.PopupSeconds = Math.Max(5, Math.Min(120, number));
                if (values.TryGetValue("CharacterMode", out text) && !string.IsNullOrWhiteSpace(text))
                    result.CharacterMode = CharacterCatalog.NormalizeDisplayName(text);
                if (values.TryGetValue("IsRunning", out text) && bool.TryParse(text, out flag))
                    result.IsRunning = flag;
                if (values.TryGetValue("SoundEnabled", out text) && bool.TryParse(text, out flag))
                    result.SoundEnabled = flag;
                else if (values.TryGetValue("VoiceEnabled", out text) && bool.TryParse(text, out flag))
                    result.SoundEnabled = flag;
            }
            catch
            {
                // A damaged settings file should never prevent the reminder from starting.
            }
            return result;
        }

        public void Save()
        {
            string directory = Path.GetDirectoryName(SettingsPath);
            Directory.CreateDirectory(directory);
            File.WriteAllLines(SettingsPath, new[]
            {
                "IntervalMinutes=" + IntervalMinutes.ToString(CultureInfo.InvariantCulture),
                "PopupSeconds=" + PopupSeconds.ToString(CultureInfo.InvariantCulture),
                "CharacterMode=" + CharacterCatalog.NormalizeDisplayName(CharacterMode),
                "IsRunning=" + IsRunning.ToString(CultureInfo.InvariantCulture),
                "SoundEnabled=" + SoundEnabled.ToString(CultureInfo.InvariantCulture)
            });
        }
    }
}
