# Refresh Taskbar

A lightweight, zero-dependency native Windows C# system tray and desktop utility that instantly force-refreshes the Windows taskbar and `explorer.exe` shell without visual lag or command prompt flashes.

---

## Highlights

- **Instant Force Refresh:** Terminates stalled `explorer.exe` processes and revives the shell cleanly in milliseconds.
- **Silent & Windowless:** No console windows, popups, or jarring visual flashes.
- **Desktop Context Menu Integration (Dynamic):**
  - Appears in your Windows Desktop right-click context menu **only while the app is running**.
  - Cleanly self-unregisters and vanishes from the Desktop right-click menu whenever the app is closed.
  - Can also be toggled directly from the tray context menu (`Desktop right-click menu [ON/OFF]`).
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

## Compilation

Builds using the native Windows C# compiler (`csc.exe`):

```cmd
build.bat
```

Or directly via command line:

```cmd
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:winexe /optimize+ /platform:anycpu /win32icon:"refresh.ico" /r:System.dll,System.Windows.Forms.dll,System.Drawing.dll /out:"RefreshTaskbar.exe" "RefreshTaskbar.cs"
```

---

## File Structure

```text
Refresh Taskbar/
├── RefreshTaskbar.cs       # Native C# source (Tray context, Desktop menu IPC, ForceRefresh)
├── RefreshTaskbar.exe      # ~33 KB compiled standalone Windows binary
├── refresh.ico             # High-res monochrome icon (16x16 to 256x256)
├── refresh.png             # 256x256 source PNG asset
├── make_icon.py            # Python generator for the custom monochrome icon
├── build.bat               # Fast single-click native compilation script
├── Refresh Task Bar.bat    # Original legacy 6-line batch script
└── README.md               # Project documentation
```
