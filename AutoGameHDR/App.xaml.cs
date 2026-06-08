using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using Hardcodet.Wpf.TaskbarNotification;
using WinForms = System.Windows.Forms;
using Sche = Microsoft.Win32.TaskScheduler;

namespace AutoGameHDR
{
    public partial class App : Application
    {
        private const string GITHUB_WHITELIST_URL = "https://raw.githubusercontent.com/sysxfml/HDR-Game-Database/main/games_list.txt";
        private const string APP_NAME = "AutoGameHDR";

        private static Mutex _mutex = null;

        private TaskbarIcon _trayIcon;
        private DispatcherTimer _fastPoller;

        private MenuItem _themeAutoItem;
        private MenuItem _themeLightItem;
        private MenuItem _themeDarkItem;

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

            if (!Directory.Exists(_configFolder))
            {
                Directory.CreateDirectory(_configFolder);
            }

            _userListPath = Path.Combine(_configFolder, "user_games.txt");
            _globalListPath = Path.Combine(_configFolder, "global_games.txt");
            _lastCheckPath = Path.Combine(_configFolder, "last_update_check.txt");
            _themeConfigPath = Path.Combine(_configFolder, "theme_config.txt");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            const string mutexName = "Global\\AutoGameHDR_Unique_Mutex_ID_v1";
            bool createdNew;

            _mutex = new Mutex(true, mutexName, out createdNew);

            InitLocalization();

            if (!createdNew)
            {
                MessageBox.Show(GetText("MsgAlreadyRunning"),
                                GetText("PromptTitle"),
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                Application.Current.Shutdown();
                return;
            }

            base.OnStartup(e);

            LoadThemeSetting();
            InitializeTrayIcon();
            LoadLocalData();

            if (_trayIcon != null)
            {
                _trayIcon.ShowBalloonTip(APP_NAME, GetText("MsgServiceStarted"), BalloonIcon.Info);
            }

            Task.Run(() => CheckForUpdates(false));
            StartFastPolling();
        }

        private void StartFastPolling()
        {
            _fastPoller = new DispatcherTimer();
            _fastPoller.Interval = TimeSpan.FromMilliseconds(500);
            _fastPoller.Tick += FastPoller_Tick;
            _fastPoller.Start();
        }

        private void FastPoller_Tick(object sender, EventArgs e)
        {
            HashSet<string> currentProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var p in Process.GetProcesses())
                {
                    currentProcesses.Add(p.ProcessName + ".exe");
                }
            }
            catch
            {
                return;
            }

            if (_currentRunningHdrGame == null)
            {
                foreach (var proc in currentProcesses)
                {
                    if (IsIgnoredProcess(proc))
                    {
                        continue;
                    }

                    if ((_userWhitelist.Contains(proc) || _globalWhitelist.Contains(proc)) && !_disabledUserGames.Contains(proc))
                    {
                        TurnHdrOn(proc);
                        if (!_recentProcesses.Contains(proc))
                        {
                            AddToRecentHistory(proc);
                        }
                        break;
                    }
                }
            }
            else
            {
                if (!currentProcesses.Contains(_currentRunningHdrGame))
                {
                    TurnHdrOff(_currentRunningHdrGame);
                }
            }
        }

        private void TurnHdrOn(string processName)
        {
            _currentRunningHdrGame = processName;
            _trayIcon.ShowBalloonTip("Auto HDR", string.Format(GetText("MsgHdrOn"), processName), BalloonIcon.Info);
            SimulateHdrToggle();
        }

        private void TurnHdrOff(string processName)
        {
            _currentRunningHdrGame = null;

            Task.Run(async () =>
            {
                await Task.Delay(1500);
                Dispatcher.Invoke(() =>
                {
                    _trayIcon.ShowBalloonTip("Auto HDR", string.Format(GetText("MsgHdrOff"), processName), BalloonIcon.Info);
                    SimulateHdrToggle();
                });
            });
        }

        private void InitializeTrayIcon()
        {
            _trayIcon = new TaskbarIcon();
            try
            {
                var iconUri = new Uri("pack://application:,,,/app.ico");
                var streamInfo = GetResourceStream(iconUri);
                if (streamInfo != null)
                {
                    _trayIcon.Icon = new System.Drawing.Icon(streamInfo.Stream);
                }
                else
                {
                    _trayIcon.Icon = System.Drawing.SystemIcons.Shield;
                }
            }
            catch
            {
                _trayIcon.Icon = System.Drawing.SystemIcons.Shield;
            }

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

            var themeItem = new MenuItem { Header = GetText("ThemeSettings") };

            _themeAutoItem = new MenuItem { Header = GetText("ThemeAuto") };
            _themeAutoItem.Click += (s, e) => SaveThemeSetting(AppTheme.Auto);

            _themeLightItem = new MenuItem { Header = GetText("ThemeLight") };
            _themeLightItem.Click += (s, e) => SaveThemeSetting(AppTheme.Light);

            _themeDarkItem = new MenuItem { Header = GetText("ThemeDark") };
            _themeDarkItem.Click += (s, e) => SaveThemeSetting(AppTheme.Dark);

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
            if (_themeAutoItem == null)
            {
                return;
            }
            _themeAutoItem.IsChecked = ThemeManager.CurrentThemePreference == AppTheme.Auto;
            _themeLightItem.IsChecked = ThemeManager.CurrentThemePreference == AppTheme.Light;
            _themeDarkItem.IsChecked = ThemeManager.CurrentThemePreference == AppTheme.Dark;
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
            foreach (Window w in Application.Current.Windows)
            {
                if (w is GlobalListWindow)
                {
                    w.Activate();
                    return;
                }
            }
            var win = new GlobalListWindow(_globalWhitelist);
            win.Show();
        }

        private void ShowUserList()
        {
            foreach (Window w in Application.Current.Windows)
            {
                if (w is GameListWindow)
                {
                    w.Activate();
                    return;
                }
            }
            var win = new GameListWindow(_userWhitelist, _disabledUserGames);
            win.Show();
        }

        public void UpdateUserList(List<GameItem> items)
        {
            _userWhitelist.Clear();
            _disabledUserGames.Clear();
            foreach (var item in items)
            {
                if (item.IsEnabled)
                {
                    _userWhitelist.Add(item.ProcessName);
                }
                else
                {
                    _disabledUserGames.Add(item.ProcessName);
                }
            }
            SaveUserList();
            MessageBox.Show(GetText("MsgSaveSuccess"), APP_NAME, MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
        }

        private void AddCustomGame(string processName)
        {
            if (_disabledUserGames.Contains(processName))
            {
                _disabledUserGames.Remove(processName);
            }
            if (!_userWhitelist.Contains(processName))
            {
                _userWhitelist.Add(processName);
                SaveUserList();
                _trayIcon.ShowBalloonTip("Auto HDR", string.Format(GetText("MsgAddSuccess"), processName), BalloonIcon.Info);
            }
        }

        private void AddToRecentHistory(string processName)
        {
            lock (_recentProcesses)
            {
                if (_recentProcesses.Count >= 10)
                {
                    _recentProcesses.Dequeue();
                }
                _recentProcesses.Enqueue(processName);
            }
        }

        private bool IsIgnoredProcess(string name)
        {
            var lower = name.ToLower();
            return lower == "svchost.exe" || lower == "explorer.exe" || lower == "searchhost.exe" ||
                   lower == "chrome.exe" || lower == "msedge.exe" || lower == "discord.exe" ||
                   lower == "autogamehdr.exe" || lower == "taskmgr.exe";
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
            Thread.Sleep(30);
            keybd_event(VK_MENU, 0, KEYEVENTF_EXTENDEDKEY, 0);
            Thread.Sleep(30);
            keybd_event(VK_B, 0, KEYEVENTF_EXTENDEDKEY, 0);
            Thread.Sleep(30);

            keybd_event(VK_B, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
            Thread.Sleep(30);
            keybd_event(VK_MENU, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
            Thread.Sleep(30);
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
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }
                        string trim = line.Trim();
                        if (trim.StartsWith("#"))
                        {
                            _disabledUserGames.Add(trim.Substring(1));
                        }
                        else
                        {
                            _userWhitelist.Add(trim);
                        }
                    }
                }
                catch { }
            }
            if (File.Exists(_globalListPath))
            {
                try
                {
                    foreach (var line in File.ReadAllLines(_globalListPath))
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            _globalWhitelist.Add(line.Trim());
                        }
                    }
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

        private bool IsStartupEnabled()
        {
            try
            {
                using (Sche.TaskService ts = new Sche.TaskService())
                {
                    return ts.FindTask(APP_NAME) != null;
                }
            }
            catch { return false; }
        }

        private void ToggleStartup(bool enable)
        {
            try
            {
                using (Sche.TaskService ts = new Sche.TaskService())
                {
                    if (enable)
                    {
                        string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                        Sche.TaskDefinition td = ts.NewTask();
                        td.RegistrationInfo.Description = "Auto-start AutoGameHDR with Admin privileges.";
                        td.Principal.RunLevel = Sche.TaskRunLevel.Highest;
                        td.Triggers.Add(new Sche.LogonTrigger());
                        td.Actions.Add(new Sche.ExecAction(exePath, null, Path.GetDirectoryName(exePath)));
                        td.Settings.DisallowStartIfOnBatteries = false;
                        td.Settings.StopIfGoingOnBatteries = false;
                        td.Settings.ExecutionTimeLimit = TimeSpan.Zero;
                        ts.RootFolder.RegisterTaskDefinition(APP_NAME, td);
                        MessageBox.Show(GetText("MsgStartupSetSuccess"), GetText("PromptTitle"), MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                    }
                    else
                    {
                        ts.RootFolder.DeleteTask(APP_NAME, false);
                        MessageBox.Show(GetText("MsgStartupCancelSuccess"), GetText("PromptTitle"), MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(GetText("MsgStartupFail") + "\n" + ex.Message, GetText("PromptError"), MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
            }
        }

        public string GetText(string key)
        {
            if (_texts != null && _texts.ContainsKey(key))
            {
                return _texts[key];
            }
            return key;
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
                    { "PromptTitle", "提示" },
                    { "PromptError", "错误" },
                    { "MsgAddSuccess", "已添加 {0}" },
                    { "MsgHdrOn", "识别到 {0}，正在开启 HDR" },
                    { "MsgHdrOff", "游戏关闭，正在关闭 HDR" },
                    { "MsgUpdateStart", "正在连接 GitHub 更新名单..." },
                    { "MsgUpdateSuccess", "更新成功！\n云端名单现包含 {0} 个游戏。" },
                    { "MsgUpdateFail", "更新失败，请检查网络。\n\n错误：{0}" },
                    { "MsgAlreadyRunning", "AutoGameHDR 已经在后台运行中！\n请检查任务栏右下角的托盘图标。" },
                    { "MsgServiceStarted", "服务已启动，正在后台监测游戏..." },
                    { "MsgSaveSuccess", "名单更新成功！" },
                    { "MsgStartupSetSuccess", "已成功设置开机自启 (计划任务)。" },
                    { "MsgStartupCancelSuccess", "已取消开机自启。" },
                    { "MsgStartupFail", "设置开机启动失败：" },

                    { "Lang_AddGameTitle", "添加游戏到自定义名单" },
                    { "Lang_ProcSelectMethod1", "方式 1: 从正在运行的进程中选择 (双击添加)" },
                    { "Lang_ProcSelectMethod2", "方式 2: 点击左下角“浏览文件”手动选择 exe" },
                    { "Lang_BtnBrowse", "📂 浏览文件..." },
                    { "Lang_BtnRefresh", "刷新" },
                    { "Lang_BtnCancel", "取消" },
                    { "Lang_BtnAddSelected", "添加选中" },
                    { "Lang_MsgNoProcessSelected", "请先选择一个进程！" },
                    { "Lang_BrowseFileTitle", "选择游戏主程序 (.exe)" },
                    { "Lang_BrowseFileFilter", "可执行文件 (*.exe)|*.exe" },
                    { "Lang_MsgBrowseBoxFail", "打开文件浏览框失败：" },

                    { "Lang_GlobalListReadOnly", "在线云端白名单 (只读)" },
                    { "Lang_GlobalListTitlePrefix", "在线云端白名单" },
                    { "Lang_GlobalListDesc", "此列表每日从 GitHub 自动更新，包含原生支持 HDR 的游戏。" },
                    { "Lang_BtnClose", "关闭" },

                    { "Lang_UserListTitle", "自定义游戏白名单管理" },
                    { "Lang_UserListMainHeader", "管理您的本地游戏列表" },
                    { "Lang_UserListDesc", "勾选左侧方框以启用自动 HDR，点击右侧按钮移除条目。" },
                    { "Lang_ColEnabled", "启用" },
                    { "Lang_ColProcessName", "游戏进程名 (.exe)" },
                    { "Lang_ColAction", "操作" },
                    { "Lang_BtnDelete", "删除" },
                    { "Lang_BtnImport", "📥 导入 TXT" },
                    { "Lang_BtnExport", "📤 导出 TXT" },
                    { "Lang_BtnSave", "保存并生效" },
                    { "Lang_ImportTitle", "导入游戏列表 (TXT)" },
                    { "Lang_ImportFilter", "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*" },
                    { "Lang_MsgImportSuccess", "成功导入 {0} 个新游戏！\n(重复项已自动忽略)" },
                    { "Lang_MsgImportFail", "导入失败：" },
                    { "Lang_ExportTitle", "导出游戏列表" },
                    { "Lang_ExportFilter", "文本文件 (*.txt)|*.txt" },
                    { "Lang_MsgExportSuccess", "导出成功！" },
                    { "Lang_MsgExportFail", "导出失败：" }
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
                    { "PromptTitle", "Notification" },
                    { "PromptError", "Error" },
                    { "MsgAddSuccess", "Added {0}" },
                    { "MsgHdrOn", "Detected {0}, enabling HDR" },
                    { "MsgHdrOff", "Game closed, disabling HDR" },
                    { "MsgUpdateStart", "Checking GitHub for updates..." },
                    { "MsgUpdateSuccess", "Update Successful!\nOnline list now has {0} games." },
                    { "MsgUpdateFail", "Update Failed. Check internet.\n\nError: {0}" },
                    { "MsgAlreadyRunning", "AutoGameHDR is already running in the background!\nPlease check the system tray icon at the bottom right." },
                    { "MsgServiceStarted", "Service started, monitoring games in background..." },
                    { "MsgSaveSuccess", "List updated successfully!" },
                    { "MsgStartupSetSuccess", "Successfully set to run at startup (Scheduled Task)." },
                    { "MsgStartupCancelSuccess", "Startup task canceled successfully." },
                    { "MsgStartupFail", "Failed to configure startup:" },

                    { "Lang_AddGameTitle", "Add Game to Custom List" },
                    { "Lang_ProcSelectMethod1", "Option 1: Select from running processes (Double-click to add)" },
                    { "Lang_ProcSelectMethod2", "Option 2: Click 'Browse File...' at bottom left to select exe manually" },
                    { "Lang_BtnBrowse", "📂 Browse File..." },
                    { "Lang_BtnRefresh", "Refresh" },
                    { "Lang_BtnCancel", "Cancel" },
                    { "Lang_BtnAddSelected", "Add Selected" },
                    { "Lang_MsgNoProcessSelected", "Please select a process first!" },
                    { "Lang_BrowseFileTitle", "Select Main Game Executable (.exe)" },
                    { "Lang_BrowseFileFilter", "Executable Files (*.exe)|*.exe" },
                    { "Lang_MsgBrowseBoxFail", "Failed to open file browser:" },

                    { "Lang_GlobalListReadOnly", "Online Cloud Whitelist (Read-Only)" },
                    { "Lang_GlobalListTitlePrefix", "Online Cloud Whitelist" },
                    { "Lang_GlobalListDesc", "This list is automatically pulled from GitHub daily, containing native HDR supported titles." },
                    { "Lang_BtnClose", "Close" },

                    { "Lang_UserListTitle", "Manage Custom Game Whitelist" },
                    { "Lang_UserListMainHeader", "Manage Local Game List" },
                    { "Lang_UserListDesc", "Check the left box to enable auto HDR, click the right button to remove entries." },
                    { "Lang_ColEnabled", "Enable" },
                    { "Lang_ColProcessName", "Game Process (.exe)" },
                    { "Lang_ColAction", "Action" },
                    { "Lang_BtnDelete", "Delete" },
                    { "Lang_BtnImport", "📥 Import TXT" },
                    { "Lang_BtnExport", "📤 Export TXT" },
                    { "Lang_BtnSave", "Save & Apply" },
                    { "Lang_ImportTitle", "Import Game List (TXT)" },
                    { "Lang_ImportFilter", "Text Files (*.txt)|*.txt|All Files (*.*)|*.*" },
                    { "Lang_MsgImportSuccess", "Successfully imported {0} new games!\n(Duplicates were automatically ignored)" },
                    { "Lang_MsgImportFail", "Import failed:" },
                    { "Lang_ExportTitle", "Export Game List" },
                    { "Lang_ExportFilter", "Text Files (*.txt)|*.txt" },
                    { "Lang_MsgExportSuccess", "Export successful!" },
                    { "Lang_MsgExportFail", "Export failed:" }
                };
            }

            foreach (var kvp in _texts)
            {
                Application.Current.Resources[kvp.Key] = kvp.Value;
            }
        }

        private async Task CheckForUpdates(bool isManual)
        {
            try
            {
                if (isManual)
                {
                    _trayIcon.ShowBalloonTip("AutoGameHDR", GetText("MsgUpdateStart"), BalloonIcon.Info);
                }
                string today = DateTime.Now.ToString("yyyy-MM-dd");
                if (!isManual && File.Exists(_lastCheckPath))
                {
                    string lastDate = File.ReadAllText(_lastCheckPath).Trim();
                    if (lastDate == today)
                    {
                        return;
                    }
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
                            foreach (var line in lines)
                            {
                                _globalWhitelist.Add(line.Trim());
                            }
                        }
                        File.WriteAllText(_lastCheckPath, today);
                        if (isManual)
                        {
                            MessageBox.Show(string.Format(GetText("MsgUpdateSuccess"), lines.Length), GetText("PromptTitle"), MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (isManual)
                {
                    MessageBox.Show(string.Format(GetText("MsgUpdateFail"), ex.Message), GetText("PromptError"), MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                }
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_trayIcon != null)
            {
                _trayIcon.Dispose();
            }
            base.OnExit(e);
        }
    }
}