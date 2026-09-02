using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace EmmuRpc
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            TempAppLauncher.RemoveAbandonedDirectories();

            if (args.Length >= 2 && args[0] == "--headless-launch64")
            {
                string name = Encoding.UTF8.GetString(Convert.FromBase64String(args[1]));
                int autoExit = 0;
                if (args.Length >= 4 && args[2] == "--test-auto-exit-ms")
                    Int32.TryParse(args[3], out autoExit);
                TempAppLauncher.Launch(name, autoExit);
                return;
            }

            Application.Run(new LauncherWindow());
        }
    }

    internal sealed class LauncherWindow : Form
    {
        private readonly TextBox _searchBox;
        private readonly NeonPanel _resultsPanel;
        private readonly NeonListBox _resultsList;
        private readonly Label _idleHint;
        private readonly Label _countLabel;
        private readonly NeonButton _launchButton;
        private readonly Label _statusLabel;
        private bool _fillingFromCatalog;

        public LauncherWindow()
        {
            Text = "EMMU RPC";
            AccessibleName = "EMMU RPC";
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(680, 650);
            MinimumSize = new Size(600, 590);
            MaximumSize = new Size(900, 790);
            BackColor = Color.FromArgb(7, 10, 19);
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);

            Label heading = new Label();
            heading.Text = "EMMU RPC";
            heading.Font = new Font("Segoe UI Semibold", 27F, FontStyle.Bold, GraphicsUnit.Point);
            heading.ForeColor = Color.FromArgb(239, 244, 255);
            heading.AutoSize = false;
            heading.TextAlign = ContentAlignment.MiddleLeft;
            heading.SetBounds(126, 29, 484, 53);
            heading.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label subtitle = new Label();
            subtitle.Text = "PROCESS IDENTITY LAUNCHER";
            subtitle.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);
            subtitle.ForeColor = Color.FromArgb(0, 221, 255);
            subtitle.AutoSize = false;
            subtitle.TextAlign = ContentAlignment.MiddleLeft;
            subtitle.SetBounds(128, 78, 482, 25);
            subtitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            PictureBox logo = new PictureBox();
            logo.SetBounds(69, 34, 48, 48);
            logo.SizeMode = PictureBoxSizeMode.StretchImage;
            try { logo.Image = Icon.ToBitmap(); } catch { }

            Label sectionTag = new Label();
            sectionTag.Text = "// TARGET PROCESS";
            sectionTag.Font = new Font("Consolas", 9F, FontStyle.Bold, GraphicsUnit.Point);
            sectionTag.ForeColor = Color.FromArgb(159, 112, 255);
            sectionTag.AutoSize = false;
            sectionTag.SetBounds(70, 132, 300, 25);

            Label searchLabel = new Label();
            searchLabel.Text = "GAME / APPLICATION NAME";
            searchLabel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point);
            searchLabel.ForeColor = Color.FromArgb(185, 196, 219);
            searchLabel.SetBounds(70, 158, 310, 24);

            NeonPanel inputPanel = new NeonPanel();
            inputPanel.SetBounds(70, 184, 540, 49);
            inputPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            inputPanel.BorderColor = Color.FromArgb(0, 221, 255);

            Label prompt = new Label();
            prompt.Text = ">";
            prompt.Font = new Font("Consolas", 13F, FontStyle.Bold, GraphicsUnit.Point);
            prompt.ForeColor = Color.FromArgb(0, 234, 255);
            prompt.BackColor = Color.Transparent;
            prompt.TextAlign = ContentAlignment.MiddleCenter;
            prompt.SetBounds(8, 9, 25, 29);

            _searchBox = new TextBox();
            _searchBox.Font = new Font("Segoe UI", 11.5F, FontStyle.Regular, GraphicsUnit.Point);
            _searchBox.ForeColor = Color.FromArgb(239, 244, 255);
            _searchBox.BackColor = Color.FromArgb(14, 21, 37);
            _searchBox.BorderStyle = BorderStyle.None;
            _searchBox.SetBounds(39, 12, 486, 28);
            _searchBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _searchBox.AccessibleName = "Search game name";
            _searchBox.MaxLength = 180;
            NativeMethods.SetCueText(_searchBox, "Enter or search a game name...");
            _searchBox.TextChanged += delegate { UpdateSearchResults(); };
            _searchBox.KeyDown += SearchBoxOnKeyDown;
            inputPanel.Controls.Add(prompt);
            inputPanel.Controls.Add(_searchBox);

            _resultsPanel = new NeonPanel();
            _resultsPanel.SetBounds(70, 252, 540, 222);
            _resultsPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _resultsPanel.BorderColor = Color.FromArgb(133, 84, 255);
            _resultsPanel.Padding = new Padding(2);
            _resultsPanel.Visible = false;

            _resultsList = new NeonListBox();
            _resultsList.Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
            _resultsList.Dock = DockStyle.Fill;
            _resultsList.MouseClick += ResultsListOnMouseClick;
            _resultsList.KeyDown += ResultsListOnKeyDown;
            _resultsPanel.Controls.Add(_resultsList);

            _idleHint = new Label();
            _idleHint.Text = "AWAITING INPUT\r\n\r\nType a game name to scan the local database.\r\nCustom process names are supported.";
            _idleHint.Font = new Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point);
            _idleHint.ForeColor = Color.FromArgb(100, 121, 151);
            _idleHint.BackColor = Color.FromArgb(9, 14, 25);
            _idleHint.TextAlign = ContentAlignment.MiddleCenter;
            _idleHint.SetBounds(70, 252, 540, 222);
            _idleHint.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            _countLabel = new Label();
            _countLabel.Font = new Font("Consolas", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
            _countLabel.ForeColor = Color.FromArgb(0, 202, 222);
            _countLabel.BackColor = Color.Transparent;
            _countLabel.AutoSize = false;
            _countLabel.TextAlign = ContentAlignment.MiddleLeft;
            _countLabel.SetBounds(70, 480, 540, 26);
            _countLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            _launchButton = new NeonButton();
            _launchButton.Text = "LAUNCH APPLICATION";
            _launchButton.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold, GraphicsUnit.Point);
            _launchButton.ForeColor = Color.White;
            _launchButton.SetBounds(70, 516, 540, 50);
            _launchButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _launchButton.Click += LaunchButtonOnClick;

            _statusLabel = new Label();
            _statusLabel.Text = "";
            _statusLabel.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);
            _statusLabel.ForeColor = Color.FromArgb(138, 151, 179);
            _statusLabel.BackColor = Color.Transparent;
            _statusLabel.AutoSize = false;
            _statusLabel.AutoEllipsis = true;
            _statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            _statusLabel.SetBounds(70, 577, 540, 36);
            _statusLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            Label footer = new Label();
            footer.Text = "EMMU // LOCAL MODE // NO NETWORK REQUIRED";
            footer.Font = new Font("Consolas", 8F, FontStyle.Regular, GraphicsUnit.Point);
            footer.ForeColor = Color.FromArgb(70, 84, 111);
            footer.BackColor = Color.Transparent;
            footer.TextAlign = ContentAlignment.MiddleCenter;
            footer.SetBounds(70, 616, 540, 22);
            footer.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            Controls.Add(logo);
            Controls.Add(heading);
            Controls.Add(subtitle);
            Controls.Add(sectionTag);
            Controls.Add(searchLabel);
            Controls.Add(inputPanel);
            Controls.Add(_idleHint);
            Controls.Add(_resultsPanel);
            Controls.Add(_countLabel);
            Controls.Add(_launchButton);
            Controls.Add(_statusLabel);
            Controls.Add(footer);

            try
            {
                GameNameCatalog.Load();
                ShowIdleState("TYPE TO SEARCH  //  " + GameNameCatalog.Count.ToString("N0") + " GAMES OFFLINE");
            }
            catch (Exception ex)
            {
                _statusLabel.ForeColor = Color.FromArgb(190, 55, 55);
                _statusLabel.Text = "Could not load game list: " + ex.Message;
            }
        }

        private void SearchBoxOnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down && _resultsPanel.Visible && _resultsList.Items.Count > 0)
            {
                _resultsList.Focus();
                if (_resultsList.SelectedIndex < 0)
                    _resultsList.SelectedIndex = 0;
                e.SuppressKeyPress = true;
            }
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                if (_resultsPanel.Visible && _resultsList.SelectedItem != null)
                    FillFromCatalog(_resultsList.SelectedItem.ToString());
            }
            else if (e.KeyCode == Keys.Escape)
            {
                _searchBox.Clear();
                e.SuppressKeyPress = true;
            }
        }

        private void ResultsListOnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                if (_resultsList.SelectedItem != null)
                    FillFromCatalog(_resultsList.SelectedItem.ToString());
            }
        }

        private void ResultsListOnMouseClick(object sender, MouseEventArgs e)
        {
            int index = _resultsList.IndexFromPoint(e.Location);
            if (index >= 0)
                FillFromCatalog(_resultsList.Items[index].ToString());
        }

        private void LaunchButtonOnClick(object sender, EventArgs e)
        {
            LaunchRequestedApp();
        }

        private void LaunchRequestedApp()
        {
            string displayName = (_searchBox.Text ?? String.Empty).Trim();
            if (displayName.Length == 0)
            {
                _statusLabel.ForeColor = Color.FromArgb(190, 55, 55);
                _statusLabel.Text = "[ INPUT REQUIRED ] Enter a game or application name.";
                _searchBox.Focus();
                return;
            }

            _launchButton.Enabled = false;
            _statusLabel.ForeColor = Color.FromArgb(0, 221, 255);
            _statusLabel.Text = "[ INITIALIZING ] Creating process identity...";
            Refresh();

            try
            {
                Process process = TempAppLauncher.Launch(displayName, 0);
                _statusLabel.ForeColor = Color.FromArgb(52, 255, 167);
                _statusLabel.Text = "[ ONLINE ] " + displayName + "  //  PID " + process.Id;
            }
            catch (Exception ex)
            {
                _statusLabel.ForeColor = Color.FromArgb(255, 83, 128);
                _statusLabel.Text = "[ ERROR ] " + ex.Message;
            }
            finally
            {
                _launchButton.Enabled = true;
            }
        }

        private void UpdateSearchResults()
        {
            if (_fillingFromCatalog)
                return;

            string query = (_searchBox.Text ?? String.Empty).Trim();
            if (query.Length == 0)
            {
                ShowIdleState("TYPE TO SEARCH  //  " + GameNameCatalog.Count.ToString("N0") + " GAMES OFFLINE");
                return;
            }

            List<string> matches = GameNameCatalog.Search(_searchBox.Text, 100);
            _resultsList.BeginUpdate();
            try
            {
                _resultsList.Items.Clear();
                for (int i = 0; i < matches.Count; i++)
                    _resultsList.Items.Add(matches[i]);
                _resultsList.SelectedIndex = -1;
            }
            finally
            {
                _resultsList.EndUpdate();
            }

            if (matches.Count == 0)
            {
                _resultsPanel.Visible = false;
                _idleHint.Visible = true;
                _idleHint.Text = "CUSTOM IDENTITY\r\n\r\nNo database match found.\r\nYour typed name is ready for manual launch.";
                _countLabel.Text = "CUSTOM NAME READY  //  CLICK LAUNCH TO CONTINUE";
            }
            else
            {
                _idleHint.Visible = false;
                _resultsPanel.Visible = true;
                _resultsPanel.BringToFront();
                _countLabel.Text = matches.Count + " MATCH" + (matches.Count == 1 ? "" : "ES") + "  //  CLICK ONCE TO FILL NAME";
            }
        }

        private void FillFromCatalog(string gameName)
        {
            _fillingFromCatalog = true;
            try
            {
                _searchBox.Text = gameName;
                _searchBox.SelectionStart = _searchBox.TextLength;
                _resultsList.SelectedIndex = -1;
                _resultsPanel.Visible = false;
                _idleHint.Visible = true;
                _idleHint.Text = "TARGET LOCKED\r\n\r\n" + gameName + "\r\n\r\nClick LAUNCH APPLICATION when ready.";
                _countLabel.Text = "CATALOG NAME SELECTED  //  AWAITING LAUNCH COMMAND";
                _searchBox.Focus();
            }
            finally
            {
                _fillingFromCatalog = false;
            }
        }

        private void ShowIdleState(string status)
        {
            _resultsList.Items.Clear();
            _resultsList.SelectedIndex = -1;
            _resultsPanel.Visible = false;
            _idleHint.Visible = true;
            _idleHint.Text = "AWAITING INPUT\r\n\r\nType a game name to scan the local database.\r\nCustom process names are supported.";
            _countLabel.Text = status;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (LinearGradientBrush background = new LinearGradientBrush(
                ClientRectangle,
                Color.FromArgb(6, 9, 17),
                Color.FromArgb(11, 16, 31),
                90F))
                e.Graphics.FillRectangle(background, ClientRectangle);

            using (Pen cyanGlow = new Pen(Color.FromArgb(35, 0, 221, 255), 2F))
                e.Graphics.DrawEllipse(cyanGlow, -180, 90, 420, 420);
            using (Pen violetGlow = new Pen(Color.FromArgb(32, 143, 78, 255), 2F))
                e.Graphics.DrawEllipse(violetGlow, ClientSize.Width - 230, -190, 420, 420);

            using (LinearGradientBrush line = new LinearGradientBrush(
                new Rectangle(70, 112, Math.Max(1, ClientSize.Width - 140), 2),
                Color.FromArgb(0, 221, 255),
                Color.FromArgb(145, 78, 255),
                0F))
                e.Graphics.FillRectangle(line, 70, 112, Math.Max(1, ClientSize.Width - 140), 1);
        }
    }

    internal static class TempAppLauncher
    {
        private const string ResourceName = "EmmuRpc.Resources.GeneratedApp.exe";

        public static Process Launch(string displayName, int autoExitMilliseconds)
        {
            if (String.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Application name is required.", "displayName");

            string safeName = FileNameSanitizer.MakeSafe(displayName);
            string root = Path.Combine(Path.GetTempPath(), "EMMU-RPC");
            string directory = Path.Combine(root, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            string executablePath = Path.Combine(directory, safeName + ".exe");

            try
            {
                using (Stream input = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName))
                {
                    if (input == null)
                        throw new InvalidOperationException("Embedded application template is missing.");
                    using (FileStream output = new FileStream(executablePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        input.CopyTo(output);
                }

                try
                {
                    VersionResourceWriter.Apply(executablePath, displayName, safeName + ".exe");
                }
                catch
                {
                    // The executable filename and window title still carry the requested name.
                }

                string name64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(displayName));
                string cleanup64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(directory));
                string arguments = "--name64 " + name64 + " --cleanup-dir64 " + cleanup64;
                if (autoExitMilliseconds > 0)
                    arguments += " --test-auto-exit-ms " + autoExitMilliseconds;

                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = executablePath;
                info.Arguments = arguments;
                info.WorkingDirectory = directory;
                info.UseShellExecute = false;
                Process process = Process.Start(info);
                if (process == null)
                    throw new InvalidOperationException("Windows did not start the generated application.");
                return process;
            }
            catch
            {
                try { Directory.Delete(directory, true); } catch { }
                throw;
            }
        }

        public static void RemoveAbandonedDirectories()
        {
            string root = Path.Combine(Path.GetTempPath(), "EMMU-RPC");
            if (!Directory.Exists(root))
                return;

            try
            {
                foreach (string directory in Directory.GetDirectories(root))
                {
                    try
                    {
                        if (Directory.GetCreationTimeUtc(directory) < DateTime.UtcNow.AddHours(-24))
                            Directory.Delete(directory, true);
                    }
                    catch { }
                }
            }
            catch { }
        }
    }

    internal static class FileNameSanitizer
    {
        private static readonly string[] ReservedNames =
        {
            "CON", "PRN", "AUX", "NUL", "CLOCK$",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        public static string MakeSafe(string requestedName)
        {
            string normalized = requestedName.Normalize(NormalizationForm.FormC);
            StringBuilder builder = new StringBuilder(normalized.Length);
            bool previousWasSpace = false;

            foreach (char ch in normalized)
            {
                char replacement = ch;
                if (ch < 32 || ch == '<' || ch == '>' || ch == ':' || ch == '"' ||
                    ch == '/' || ch == '\\' || ch == '|' || ch == '?' || ch == '*')
                    replacement = ' ';

                if (Char.IsWhiteSpace(replacement))
                {
                    if (!previousWasSpace)
                        builder.Append(' ');
                    previousWasSpace = true;
                }
                else
                {
                    builder.Append(replacement);
                    previousWasSpace = false;
                }
            }

            string safe = builder.ToString().Trim(' ', '.');
            if (safe.Length == 0)
                safe = "Application";
            if (safe.Length > 80)
            {
                int length = 80;
                if (Char.IsHighSurrogate(safe[length - 1]))
                    length--;
                safe = safe.Substring(0, length).Trim(' ', '.');
            }

            string deviceStem = safe.Split('.')[0];
            foreach (string reserved in ReservedNames)
            {
                if (String.Equals(deviceStem, reserved, StringComparison.OrdinalIgnoreCase))
                {
                    safe = deviceStem + " App" + safe.Substring(deviceStem.Length);
                    break;
                }
            }
            return safe;
        }
    }

    internal static class NativeMethods
    {
        private const int EM_SETCUEBANNER = 0x1501;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        public static void SetCueText(TextBox textBox, string cue)
        {
            if (!textBox.IsHandleCreated)
            {
                textBox.HandleCreated += delegate { SendMessage(textBox.Handle, EM_SETCUEBANNER, IntPtr.Zero, cue); };
            }
            else
            {
                SendMessage(textBox.Handle, EM_SETCUEBANNER, IntPtr.Zero, cue);
            }
        }
    }
}

