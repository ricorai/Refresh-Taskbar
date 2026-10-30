# Refresh Taskbar

A lightweight, zero-dependency native Windows C# system tray utility that instantly force-refreshes the Windows taskbar and `explorer.exe` shell without visual lag or command prompt flashes.

---

## Highlights

- **Instant Force Refresh:** Terminates stalled `explorer.exe` processes and revives the shell cleanly in milliseconds.
- **Silent & Windowless:** No console windows, popups, or jarring visual flashes.
- **Persistent Tray State:** Listens for the Win32 `TaskbarCreated` message to automatically re-anchor its system tray icon across Explorer restarts.
- **Single-Instance with IPC Broadcast:** Running `RefreshTaskbar.exe` while it is already in the tray broadcasts a registered Windows message to trigger an immediate refresh and exits in `<10ms`.
- **Minimalist 2-Item Menu:** Right-click context menu features only `Open on startup` and `Close`.
- **Tiny Footprint:** Single ~30 KB standalone native binary with zero runtime dependencies beyond standard Windows .NET.

---

## Usage

- **Left-Click Tray Icon:** Instantly force-refreshes the taskbar and desktop shell.
- **Right-Click Tray Icon:**
  - **Open on startup:** Toggle automatic launch with Windows (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`).
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
├── RefreshTaskbar.cs       # Native C# source (Tray context, TaskbarCreated handler, ForceRefresh)
├── RefreshTaskbar.exe      # 30 KB compiled standalone Windows binary
├── refresh.ico             # High-res multi-resolution icon (16x16 to 256x256)
├── refresh.png             # 256x256 source PNG asset
├── make_icon.py            # Python generator for the custom modern icon
├── build.bat               # Fast single-click native compilation script
├── Refresh Task Bar.bat    # Original legacy 6-line batch script
└── README.md               # Project documentation
```
