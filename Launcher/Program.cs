using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
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
        private readonly Button _searchButton;
        private readonly ListBox _resultsList;
        private readonly Label _countLabel;
        private readonly Button _launchButton;
        private readonly Label _statusLabel;

        public LauncherWindow()
        {
            Text = "EMMU RPC";
            AccessibleName = "EMMU RPC";
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(620, 590);
            MinimumSize = new Size(540, 520);
            MaximumSize = new Size(860, 780);
            BackColor = Color.FromArgb(245, 247, 251);
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

            Label heading = new Label();
            heading.Text = "EMMU RPC";
            heading.Font = new Font("Segoe UI Semibold", 25F, FontStyle.Bold, GraphicsUnit.Point);
            heading.ForeColor = Color.FromArgb(25, 30, 43);
            heading.AutoSize = false;
            heading.TextAlign = ContentAlignment.MiddleCenter;
            heading.SetBounds(60, 24, 500, 55);
            heading.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label subtitle = new Label();
            subtitle.Text = "Search 5,000 game names or enter your own";
            subtitle.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            subtitle.ForeColor = Color.FromArgb(100, 109, 128);
            subtitle.AutoSize = false;
            subtitle.TextAlign = ContentAlignment.MiddleCenter;
            subtitle.SetBounds(55, 75, 510, 28);
            subtitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label searchLabel = new Label();
            searchLabel.Text = "Game or application name";
            searchLabel.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point);
            searchLabel.ForeColor = Color.FromArgb(55, 63, 82);
            searchLabel.SetBounds(60, 116, 310, 24);

            _searchBox = new TextBox();
            _searchBox.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
            _searchBox.SetBounds(60, 141, 390, 32);
            _searchBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _searchBox.AccessibleName = "Search game name";
            _searchBox.MaxLength = 180;
            NativeMethods.SetCueText(_searchBox, "Search game name");
            _searchBox.TextChanged += delegate { UpdateSearchResults(); };
            _searchBox.KeyDown += SearchBoxOnKeyDown;

            _searchButton = new Button();
            _searchButton.Text = "Search";
            _searchButton.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point);
            _searchButton.ForeColor = Color.FromArgb(63, 72, 94);
            _searchButton.BackColor = Color.White;
            _searchButton.FlatStyle = FlatStyle.Flat;
            _searchButton.FlatAppearance.BorderColor = Color.FromArgb(205, 210, 222);
            _searchButton.Cursor = Cursors.Hand;
            _searchButton.SetBounds(460, 140, 100, 34);
            _searchButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _searchButton.Click += delegate { UpdateSearchResults(); _searchBox.Focus(); };

            _resultsList = new ListBox();
            _resultsList.Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
            _resultsList.ForeColor = Color.FromArgb(30, 34, 45);
            _resultsList.BackColor = Color.White;
            _resultsList.BorderStyle = BorderStyle.FixedSingle;
            _resultsList.IntegralHeight = false;
            _resultsList.HorizontalScrollbar = true;
            _resultsList.SetBounds(60, 188, 500, 250);
            _resultsList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _resultsList.DoubleClick += delegate { LaunchRequestedApp(); };
            _resultsList.KeyDown += ResultsListOnKeyDown;

            _countLabel = new Label();
            _countLabel.ForeColor = Color.FromArgb(100, 109, 128);
            _countLabel.AutoSize = false;
            _countLabel.TextAlign = ContentAlignment.MiddleLeft;
            _countLabel.SetBounds(60, 443, 500, 27);
            _countLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            _launchButton = new Button();
            _launchButton.Text = "Launch Selected";
            _launchButton.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold, GraphicsUnit.Point);
            _launchButton.ForeColor = Color.White;
            _launchButton.BackColor = Color.FromArgb(88, 101, 242);
            _launchButton.FlatStyle = FlatStyle.Flat;
            _launchButton.FlatAppearance.BorderSize = 0;
            _launchButton.Cursor = Cursors.Hand;
            _launchButton.SetBounds(60, 480, 500, 43);
            _launchButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _launchButton.Click += LaunchButtonOnClick;

            _statusLabel = new Label();
            _statusLabel.Text = "";
            _statusLabel.ForeColor = Color.FromArgb(72, 81, 101);
            _statusLabel.AutoSize = false;
            _statusLabel.AutoEllipsis = true;
            _statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            _statusLabel.SetBounds(60, 532, 500, 34);
            _statusLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            Controls.Add(heading);
            Controls.Add(subtitle);
            Controls.Add(searchLabel);
            Controls.Add(_searchBox);
            Controls.Add(_searchButton);
            Controls.Add(_resultsList);
            Controls.Add(_countLabel);
            Controls.Add(_launchButton);
            Controls.Add(_statusLabel);
            AcceptButton = _launchButton;

            try
            {
                GameNameCatalog.Load();
                UpdateSearchResults();
            }
            catch (Exception ex)
            {
                _statusLabel.ForeColor = Color.FromArgb(190, 55, 55);
                _statusLabel.Text = "Could not load game list: " + ex.Message;
            }
        }

        private void SearchBoxOnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down && _resultsList.Items.Count > 0)
            {
                _resultsList.Focus();
                if (_resultsList.SelectedIndex < 0)
                    _resultsList.SelectedIndex = 0;
                e.SuppressKeyPress = true;
            }
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                LaunchRequestedApp();
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
                LaunchRequestedApp();
            }
        }

        private void LaunchButtonOnClick(object sender, EventArgs e)
        {
            LaunchRequestedApp();
        }

        private void LaunchRequestedApp()
        {
            string displayName = _resultsList.SelectedItem as string;
            if (String.IsNullOrWhiteSpace(displayName))
                displayName = (_searchBox.Text ?? String.Empty).Trim();
            if (displayName.Length == 0)
            {
                _statusLabel.ForeColor = Color.FromArgb(190, 55, 55);
                _statusLabel.Text = "Search for a game or enter a custom name first.";
                _searchBox.Focus();
                return;
            }

            _launchButton.Enabled = false;
            _statusLabel.ForeColor = Color.FromArgb(72, 81, 101);
            _statusLabel.Text = "Launching…";
            Refresh();

            try
            {
                Process process = TempAppLauncher.Launch(displayName, 0);
                _statusLabel.ForeColor = Color.FromArgb(34, 135, 88);
                _statusLabel.Text = "Launched “" + displayName + "”  •  PID " + process.Id;
            }
            catch (Exception ex)
            {
                _statusLabel.ForeColor = Color.FromArgb(190, 55, 55);
                _statusLabel.Text = "Could not launch: " + ex.Message;
            }
            finally
            {
                _launchButton.Enabled = true;
            }
        }

        private void UpdateSearchResults()
        {
            List<string> matches = GameNameCatalog.Search(_searchBox.Text, 100);
            _resultsList.BeginUpdate();
            try
            {
                _resultsList.Items.Clear();
                for (int i = 0; i < matches.Count; i++)
                    _resultsList.Items.Add(matches[i]);
                if (_resultsList.Items.Count > 0)
                    _resultsList.SelectedIndex = 0;
            }
            finally
            {
                _resultsList.EndUpdate();
            }

            string query = (_searchBox.Text ?? String.Empty).Trim();
            if (query.Length == 0)
                _countLabel.Text = "Showing the first 100 of " + GameNameCatalog.Count.ToString("N0") + " games";
            else if (matches.Count == 0)
                _countLabel.Text = "No catalog match - Launch Selected will use your custom name";
            else
                _countLabel.Text = "Showing " + matches.Count + " matching result" + (matches.Count == 1 ? "" : "s");
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
