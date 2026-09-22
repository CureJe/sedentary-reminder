using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace StandUpBuddy
{
    internal enum ReminderResult
    {
        Completed,
        Snoozed,
        TimedOut
    }

    internal sealed class ReminderForm : Form
    {
        private const int WsExLayered = 0x00080000;
        private const int UlwAlpha = 0x00000002;
        private const byte AcSrcOver = 0x00;
        private const byte AcSrcAlpha = 0x01;
        private const uint SpiGetClientAreaAnimation = 0x1042;
        private const uint DibRgbColors = 0;

        private readonly CharacterKind character;
        private readonly int popupSeconds;
        private readonly bool preview;
        private readonly bool soundEnabled;
        private readonly bool reduceMotion;
        private readonly Stopwatch watch;
        private readonly Timer animationTimer;
        private readonly Timer closeTimer;
        private readonly Rectangle screenArea;
        private readonly Bitmap surface;
        private readonly Bitmap characterImage;
        private readonly float entranceDuration;
        private IntPtr layerScreenDc;
        private IntPtr layerMemoryDc;
        private IntPtr layerBitmap;
        private IntPtr layerOldBitmap;
        private bool finalFrameRendered;
        private Stream soundStream;
        private SoundPlayer soundPlayer;

        public ReminderResult Result { get; private set; }

        public ReminderForm(CharacterKind character, int popupSeconds, bool preview, bool soundEnabled)
        {
            this.character = character;
            this.popupSeconds = popupSeconds;
            this.preview = preview;
            this.soundEnabled = soundEnabled;
            Result = ReminderResult.TimedOut;
            entranceDuration = GetEntranceDuration(character);
            reduceMotion = !ClientAreaAnimationEnabled();

            Rectangle desktopArea = Screen.FromPoint(Cursor.Position).WorkingArea;
            screenArea = GetStageArea(desktopArea, character);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = screenArea;
            ShowInTaskbar = false;
            TopMost = true;
            KeyPreview = true;
            Cursor = Cursors.Hand;
            Text = "Time to move";
            surface = CreateLayerSurface(Math.Max(1, screenArea.Width), Math.Max(1, screenArea.Height));
            characterImage = LoadCharacterImage(character, surface.Size);

            watch = new Stopwatch();
            animationTimer = new Timer();
            animationTimer.Interval = 40;
            animationTimer.Tick += Animate;

            closeTimer = new Timer();
            closeTimer.Interval = Math.Max(1000, popupSeconds * 1000);
            closeTimer.Tick += delegate
            {
                Result = ReminderResult.TimedOut;
                Close();
            };

            Shown += HandleShown;
            MouseDown += HandleMouseDown;
            FormClosing += delegate
            {
                animationTimer.Stop();
                closeTimer.Stop();
                StopCharacterSound();
            };
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;
                parameters.ExStyle |= WsExLayered;
                return parameters;
            }
        }

        private void HandleShown(object sender, EventArgs e)
        {
            Activate();
            Focus();
            watch.Start();
            RenderFrame(reduceMotion ? entranceDuration : 0f);
            if (!reduceMotion) animationTimer.Start();
            if (!preview) closeTimer.Start();
            StartCharacterSound();
        }

        private void StartCharacterSound()
        {
            if (!soundEnabled) return;
            try
            {
                string resourceName = "StandUpBuddy.Audio." + GetSoundFilename(character);
                soundStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
                if (soundStream == null) return;
                soundPlayer = new SoundPlayer(soundStream);
                soundPlayer.Load();
                soundPlayer.Play();
            }
            catch
            {
                StopCharacterSound();
            }
        }

        private void StopCharacterSound()
        {
            if (soundPlayer != null)
            {
                try { soundPlayer.Stop(); } catch { }
                soundPlayer.Dispose();
                soundPlayer = null;
            }
            if (soundStream != null)
            {
                soundStream.Dispose();
                soundStream = null;
            }
        }

        private static string GetSoundFilename(CharacterKind kind)
        {
            if (kind == CharacterKind.Corgi) return "corgi.wav";
            if (kind == CharacterKind.RedPanda) return "red-panda.wav";
            if (kind == CharacterKind.MechGuardian) return "mech.wav";
            if (kind == CharacterKind.WebRanger) return "web-ranger.wav";
            return "cat.wav";
        }

        private void Animate(object sender, EventArgs e)
        {
            float elapsed = (float)watch.Elapsed.TotalSeconds;
            if (elapsed >= entranceDuration)
            {
                if (!finalFrameRendered)
                {
                    RenderFrame(entranceDuration);
                    finalFrameRendered = true;
                }
                animationTimer.Stop();
                return;
            }
            RenderFrame(elapsed);
        }

        private void HandleMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && !preview)
                Result = ReminderResult.Snoozed;
            else
                Result = ReminderResult.Completed;
            Close();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape || keyData == Keys.Enter || keyData == Keys.Space)
            {
                Result = ReminderResult.Completed;
                Close();
                return true;
            }
            if (!preview && keyData == Keys.S)
            {
                Result = ReminderResult.Snoozed;
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void RenderFrame(float elapsed)
        {
            using (Graphics g = Graphics.FromImage(surface))
            {
                g.Clear(Color.Transparent);
                using (Brush hitSurface = new SolidBrush(Color.FromArgb(1, 0, 0, 0)))
                    g.FillRectangle(hitSurface, 0, 0, surface.Width, surface.Height);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.CompositingQuality = CompositingQuality.HighSpeed;
                g.InterpolationMode = InterpolationMode.Bilinear;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

                if (character == CharacterKind.Corgi) DrawCorgiScene(g, elapsed);
                else if (character == CharacterKind.RedPanda) DrawRedPandaScene(g, elapsed);
                else if (character == CharacterKind.MechGuardian) DrawMechScene(g, elapsed);
                else if (character == CharacterKind.WebRanger) DrawWebRangerScene(g, elapsed);
                else DrawCatScene(g, elapsed);
            }
            PushLayer(surface);
        }

        private void DrawCatScene(Graphics g, float time)
        {
            float raw = Progress(time, 0f, 1.08f);
            float climb = EaseOutBack(raw);
            float bubble = EaseOutCubic(Progress(time, 0.82f, 0.48f));
            float height = Math.Min(surface.Height * .78f, surface.Width * .48f * characterImage.Height / characterImage.Width);
            float centerX = surface.Width * .29f;
            float finalY = surface.Height - height * .43f;
            float y = Lerp(surface.Height + height * .52f, finalY, climb);
            float energy = (float)Math.Sin(raw * Math.PI);

            DrawGroundShadow(g, centerX, surface.Height - 18f, height * .24f, climb * .38f);
            DrawCharacterImage(g, centerX, y, height, true, Lerp(3f, 0f, climb), 1f - energy * .025f, 1f + energy * .045f);

            if (bubble > 0f)
            {
                float width = Math.Min(surface.Width * .52f, 650f * UiScale());
                float panelHeight = Math.Min(surface.Height * .19f, 205f * UiScale());
                RectangleF rect = FitRect(centerX + height * .22f, surface.Height * .19f, width, panelHeight);
                DrawSpeechBubble(g, rect, bubble, "Take a break!", "Stretch and take a short walk");
            }
        }

        private void DrawCorgiScene(Graphics g, float time)
        {
            float raw = Progress(time, 0f, 1.02f);
            float run = EaseOutCubic(raw);
            float unfurl = EaseOutCubic(Progress(time, .78f, .5f));
            float width = Math.Min(surface.Width * .59f, surface.Height * .8f * characterImage.Width / characterImage.Height);
            float finalX = surface.Width * .31f;
            float x = Lerp(-width * .58f, finalX, run);
            float baseY = surface.Height * .67f;
            float y = baseY - (float)Math.Sin(raw * Math.PI) * surface.Height * .105f;
            float rotation = Lerp(-8f, 0f, run) - (float)Math.Sin(raw * Math.PI) * 3f;

            DrawSpeedLines(g, x - width * .42f, y, 1f - run, Color.FromArgb(190, 121, 67));
            DrawGroundShadow(g, finalX, surface.Height * .83f, width * .28f, run * .34f);
            if (raw > .62f) DrawDust(g, finalX - width * .28f, surface.Height * .82f, Progress(time, .62f, .58f));
            DrawCharacterImage(g, x, y, width, false, rotation, 1f + (float)Math.Sin(raw * Math.PI) * .035f, 1f - (float)Math.Sin(raw * Math.PI) * .035f);

            if (unfurl > 0f)
            {
                float ribbonWidth = Math.Min(surface.Width * .52f, 670f * UiScale());
                RectangleF ribbon = FitRect(finalX + width * .29f, surface.Height * .3f, ribbonWidth * unfurl, Math.Min(surface.Height * .17f, 180f * UiScale()));
                DrawRibbon(g, ribbon, unfurl, "Walk and hydrate", "Rest your shoulders and eyes");
            }
        }

        private void DrawRedPandaScene(Graphics g, float time)
        {
            float raw = Progress(time, 0f, 1.28f);
            float roll = EaseInOutCubic(raw);
            float width = Math.Min(surface.Width * .58f, surface.Height * .82f * characterImage.Width / characterImage.Height);
            float finalX = surface.Width * .32f;
            float finalY = surface.Height * .6f;
            float x = Lerp(-width * .46f, finalX, roll);
            float y = Lerp(surface.Height * .18f, finalY, roll) - (float)Math.Sin(raw * Math.PI) * surface.Height * .19f;
            float rotation = Lerp(-305f, 0f, roll);

            DrawGroundShadow(g, finalX, surface.Height * .86f, width * .3f, roll * .34f);
            DrawCharacterImage(g, x, y, width, false, rotation, 1f, 1f);
        }

        private void DrawMechScene(Graphics g, float time)
        {
            float raw = Progress(time, 0f, 1.18f);
            float land = EaseOutBack(raw);
            float impact = Progress(time, .98f, .42f);
            float hologram = EaseOutCubic(Progress(time, 1.16f, .52f));
            float height = Math.Min(surface.Height * .8f, surface.Width * .46f * characterImage.Height / characterImage.Width);
            float x = surface.Width * .32f;
            float finalY = surface.Height * .6f;
            float y = Lerp(-height * .62f, finalY, land);

            DrawGroundShadow(g, x, surface.Height * .91f, height * .24f, land * .42f);
            if (impact > 0f) DrawImpactRing(g, x, surface.Height * .89f, impact, UiScale());
            DrawCharacterImage(g, x, y, height, true, Lerp(-3f, 0f, land), 1f + (float)Math.Sin(raw * Math.PI) * .018f, 1f - (float)Math.Sin(raw * Math.PI) * .025f);

            if (hologram > 0f)
            {
                RectangleF panel = FitRect(surface.Width * .48f, surface.Height * .25f, Math.Min(surface.Width * .5f, 690f * UiScale()), Math.Min(surface.Height * .21f, 220f * UiScale()));
                DrawHologram(g, panel, hologram, "Time to recharge", "Roll your shoulders. Stand up. Move.");
            }
        }

        private void DrawWebRangerScene(Graphics g, float time)
        {
            float raw = Progress(time, 0f, 1.08f);
            float leap = EaseOutCubic(raw);
            float shoot = EaseOutCubic(Progress(time, .56f, .38f));
            float web = EaseOutCubic(Progress(time, .83f, .66f));
            float sign = EaseOutCubic(Progress(time, 1.34f, .46f));
            float height = Math.Min(surface.Height * .82f, surface.Width * .46f * characterImage.Height / characterImage.Width);
            float heroX = Lerp(surface.Width + height * .4f, surface.Width * .60f, leap);
            float heroY = Lerp(surface.Height * .16f, surface.Height * .59f, leap) - (float)Math.Sin(raw * Math.PI) * surface.Height * .14f;
            float webX = surface.Width * .82f;
            float webY = surface.Height * .28f;
            float poseWave = (float)Math.Sin(raw * Math.PI);
            float poseAngle = Lerp(18f, -3f, leap);
            float poseScaleX = 1f + poseWave * .025f;
            float poseScaleY = 1f - poseWave * .025f;

            if (shoot > 0f)
            {
                float radians = poseAngle * (float)Math.PI / 180f;
                float handX = height * .23f * poseScaleX;
                float handY = -height * .27f * poseScaleY;
                PointF hand = new PointF(
                    heroX + handX * (float)Math.Cos(radians) - handY * (float)Math.Sin(radians),
                    heroY + handX * (float)Math.Sin(radians) + handY * (float)Math.Cos(radians));
                PointF target = new PointF(Lerp(hand.X, webX, shoot), Lerp(hand.Y, webY, shoot));
                using (Pen shadow = new Pen(Color.FromArgb(110, 18, 27, 36), 6f * UiScale()))
                using (Pen silk = new Pen(Color.FromArgb(245, 245, 248, 250), 2.3f * UiScale()))
                {
                    g.DrawLine(shadow, hand, target);
                    g.DrawLine(silk, hand, target);
                }
            }

            if (web > 0f) DrawExpandingWeb(g, webX, webY, Math.Min(surface.Width, surface.Height) * .28f * web, web);
            DrawCharacterImage(g, heroX, heroY, height, true, poseAngle, poseScaleX, poseScaleY);

            if (sign > 0f)
            {
                float plaqueWidth = Math.Min(surface.Width * .5f, 650f * UiScale());
                RectangleF plaque = FitRect(webX - plaqueWidth / 2f, webY + Math.Min(surface.Width, surface.Height) * .19f, plaqueWidth, Math.Min(surface.Height * .17f, 185f * UiScale()));
                DrawWebPlaque(g, plaque, sign, "Take a break", "Stand up and stretch your back");
            }
        }

        private void DrawCharacterImage(Graphics g, float centerX, float centerY, float targetSize, bool sizeByHeight, float rotation, float scaleX, float scaleY)
        {
            float sourceSize = sizeByHeight ? characterImage.Height : characterImage.Width;
            float scale = targetSize / Math.Max(1f, sourceSize);
            GraphicsState state = g.Save();
            g.TranslateTransform(centerX, centerY);
            g.RotateTransform(rotation);
            g.ScaleTransform(scale * scaleX, scale * scaleY);
            g.DrawImage(characterImage, -characterImage.Width / 2f, -characterImage.Height / 2f, characterImage.Width, characterImage.Height);
            if (character == CharacterKind.RedPanda) DrawRedPandaSignText(g);
            g.Restore(state);
        }

        private void DrawRedPandaSignText(Graphics g)
        {
            float assetScale = characterImage.Width / 1100f;
            GraphicsState state = g.Save();
            g.TranslateTransform(-52f * assetScale, -376f * assetScale);
            g.RotateTransform(-8f);
            using (Font titleFont = new Font("Segoe UI", 36f * assetScale, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font subtitleFont = new Font("Segoe UI", 20f * assetScale, FontStyle.Regular, GraphicsUnit.Pixel))
            using (Brush titleBrush = new SolidBrush(Color.FromArgb(69, 44, 29)))
            using (Brush subtitleBrush = new SolidBrush(Color.FromArgb(112, 72, 45)))
            using (StringFormat center = new StringFormat())
            {
                center.Alignment = StringAlignment.Center;
                center.LineAlignment = StringAlignment.Center;
                g.DrawString("Time to move", titleFont, titleBrush, new RectangleF(-150f * assetScale, -52f * assetScale, 300f * assetScale, 58f * assetScale), center);
                g.DrawString("Stretch and walk", subtitleFont, subtitleBrush, new RectangleF(-150f * assetScale, 3f * assetScale, 300f * assetScale, 42f * assetScale), center);
            }
            g.Restore(state);
        }

        private static void DrawGroundShadow(Graphics g, float x, float y, float radius, float opacity)
        {
            if (opacity <= 0f) return;
            using (Brush shadow = new SolidBrush(Color.FromArgb((int)(95 * Clamp(opacity)), 20, 24, 28)))
                g.FillEllipse(shadow, x - radius, y - radius * .18f, radius * 2f, radius * .36f);
        }

        private void DrawSpeechBubble(Graphics g, RectangleF rect, float progress, string title, string subtitle)
        {
            float scale = Lerp(0.94f, 1f, progress);
            GraphicsState state = g.Save();
            g.TranslateTransform(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
            g.ScaleTransform(scale, scale);
            g.TranslateTransform(-rect.Width / 2f, -rect.Height / 2f);
            RectangleF local = new RectangleF(0, 0, rect.Width, rect.Height);
            using (GraphicsPath path = RoundRect(local, 30f))
            using (Brush shadow = new SolidBrush(Color.FromArgb((int)(42 * progress), 28, 33, 38)))
            using (Brush fill = new SolidBrush(Color.FromArgb((int)(246 * progress), 255, 252, 244)))
            using (Pen outline = new Pen(Color.FromArgb((int)(210 * progress), 54, 48, 43), 3f))
            {
                Matrix move = new Matrix();
                move.Translate(0, 10f);
                path.Transform(move);
                g.FillPath(shadow, path);
                move.Dispose();
            }
            using (GraphicsPath path = RoundRect(local, 30f))
            using (Brush fill = new SolidBrush(Color.FromArgb((int)(250 * progress), 255, 252, 244)))
            using (Pen outline = new Pen(Color.FromArgb((int)(220 * progress), 54, 48, 43), 3f))
            {
                g.FillPath(fill, path);
                g.DrawPath(outline, path);
            }
            PointF[] tail = { new PointF(12, local.Height - 46), new PointF(-28, local.Height - 12), new PointF(36, local.Height - 25) };
            using (Brush fill = new SolidBrush(Color.FromArgb((int)(250 * progress), 255, 252, 244)))
            using (Pen outline = new Pen(Color.FromArgb((int)(220 * progress), 54, 48, 43), 3f))
            {
                g.FillPolygon(fill, tail);
                g.DrawLines(outline, tail);
            }
            DrawMessageText(g, local, title, subtitle, Color.FromArgb(50, 46, 43), Color.FromArgb(112, 104, 96), progress);
            g.Restore(state);
        }

        private void DrawRibbon(Graphics g, RectangleF rect, float progress, string title, string subtitle)
        {
            if (rect.Width < 8f) return;
            PointF[] shape =
            {
                new PointF(rect.Left, rect.Top + 16), new PointF(rect.Right - 26, rect.Top + 16),
                new PointF(rect.Right, rect.Top + rect.Height / 2), new PointF(rect.Right - 26, rect.Bottom - 16),
                new PointF(rect.Left, rect.Bottom - 16), new PointF(rect.Left + 18, rect.Top + rect.Height / 2)
            };
            using (Brush shadow = new SolidBrush(Color.FromArgb((int)(45 * progress), 36, 30, 25)))
                g.FillPolygon(shadow, Offset(shape, 0, 10));
            using (Brush fill = new SolidBrush(Color.FromArgb((int)(255 * progress), 247, 194, 99)))
            using (Pen outline = new Pen(Color.FromArgb((int)(230 * progress), 63, 48, 37), 3f))
            {
                g.FillPolygon(fill, shape);
                g.DrawPolygon(outline, shape);
            }
            DrawMessageText(g, rect, title, subtitle, Color.FromArgb(52, 42, 34), Color.FromArgb(105, 77, 53), progress);
        }

        private void DrawHologram(Graphics g, RectangleF rect, float progress, string title, string subtitle)
        {
            float visibleHeight = rect.Height * progress;
            RectangleF live = new RectangleF(rect.X, rect.Bottom - visibleHeight, rect.Width, visibleHeight);
            using (Brush beam = new LinearGradientBrush(new PointF(live.Left, live.Bottom), new PointF(live.Left, live.Top), Color.FromArgb(20, 74, 230, 216), Color.FromArgb(110, 74, 230, 216)))
                g.FillPolygon(beam, new[] { new PointF(live.Left + 38, live.Bottom), new PointF(live.Right - 38, live.Bottom), new PointF(live.Right, live.Top), new PointF(live.Left, live.Top) });
            using (Pen edge = new Pen(Color.FromArgb((int)(220 * progress), 74, 230, 216), 3f))
            {
                g.DrawLine(edge, live.Left, live.Top, live.Right, live.Top);
                g.DrawLine(edge, live.Left, live.Top, live.Left, live.Top + 28);
                g.DrawLine(edge, live.Right, live.Top, live.Right, live.Top + 28);
            }
            if (progress > .65f)
                DrawMessageText(g, rect, title, subtitle, Color.FromArgb(18, 82, 92), Color.FromArgb(38, 111, 118), Progress(progress, .65f, .35f));
        }

        private void DrawWebPlaque(Graphics g, RectangleF rect, float progress, string title, string subtitle)
        {
            float top = rect.Top - 74f * progress;
            using (Pen shadow = new Pen(Color.FromArgb((int)(100 * progress), 25, 32, 42), 6f))
            using (Pen silk = new Pen(Color.FromArgb((int)(245 * progress), 245, 248, 250), 2.3f))
            {
                g.DrawLine(shadow, rect.Left + rect.Width * .22f, top, rect.Left + rect.Width * .22f, rect.Top);
                g.DrawLine(shadow, rect.Right - rect.Width * .22f, top, rect.Right - rect.Width * .22f, rect.Top);
                g.DrawLine(silk, rect.Left + rect.Width * .22f, top, rect.Left + rect.Width * .22f, rect.Top);
                g.DrawLine(silk, rect.Right - rect.Width * .22f, top, rect.Right - rect.Width * .22f, rect.Top);
            }
            RectangleF live = new RectangleF(rect.X, Lerp(rect.Y - 30f, rect.Y, progress), rect.Width, rect.Height);
            using (GraphicsPath path = RoundRect(live, 22f))
            using (Brush shadow = new SolidBrush(Color.FromArgb((int)(54 * progress), 18, 27, 36)))
            {
                Matrix move = new Matrix(); move.Translate(0, 9); path.Transform(move); g.FillPath(shadow, path); move.Dispose();
            }
            using (GraphicsPath path = RoundRect(live, 22f))
            using (Brush fill = new SolidBrush(Color.FromArgb((int)(248 * progress), 245, 247, 244)))
            using (Pen edge = new Pen(Color.FromArgb((int)(235 * progress), 37, 54, 67), 4f))
            {
                g.FillPath(fill, path);
                g.DrawPath(edge, path);
            }
            DrawMessageText(g, live, title, subtitle, Color.FromArgb(190, 66, 61), Color.FromArgb(74, 86, 95), progress);
        }

        private void DrawMessageText(Graphics g, RectangleF rect, string title, string subtitle, Color titleColor, Color subtitleColor, float opacity)
        {
            float scale = Math.Max(.72f, Math.Min(1.08f, rect.Width / 470f));
            using (Font titleFont = new Font("Segoe UI", 22f * scale, FontStyle.Bold))
            using (Font subtitleFont = new Font("Segoe UI", 10.5f * scale, FontStyle.Regular))
            using (Brush titleBrush = new SolidBrush(Color.FromArgb((int)(255 * Clamp(opacity)), titleColor)))
            using (Brush subtitleBrush = new SolidBrush(Color.FromArgb((int)(245 * Clamp(opacity)), subtitleColor)))
            using (StringFormat center = new StringFormat())
            {
                center.Alignment = StringAlignment.Center;
                center.LineAlignment = StringAlignment.Center;
                RectangleF titleRect = new RectangleF(rect.X + 18, rect.Y + rect.Height * .12f, rect.Width - 36, rect.Height * .48f);
                RectangleF subRect = new RectangleF(rect.X + 18, rect.Y + rect.Height * .57f, rect.Width - 36, rect.Height * .24f);
                g.DrawString(title, titleFont, titleBrush, titleRect, center);
                g.DrawString(subtitle, subtitleFont, subtitleBrush, subRect, center);
            }
            using (Font hintFont = new Font("Segoe UI", 8f * scale, FontStyle.Regular))
            using (Brush hintBrush = new SolidBrush(Color.FromArgb((int)(170 * Clamp(opacity)), subtitleColor)))
            using (StringFormat center = new StringFormat())
            {
                center.Alignment = StringAlignment.Center;
                g.DrawString(preview ? "Click to close preview" : "Click to dismiss / Right-click or S to snooze", hintFont, hintBrush, new RectangleF(rect.X, rect.Bottom - rect.Height * .2f, rect.Width, rect.Height * .16f), center);
            }
        }

        private static void DrawExpandingWeb(Graphics g, float x, float y, float radius, float opacity)
        {
            if (radius < 2f) return;
            using (Pen shadow = new Pen(Color.FromArgb((int)(95 * opacity), 17, 27, 36), 5f))
            using (Pen silk = new Pen(Color.FromArgb((int)(235 * opacity), 244, 247, 249), 2f))
            {
                for (int i = 0; i < 12; i++)
                {
                    double angle = Math.PI * 2 * i / 12.0;
                    PointF end = new PointF(x + (float)Math.Cos(angle) * radius, y + (float)Math.Sin(angle) * radius);
                    g.DrawLine(shadow, x, y, end.X, end.Y);
                    g.DrawLine(silk, x, y, end.X, end.Y);
                }
                for (int ring = 1; ring <= 4; ring++)
                {
                    float r = radius * ring / 4f;
                    RectangleF circle = new RectangleF(x - r, y - r, r * 2, r * 2);
                    g.DrawEllipse(shadow, circle);
                    g.DrawEllipse(silk, circle);
                }
            }
        }

        private static void DrawSpeedLines(Graphics g, float x, float y, float opacity, Color color)
        {
            using (Pen pen = new Pen(Color.FromArgb((int)(180 * Clamp(opacity)), color), 5f))
            {
                pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round;
                g.DrawLine(pen, x - 150, y - 68, x - 35, y - 68);
                g.DrawLine(pen, x - 190, y - 18, x - 54, y - 18);
                g.DrawLine(pen, x - 130, y + 33, x - 22, y + 33);
            }
        }

        private static void DrawDust(Graphics g, float x, float y, float progress)
        {
            using (Brush dust = new SolidBrush(Color.FromArgb((int)(110 * (1f - Clamp(progress))), 193, 162, 121)))
            {
                g.FillEllipse(dust, x - progress * 90, y - progress * 45, 42 + progress * 35, 26 + progress * 24);
                g.FillEllipse(dust, x - progress * 35, y - progress * 70, 31 + progress * 25, 25 + progress * 21);
            }
        }

        private static void DrawImpactRing(Graphics g, float x, float y, float progress, float scale)
        {
            float p = Clamp(progress);
            float radius = Lerp(15f, 170f, EaseOutCubic(p)) * scale;
            using (Pen ring = new Pen(Color.FromArgb((int)(190 * (1f - p)), 74, 230, 216), 5f * scale))
                g.DrawEllipse(ring, x - radius, y - radius * .22f, radius * 2, radius * .44f);
        }

        private void PushLayer(Bitmap bitmap)
        {
            NativePoint source = new NativePoint(0, 0);
            NativePoint destination = new NativePoint(screenArea.Left, screenArea.Top);
            NativeSize size = new NativeSize(bitmap.Width, bitmap.Height);
            BlendFunction blend = new BlendFunction();
            blend.BlendOp = AcSrcOver;
            blend.SourceConstantAlpha = 255;
            blend.AlphaFormat = AcSrcAlpha;
            if (!UpdateLayeredWindow(Handle, layerScreenDc, ref destination, ref size, layerMemoryDc, ref source, 0, ref blend, UlwAlpha))
                Text = "Transparent window initialization failed: " + Marshal.GetLastWin32Error();
        }

        private Bitmap CreateLayerSurface(int width, int height)
        {
            layerScreenDc = GetDC(IntPtr.Zero);
            layerMemoryDc = CreateCompatibleDC(layerScreenDc);
            BitmapInfo info = new BitmapInfo();
            info.Header.Size = (uint)Marshal.SizeOf(typeof(BitmapInfoHeader));
            info.Header.Width = width;
            info.Header.Height = -height;
            info.Header.Planes = 1;
            info.Header.BitCount = 32;
            info.Header.Compression = 0;

            IntPtr pixels;
            layerBitmap = CreateDIBSection(layerScreenDc, ref info, DibRgbColors, out pixels, IntPtr.Zero, 0);
            if (layerBitmap == IntPtr.Zero || pixels == IntPtr.Zero)
                throw new InvalidOperationException("Unable to create the transparent character window.");
            layerOldBitmap = SelectObject(layerMemoryDc, layerBitmap);
            return new Bitmap(width, height, width * 4, PixelFormat.Format32bppPArgb, pixels);
        }

        private float UiScale()
        {
            return Math.Max(.72f, Math.Min(1.15f, Math.Min(surface.Width / 1600f, surface.Height / 900f)));
        }

        private RectangleF FitRect(float x, float y, float width, float height)
        {
            float margin = 24f;
            if (x + width > surface.Width - margin) x = surface.Width - margin - width;
            if (x < margin) x = margin;
            if (y + height > surface.Height - margin) y = surface.Height - margin - height;
            if (y < margin) y = margin;
            return new RectangleF(x, y, width, height);
        }

        private static GraphicsPath RoundRect(RectangleF rect, float radius)
        {
            float diameter = radius * 2f;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static PointF[] Offset(PointF[] points, float x, float y)
        {
            PointF[] result = new PointF[points.Length];
            for (int i = 0; i < points.Length; i++) result[i] = new PointF(points[i].X + x, points[i].Y + y);
            return result;
        }

        private static Bitmap LoadCharacterImage(CharacterKind kind, Size stageSize)
        {
            string filename;
            if (kind == CharacterKind.Corgi) filename = "corgi.png";
            else if (kind == CharacterKind.RedPanda) filename = "red-panda.png";
            else if (kind == CharacterKind.MechGuardian) filename = "mech.png";
            else if (kind == CharacterKind.WebRanger) filename = "web-ranger.png";
            else filename = "cat.png";

            string resourceName = "StandUpBuddy.Assets." + filename;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null) throw new InvalidOperationException("Missing character asset: " + resourceName);
                using (Image source = Image.FromStream(stream))
                {
                    float target;
                    bool byHeight = kind == CharacterKind.OrangeCat || kind == CharacterKind.MechGuardian || kind == CharacterKind.WebRanger;
                    if (kind == CharacterKind.Corgi)
                        target = Math.Min(stageSize.Width * .59f, stageSize.Height * .8f * source.Width / source.Height);
                    else if (kind == CharacterKind.RedPanda)
                        target = Math.Min(stageSize.Width * .58f, stageSize.Height * .82f * source.Width / source.Height);
                    else if (kind == CharacterKind.MechGuardian)
                        target = Math.Min(stageSize.Height * .8f, stageSize.Width * .46f * source.Height / source.Width);
                    else if (kind == CharacterKind.WebRanger)
                        target = Math.Min(stageSize.Height * .82f, stageSize.Width * .46f * source.Height / source.Width);
                    else
                        target = Math.Min(stageSize.Height * .78f, stageSize.Width * .48f * source.Height / source.Width);

                    float current = byHeight ? source.Height : source.Width;
                    float scale = Math.Min(1f, target / Math.Max(1f, current));
                    int width = Math.Max(1, (int)Math.Round(source.Width * scale));
                    int height = Math.Max(1, (int)Math.Round(source.Height * scale));
                    Bitmap prepared = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
                    using (Graphics graphics = Graphics.FromImage(prepared))
                    {
                        graphics.CompositingMode = CompositingMode.SourceCopy;
                        graphics.CompositingQuality = CompositingQuality.HighQuality;
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        graphics.DrawImage(source, 0, 0, width, height);
                    }
                    return prepared;
                }
            }
        }

        private static Rectangle GetStageArea(Rectangle desktop, CharacterKind kind)
        {
            int width = Math.Max(1, (int)Math.Round(desktop.Width * .58));
            int height = Math.Max(1, (int)Math.Round(desktop.Height * .9));
            int left = kind == CharacterKind.WebRanger ? desktop.Right - width : desktop.Left;
            int top = desktop.Top + (desktop.Height - height) / 2;
            return new Rectangle(left, top, width, height);
        }

        private static float GetEntranceDuration(CharacterKind kind)
        {
            if (kind == CharacterKind.WebRanger) return 2f;
            if (kind == CharacterKind.MechGuardian) return 1.82f;
            if (kind == CharacterKind.RedPanda) return 1.42f;
            if (kind == CharacterKind.Corgi) return 1.48f;
            return 1.52f;
        }

        private static float Progress(float value, float start, float duration)
        {
            return Clamp((value - start) / Math.Max(.001f, duration));
        }

        private static float Clamp(float value) { return Math.Max(0f, Math.Min(1f, value)); }
        private static float Lerp(float from, float to, float amount) { return from + (to - from) * Clamp(amount); }
        private static float EaseOutCubic(float x) { return 1f - (float)Math.Pow(1f - Clamp(x), 3); }
        private static float EaseInOutCubic(float x)
        {
            x = Clamp(x);
            return x < .5f ? 4f * x * x * x : 1f - (float)Math.Pow(-2f * x + 2f, 3) / 2f;
        }
        private static float EaseOutBack(float x)
        {
            x = Clamp(x);
            const float c1 = 1.2f;
            const float c3 = c1 + 1f;
            return 1f + c3 * (float)Math.Pow(x - 1f, 3) + c1 * (float)Math.Pow(x - 1f, 2);
        }

        private static bool ClientAreaAnimationEnabled()
        {
            bool enabled;
            if (!SystemParametersInfo(SpiGetClientAreaAnimation, 0, out enabled, 0)) return true;
            return enabled;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (animationTimer != null) animationTimer.Dispose();
                if (closeTimer != null) closeTimer.Dispose();
                StopCharacterSound();
                if (characterImage != null) characterImage.Dispose();
                if (surface != null) surface.Dispose();
                if (layerOldBitmap != IntPtr.Zero && layerMemoryDc != IntPtr.Zero)
                {
                    SelectObject(layerMemoryDc, layerOldBitmap);
                    layerOldBitmap = IntPtr.Zero;
                }
                if (layerBitmap != IntPtr.Zero)
                {
                    DeleteObject(layerBitmap);
                    layerBitmap = IntPtr.Zero;
                }
                if (layerMemoryDc != IntPtr.Zero)
                {
                    DeleteDC(layerMemoryDc);
                    layerMemoryDc = IntPtr.Zero;
                }
                if (layerScreenDc != IntPtr.Zero)
                {
                    ReleaseDC(IntPtr.Zero, layerScreenDc);
                    layerScreenDc = IntPtr.Zero;
                }
            }
            base.Dispose(disposing);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            public int X;
            public int Y;
            public NativePoint(int x, int y) { X = x; Y = y; }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeSize
        {
            public int Width;
            public int Height;
            public NativeSize(int width, int height) { Width = width; Height = height; }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BitmapInfoHeader
        {
            public uint Size;
            public int Width;
            public int Height;
            public ushort Planes;
            public ushort BitCount;
            public uint Compression;
            public uint SizeImage;
            public int XPelsPerMeter;
            public int YPelsPerMeter;
            public uint ColorsUsed;
            public uint ColorsImportant;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BitmapInfo
        {
            public BitmapInfoHeader Header;
            public uint Colors;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BlendFunction
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr destinationDc, ref NativePoint destination, ref NativeSize size, IntPtr sourceDc, ref NativePoint source, int colorKey, ref BlendFunction blend, int flags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr dc);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr dc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr dc, IntPtr bitmap);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr handle);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CreateDIBSection(IntPtr dc, ref BitmapInfo bitmapInfo, uint usage, out IntPtr bits, IntPtr section, uint offset);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(uint action, uint parameter, out bool value, uint update);
    }
}
