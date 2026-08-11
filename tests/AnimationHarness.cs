using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;

namespace StandUpBuddy
{
    internal static class AnimationHarness
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            int value;
            CharacterKind kind = args.Length > 0 && int.TryParse(args[0], out value) && value >= 0 && value < CharacterCatalog.Count
                ? (CharacterKind)value
                : CharacterKind.OrangeCat;
            string screenshotPath = args.Length > 1 ? args[1] : null;
            using (ReminderForm reminder = new ReminderForm(kind, 5, screenshotPath != null, false))
            {
                if (screenshotPath != null)
                {
                    Timer captureTimer = new Timer();
                    captureTimer.Interval = 2300;
                    captureTimer.Tick += delegate
                    {
                        captureTimer.Stop();
                        CaptureReminder(reminder, screenshotPath);
                        reminder.Close();
                        captureTimer.Dispose();
                    };
                    reminder.Shown += delegate { captureTimer.Start(); };
                }
                Application.Run(reminder);
            }
        }

        private static void CaptureReminder(ReminderForm reminder, string path)
        {
            FieldInfo field = typeof(ReminderForm).GetField("surface", BindingFlags.Instance | BindingFlags.NonPublic);
            Bitmap surface = field == null ? null : field.GetValue(reminder) as Bitmap;
            if (surface == null) throw new InvalidOperationException("Unable to access the rendered reminder surface.");
            using (Bitmap snapshot = surface.Clone(new Rectangle(0, 0, surface.Width, surface.Height), PixelFormat.Format32bppArgb))
                snapshot.Save(path, ImageFormat.Png);
        }
    }
}
