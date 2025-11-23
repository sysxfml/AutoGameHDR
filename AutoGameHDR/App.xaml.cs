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
using Hardcodet.Wpf.TaskbarNotification;

namespace AutoGameHDR
{
    public partial class App : Application
    {
        // GitHub Raw URL (你的仓库地址)
        private const string GITHUB_WHITELIST_URL = "https://raw.githubusercontent.com/sysxfml/HDR-Game-Database/main/games_list.txt";

        private TaskbarIcon _trayIcon;
        private ManagementEventWatcher _startWatcher;
        private ManagementEventWatcher _stopWatcher;

        private HashSet<string> _userWhitelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _globalWhitelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private Queue<string> _recentProcesses = new Queue<string>(5);
        private string _currentRunningHdrGame = null;

        // 配置路径变量
        private readonly string _configFolder;
        private readonly string _userListPath;
        private readonly string _globalListPath;
        private readonly string _lastCheckPath;

        private Dictionary<string, string> _texts;

        public App()
        {
            // 将配置文件路径设置在 AppData/Local/AutoGameHDR
            // 这样无论 EXE 放在哪里（包括桌面或 Program Files），都有权限读写文件
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _configFolder = Path.Combine(localAppData, "AutoGameHDR");

            if (!Directory.Exists(_configFolder)) Directory.CreateDirectory(_configFolder);

            _userListPath = Path.Combine(_configFolder, "user_games.txt");
            _globalListPath = Path.Combine(_configFolder, "global_games.txt");
            _lastCheckPath = Path.Combine(_configFolder, "last_update_check.txt");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            InitLocalization();
            InitializeTrayIcon();
            LoadLocalData();
            Task.Run(() => CheckForUpdates());
            StartProcessWatcher();
        }

        private void InitLocalization()
        {
            var culture = System.Globalization.CultureInfo.CurrentUICulture.Name;

            if (culture.StartsWith("zh-CN", StringComparison.OrdinalIgnoreCase))
            {
                _texts = new Dictionary<string, string> {
                    { "Title", "AutoGameHDR (运行中)" },
                    { "AddRecent", "添加刚才运行的游戏..." },
                    { "NoRecent", "(暂无记录)" },
                    { "Added", "[已添加] " },
                    { "ViewUserList", "查看我的自定义名单" },
                    { "Exit", "退出" },
                    { "MsgAddSuccess", "已添加 {0}" },
                    { "MsgHdrOn", "识别到 {0}，正在开启 HDR" },
                    { "MsgHdrOff", "游戏关闭，正在关闭 HDR" },
                    { "ListTitle", "自定义名单" }
                };
            }
            else if (culture.StartsWith("zh-TW", StringComparison.OrdinalIgnoreCase) ||
                     culture.StartsWith("zh-HK", StringComparison.OrdinalIgnoreCase))
            {
                _texts = new Dictionary<string, string> {
                    { "Title", "AutoGameHDR (運行中)" },
                    { "AddRecent", "加入剛剛執行的遊戲..." },
                    { "NoRecent", "(暫無紀錄)" },
                    { "Added", "[已加入] " },
                    { "ViewUserList", "查看我的自訂名單" },
                    { "Exit", "退出" },
                    { "MsgAddSuccess", "已加入 {0}" },
                    { "MsgHdrOn", "偵測到 {0}，正在開啟 HDR" },
                    { "MsgHdrOff", "遊戲關閉，正在關閉 HDR" },
                    { "ListTitle", "自訂名單" }
                };
            }
            else
            {
                _texts = new Dictionary<string, string> {
                    { "Title", "AutoGameHDR (Running)" },
                    { "AddRecent", "Add Recent Game..." },
                    { "NoRecent", "(No history)" },
                    { "Added", "[Added] " },
                    { "ViewUserList", "View My List" },
                    { "Exit", "Exit" },
                    { "MsgAddSuccess", "Added {0}" },
                    { "MsgHdrOn", "Detected {0}, enabling HDR" },
                    { "MsgHdrOff", "Game closed, disabling HDR" },
                    { "ListTitle", "My Custom List" }
                };
            }
        }

        private string GetText(string key) => _texts.ContainsKey(key) ? _texts[key] : key;

        private async Task CheckForUpdates()
        {
            try
            {
                string today = DateTime.Now.ToString("yyyy-MM-dd");
                if (File.Exists(_lastCheckPath))
                {
                    string lastDate = File.ReadAllText(_lastCheckPath).Trim();
                    if (lastDate == today) return;
                }

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("AutoGameHDR-Client");

                    string content = await client.GetStringAsync(GITHUB_WHITELIST_URL);

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
                    }
                }
            }
            catch { }
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
                        if (!string.IsNullOrWhiteSpace(line)) _userWhitelist.Add(line.Trim());
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
                File.WriteAllLines(_userListPath, _userWhitelist);
            }
            catch { }
        }

        private void InitializeTrayIcon()
        {
            _trayIcon = new TaskbarIcon();

            // 【核心修改】从内部资源流读取图标，完美支持单文件打包
            try
            {
                // Uri 格式说明：pack://application:,,,/你的文件名.ico
                var iconUri = new Uri("pack://application:,,,/app.ico");
                var streamInfo = GetResourceStream(iconUri);

                if (streamInfo != null)
                {
                    _trayIcon.Icon = new System.Drawing.Icon(streamInfo.Stream);
                }
                else
                {
                    // 如果资源读取失败，回退到系统图标
                    _trayIcon.Icon = System.Drawing.SystemIcons.Shield;
                }
            }
            catch
            {
                // 任何异常都回退到系统图标，保证程序不崩
                _trayIcon.Icon = System.Drawing.SystemIcons.Shield;
            }

            _trayIcon.ToolTipText = GetText("Title");

            var contextMenu = new ContextMenu();

            var addRecentItem = new MenuItem { Header = GetText("AddRecent") };
            addRecentItem.SubmenuOpened += (s, e) => PopulateRecentGamesMenu(addRecentItem);
            contextMenu.Items.Add(addRecentItem);

            var showListItem = new MenuItem { Header = GetText("ViewUserList") };
            showListItem.Click += (s, e) => ShowUserList();
            contextMenu.Items.Add(showListItem);

            contextMenu.Items.Add(new Separator());

            var exitItem = new MenuItem { Header = GetText("Exit") };
            exitItem.Click += (s, e) => Shutdown();
            contextMenu.Items.Add(exitItem);

            _trayIcon.ContextMenu = contextMenu;
        }

        private void ShowUserList()
        {
            string list = string.Join("\n", _userWhitelist);
            if (string.IsNullOrEmpty(list)) list = "(Empty)";
            MessageBox.Show(list, GetText("ListTitle"));
        }

        private void PopulateRecentGamesMenu(MenuItem parentItem)
        {
            parentItem.Items.Clear();
            if (_recentProcesses.Count == 0)
            {
                parentItem.Items.Add(new MenuItem { Header = GetText("NoRecent"), IsEnabled = false });
                return;
            }
            foreach (var procName in _recentProcesses.Reverse())
            {
                var item = new MenuItem();

                bool isUserAdded = _userWhitelist.Contains(procName);
                bool isGlobalAdded = _globalWhitelist.Contains(procName);

                if (isUserAdded || isGlobalAdded)
                {
                    item.Header = GetText("Added") + procName;
                    item.IsEnabled = false;
                }
                else
                {
                    item.Header = procName;
                    item.Click += (s, e) => AddCustomGame(procName);
                }
                parentItem.Items.Add(item);
            }
        }

        private void AddCustomGame(string processName)
        {
            if (!_userWhitelist.Contains(processName))
            {
                _userWhitelist.Add(processName);
                SaveUserList();

                MessageBox.Show(string.Format(GetText("MsgAddSuccess"), processName));

                if (IsProcessRunning(processName) && _currentRunningHdrGame == null)
                {
                    _currentRunningHdrGame = processName;
                    Dispatcher.Invoke(() =>
                    {
                        _trayIcon.ShowBalloonTip("Auto HDR", string.Format(GetText("MsgHdrOn"), processName), BalloonIcon.Info);
                        SimulateHdrToggle();
                    });
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
            catch { }
        }

        private void OnProcessStarted(object sender, EventArrivedEventArgs e)
        {
            string processName = e.NewEvent.Properties["ProcessName"].Value.ToString();
            if (IsIgnoredProcess(processName)) return;

            AddToRecentHistory(processName);

            if (_userWhitelist.Contains(processName) || _globalWhitelist.Contains(processName))
            {
                if (_currentRunningHdrGame == null)
                {
                    _currentRunningHdrGame = processName;
                    Dispatcher.Invoke(() =>
                    {
                        _trayIcon.ShowBalloonTip("Auto HDR", string.Format(GetText("MsgHdrOn"), processName), BalloonIcon.Info);
                        SimulateHdrToggle();
                    });
                }
            }
        }

        private void OnProcessStopped(object sender, EventArrivedEventArgs e)
        {
            string processName = e.NewEvent.Properties["ProcessName"].Value.ToString();
            if (_currentRunningHdrGame == processName)
            {
                _currentRunningHdrGame = null;
                Dispatcher.Invoke(() =>
                {
                    _trayIcon.ShowBalloonTip("Auto HDR", string.Format(GetText("MsgHdrOff"), processName), BalloonIcon.Info);
                    SimulateHdrToggle();
                });
            }
        }

        private void AddToRecentHistory(string processName)
        {
            if (_recentProcesses.Count > 0 && _recentProcesses.Last() == processName) return;
            lock (_recentProcesses)
            {
                if (_recentProcesses.Count >= 5) _recentProcesses.Dequeue();
                _recentProcesses.Enqueue(processName);
            }
        }

        private bool IsIgnoredProcess(string name)
        {
            var lower = name.ToLower();
            return lower == "svchost.exe" || lower == "explorer.exe" || lower == "searchhost.exe" ||
                   lower == "chrome.exe" || lower == "msedge.exe" || lower == "discord.exe";
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

        protected override void OnExit(ExitEventArgs e)
        {
            _trayIcon?.Dispose();
            _startWatcher?.Stop();
            _stopWatcher?.Stop();
            base.OnExit(e);
        }
    }
}