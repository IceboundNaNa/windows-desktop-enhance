# Windows Desktop Enhance

<p align="center">
  <strong>Lightweight Windows desktop icon right-alignment, exclusion zones &amp; layout guard powered by native Shell COM APIs.</strong><br/>
  <strong>基于 Windows 底层 Shell COM 接口的轻量级桌面图标靠右排列、避让区域与防重置守护工具。</strong>
</p>

<p align="center">
  <a href="#english">English</a> · <a href="#简体中文">简体中文</a>
</p>

---

<a id="english"></a>

## English

### Overview

**Windows Desktop Enhance** is a lightweight, zero-dependency Windows desktop utility built on native Shell COM and Win32 APIs. It automatically aligns desktop icons to the right side of your screen while preserving Windows' authentic native sorting rules (**Name**, **Size**, **Item Type**, **Date Modified**), lets you draw custom desktop exclusion zones, and guards your saved layout against unexpected system resets.

### Key Features

- **Native Shell COM Engine**
  - Connects directly to Windows Explorer's desktop view via `IShellWindows` → `IServiceProvider` → `IShellBrowser` → `IShellView` → `IFolderView` / `IFolderView2` / `IShellFolder2`.
  - Uses `IShellFolder2::MapColumnToSCID` and `IShellFolder::CompareIDs` to preserve 100% authentic Windows sorting (natural numeric order, folder priority, system virtual icons like *This PC* / *Recycle Bin*, ascending & descending).
  - Flicker-free batch positioning via `WM_SETREDRAW` + `IFolderView::SelectAndPositionItems`, with automatic taskbar work-area detection (`GetMonitorInfo`) and native grid spacing (`IFolderView::GetSpacing`).
- **Auto Right-Align & System Takeover**
  - Monitors User & Public Desktop folders for file changes and display/session events.
  - Takes over Windows' built-in left-aligned auto-arrange (`FWF_AUTOARRANGE`) automatically to prevent conflicts.
- **Desktop Exclusion Zones**
  - Draw one or more exclusion rectangles directly on your desktop (Per-Monitor DPI aware) to keep wallpapers or desktop widgets unobstructed.
  - Integrated directly into the Overview live desktop preview — toggle, edit/redraw, or remove zones anytime.
- **Layout Snapshots & Reset Guard**
  - Save and restore exact desktop icon coordinates with one click.
  - Automatically restores your saved layout after display resolution changes, lock/unlock, wake from sleep, Explorer restarts, or unexpected Windows resets (automatically pauses while you are dragging icons).
- **Material Design 3 Theme & Multi-Language UI**
  - Dynamic tonal color scheme with 6 preset theme colors, Light / Dark / System / Pure Black modes, and anti-aliased rounded window corners.
  - Auto-detects Windows display language with live switching: **English**, **简体中文**, **繁體中文**, **日本語**, **한국어**.

### Two Editions

| Edition | File | Description |
| :--- | :--- | :--- |
| **Portable** | `Windows Desktop Enhance.exe` | Zero-install standalone executable (~138 KB). Run directly anywhere. |
| **Installer** | `Windows Desktop Enhance Setup.exe` | Multi-language Material 3 setup wizard (~166 KB). Choose install path, desktop shortcut, and synced Windows startup; registers Start Menu shortcut and Windows Apps uninstaller. |

### Project Structure

```text
windows desktop enhance/
├── Windows Desktop Enhance.exe         # Portable edition (no installation required)
├── Windows Desktop Enhance Setup.exe   # Installer edition (multi-language setup wizard)
├── build.ps1                           # One-click build script for both editions
├── README.md
├── src/                                # Main application source code (WPF / C#)
│   ├── WindowsDesktopEnhance.csproj
│   ├── Models/                         # Settings & desktop icon data models
│   ├── Native/                         # Shell COM / Win32 interop & layout engine
│   ├── Services/                       # File watcher, layout guard, tray, startup, i18n
│   └── Themes/                         # Material Design 3 dynamic tonal styles
└── installer/                          # Multi-language installer source code
```

### Build from Source

No Visual Studio required — uses the built-in Windows .NET Framework 4.8 MSBuild:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Both `Windows Desktop Enhance.exe` and `Windows Desktop Enhance Setup.exe` will be built directly into the repository root.

---

<a id="简体中文"></a>

## 简体中文

### 项目简介

**Windows Desktop Enhance** 是一款基于 Windows 底层 Shell COM 与 Win32 API 开发的轻量级桌面增强工具。它在完整保留 Windows 原生排序规则（**名称**、**大小**、**项目类型**、**修改日期**）的前提下，支持桌面图标自动靠右排列、接管系统自动排列防止冲突、在桌面上直接框选避让区域，以及在系统重置图标时自动还原已保存的桌面布局。

### 核心特性

- **底层 Shell COM 引擎与原生排序**
  - 通过 `IShellWindows` → `IServiceProvider` → `IShellBrowser` → `IShellView` → `IFolderView` / `IFolderView2` / `IShellFolder2` 直接操作桌面视图。
  - 动态调用 `IShellFolder2::MapColumnToSCID` 与 `IShellFolder::CompareIDs`，100% 保留 Windows 原生排序逻辑（自然数字排序、文件夹优先、此电脑/回收站等系统图标顺序，支持升序与降序）。
  - 结合 `WM_SETREDRAW` 与 `IFolderView::SelectAndPositionItems` 实现无闪烁批量定位，自动适配任务栏工作区（`GetMonitorInfo`）与系统原生网格间距（`IFolderView::GetSpacing`）。
- **自动靠右排列与系统接管**
  - 实时监听用户桌面与公共桌面目录的新增、删除、重命名事件及显示/会话变化。
  - 开启后自动接管并禁用系统自带左侧自动排列（`FWF_AUTOARRANGE`），防止系统冲突。
- **桌面避让区域**
  - 支持在真实桌面上拖曳框选一个或多个不放置图标的区域（完整适配 Per-Monitor DPI 缩放），保护壁纸主体或桌面小组件不被遮挡。
  - 直接集成在「概览 → 桌面预览」中，支持随时开关、修改重选或删除。
- **布局快照与防重置守护**
  - 一键保存与还原当前桌面所有图标的精确坐标快照。
  - 支持开机自动启动（安装器与主程序状态双向同步），在分辨率切换、锁屏解锁、睡眠唤醒、资源管理器重启或系统将图标切回左侧时自动还原布局（鼠标拖拽期间自动避让）。
- **Material Design 3 主题与多语言**
  - 动态色调配色系统，内置 6 款主题色、浅色/深色/跟随系统/纯黑背景，统一 16px 抗锯齿圆角窗口。
  - 默认自动匹配 Windows 系统显示语言，支持免重启切换：**简体中文**、**繁體中文**、**English**、**日本語**、**한국어**。

### 两个发行版本

| 版本 | 文件名 | 说明 |
| :--- | :--- | :--- |
| **免安装绿色版** | `Windows Desktop Enhance.exe` | 单文件零依赖（约 138 KB），无需安装，直接双击运行。 |
| **安装版** | `Windows Desktop Enhance Setup.exe` | 多语言图形安装向导（约 166 KB），支持自定义安装路径、创建桌面快捷方式、同步开机自启动，并自动注册开始菜单快捷方式与 Windows 卸载项。 |

### 目录结构

```text
windows desktop enhance/
├── Windows Desktop Enhance.exe         # 免安装绿色版
├── Windows Desktop Enhance Setup.exe   # 安装版安装包
├── build.ps1                           # 一键同时编译免安装版与安装版
├── README.md
├── src/                                # 主程序源码（WPF / C#）
│   ├── WindowsDesktopEnhance.csproj
│   ├── Models/                         # 配置与图标数据模型
│   ├── Native/                         # Shell COM / Win32 底层接口与排列引擎
│   ├── Services/                       # 文件监听、布局守护、托盘、自启动、多语言
│   └── Themes/                         # Material Design 3 动态色调主题
└── installer/                          # 多语言安装向导源码
```

### 从源码编译

无需安装 Visual Studio，直接使用 Windows 内置的 .NET Framework 4.8 MSBuild 即可一键编译两个版本：

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

编译产物 `Windows Desktop Enhance.exe` 与 `Windows Desktop Enhance Setup.exe` 将直接输出到项目根目录。

---

## Requirements / 运行环境

- Windows 10 / Windows 11 (x64)
- .NET Framework 4.8 (pre-installed on Windows 10/11 / 系统自带)

## License

MIT License
