using System;
using System.Drawing;
using System.Windows.Forms;

namespace StandUpBuddy
{
    internal sealed class MainForm : Form
    {
        private static readonly Color Ink = Color.FromArgb(37, 43, 52);
        private static readonly Color Muted = Color.FromArgb(111, 119, 130);
        private static readonly Color Paper = Color.FromArgb(247, 244, 237);
        private static readonly Color Accent = Color.FromArgb(235, 94, 67);

        private readonly AppSettings settings;
        private readonly Timer clock;
        private readonly NotifyIcon trayIcon;
        private readonly NumericUpDown intervalInput;
        private readonly NumericUpDown durationInput;
        private readonly ComboBox characterInput;
        private readonly CheckBox soundInput;
        private readonly CheckBox startupInput;
        private readonly Label statusLabel;
        private readonly Label nextLabel;
        private readonly Button startButton;
        private DateTime nextReminder;
        private bool isRunning;
        private bool reminderVisible;
        private bool allowClose;
        private bool trayHintShown;
        private bool suppressStartupChange;
        private readonly bool startHidden;
        private bool initialVisibilityHandled;
        private int rotationIndex;

        public MainForm(bool startHidden)
        {
            this.startHidden = startHidden;
            settings = AppSettings.Load();
            Text = "起身啦";
            ClientSize = new Size(720, 610);
            MinimumSize = new Size(680, 590);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Paper;
            ForeColor = Ink;
            Font = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Regular);
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Information;

            Panel hero = new Panel();
            hero.Dock = DockStyle.Top;
            hero.Height = 178;
            hero.BackColor = Ink;
            hero.Paint += PaintHero;
            Controls.Add(hero);

            Label eyebrow = MakeLabel("STAND UP / 轻量久坐提醒", 13, FontStyle.Bold, Color.FromArgb(244, 177, 79));
            eyebrow.Location = new Point(38, 28);
            eyebrow.AutoSize = true;
            hero.Controls.Add(eyebrow);

            Label title = MakeLabel("把休息，放回工作节奏里。", 23, FontStyle.Bold, Color.White);
            title.Location = new Point(36, 57);
            title.AutoSize = true;
            hero.Controls.Add(title);

            Label subtitle = MakeLabel("安静计时，到点让角色从屏幕边缘登场提醒你活动。", 10, FontStyle.Regular, Color.FromArgb(205, 210, 217));
            subtitle.Location = new Point(40, 111);
            subtitle.AutoSize = true;
            hero.Controls.Add(subtitle);

            Panel card = new Panel();
            card.Location = new Point(34, 204);
            card.Size = new Size(652, 270);
            card.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            card.BackColor = Color.White;
            card.Paint += delegate(object sender, PaintEventArgs e)
            {
                using (Pen pen = new Pen(Color.FromArgb(224, 220, 211)))
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };
            Controls.Add(card);

            Label cardTitle = MakeLabel("提醒设置", 13, FontStyle.Bold, Ink);
            cardTitle.Location = new Point(26, 20);
            cardTitle.AutoSize = true;
            card.Controls.Add(cardTitle);

            card.Controls.Add(MakeFieldLabel("每隔", new Point(28, 75)));
            intervalInput = MakeNumberInput(1, 240, settings.IntervalMinutes);
            intervalInput.Location = new Point(106, 67);
            card.Controls.Add(intervalInput);
            Label minuteUnit = MakeFieldLabel("分钟提醒一次", new Point(208, 75));
            minuteUnit.AutoSize = true;
            card.Controls.Add(minuteUnit);

            card.Controls.Add(MakeFieldLabel("弹窗", new Point(362, 75)));
            durationInput = MakeNumberInput(5, 120, settings.PopupSeconds);
            durationInput.Location = new Point(438, 67);
            card.Controls.Add(durationInput);
            Label secondUnit = MakeFieldLabel("秒", new Point(540, 75));
            secondUnit.AutoSize = true;
            card.Controls.Add(secondUnit);

            card.Controls.Add(MakeFieldLabel("出场角色", new Point(28, 132)));
            characterInput = new ComboBox();
            characterInput.DropDownStyle = ComboBoxStyle.DropDownList;
            characterInput.FlatStyle = FlatStyle.Flat;
            characterInput.Font = new Font("Microsoft YaHei UI", 10f);
            characterInput.Location = new Point(106, 125);
            characterInput.Size = new Size(204, 30);
            characterInput.Items.AddRange(CharacterCatalog.DisplayNames);
            string configuredCharacter = settings.CharacterMode.IndexOf("蛛", StringComparison.Ordinal) >= 0
                ? CharacterCatalog.DisplayNames[5]
                : settings.CharacterMode;
            int selected = characterInput.Items.IndexOf(configuredCharacter);
            characterInput.SelectedIndex = selected >= 0 ? selected : 0;
            card.Controls.Add(characterInput);

            Label roleNote = MakeLabel("写实角色素材 · 五位角色独立动画", 8.5f, FontStyle.Regular, Muted);
            roleNote.Location = new Point(330, 132);
            roleNote.AutoSize = true;
            card.Controls.Add(roleNote);

            soundInput = new CheckBox();
            soundInput.Text = "播放角色提示音";
            soundInput.Font = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Regular);
            soundInput.ForeColor = Ink;
            soundInput.BackColor = Color.Transparent;
            soundInput.AutoSize = true;
            soundInput.Location = new Point(28, 168);
            soundInput.Checked = settings.SoundEnabled;
            card.Controls.Add(soundInput);

            startupInput = new CheckBox();
            startupInput.Text = "开机后自动运行（从托盘安静启动）";
            startupInput.Font = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Regular);
            startupInput.ForeColor = Ink;
            startupInput.BackColor = Color.Transparent;
            startupInput.AutoSize = true;
            startupInput.Location = new Point(248, 168);
            startupInput.Checked = StartupManager.IsEnabled();
            startupInput.CheckedChanged += StartupChanged;
            card.Controls.Add(startupInput);

            Button previewButton = MakeButton("预览提醒", Color.White, Ink, true);
            previewButton.Location = new Point(28, 207);
            previewButton.Size = new Size(128, 43);
            previewButton.Click += delegate { ShowReminder(true); };
            card.Controls.Add(previewButton);

            startButton = MakeButton("", Accent, Color.White, false);
            startButton.Location = new Point(170, 207);
            startButton.Size = new Size(154, 43);
            startButton.Click += ToggleRunning;
            card.Controls.Add(startButton);

            Label lightNote = MakeLabel("关闭窗口后仍在托盘安静运行", 8.5f, FontStyle.Regular, Muted);
            lightNote.Location = new Point(354, 220);
            lightNote.AutoSize = true;
            card.Controls.Add(lightNote);

            statusLabel = MakeLabel("", 9.5f, FontStyle.Bold, Accent);
            statusLabel.Location = new Point(38, 504);
            statusLabel.AutoSize = true;
            Controls.Add(statusLabel);

            nextLabel = MakeLabel("", 16f, FontStyle.Bold, Ink);
            nextLabel.Location = new Point(36, 530);
            nextLabel.AutoSize = true;
            Controls.Add(nextLabel);

            Label footer = MakeLabel("角色停稳后立即停止动画刷新；没有持续运行的背景动效。", 8.5f, FontStyle.Regular, Muted);
            footer.Location = new Point(38, 574);
            footer.AutoSize = true;
            footer.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            Controls.Add(footer);

            intervalInput.ValueChanged += SettingsChanged;
            durationInput.ValueChanged += SettingsChanged;
            characterInput.SelectedIndexChanged += SettingsChanged;
            soundInput.CheckedChanged += SettingsChanged;

            ContextMenuStrip trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("打开设置", null, delegate { RestoreWindow(); });
            trayMenu.Items.Add("立即预览", null, delegate { ShowReminder(true); });
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("退出", null, delegate { ExitApplication(); });
            trayIcon = new NotifyIcon();
            trayIcon.Icon = Icon;
            trayIcon.Text = "起身啦";
            trayIcon.Visible = true;
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.DoubleClick += delegate { RestoreWindow(); };

            clock = new Timer();
            clock.Interval = 1000;
            clock.Tick += ClockTick;
            clock.Start();

            isRunning = settings.IsRunning;
            nextReminder = DateTime.Now.AddMinutes(settings.IntervalMinutes);
            UpdateStatus();
            FormClosing += HandleFormClosing;
            Resize += HandleResize;
        }

        protected override void SetVisibleCore(bool value)
        {
            if (startHidden && !initialVisibilityHandled && value)
            {
                initialVisibilityHandled = true;
                base.SetVisibleCore(false);
                return;
            }
            base.SetVisibleCore(value);
        }

        private void PaintHero(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            int w = ((Control)sender).Width;
            using (Brush glow = new SolidBrush(Color.FromArgb(34, 244, 177, 79)))
                e.Graphics.FillEllipse(glow, w - 220, -110, 300, 300);
            using (Pen line = new Pen(Color.FromArgb(45, 255, 255, 255), 2))
            {
                e.Graphics.DrawArc(line, w - 155, 35, 92, 92, 205, 240);
                e.Graphics.DrawArc(line, w - 127, 64, 36, 36, 10, 300);
            }
        }

        private static Label MakeLabel(string text, float size, FontStyle style, Color color)
        {
            Label label = new Label();
            label.Text = text;
            label.Font = new Font("Microsoft YaHei UI", size, style);
            label.ForeColor = color;
            label.BackColor = Color.Transparent;
            return label;
        }

        private static Label MakeFieldLabel(string text, Point location)
        {
            Label label = MakeLabel(text, 10f, FontStyle.Bold, Ink);
            label.Location = location;
            label.Size = new Size(74, 28);
            return label;
        }

        private static NumericUpDown MakeNumberInput(int minimum, int maximum, int value)
        {
            NumericUpDown input = new NumericUpDown();
            input.Minimum = minimum;
            input.Maximum = maximum;
            input.Value = value;
            input.Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold);
            input.Size = new Size(90, 30);
            input.BorderStyle = BorderStyle.FixedSingle;
            return input;
        }

        private static Button MakeButton(string text, Color background, Color foreground, bool bordered)
        {
            Button button = new Button();
            button.Text = text;
            button.Font = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Bold);
            button.BackColor = background;
            button.ForeColor = foreground;
            button.FlatStyle = FlatStyle.Flat;
            button.Cursor = Cursors.Hand;
            button.FlatAppearance.BorderSize = bordered ? 1 : 0;
            button.FlatAppearance.BorderColor = Color.FromArgb(188, 187, 182);
            return button;
        }

        private void ToggleRunning(object sender, EventArgs e)
        {
            CaptureSettings();
            isRunning = !isRunning;
            if (isRunning) nextReminder = DateTime.Now.AddMinutes(settings.IntervalMinutes);
            settings.IsRunning = isRunning;
            settings.Save();
            UpdateStatus();
        }

        private void SettingsChanged(object sender, EventArgs e)
        {
            if (!IsHandleCreated) return;
            int previousInterval = settings.IntervalMinutes;
            CaptureSettings();
            if (isRunning && settings.IntervalMinutes != previousInterval)
                nextReminder = DateTime.Now.AddMinutes(settings.IntervalMinutes);
            settings.Save();
            UpdateStatus();
        }

        private void StartupChanged(object sender, EventArgs e)
        {
            if (suppressStartupChange) return;
            try
            {
                StartupManager.SetEnabled(startupInput.Checked);
            }
            catch (Exception error)
            {
                suppressStartupChange = true;
                startupInput.Checked = !startupInput.Checked;
                suppressStartupChange = false;
                MessageBox.Show(this, "无法修改开机自启设置。\n\n" + error.Message, "起身啦", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void CaptureSettings()
        {
            settings.IntervalMinutes = (int)intervalInput.Value;
            settings.PopupSeconds = (int)durationInput.Value;
            settings.CharacterMode = Convert.ToString(characterInput.SelectedItem);
            settings.IsRunning = isRunning;
            settings.SoundEnabled = soundInput.Checked;
        }

        private void ClockTick(object sender, EventArgs e)
        {
            if (isRunning && !reminderVisible && DateTime.Now >= nextReminder)
                ShowReminder(false);
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            if (!isRunning)
            {
                statusLabel.Text = "已暂停";
                nextLabel.Text = "需要时，再把节奏接回来";
                startButton.Text = "开始提醒";
                return;
            }

            TimeSpan left = nextReminder - DateTime.Now;
            if (left < TimeSpan.Zero) left = TimeSpan.Zero;
            statusLabel.Text = "正在安静计时";
            nextLabel.Text = string.Format("下次提醒  {0:00}:{1:00}", (int)left.TotalMinutes, left.Seconds);
            startButton.Text = "暂停提醒";
        }

        private CharacterKind SelectCharacter()
        {
            string selected = Convert.ToString(characterInput.SelectedItem);
            if (selected == CharacterCatalog.DisplayNames[0])
            {
                CharacterKind kind = (CharacterKind)(rotationIndex % CharacterCatalog.Count);
                rotationIndex++;
                return kind;
            }
            return CharacterCatalog.FromDisplayName(selected);
        }

        private void ShowReminder(bool preview)
        {
            if (reminderVisible) return;
            CaptureSettings();
            settings.Save();
            reminderVisible = true;
            try
            {
                CharacterKind selectedCharacter = SelectCharacter();
                using (ReminderForm reminder = new ReminderForm(selectedCharacter, settings.PopupSeconds, preview, settings.SoundEnabled))
                {
                    reminder.ShowDialog(this);
                    if (!preview)
                    {
                        if (reminder.Result == ReminderResult.Snoozed)
                            nextReminder = DateTime.Now.AddMinutes(5);
                        else
                            nextReminder = DateTime.Now.AddMinutes(settings.IntervalMinutes);
                    }
                }
            }
            finally
            {
                reminderVisible = false;
                UpdateStatus();
            }
        }

        private void HandleResize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized) Hide();
        }

        private void HandleFormClosing(object sender, FormClosingEventArgs e)
        {
            if (!allowClose && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                if (!trayHintShown)
                {
                    trayIcon.ShowBalloonTip(2500, "起身啦仍在运行", "双击托盘图标可重新打开设置。", ToolTipIcon.Info);
                    trayHintShown = true;
                }
                return;
            }
            CaptureSettings();
            settings.Save();
            trayIcon.Visible = false;
        }

        private void RestoreWindow()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void ExitApplication()
        {
            allowClose = true;
            trayIcon.Visible = false;
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (clock != null) clock.Dispose();
                if (trayIcon != null) trayIcon.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
