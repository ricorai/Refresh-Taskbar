using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        internal const string MsgTriggerName = "RefreshTaskbar_Trigger_Msg";
        internal const string MsgCloseName = "RefreshTaskbar_Close_Msg";

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

            int wmTrigger = RegisterWindowMessage(MsgTriggerName);
            int wmClose = RegisterWindowMessage(MsgCloseName);

            // Handle CLI flags
            if (args != null && args.Length > 0)
            {
                string arg0 = args[0].ToLowerInvariant();
                if (arg0 == "--close" || arg0 == "/close" || arg0 == "--stop" || arg0 == "/stop")
                {
                    if (wmClose != 0)
                    {
                        PostMessage((IntPtr)0xFFFF /* HWND_BROADCAST */, (uint)wmClose, IntPtr.Zero, IntPtr.Zero);
                    }
                    return;
                }
                if (arg0 == "--refresh" || arg0 == "/refresh" || arg0 == "-r")
                {
                    if (wmTrigger != 0)
                    {
                        PostMessage((IntPtr)0xFFFF /* HWND_BROADCAST */, (uint)wmTrigger, IntPtr.Zero, IntPtr.Zero);
                    }
                    return;
                }
            }

            bool createdNew;
            using (Mutex mutex = new Mutex(true, MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    // Already running in background: trigger instant refresh and exit
                    if (wmTrigger != 0)
                    {
                        PostMessage((IntPtr)0xFFFF /* HWND_BROADCAST */, (uint)wmTrigger, IntPtr.Zero, IntPtr.Zero);
                    }
                    return;
                }

                // Ensure desktop menu is removed if process terminates
                AppDomain.CurrentDomain.ProcessExit += delegate
                {
                    RefreshTaskbarContext.RemoveDesktopContextMenu();
                };

                try
                {
                    SystemEvents.SessionEnding += delegate
                    {
                        RefreshTaskbarContext.RemoveDesktopContextMenu();
                    };
                }
                catch { }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                using (RefreshTaskbarContext context = new RefreshTaskbarContext(wmTrigger, wmClose))
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
        private const string DesktopShellKey = @"Software\Classes\DesktopBackground\Shell\RefreshTaskbar";

        private NotifyIcon _notifyIcon;
        private ContextMenuStrip _contextMenu;
        private ToolStripMenuItem _itemRefresh;
        private ToolStripMenuItem _itemDesktopMenu;
        private ToolStripMenuItem _itemStartup;
        private ToolStripMenuItem _itemClose;
        private MessageWindow _messageWindow;

        private static bool _isRefreshing = false;
        private static readonly object _refreshLock = new object();
        private static bool _desktopMenuEnabled = true;

        public RefreshTaskbarContext(int wmTrigger, int wmClose)
        {
            // Register desktop context menu on startup while app is running
            SetDesktopContextMenu(true);

            InitializeTray();

            _messageWindow = new MessageWindow(
                new Action(OnTaskbarCreated),
                new Action(TriggerRefreshFromMessage),
                new Action(ExitApp),
                wmTrigger,
                wmClose
            );
        }

        private void InitializeTray()
        {
            _contextMenu = new ContextMenuStrip();
            _contextMenu.Renderer = new ModernMenuRenderer();
            _contextMenu.ShowImageMargin = false;
            _contextMenu.ShowCheckMargin = true;
            _contextMenu.Font = new Font("Segoe UI", 9.0f, FontStyle.Regular);

            // 1. Refresh Taskbar (Primary Action)
            _itemRefresh = new ToolStripMenuItem("Refresh Taskbar");
            _itemRefresh.Font = new Font("Segoe UI", 9.0f, FontStyle.Bold);
            _itemRefresh.ForeColor = Color.FromArgb(17, 17, 17);
            _itemRefresh.Click += delegate(object sender, EventArgs e)
            {
                ForceRefresh();
            };

            // 2. Desktop Right-Click Menu Toggle
            _itemDesktopMenu = new ToolStripMenuItem();
            _itemDesktopMenu.CheckOnClick = false;
            UpdateDesktopMenuIndicator();
            _itemDesktopMenu.Click += delegate(object sender, EventArgs e)
            {
                _desktopMenuEnabled = !_desktopMenuEnabled;
                SetDesktopContextMenu(_desktopMenuEnabled);
                UpdateDesktopMenuIndicator();
            };

            // 3. Open on Startup Toggle
            _itemStartup = new ToolStripMenuItem();
            _itemStartup.CheckOnClick = false;
            UpdateStartupIndicator();
            _itemStartup.Click += delegate(object sender, EventArgs e)
            {
                bool newState = !IsStartupEnabled();
                SetStartupEnabled(newState);
                UpdateStartupIndicator();
                UpdateTrayTooltip();
            };

            // 4. Close
            _itemClose = new ToolStripMenuItem("Close");
            _itemClose.ForeColor = Color.FromArgb(17, 17, 17);
            _itemClose.Click += delegate(object sender, EventArgs e)
            {
                ExitApp();
            };

            // Build menu
            _contextMenu.Items.Add(_itemRefresh);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(_itemDesktopMenu);
            _contextMenu.Items.Add(_itemStartup);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(_itemClose);

            // Dynamically refresh indicators whenever menu opens
            _contextMenu.Opening += delegate(object sender, System.ComponentModel.CancelEventArgs e)
            {
                UpdateDesktopMenuIndicator();
                UpdateStartupIndicator();
            };

            _notifyIcon = new NotifyIcon();
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

            UpdateTrayTooltip();

            // Left-click triggers instant force refresh
            _notifyIcon.MouseClick += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    ForceRefresh();
                }
            };

            // Double-click also triggers instant force refresh
            _notifyIcon.DoubleClick += delegate(object sender, EventArgs e)
            {
                ForceRefresh();
            };

            _notifyIcon.Visible = true;
        }

        private void UpdateDesktopMenuIndicator()
        {
            bool isRegistered = IsDesktopMenuRegistered();
            _itemDesktopMenu.Checked = isRegistered;
            if (isRegistered)
            {
                _itemDesktopMenu.Text = "Desktop right-click menu    [ON]";
                _itemDesktopMenu.ForeColor = Color.FromArgb(17, 17, 17);
            }
            else
            {
                _itemDesktopMenu.Text = "Desktop right-click menu    [OFF]";
                _itemDesktopMenu.ForeColor = Color.FromArgb(107, 114, 128);
            }
        }

        private void UpdateStartupIndicator()
        {
            bool enabled = IsStartupEnabled();
            _itemStartup.Checked = enabled;
            if (enabled)
            {
                _itemStartup.Text = "Open on startup    [ON]";
                _itemStartup.ForeColor = Color.FromArgb(17, 17, 17);
            }
            else
            {
                _itemStartup.Text = "Open on startup    [OFF]";
                _itemStartup.ForeColor = Color.FromArgb(107, 114, 128);
            }
        }

        private void UpdateTrayTooltip()
        {
            if (_notifyIcon == null) return;
            bool startup = IsStartupEnabled();
            string status = startup ? "Startup: ON" : "Startup: OFF";
            _notifyIcon.Text = string.Format("Refresh Taskbar\nClick to refresh | {0}", status);
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

        public static bool IsDesktopMenuRegistered()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(DesktopShellKey, false))
                {
                    return key != null;
                }
            }
            catch
            {
                return false;
            }
        }

        public static void SetDesktopContextMenu(bool enable)
        {
            try
            {
                if (enable)
                {
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(DesktopShellKey))
                    {
                        if (key != null)
                        {
                            key.SetValue("", "Refresh Taskbar");
                            key.SetValue("Icon", Application.ExecutablePath);
                            key.SetValue("Position", "Top");

                            using (RegistryKey cmdKey = key.CreateSubKey("command"))
                            {
                                if (cmdKey != null)
                                {
                                    cmdKey.SetValue("", "\"" + Application.ExecutablePath + "\" --refresh");
                                }
                            }
                        }
                    }
                }
                else
                {
                    RemoveDesktopContextMenu();
                }
            }
            catch { }
        }

        public static void RemoveDesktopContextMenu()
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(DesktopShellKey, false);
            }
            catch { }
        }

        private static bool IsStartupEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false))
                {
                    if (key == null) return false;
                    object val = key.GetValue(RunValueName);
                    if (val == null) return false;

                    // Self-healing: if path changed, update to current location
                    string currentPath = "\"" + Application.ExecutablePath + "\"";
                    string storedPath = val.ToString();
                    if (!string.Equals(storedPath, currentPath, StringComparison.OrdinalIgnoreCase))
                    {
                        using (RegistryKey writeKey = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true))
                        {
                            if (writeKey != null)
                            {
                                writeKey.SetValue(RunValueName, currentPath);
                            }
                        }
                    }
                    return true;
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
            // Remove desktop context menu cleanly on exit
            RemoveDesktopContextMenu();

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

    internal class ModernMenuRenderer : ToolStripProfessionalRenderer
    {
        public ModernMenuRenderer() : base(new LightColorTable()) { }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Selected ? Color.Black : (e.Item.ForeColor.IsEmpty ? Color.FromArgb(17, 17, 17) : e.Item.ForeColor);
            base.OnRenderItemText(e);
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            Rectangle r = new Rectangle(e.ImageRectangle.X + 2, e.ImageRectangle.Y + 2, 14, 14);
            using (SolidBrush b = new SolidBrush(Color.FromArgb(17, 17, 17))) // Pure black badge
            using (Pen p = new Pen(Color.White, 2.0f))                         // Pure white checkmark tick
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillEllipse(b, r);

                Point[] checkPoints = new Point[] {
                    new Point(r.X + 3, r.Y + 7),
                    new Point(r.X + 6, r.Y + 10),
                    new Point(r.X + 11, r.Y + 4)
                };
                e.Graphics.DrawLines(p, checkPoints);
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.Height / 2;
            using (Pen p = new Pen(Color.FromArgb(229, 231, 235), 1.0f))
            {
                e.Graphics.DrawLine(p, 8, y, e.Item.Width - 8, y);
            }
        }
    }

    internal class LightColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return Color.White; } }
        public override Color MenuBorder { get { return Color.FromArgb(209, 213, 219); } }
        public override Color MenuItemBorder { get { return Color.Transparent; } }
        public override Color MenuItemSelected { get { return Color.FromArgb(243, 244, 246); } }
        public override Color MenuItemSelectedGradientBegin { get { return Color.FromArgb(243, 244, 246); } }
        public override Color MenuItemSelectedGradientEnd { get { return Color.FromArgb(243, 244, 246); } }
        public override Color CheckBackground { get { return Color.Transparent; } }
        public override Color CheckSelectedBackground { get { return Color.Transparent; } }
        public override Color ImageMarginGradientBegin { get { return Color.White; } }
        public override Color ImageMarginGradientMiddle { get { return Color.White; } }
        public override Color ImageMarginGradientEnd { get { return Color.White; } }
        public override Color SeparatorDark { get { return Color.FromArgb(229, 231, 235); } }
        public override Color SeparatorLight { get { return Color.White; } }
    }

    internal class MessageWindow : NativeWindow
    {
        private static readonly int WM_TASKBARCREATED = Program.RegisterWindowMessage("TaskbarCreated");
        private readonly Action _onTaskbarCreated;
        private readonly Action _onTriggerRefresh;
        private readonly Action _onClose;
        private readonly int _wmTriggerRefresh;
        private readonly int _wmClose;

        public MessageWindow(Action onTaskbarCreated, Action onTriggerRefresh, Action onClose, int wmTriggerRefresh, int wmClose)
        {
            _onTaskbarCreated = onTaskbarCreated;
            _onTriggerRefresh = onTriggerRefresh;
            _onClose = onClose;
            _wmTriggerRefresh = wmTriggerRefresh;
            _wmClose = wmClose;

            CreateParams cp = new CreateParams();
            cp.Parent = IntPtr.Zero; // Top-level invisible window to receive broadcast messages
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
            else if (m.Msg == _wmClose && _wmClose != 0)
            {
                if (_onClose != null)
                {
                    _onClose();
                }
            }

            base.WndProc(ref m);
        }
    }
}
