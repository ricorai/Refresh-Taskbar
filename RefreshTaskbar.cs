using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace RefreshTaskbarApp
{
    internal static class Program
    {
        private const string MutexName = "RefreshTaskbar_SingleInstance_Mutex";
        internal const string MsgName = "RefreshTaskbar_Trigger_Msg";

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern int RegisterWindowMessage(string lpString);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [STAThread]
        private static void Main(string[] args)
        {
            try { SetProcessDPIAware(); } catch { }

            int wmTrigger = RegisterWindowMessage(MsgName);

            bool createdNew;
            using (Mutex mutex = new Mutex(true, MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    // Already running: broadcast refresh trigger and exit immediately
                    if (wmTrigger != 0)
                    {
                        PostMessage((IntPtr)0xFFFF /* HWND_BROADCAST */, (uint)wmTrigger, IntPtr.Zero, IntPtr.Zero);
                    }
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                using (RefreshTaskbarContext context = new RefreshTaskbarContext(wmTrigger))
                {
                    Application.Run(context);
                }
            }
        }
    }

    internal class RefreshTaskbarContext : ApplicationContext
    {
        private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValueName = "RefreshTaskbar";

        private NotifyIcon _notifyIcon;
        private ContextMenuStrip _contextMenu;
        private ToolStripMenuItem _itemStartup;
        private ToolStripMenuItem _itemClose;
        private MessageWindow _messageWindow;

        private static bool _isRefreshing = false;
        private static readonly object _refreshLock = new object();

        public RefreshTaskbarContext(int wmTrigger)
        {
            InitializeTray();

            _messageWindow = new MessageWindow(
                new Action(OnTaskbarCreated),
                new Action(TriggerRefreshFromMessage),
                wmTrigger
            );
        }

        private void InitializeTray()
        {
            _contextMenu = new ContextMenuStrip();
            _contextMenu.ShowImageMargin = false;

            _itemStartup = new ToolStripMenuItem("Open on startup");
            _itemStartup.CheckOnClick = false;
            _itemStartup.Checked = IsStartupEnabled();
            _itemStartup.Click += delegate(object sender, EventArgs e)
            {
                bool newState = !IsStartupEnabled();
                SetStartupEnabled(newState);
                _itemStartup.Checked = IsStartupEnabled();
            };

            _itemClose = new ToolStripMenuItem("Close");
            _itemClose.Click += delegate(object sender, EventArgs e)
            {
                ExitApp();
            };

            _contextMenu.Items.Add(_itemStartup);
            _contextMenu.Items.Add(_itemClose);

            _notifyIcon = new NotifyIcon();
            _notifyIcon.Text = "Refresh Taskbar";
            _notifyIcon.ContextMenuStrip = _contextMenu;

            Icon icon = LoadAppIcon();
            if (icon != null)
            {
                _notifyIcon.Icon = icon;
            }
            else
            {
                _notifyIcon.Icon = SystemIcons.Application;
            }

            _notifyIcon.MouseClick += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    ForceRefresh();
                }
            };

            _notifyIcon.DoubleClick += delegate(object sender, EventArgs e)
            {
                ForceRefresh();
            };

            _notifyIcon.Visible = true;
        }

        private Icon LoadAppIcon()
        {
            try
            {
                string dir = AppDomain.CurrentDomain.BaseDirectory;
                string icoPath = Path.Combine(dir, "refresh.ico");
                if (File.Exists(icoPath))
                {
                    return new Icon(icoPath);
                }

                return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                return null;
            }
        }

        private void OnTaskbarCreated()
        {
            try
            {
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Visible = true;
                }
            }
            catch { }
        }

        private void TriggerRefreshFromMessage()
        {
            ForceRefresh();
        }

        public static void ForceRefresh()
        {
            lock (_refreshLock)
            {
                if (_isRefreshing) return;
                _isRefreshing = true;
            }

            Thread worker = new Thread(new ThreadStart(delegate()
            {
                try
                {
                    Process[] explorers = Process.GetProcessesByName("explorer");
                    for (int i = 0; i < explorers.Length; i++)
                    {
                        try
                        {
                            explorers[i].Kill();
                        }
                        catch { }
                    }

                    int waitCount = 0;
                    while (Process.GetProcessesByName("explorer").Length > 0 && waitCount < 10)
                    {
                        Thread.Sleep(50);
                        waitCount++;
                    }

                    Thread.Sleep(200);

                    if (Process.GetProcessesByName("explorer").Length == 0)
                    {
                        string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                        string explorerPath = Path.Combine(winDir, "explorer.exe");
                        ProcessStartInfo psi = new ProcessStartInfo(explorerPath);
                        psi.UseShellExecute = true;
                        Process.Start(psi);
                    }
                }
                catch { }
                finally
                {
                    Thread.Sleep(600);
                    lock (_refreshLock)
                    {
                        _isRefreshing = false;
                    }
                }
            }));

            worker.IsBackground = true;
            worker.Start();
        }

        private static bool IsStartupEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false))
                {
                    if (key == null) return false;
                    object val = key.GetValue(RunValueName);
                    return val != null;
                }
            }
            catch
            {
                return false;
            }
        }

        private static void SetStartupEnabled(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true))
                {
                    if (key == null) return;
                    if (enable)
                    {
                        key.SetValue(RunValueName, "\"" + Application.ExecutablePath + "\"");
                    }
                    else
                    {
                        key.DeleteValue(RunValueName, false);
                    }
                }
            }
            catch { }
        }

        private void ExitApp()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }

            if (_messageWindow != null)
            {
                _messageWindow.DestroyHandle();
                _messageWindow = null;
            }

            ExitThread();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ExitApp();
            }
            base.Dispose(disposing);
        }
    }

    internal class MessageWindow : NativeWindow
    {
        private static readonly int WM_TASKBARCREATED = Program.RegisterWindowMessage("TaskbarCreated");
        private readonly Action _onTaskbarCreated;
        private readonly Action _onTriggerRefresh;
        private readonly int _wmTriggerRefresh;

        public MessageWindow(Action onTaskbarCreated, Action onTriggerRefresh, int wmTriggerRefresh)
        {
            _onTaskbarCreated = onTaskbarCreated;
            _onTriggerRefresh = onTriggerRefresh;
            _wmTriggerRefresh = wmTriggerRefresh;

            CreateParams cp = new CreateParams();
            cp.Parent = (IntPtr)(-3); // HWND_MESSAGE
            CreateHandle(cp);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_TASKBARCREATED && WM_TASKBARCREATED != 0)
            {
                if (_onTaskbarCreated != null)
                {
                    _onTaskbarCreated();
                }
            }
            else if (m.Msg == _wmTriggerRefresh && _wmTriggerRefresh != 0)
            {
                if (_onTriggerRefresh != null)
                {
                    _onTriggerRefresh();
                }
            }

            base.WndProc(ref m);
        }
    }
}
