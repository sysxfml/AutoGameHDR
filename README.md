# AutoGameHDR 🎮

![License](https://img.shields.io/github/license/sysxfml/AutoGameHDR)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)
![Version](https://img.shields.io/github/v/release/sysxfml/AutoGameHDR)

[English](#english) | [中文](#chinese)

**AutoGameHDR** is an ultra-lightweight Windows utility that automatically toggles System HDR on/off when you launch supported games. Designed to bring the console-like "Auto HDR" experience to PC gamers.

---

<a name="english"></a>
## English

### ✨ Features
* **Automatic HDR Toggling:** Detects game launch events and enables Windows HDR instantly. Disables it when you quit to save your eyes.
* **Cloud-Sync Database:** Powered by [HDR-Game-Database](https://github.com/sysxfml/HDR-Game-Database), the whitelist updates automatically every day based on PCGamingWiki data.
* **Manual Override:** Game not detected? Simply right-click the tray icon -> "Add Recent Game" to add it to your local whitelist forever.
* **Lightweight:** Runs silently in the system tray with minimal resource usage (<10MB RAM).
* **Single File:** No installation required. Portable EXE.

### 📥 Download
Go to the [**Releases**](https://github.com/sysxfml/AutoGameHDR/releases) page to download the latest `AutoGameHDR.exe`.

### 🚀 Usage
1.  Download and place `AutoGameHDR.exe` anywhere (e.g., Documents or Desktop).
2.  Run it (Run as Administrator is recommended for best detection).
3.  Find the shield icon 🛡️ (or your custom icon) in the system tray.
4.  **Launch a game!** If it's a known HDR title (e.g., *Cyberpunk 2077*, *Elden Ring*), your monitor will switch to HDR mode automatically.
5.  **Game not recognized?** * Alt-Tab to desktop.
    * Right-click the tray icon -> **"Add Recent Game..."**
    * Select your running game. Done!

---

<a name="chinese"></a>
## 中文

### ✨ 核心功能
* **HDR 自动开关：** 像主机一样，打开游戏自动开启 Windows HDR，退出游戏自动切回 SDR，保护眼睛。
* **云端白名单：** 内置云端同步功能，依赖 [HDR-Game-Database](https://github.com/sysxfml/HDR-Game-Database) 项目，每日自动更新最新的 HDR 游戏支持列表。
* **手动添加：** 遇到冷门游戏没反应？右键托盘图标即可“添加刚才运行的游戏”，永久生效。
* **极简轻量：** 无主界面，仅在托盘运行，几乎不占系统资源。
* **单文件绿色版：** 无需安装，下载即用。

### 📥 下载
请前往 [**Releases (发行版)**](https://github.com/sysxfml/AutoGameHDR/releases) 页面下载最新的 `AutoGameHDR.exe` 文件。

### 🚀 使用方法
1.  下载 `AutoGameHDR.exe` 并放在任意文件夹。
2.  运行程序 (建议右键属性勾选“以管理员身份运行”，检测更灵敏)。
3.  程序会最小化到右下角托盘区。
4.  **直接玩游戏：** 如果是支持 HDR 的大作，屏幕会自动黑屏闪烁一下进入 HDR 模式。
5.  **如果没反应：**
    * 切回桌面。
    * 右键托盘图标 -> **"添加刚才运行的游戏..."**。
    * 选择你的游戏进程。下次启动就会自动触发了！

---
### 🛠️ Built With
* C# / WPF (.NET 8.0)
* [Hardcodet.NotifyIcon.Wpf](https://github.com/hardcodet/wpf-notifyicon)
* Data Source: [HDR-Game-Database](https://github.com/sysxfml/HDR-Game-Database)
