using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;

internal static class EnglishUiHarness
{
    private static readonly BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance;
    private static int checks;

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        checks++;
    }

    [STAThread]
    private static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Assembly app = Assembly.LoadFrom(Path.GetFullPath(args[0]));
        string output = Path.GetFullPath(args[1]);
        Directory.CreateDirectory(output);
        Type catalog = app.GetType("StandUpBuddy.CharacterCatalog", true);
        MethodInfo normalize = catalog.GetMethod("NormalizeDisplayName");
        string[] names = (string[])catalog.GetField("DisplayNames").GetValue(null);
        foreach (string name in names)
            Check((string)normalize.Invoke(null, new object[] { name }) == name, "English selection changed: " + name);
        Check((string)normalize.Invoke(null, new object[] { "unknown" }) == names[0], "Unknown selection must rotate");
        Check((string)normalize.Invoke(null, new object[] { null }) == names[0], "Null selection must rotate");
        if (args.Length > 2)
        {
            // Optional original source fixture, read from Git history by the test runner.
            string original = File.ReadAllText(args[2]);
            MatchCollection literals = Regex.Matches(original, "\"([^\"]+)\"");
            Check(literals.Count == 7, "Unexpected original character catalog");
            for (int i = 0; i < literals.Count; i++)
                Check((string)normalize.Invoke(null, new object[] { literals[i].Groups[1].Value }) == names[Math.Min(i, 5)], "Legacy selection migration failed at " + i);
        }

        Type mainType = app.GetType("StandUpBuddy.MainForm", true);
        using (Form main = (Form)Activator.CreateInstance(mainType, new object[] { false }))
        {
            main.ShowInTaskbar = false;
            main.Opacity = 0;
            main.StartPosition = FormStartPosition.Manual;
            main.Location = new Point(-32000, -32000);
            main.Show();
            main.PerformLayout();
            Inspect(main);
            using (Bitmap bitmap = new Bitmap(main.Width, main.Height))
            {
                main.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                bitmap.Save(Path.Combine(output, "settings.png"), ImageFormat.Png);
            }
            main.Size = main.MinimumSize;
            main.PerformLayout();
            Inspect(main);
        }

        Type reminderType = app.GetType("StandUpBuddy.ReminderForm", true);
        Type kindType = app.GetType("StandUpBuddy.CharacterKind", true);
        string[] scenes = { "Cat", "Corgi", "RedPanda", "Mech", "WebRanger" };
        for (int i = 0; i < scenes.Length; i++)
        {
            using (Form form = (Form)Activator.CreateInstance(reminderType, new object[] { Enum.ToObject(kindType, i), 5, true, false }))
            {
                Bitmap surface = (Bitmap)reminderType.GetField("surface", Hidden).GetValue(form);
                using (Graphics g = Graphics.FromImage(surface))
                {
                    g.Clear(Color.Transparent);
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    reminderType.GetMethod("Draw" + scenes[i] + "Scene", Hidden).Invoke(form, new object[] { g, 10f });
                }
                surface.Save(Path.Combine(output, scenes[i] + ".png"), ImageFormat.Png);
                Check(surface.Width > 0, "Missing render");
                object[] keyArgs = { new Message(), Keys.Escape };
                Check((bool)reminderType.GetMethod("ProcessCmdKey", Hidden).Invoke(form, keyArgs), "Escape not handled");
                Check(reminderType.GetProperty("Result").GetValue(form, null).ToString() == "Completed", "Escape did not dismiss");
            }
        }
        Console.WriteLine("PASS: " + checks + " checks; settings and five character renders saved to " + output);
    }

    private static void Inspect(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            Check(!Regex.IsMatch(child.Text, @"[\p{IsCJKUnifiedIdeographs}]"), "Non-English UI text");
            Check(child.Right <= parent.ClientSize.Width && child.Bottom <= parent.ClientSize.Height, "Control outside parent: " + child.Text);
            if (child is Label && !child.AutoSize)
            {
                Size size = TextRenderer.MeasureText(child.Text, child.Font);
                Check(size.Width <= child.Width, "Label is too narrow: " + child.Text);
            }
            Inspect(child);
        }
    }
}
