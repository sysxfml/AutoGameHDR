# AutoGameHDR 🎮

![License](https://img.shields.io/github/license/sysxfml/AutoGameHDR)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)
![Version](https://img.shields.io/github/v/release/sysxfml/AutoGameHDR)

[English](#english) | [中文](#chinese)

**AutoGameHDR** is a simple, lightweight tool for Windows that brings the "Auto HDR" experience from consoles to your PC. It detects when you launch a game and automatically toggles Windows HDR on, then turns it off when you're done. No more digging through display settings every time you want to play.

---

<a name="english"></a>
## 🇬🇧 English

### ✨ Key Features
* **Auto HDR Toggle:** Instantly enables Windows HDR when a supported game launches and disables it upon exit. Saves you the hassle of manual switching.
* **Cloud-Synced Whitelist:** Powered by [HDR-Game-Database](https://github.com/sysxfml/HDR-Game-Database), the app automatically pulls the latest list of HDR-supported games from GitHub daily.
* **Process Selector:** Game not detected? Right-click the tray icon and use the **"Add from Running Processes"** feature to pick your game from a list—just like Cheat Engine.
* **Manual Update:** You can force an update of the online game list at any time via the right-click menu.
* **Zero Distraction:** Runs silently in the system tray with minimal resource usage (<10MB RAM). Configurable to run at Windows startup.
* **Portable:** A single `.exe` file. No installation needed.

### 📥 Download
Grab the latest `AutoGameHDR.exe` from the [**Releases Page**](https://github.com/sysxfml/AutoGameHDR/releases).

### 🚀 How to Use
1.  Download and place `AutoGameHDR.exe` anywhere you like.
2.  Run the app. **Note:** It will request Administrator privileges to ensure it can detect all game processes correctly.
3.  You'll see a small controller icon in your system tray.
4.  **Just play:** Launch a known HDR title (e.g., *Cyberpunk 2077*, *Elden Ring*), and your screen will switch to HDR mode automatically.
5.  **Game not recognized?**
    * Right-click the tray icon -> **"Add from Running Processes..."**
    * Select your game from the list. It's now saved locally!
    * You can manage your custom list via **"Manage Custom List..."**.

### 📂 Data Location
User configurations and local whitelists are stored in `%AppData%\AutoGameHDR`.

---

<a name="chinese"></a>
## 🇨🇳 中文

### ✨ 核心功能
* **HDR 自动开关：** 像主机体验一样，打开游戏自动开启 Windows HDR，退出游戏自动切回 SDR。不用再忍受桌面模式下 HDR 的发白色彩。
* **云端同步：** 软件依托于 [HDR-Game-Database](https://github.com/sysxfml/HDR-Game-Database) 项目，每天自动从 GitHub 获取最新的 HDR 游戏支持列表（基于 PCGamingWiki 数据）。
* **智能添加：** 遇到冷门游戏没反应？右键菜单选择 **“从运行中的进程添加”**，直接在列表里勾选你的游戏，立即生效。
* **手动更新：** 支持通过右键菜单手动强制更新云端白名单，随时获取最新支持库。
* **极简轻量：** 无主界面，仅在托盘静默运行，几乎不占系统资源。支持开机自启。
* **单文件版：** 一个 exe 文件搞定所有，无需安装。

### 📥 下载
请前往 [**Releases (发行版)**](https://github.com/sysxfml/AutoGameHDR/releases) 页面下载最新的 `AutoGameHDR.exe`。

### 🚀 使用指南
1.  下载 `AutoGameHDR.exe` 放在任意位置。
2.  运行程序（程序启动时会请求管理员权限，这是为了能准确检测到所有游戏进程）。
3.  程序会最小化到右下角托盘区。
4.  **直接开始游戏：** 如果是支持 HDR 的大作（如《黑神话：悟空》、《赛博朋克2077》），屏幕会自动闪烁一下进入 HDR 模式。
5.  **如果没反应：**
    * 右键托盘图标 -> **"从运行中的进程添加..."**
    * 在弹出的窗口中双击你的游戏进程。
    * 下次启动该游戏时就会自动触发了！

### 📂 数据存储
你的自定义名单和配置文件保存在 `%AppData%\AutoGameHDR` 目录下，不会污染程序所在目录。

---
### 🛠️ Built With
* C# / WPF (.NET 8.0)
* [Hardcodet.NotifyIcon.Wpf](https://github.com/hardcodet/wpf-notifyicon)
* Data Source: [HDR-Game-Database](https://github.com/sysxfml/HDR-Game-Database)
