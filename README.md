# AutoGameHDR

![License](https://img.shields.io/github/license/sysxfml/AutoGameHDR)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)
![Version](https://img.shields.io/github/v/release/sysxfml/AutoGameHDR)

[English](#english) | [中文](#chinese)

AutoGameHDR is a lightweight, portable Windows utility designed to bring the console-like "Auto HDR" experience to PC. It automatically toggles Windows HDR on when a supported game launches and turns it off when you exit, eliminating the need to dig through display settings every time you want to play.

---

<a name="english"></a>
## English

### Key Features
* **Auto HDR Toggle:** Instantly enables Windows HDR when a game launches and disables it upon exit to preserve correct SDR colors on desktop.
* **Cloud-Synced Whitelist:** The app automatically pulls the latest list of HDR-supported games from GitHub daily (powered by the HDR-Game-Database project).
* **Unified "Add Game" Window:** Game not recognized? Right-click the tray icon and select "Add Game to Custom List". You can either pick from currently running processes or browse for an executable file.
* **Dark Mode Support:** Features a polished UI with support for Light, Dark, and System Default themes.
* **Manual Update:** Force an update of the online game list anytime via the right-click menu.
* **Zero Distraction:** Runs silently in the system tray. Requires no installation (single portable EXE).

### Download
Get the latest `AutoGameHDR.exe` from the **Releases** page.

### Usage
1.  Download `AutoGameHDR.exe` and place it anywhere you like.
2.  Run the application. It will request Administrator privileges to ensure it can accurately detect game processes.
3.  The app will minimize to the system tray.
4.  **Just play:** Launch any supported HDR title (e.g., Cyberpunk 2077, Elden Ring), and your monitor will switch to HDR mode automatically.
5.  **If a game is not detected:**
    * Right-click the tray icon and select "Add Game to Custom List...".
    * **Option A:** Find your running game in the list and double-click it.
    * **Option B:** Click "Browse File..." at the bottom left to select the game's .exe manually.
6.  You can manage your local list or change the interface theme via the right-click menu.

### Data Location
User configurations and local whitelists are stored in `%AppData%\AutoGameHDR`.

---

<a name="chinese"></a>
## 中文

### 简介
AutoGameHDR 是一款极简的 Windows 工具，旨在为 PC 玩家提供类似主机（如 PS5/Xbox）的“无感 HDR”体验。它会在你启动游戏时自动开启系统的 HDR 功能，退出游戏后自动切回 SDR 模式，让你不再需要每次玩游戏前都去系统设置里手动开关。

### 核心功能
* **HDR 自动切换：** 识别游戏进程启动和关闭，自动切换 Windows 的 HDR 开关，解决桌面模式下 HDR 色彩发白的问题。
* **云端白名单同步：** 软件依托于 HDR-Game-Database 项目，每天自动从 GitHub 获取最新的 HDR 游戏支持列表（基于 PCGamingWiki 数据），无需人工维护。
* **轻松添加游戏：** 在右键菜单选择“添加游戏到自定义名单”。在弹出的窗口中，你可以直接双击正在运行的进程，或者点击“浏览文件”手动指定游戏 exe，立即生效。
* **深色模式支持：** 支持深色、浅色主题，或跟随系统设置自动切换。
* **手动更新：** 支持通过右键菜单手动强制更新云端白名单，随时获取最新支持库。
* **单文件绿色版：** 只有一个 exe 文件，无需安装，几乎不占系统资源。

### 下载
请前往 **Releases** 页面下载最新的 `AutoGameHDR.exe`。

### 使用指南
1.  下载 `AutoGameHDR.exe` 并放在任意文件夹。
2.  运行程序（程序启动时会请求管理员权限，这是为了能准确检测到所有游戏进程）。
3.  程序会最小化到右下角托盘区。
4.  **直接开始游戏：** 如果是支持 HDR 的大作（如《黑神话：悟空》、《赛博朋克2077》），屏幕会自动闪烁一下进入 HDR 模式。
5.  **如果游戏没反应：**
    * 右键点击托盘图标，选择“添加游戏到自定义名单...”。
    * **方式一：** 在列表中找到你正在玩的游戏进程，双击添加。
    * **方式二：** 点击左下角的“浏览文件...”，手动选择游戏的启动程序。
    * 添加后下次启动该游戏即可自动触发。
6.  你可以在右键菜单的“界面主题”中调整软件的外观风格。

### 数据存储
你的自定义名单和配置文件保存在 `%AppData%\AutoGameHDR` 目录下，升级或删除软件本体不会丢失配置。

---

### Built With
* C# / WPF (.NET 8.0)
* Hardcodet.NotifyIcon.Wpf
* Data Source: HDR-Game-Database
