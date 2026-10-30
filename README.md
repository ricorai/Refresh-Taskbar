# Refresh Taskbar

A lightweight, zero-dependency native Windows C# system tray utility that instantly force-refreshes the Windows taskbar and `explorer.exe` shell without visual lag or command prompt flashes.

---

## Highlights

- **Instant Force Refresh:** Terminates stalled `explorer.exe` processes and revives the shell cleanly in milliseconds.
- **Silent & Windowless:** No console windows, popups, or jarring visual flashes.
- **Windows 11 Dark Styled Menu:** Modern dark slate context menu renderer (`#1C1C1C` / `#2E2E2E`) with Electric Cyan accents (`#00E5FF`).
- **Live Startup Indicators:**
  - Dynamic `[ON]` / `[OFF]` badge in the menu item text.
  - Custom anti-aliased Electric Cyan circular checkmark badge.
  - Tray tooltip shows live status on hover (`Refresh Taskbar | Click to refresh | Startup: ON`).
  - Self-healing startup path: automatically updates the registry run key if the executable was moved to a new folder.
- **Persistent Tray State:** Listens for the Win32 `TaskbarCreated` message to automatically re-anchor its system tray icon across Explorer restarts.
- **CLI & IPC Controls:**
  - Double-clicking or running `RefreshTaskbar.exe` while it is already in the tray triggers an immediate refresh and exits in `<10ms`.
  - Pass `--refresh` or `-r` to trigger a refresh from a shortcut or command line.
  - Pass `--close` or `--stop` to cleanly close the running tray instance.
- **Tiny Footprint:** Single ~32 KB standalone native binary with zero runtime dependencies beyond standard Windows .NET.

---

## Usage

- **Left-Click Tray Icon:** Instantly force-refreshes the taskbar and desktop shell.
- **Right-Click Tray Icon:**
  - **Refresh Taskbar:** Dedicated menu trigger for the force-refresh.
  - **Open on startup:** Toggle automatic launch with Windows (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`) with live `[ON]` / `[OFF]` and checkmark indicators.
  - **Close:** Gracefully clean up handles and exit the utility.

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
├── RefreshTaskbar.cs       # Native C# source (Tray context, ModernMenuRenderer, IPC, ForceRefresh)
├── RefreshTaskbar.exe      # ~32 KB compiled standalone Windows binary
├── refresh.ico             # High-res multi-resolution icon (16x16 to 256x256)
├── refresh.png             # 256x256 source PNG asset
├── make_icon.py            # Python generator for the custom modern icon
├── build.bat               # Fast single-click native compilation script
├── Refresh Task Bar.bat    # Original legacy 6-line batch script
└── README.md               # Project documentation
```
