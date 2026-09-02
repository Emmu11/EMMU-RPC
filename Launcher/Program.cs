using System;
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
        private readonly TextBox _nameBox;
        private readonly Button _launchButton;
        private readonly Label _statusLabel;

        public LauncherWindow()
        {
            Text = "EMMU RPC";
            AccessibleName = "EMMU RPC";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(560, 340);
            MinimumSize = new Size(480, 310);
            MaximumSize = new Size(760, 480);
            BackColor = Color.FromArgb(245, 247, 251);
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

            Label heading = new Label();
            heading.Text = "EMMU RPC";
            heading.Font = new Font("Segoe UI Semibold", 25F, FontStyle.Bold, GraphicsUnit.Point);
            heading.ForeColor = Color.FromArgb(25, 30, 43);
            heading.AutoSize = false;
            heading.TextAlign = ContentAlignment.MiddleCenter;
            heading.SetBounds(60, 42, 440, 55);
            heading.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Label subtitle = new Label();
            subtitle.Text = "Launch a lightweight app for Discord Registered Games";
            subtitle.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            subtitle.ForeColor = Color.FromArgb(100, 109, 128);
            subtitle.AutoSize = false;
            subtitle.TextAlign = ContentAlignment.MiddleCenter;
            subtitle.SetBounds(55, 95, 450, 28);
            subtitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            _nameBox = new TextBox();
            _nameBox.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            _nameBox.SetBounds(90, 151, 380, 34);
            _nameBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _nameBox.AccessibleName = "Enter application name";
            _nameBox.MaxLength = 180;
            NativeMethods.SetCueText(_nameBox, "Enter application name");
            _nameBox.KeyDown += NameBoxOnKeyDown;

            _launchButton = new Button();
            _launchButton.Text = "Launch";
            _launchButton.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold, GraphicsUnit.Point);
            _launchButton.ForeColor = Color.White;
            _launchButton.BackColor = Color.FromArgb(88, 101, 242);
            _launchButton.FlatStyle = FlatStyle.Flat;
            _launchButton.FlatAppearance.BorderSize = 0;
            _launchButton.Cursor = Cursors.Hand;
            _launchButton.SetBounds(190, 208, 180, 43);
            _launchButton.Anchor = AnchorStyles.Top;
            _launchButton.Click += LaunchButtonOnClick;

            _statusLabel = new Label();
            _statusLabel.Text = "";
            _statusLabel.ForeColor = Color.FromArgb(72, 81, 101);
            _statusLabel.AutoSize = false;
            _statusLabel.AutoEllipsis = true;
            _statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            _statusLabel.SetBounds(60, 266, 440, 38);
            _statusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Controls.Add(heading);
            Controls.Add(subtitle);
            Controls.Add(_nameBox);
            Controls.Add(_launchButton);
            Controls.Add(_statusLabel);
            AcceptButton = _launchButton;
        }

        private void NameBoxOnKeyDown(object sender, KeyEventArgs e)
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
            string displayName = (_nameBox.Text ?? String.Empty).Trim();
            if (displayName.Length == 0)
            {
                _statusLabel.ForeColor = Color.FromArgb(190, 55, 55);
                _statusLabel.Text = "Enter an application name first.";
                _nameBox.Focus();
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
