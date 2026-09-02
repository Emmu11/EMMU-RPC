using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace EmmuRpc.GeneratedApp
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            string displayName = "Application";
            string cleanupDirectory = null;
            int autoExitMilliseconds = 0;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--name64" && i + 1 < args.Length)
                {
                    try
                    {
                        displayName = Encoding.UTF8.GetString(Convert.FromBase64String(args[++i]));
                    }
                    catch
                    {
                        displayName = "Application";
                    }
                }
                else if (args[i] == "--cleanup-dir64" && i + 1 < args.Length)
                {
                    try
                    {
                        cleanupDirectory = Encoding.UTF8.GetString(Convert.FromBase64String(args[++i]));
                    }
                    catch
                    {
                        cleanupDirectory = null;
                    }
                }
                else if (args[i] == "--test-auto-exit-ms" && i + 1 < args.Length)
                {
                    Int32.TryParse(args[++i], out autoExitMilliseconds);
                }
            }

            if (String.IsNullOrWhiteSpace(displayName))
                displayName = "Application";

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                using (GeneratedWindow window = new GeneratedWindow(displayName, autoExitMilliseconds))
                    Application.Run(window);
            }
            finally
            {
                CleanupScheduler.Schedule(cleanupDirectory);
            }
        }
    }

    internal sealed class GeneratedWindow : Form
    {
        private readonly int _autoExitMilliseconds;
        private readonly NotifyIcon _trayIcon;
        private bool _allowExit;

        public GeneratedWindow(string displayName, int autoExitMilliseconds)
        {
            _autoExitMilliseconds = autoExitMilliseconds;
            Text = displayName;
            AccessibleName = displayName;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(520, 290);
            MinimumSize = new Size(360, 220);
            BackColor = Color.FromArgb(248, 249, 252);
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;

            _trayIcon = new NotifyIcon();
            _trayIcon.Icon = SystemIcons.Application;
            _trayIcon.Text = displayName.Length > 63 ? displayName.Substring(0, 63) : displayName;
            _trayIcon.Visible = true;
            _trayIcon.DoubleClick += delegate { RestoreFromTray(); };

            MenuItem openItem = new MenuItem("Open");
            openItem.DefaultItem = true;
            openItem.Click += delegate { RestoreFromTray(); };
            MenuItem exitItem = new MenuItem("Exit");
            exitItem.Click += delegate { ExitCompletely(); };
            _trayIcon.ContextMenu = new ContextMenu(new MenuItem[]
            {
                openItem,
                new MenuItem("-"),
                exitItem
            });

            Panel card = new Panel();
            card.BackColor = Color.White;
            card.Size = new Size(390, 145);
            card.Anchor = AnchorStyles.None;
            card.Location = new Point((ClientSize.Width - card.Width) / 2, (ClientSize.Height - card.Height) / 2);

            Label title = new Label();
            title.Text = displayName;
            title.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold, GraphicsUnit.Point);
            title.ForeColor = Color.FromArgb(30, 34, 45);
            title.TextAlign = ContentAlignment.MiddleCenter;
            title.AutoEllipsis = true;
            title.Dock = DockStyle.Top;
            title.Height = 88;

            Label status = new Label();
            status.Text = "Running";
            status.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            status.ForeColor = Color.FromArgb(86, 96, 116);
            status.TextAlign = ContentAlignment.TopCenter;
            status.Dock = DockStyle.Fill;

            card.Controls.Add(status);
            card.Controls.Add(title);
            Controls.Add(card);

            Resize += delegate
            {
                card.Location = new Point((ClientSize.Width - card.Width) / 2, (ClientSize.Height - card.Height) / 2);
                if (WindowState == FormWindowState.Minimized)
                    BeginInvoke(new MethodInvoker(HideToTray));
            };
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (_autoExitMilliseconds > 0)
            {
                System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
                timer.Interval = Math.Max(100, _autoExitMilliseconds);
                timer.Tick += delegate
                {
                    timer.Stop();
                    timer.Dispose();
                    ExitCompletely();
                };
                timer.Start();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_allowExit && e.CloseReason != CloseReason.WindowsShutDown)
            {
                e.Cancel = true;
                HideToTray();
                return;
            }
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }
            base.Dispose(disposing);
        }

        private void HideToTray()
        {
            ShowInTaskbar = false;
            Hide();
        }

        private void RestoreFromTray()
        {
            ShowInTaskbar = true;
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void ExitCompletely()
        {
            _allowExit = true;
            _trayIcon.Visible = false;
            Close();
        }
    }

    internal static class CleanupScheduler
    {
        public static void Schedule(string directory)
        {
            if (!IsOwnedTemporaryDirectory(directory))
                return;

            try
            {
                string command = "ping 127.0.0.1 -n 3 > nul & rmdir /s /q \"" + directory + "\"";
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
                info.Arguments = "/d /q /c \"" + command + "\"";
                info.WorkingDirectory = Path.GetTempPath();
                info.UseShellExecute = false;
                info.CreateNoWindow = true;
                info.WindowStyle = ProcessWindowStyle.Hidden;
                Process.Start(info);
            }
            catch
            {
                // A later EMMU RPC run also removes abandoned temporary directories.
            }
        }

        private static bool IsOwnedTemporaryDirectory(string directory)
        {
            if (String.IsNullOrWhiteSpace(directory))
                return false;

            try
            {
                string full = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar);
                string root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "EMMU-RPC"))
                    .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                return full.StartsWith(root, StringComparison.OrdinalIgnoreCase) &&
                       full.Length > root.Length;
            }
            catch
            {
                return false;
            }
        }
    }
}
