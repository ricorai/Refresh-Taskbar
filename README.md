# Refresh Taskbar

A lightweight, zero-dependency native Windows C# system tray and desktop utility that instantly force-refreshes the Windows taskbar and `explorer.exe` shell without visual lag or command prompt flashes.

---

## Highlights

- **Instant Force Refresh:** Terminates stalled `explorer.exe` processes and revives the shell cleanly in milliseconds.
- **Silent & Windowless:** No console windows, popups, or jarring visual flashes.
- **Desktop Context Menu Integration (Dynamic):**
  - Appears in your Windows Desktop right-click context menu **only while the app is running**.
  - Positioned cleanly below the native Refresh group.
  - Automatically unregisters and vanishes from the Desktop right-click menu whenever the app is closed.
  - Can be toggled on/off directly from the tray context menu (`Desktop right-click menu [ON/OFF]`).
- **Strict Light Mode B&W Palette:**
  - Modern crisp white background (`#FFFFFF`) with pure high-contrast black text (`#111111`).
  - Solid black circular checkmark badge with crisp white tick (zero colors/cyan).
  - Hover highlights in subtle light gray (`#F3F4F6`).
- **Live Startup Indicators:**
  - Dynamic `[ON]` / `[OFF]` badge in the menu item text.
  - Tray tooltip shows live status on hover (`Refresh Taskbar | Click to refresh | Startup: ON`).
  - Self-healing startup path: automatically updates the registry run key if the executable was moved to a new folder.
- **Persistent Tray State:** Listens for the Win32 `TaskbarCreated` message to automatically re-anchor its system tray icon across Explorer restarts.
- **CLI & IPC Controls:**
  - Double-clicking or running `RefreshTaskbar.exe` while it is already in the tray triggers an immediate refresh and exits in `<10ms`.
  - Pass `--refresh` or `-r` to trigger a refresh from a shortcut or command line.
  - Pass `--close` or `--stop` to cleanly close the running tray instance and unregister desktop integration.
- **Tiny Footprint:** Single ~33 KB standalone native binary with zero runtime dependencies beyond standard Windows .NET.

---

## Usage

- **Desktop Right-Click:** Click **Refresh Taskbar** directly from the desktop menu (available while app is up).
- **Left-Click Tray Icon:** Instantly force-refreshes the taskbar and desktop shell.
- **Right-Click Tray Icon:**
  - **Refresh Taskbar:** Dedicated menu trigger for the force-refresh.
  - **Desktop right-click menu:** Toggle whether it appears on desktop background right-click (`[ON]` / `[OFF]`).
  - **Open on startup:** Toggle automatic launch with Windows (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`) with live `[ON]` / `[OFF]` indicators.
  - **Close:** Gracefully unregister desktop context menu, clean up handles, and exit the utility.

---

## Directory Architecture

```text
Refresh Taskbar/
├── assets/                 # How it looks (visual assets & icon generator)
│   ├── refresh.ico         # High-res monochrome icon (16x16 to 256x256)
│   ├── refresh.png         # 256x256 source PNG asset
│   └── make_icon.py        # Python procedural generator for the custom icons
├── src/                    # How it works (engine source code & build script)
│   ├── RefreshTaskbar.cs   # Native C# source (Tray context, Desktop menu IPC, ForceRefresh)
│   └── build.bat           # 1-click native compiler script (outputs to ../RefreshTaskbar.exe)
├── history/                # History (legacy origin scripts & snapshots)
│   └── Refresh Task Bar.bat# Original legacy 6-line batch script
├── .gitignore              # Git ignore rules for build cache & temp files
├── README.md               # Project documentation & architecture guide
└── RefreshTaskbar.exe      # Standalone compiled 33 KB native Windows binary
```
