using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

// This harness compiles the production forms and timers with in-memory settings.
// It never reads or writes the user's settings file or changes startup registration.
namespace StandUpBuddy
{
    internal sealed class AppSettings
    {
        public int IntervalMinutes = 1;
        public int PopupSeconds = 5;
        public string CharacterMode = "Rotate characters";
        public bool IsRunning = true;
        public bool SoundEnabled = false;
        public static AppSettings Load() { return new AppSettings(); }
        public void Save() { }
    }
}

internal static class NaturalCycleHarness
{
    private static readonly BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance;
    private static void Require(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
    }

    [STAThread]
    private static int Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Stopwatch elapsed = Stopwatch.StartNew();
        Exception failure = null;
        bool passed = false;
        int appearances = 0;
        double firstClosed = -1;
        double snoozedAt = -1;
        Form previous = null;
        HashSet<Form> seen = new HashSet<Form>();
        using (StandUpBuddy.MainForm main = new StandUpBuddy.MainForm(false))
        using (Timer observer = new Timer())
        {
            main.ShowInTaskbar = false;
            main.Opacity = 0;
            main.StartPosition = FormStartPosition.Manual;
            main.Location = new Point(-32000, -32000);
            observer.Interval = 100;
            observer.Tick += delegate
            {
                try
                {
                    double seconds = elapsed.Elapsed.TotalSeconds;
                    Require(seconds < 480, "Natural-cycle test exceeded eight minutes");
                    Form current = null;
                    foreach (Form form in Application.OpenForms)
                        if (form is StandUpBuddy.ReminderForm) current = form;
                    if (current != null && seen.Add(current))
                    {
                        appearances++;
                        current.Opacity = 0;
                        Require(appearances <= 3, "Duplicate or unexpected reminder");
                        Console.WriteLine("Reminder " + appearances + " at " + seconds.ToString("F1") + " seconds");
                        if (appearances == 1)
                            Require(seconds >= 59 && seconds < 75, "First reminder did not follow the one-minute timer");
                        if (appearances == 2)
                        {
                            Require(firstClosed >= 0 && seconds - firstClosed >= 59, "Second reminder arrived too early");
                            object[] keys = { new Message(), Keys.S };
                            current.GetType().GetMethod("ProcessCmdKey", Hidden).Invoke(current, keys);
                            Require(((StandUpBuddy.ReminderForm)current).Result == StandUpBuddy.ReminderResult.Snoozed,
                                    "S did not request snooze");
                            snoozedAt = seconds;
                        }
                        if (appearances == 3)
                        {
                            Require(snoozedAt >= 0 && seconds - snoozedAt >= 299,
                                    "Snooze did not wait five real minutes");
                            object[] keys = { new Message(), Keys.Escape };
                            current.GetType().GetMethod("ProcessCmdKey", Hidden).Invoke(current, keys);
                            Require(((StandUpBuddy.ReminderForm)current).Result == StandUpBuddy.ReminderResult.Completed,
                                    "Escape did not complete the reminder");
                        }
                        previous = current;
                    }
                    if (current == null && previous != null)
                    {
                        if (appearances == 1)
                        {
                            Require(((StandUpBuddy.ReminderForm)previous).Result == StandUpBuddy.ReminderResult.TimedOut,
                                    "First reminder did not auto-dismiss");
                            firstClosed = seconds;
                            Console.WriteLine("Automatic dismissal at " + seconds.ToString("F1") + " seconds");
                        }
                        DateTime deadline = (DateTime)typeof(StandUpBuddy.MainForm).GetField("nextReminder", Hidden).GetValue(main);
                        double remaining = (deadline - DateTime.Now).TotalSeconds;
                        if (appearances == 2)
                            Require(remaining > 295 && remaining <= 300, "Snooze deadline was not five minutes");
                        if (appearances == 3)
                        {
                            Require(remaining > 55 && remaining <= 60, "Normal interval did not resume after Escape");
                            passed = true;
                            observer.Stop();
                            Application.ExitThread();
                        }
                        previous = null;
                    }
                }
                catch (Exception error)
                {
                    failure = error;
                    observer.Stop();
                    Application.ExitThread();
                }
            };
            observer.Start();
            Application.Run(main);
        }
        if (failure != null || !passed)
        {
            Console.Error.WriteLine("FAIL: " + (failure == null ? "Message loop stopped early" : failure.ToString()));
            return 1;
        }
        Console.WriteLine("PASS: real-clock reminder, automatic timeout, second cycle, five-minute snooze, Escape and reschedule; "
                          + elapsed.Elapsed.TotalSeconds.ToString("F1") + " seconds. In-memory settings; sound off.");
        return 0;
    }
}
