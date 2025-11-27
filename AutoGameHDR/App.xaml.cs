using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Hardcodet.Wpf.TaskbarNotification;
using WinForms = System.Windows.Forms;

namespace AutoGameHDR
{
    public partial class App : Application
    {
        private const string GITHUB_WHITELIST_URL = "https://raw.githubusercontent.com/sysxfml/HDR-Game-Database/main/games_list.txt";
        private const string APP_NAME = "AutoGameHDR";

        private TaskbarIcon _trayIcon;
        private ManagementEventWatcher _startWatcher;
        private ManagementEventWatcher _stopWatcher;

        private HashSet<string> _userWhitelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _disabledUserGames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _globalWhitelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private Queue<string> _recentProcesses = new Queue<string>(10);
        private string _currentRunningHdrGame = null;

        private readonly string _configFolder;
        private readonly string _userListPath;
        private readonly string _globalListPath;
        private readonly string _lastCheckPath;
        private readonly string _themeConfigPath;

        private Dictionary<string, string> _texts;

        public App()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _configFolder = Path.Combine(localAppData, "AutoGameHDR");

            if (!Directory.Exists(_configFolder)) Directory.CreateDirectory(_configFolder);

            _userListPath = Path.Combine(_configFolder, "user_games.txt");
            _globalListPath = Path.Combine(_configFolder, "global_games.txt");
            _lastCheckPath = Path.Combine(_configFolder, "last_update_check.txt");
            _themeConfigPath = Path.Combine(_configFolder, "theme_config.txt");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            InitLocalization();
            LoadThemeSetting();
            InitializeTrayIcon();
            LoadLocalData();

            Task.Run(() => CheckForUpdates(false));
            StartProcessWatcher();
        }

        private void LoadThemeSetting()
        {
            try
            {
                if (File.Exists(_themeConfigPath))
                {
                    string themeStr = File.ReadAllText(_themeConfigPath).Trim();
                    if (Enum.TryParse(themeStr, out AppTheme theme))
                    {
                        ThemeManager.ApplyTheme(theme);
                        return;
                    }
                }
            }
            catch { }
            ThemeManager.ApplyTheme(AppTheme.Auto);
        }

        private void SaveThemeSetting(AppTheme theme)
        {
            try
            {
                ThemeManager.ApplyTheme(theme);
                File.WriteAllText(_themeConfigPath, theme.ToString());
                UpdateThemeMenuCheckState();
            }
            catch { }
        }

        // ==========================================
        //  UI 与 交互逻辑
        // ==========================================
        private MenuItem _themeAutoItem;
        private MenuItem _themeLightItem;
        private MenuItem _themeDarkItem;

        private void InitializeTrayIcon()
        {
            _trayIcon = new TaskbarIcon();
            try
            {
                var iconUri = new Uri("pack://application:,,,/app.ico");
                var streamInfo = GetResourceStream(iconUri);
                if (streamInfo != null) _trayIcon.Icon = new System.Drawing.Icon(streamInfo.Stream);
                else _trayIcon.Icon = System.Drawing.SystemIcons.Shield;
            }
            catch { _trayIcon.Icon = System.Drawing.SystemIcons.Shield; }

            _trayIcon.ToolTipText = GetText("Title");

            var contextMenu = new ContextMenu();

            var addItem = new MenuItem { Header = GetText("AddGame") };
            addItem.Click += (s, e) => OpenAddGameWindow();
            contextMenu.Items.Add(addItem);

            contextMenu.Items.Add(new Separator());

            var manageItem = new MenuItem { Header = GetText("ViewUserList") };
            manageItem.Click += (s, e) => ShowUserList();
            contextMenu.Items.Add(manageItem);

            var onlineItem = new MenuItem { Header = GetText("ViewOnlineList") };
            onlineItem.Click += (s, e) => ShowGlobalList();
            contextMenu.Items.Add(onlineItem);

            var updateItem = new MenuItem { Header = GetText("UpdateOnline") };
            updateItem.Click += async (s, e) => await CheckForUpdates(true);
            contextMenu.Items.Add(updateItem);

            contextMenu.Items.Add(new Separator());

            // 【修复】主题菜单
            var themeItem = new MenuItem { Header = GetText("ThemeSettings") };

            _themeAutoItem = new MenuItem { Header = GetText("ThemeAuto") };
            _themeAutoItem.Click += (s, e) => SaveThemeSetting(AppTheme.Auto);

            _themeLightItem = new MenuItem { Header = GetText("ThemeLight") };
            _themeLightItem.Click += (s, e) => SaveThemeSetting(AppTheme.Light);

            _themeDarkItem = new MenuItem { Header = GetText("ThemeDark") };
            _themeDarkItem.Click += (s, e) => SaveThemeSetting(AppTheme.Dark);

            // 之前漏掉了这两行：
            themeItem.Items.Add(_themeAutoItem);
            themeItem.Items.Add(_themeLightItem);
            themeItem.Items.Add(_themeDarkItem);

            contextMenu.Items.Add(themeItem);
            UpdateThemeMenuCheckState();

            var startupItem = new MenuItem { Header = GetText("RunAtStartup"), IsCheckable = true };
            startupItem.IsChecked = IsStartupEnabled();
            startupItem.Click += (s, e) => ToggleStartup(startupItem.IsChecked);
            contextMenu.Items.Add(startupItem);

            contextMenu.Items.Add(new Separator());

            var exitItem = new MenuItem { Header = GetText("Exit") };
            exitItem.Click += (s, e) => Shutdown();
            contextMenu.Items.Add(exitItem);

            _trayIcon.ContextMenu = contextMenu;
        }

        private void UpdateThemeMenuCheckState()
        {
            if (_themeAutoItem == null) return;
            _themeAutoItem.IsChecked = ThemeManager.CurrentThemePreference == AppTheme.Auto;
            _themeLightItem.IsChecked = ThemeManager.CurrentThemePreference == AppTheme.Light;
            _themeDarkItem.IsChecked = ThemeManager.CurrentThemePreference == AppTheme.Dark;
        }

        private void OpenAddGameWindow()
        {
            var win = new ProcessSelectorWindow();
            if (win.ShowDialog() == true)
            {
                AddCustomGame(win.SelectedProcessName);
            }
        }

        private void ShowGlobalList()
        {
            foreach (Window w in Application.Current.Windows) { if (w is GlobalListWindow) { w.Activate(); return; } }
            var win = new GlobalListWindow(_globalWhitelist);
            win.Show();
        }

        private void ShowUserList()
        {
            foreach (Window w in Application.Current.Windows) { if (w is GameListWindow) { w.Activate(); return; } }
            var win = new GameListWindow(_userWhitelist, _disabledUserGames);
            win.Show();
        }

        public void UpdateUserList(List<GameItem> items)
        {
            _userWhitelist.Clear();
            _disabledUserGames.Clear();
            foreach (var item in items)
            {
                if (item.IsEnabled) _userWhitelist.Add(item.ProcessName);
                else _disabledUserGames.Add(item.ProcessName);
            }
            SaveUserList();
            MessageBox.Show("名单更新成功！", "AutoGameHDR", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
        }

        private bool IsStartupEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    return key.GetValue(APP_NAME) != null;
                }
            }
            catch { return false; }
        }

        private void ToggleStartup(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (enable)
                    {
                        string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                        key.SetValue(APP_NAME, $"\"{exePath}\"");
                    }
                    else key.DeleteValue(APP_NAME, false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("设置开机启动失败：\n" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
            }
        }

        private void AddCustomGame(string processName)
        {
            if (_disabledUserGames.Contains(processName)) _disabledUserGames.Remove(processName);
            if (!_userWhitelist.Contains(processName))
            {
                _userWhitelist.Add(processName);
                SaveUserList();
                _trayIcon.ShowBalloonTip("Auto HDR", string.Format(GetText("MsgAddSuccess"), processName), BalloonIcon.Info);
                if (IsProcessRunning(processName) && _currentRunningHdrGame == null)
                {
                    _currentRunningHdrGame = processName;
                    Dispatcher.Invoke(() => { _trayIcon.ShowBalloonTip("Auto HDR", string.Format(GetText("MsgHdrOn"), processName), BalloonIcon.Info); SimulateHdrToggle(); });
                }
            }
        }

        private void StartProcessWatcher()
        {
            try
            {
                var startQuery = new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace");
                _startWatcher = new ManagementEventWatcher(startQuery);
                _startWatcher.EventArrived += OnProcessStarted;
                _startWatcher.Start();
                var stopQuery = new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace");
                _stopWatcher = new ManagementEventWatcher(stopQuery);
                _stopWatcher.EventArrived += OnProcessStopped;
                _stopWatcher.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("无法启动进程监听！\n请确保已赋予软件管理员权限。\n\n错误信息：" + ex.Message, "严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void OnProcessStarted(object sender, EventArrivedEventArgs e)
        {
            string processName = e.NewEvent.Properties["ProcessName"].Value.ToString();
            if (IsIgnoredProcess(processName)) return;
            if ((_userWhitelist.Contains(processName) || _globalWhitelist.Contains(processName)) && !_disabledUserGames.Contains(processName))
            {
                if (_currentRunningHdrGame == null)
                {
                    _currentRunningHdrGame = processName;
                    Dispatcher.Invoke(() => { _trayIcon.ShowBalloonTip("Auto HDR", string.Format(GetText("MsgHdrOn"), processName), BalloonIcon.Info); SimulateHdrToggle(); });
                }
            }
        }

        private void OnProcessStopped(object sender, EventArrivedEventArgs e)
        {
            string processName = e.NewEvent.Properties["ProcessName"].Value.ToString();
            if (string.Equals(_currentRunningHdrGame, processName, StringComparison.OrdinalIgnoreCase))
            {
                _currentRunningHdrGame = null;
                Task.Run(async () => { await Task.Delay(2000); Dispatcher.Invoke(() => { _trayIcon.ShowBalloonTip("Auto HDR", string.Format(GetText("MsgHdrOff"), processName), BalloonIcon.Info); SimulateHdrToggle(); }); });
            }
        }

        private bool IsIgnoredProcess(string name)
        {
            var lower = name.ToLower();
            return lower == "svchost.exe" || lower == "explorer.exe" || lower == "searchhost.exe" ||
                   lower == "chrome.exe" || lower == "msedge.exe" || lower == "discord.exe" ||
                   lower == "autogamehdr.exe" || lower == "taskmgr.exe";
        }

        private bool IsProcessRunning(string name)
        {
            try
            {
                var procNameWithoutExt = Path.GetFileNameWithoutExtension(name);
                return Process.GetProcessesByName(procNameWithoutExt).Length > 0;
            }
            catch { return false; }
        }

        private void InitLocalization()
        {
            var culture = System.Globalization.CultureInfo.CurrentUICulture.Name;
            if (culture.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            {
                _texts = new Dictionary<string, string> {
                    { "Title", "AutoGameHDR (运行中)" },
                    { "AddGame", "➕ 添加游戏到自定义名单..." },
                    { "ViewUserList", "📋 管理自定义名单..." },
                    { "ViewOnlineList", "☁️ 查看在线白名单..." },
                    { "UpdateOnline", "🔄 手动更新在线名单" },
                    { "ThemeSettings", "🎨 界面主题" },
                    { "ThemeAuto", "跟随系统" },
                    { "ThemeLight", "浅色模式" },
                    { "ThemeDark", "深色模式" },
                    { "RunAtStartup", "🚀 开机自动启动" },
                    { "Exit", "❌ 退出" },
                    { "MsgAddSuccess", "已添加 {0}" },
                    { "MsgHdrOn", "识别到 {0}，正在开启 HDR" },
                    { "MsgHdrOff", "游戏关闭，正在关闭 HDR" },
                    { "MsgUpdateStart", "正在连接 GitHub 更新名单..." },
                    { "MsgUpdateSuccess", "更新成功！\n云端名单现包含 {0} 个游戏。" },
                    { "MsgUpdateFail", "更新失败，请检查网络。\n\n错误：{0}" }
                };
            }
            else
            {
                _texts = new Dictionary<string, string> {
                    { "Title", "AutoGameHDR (Running)" },
                    { "AddGame", "➕ Add Game to Custom List..." },
                    { "ViewUserList", "📋 Manage Custom List..." },
                    { "ViewOnlineList", "☁️ View Online Whitelist..." },
                    { "UpdateOnline", "🔄 Update Online List" },
                    { "ThemeSettings", "🎨 Interface Theme" },
                    { "ThemeAuto", "System Default" },
                    { "ThemeLight", "Light Mode" },
                    { "ThemeDark", "Dark Mode" },
                    { "RunAtStartup", "🚀 Run at Startup" },
                    { "Exit", "❌ Exit" },
                    { "MsgAddSuccess", "Added {0}" },
                    { "MsgHdrOn", "Detected {0}, enabling HDR" },
                    { "MsgHdrOff", "Game closed, disabling HDR" },
                    { "MsgUpdateStart", "Checking GitHub for updates..." },
                    { "MsgUpdateSuccess", "Update Successful!\nOnline list now has {0} games." },
                    { "MsgUpdateFail", "Update Failed. Check internet.\n\nError: {0}" }
                };
            }
        }

        private string GetText(string key) => _texts.ContainsKey(key) ? _texts[key] : key;

        private async Task CheckForUpdates(bool isManual)
        {
            try
            {
                if (isManual) _trayIcon.ShowBalloonTip("AutoGameHDR", GetText("MsgUpdateStart"), BalloonIcon.Info);
                string today = DateTime.Now.ToString("yyyy-MM-dd");
                if (!isManual && File.Exists(_lastCheckPath))
                {
                    string lastDate = File.ReadAllText(_lastCheckPath).Trim();
                    if (lastDate == today) return;
                }
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(15);
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("AutoGameHDR-Client");
                    string url = GITHUB_WHITELIST_URL + "?t=" + DateTime.Now.Ticks;
                    string content = await client.GetStringAsync(url);
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        File.WriteAllLines(_globalListPath, lines);
                        lock (_globalWhitelist)
                        {
                            _globalWhitelist.Clear();
                            foreach (var line in lines) _globalWhitelist.Add(line.Trim());
                        }
                        File.WriteAllText(_lastCheckPath, today);
                        if (isManual)
                        {
                            MessageBox.Show(string.Format(GetText("MsgUpdateSuccess"), lines.Length), "Update", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (isManual)
                {
                    MessageBox.Show(string.Format(GetText("MsgUpdateFail"), ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                }
            }
        }

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);
        private const int KEYEVENTF_EXTENDEDKEY = 0x1;
        private const int KEYEVENTF_KEYUP = 0x2;
        private const byte VK_LWIN = 0x5B;
        private const byte VK_MENU = 0x12;
        private const byte VK_B = 0x42;

        private void SimulateHdrToggle()
        {
            keybd_event(VK_LWIN, 0, KEYEVENTF_EXTENDEDKEY, 0);
            keybd_event(VK_MENU, 0, KEYEVENTF_EXTENDEDKEY, 0);
            keybd_event(VK_B, 0, KEYEVENTF_EXTENDEDKEY, 0);
            keybd_event(VK_B, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
            keybd_event(VK_MENU, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
            keybd_event(VK_LWIN, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
        }

        private void LoadLocalData()
        {
            if (File.Exists(_userListPath))
            {
                try
                {
                    foreach (var line in File.ReadAllLines(_userListPath))
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        string trim = line.Trim();
                        if (trim.StartsWith("#")) _disabledUserGames.Add(trim.Substring(1));
                        else _userWhitelist.Add(trim);
                    }
                }
                catch { }
            }
            if (File.Exists(_globalListPath))
            {
                try
                {
                    foreach (var line in File.ReadAllLines(_globalListPath))
                        if (!string.IsNullOrWhiteSpace(line)) _globalWhitelist.Add(line.Trim());
                }
                catch { }
            }
        }

        private void SaveUserList()
        {
            try
            {
                var lines = new List<string>();
                lines.AddRange(_userWhitelist);
                lines.AddRange(_disabledUserGames.Select(g => "#" + g));
                File.WriteAllLines(_userListPath, lines);
            }
            catch { }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _trayIcon?.Dispose();
            _startWatcher?.Stop();
            _stopWatcher?.Stop();
            base.OnExit(e);
        }
    }
}