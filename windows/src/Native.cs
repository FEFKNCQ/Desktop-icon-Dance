using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DesktopDance;

internal static class Native
{
	public const int GWL_EXSTYLE = -20;

	public const long WS_EX_TOOLWINDOW = 128L;

	public const long WS_EX_NOACTIVATE = 134217728L;

	public static readonly nint HWND_BOTTOM = new IntPtr(1);

	public const uint SWP_NOSIZE = 1u;

	public const uint SWP_NOMOVE = 2u;

	public const uint SWP_NOZORDER = 4u;

	public const uint SWP_NOACTIVATE = 16u;

	public const int WM_WINDOWPOSCHANGING = 70;

	public const int WM_MOUSEACTIVATE = 33;

	public const int WM_HOTKEY = 786;

	public const int WM_APP = 32768;

	public const int MA_NOACTIVATE = 3;

	public const uint MOD_NOREPEAT = 16384u;

	public const uint VK_ESCAPE = 27u;

	public const uint SPI_GETDESKWALLPAPER = 115u;

	public const uint WDA_NONE = 0u;

	public const uint WDA_EXCLUDEFROMCAPTURE = 17u;

	public const int SM_XVIRTUALSCREEN = 76;

	public const int SM_YVIRTUALSCREEN = 77;

	public const int SM_CXVIRTUALSCREEN = 78;

	public const int SM_CYVIRTUALSCREEN = 79;

	public const int COLOR_DESKTOP = 1;

	public const uint GW_HWNDNEXT = 2u;

	public const uint GW_HWNDPREV = 3u;

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

	[DllImport("user32.dll")]
	public static extern bool UnregisterHotKey(nint hWnd, int id);

	[DllImport("user32.dll", EntryPoint = "GetMessageW")]
	public static extern int GetMessage(out MSG lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

	[DllImport("user32.dll", EntryPoint = "PeekMessageW")]
	public static extern bool PeekMessage(out MSG lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

	[DllImport("user32.dll", EntryPoint = "PostThreadMessageW")]
	public static extern bool PostThreadMessage(uint idThread, uint msg, nint wParam, nint lParam);

	[DllImport("kernel32.dll")]
	public static extern uint GetCurrentThreadId();

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

	[DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
	public static extern nint GetWindowLongPtr(nint hWnd, int nIndex);

	[DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
	public static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

	[DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "FindWindowExW")]
	public static extern nint FindWindowEx(nint hWndParent, nint hWndChildAfter, string? lpszClass, string? lpszWindow);

	[DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "FindWindowW")]
	public static extern nint FindWindow(string? lpClassName, string? lpWindowName);

	[DllImport("user32.dll")]
	public static extern nint GetWindow(nint hWnd, uint uCmd);

	[DllImport("user32.dll")]
	public static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

	[DllImport("user32.dll")]
	public static extern bool IsWindowVisible(nint hWnd);

	[DllImport("user32.dll")]
	public static extern int GetSystemMetrics(int nIndex);

	[DllImport("user32.dll")]
	public static extern uint GetSysColor(int nIndex);

	[DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "SystemParametersInfoW", SetLastError = true)]
	public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, StringBuilder pvParam, uint fWinIni);

	[DllImport("user32.dll")]
	public static extern nint MonitorFromPoint(POINT pt, uint dwFlags);

	[DllImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
	public static extern bool GetMonitorInfo(nint hMonitor, ref MONITORINFO lpmi);

	[DllImport("shcore.dll")]
	public static extern int GetDpiForMonitor(nint hMonitor, int dpiType, out uint dpiX, out uint dpiY);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern bool SetWindowDisplayAffinity(nint hWnd, uint dwAffinity);

	[DllImport("user32.dll")]
	public static extern nint GetDC(nint hWnd);

	[DllImport("user32.dll")]
	public static extern int ReleaseDC(nint hWnd, nint hDC);

	[DllImport("gdi32.dll")]
	public static extern bool DeleteObject(nint hObject);

	[DllImport("gdi32.dll", EntryPoint = "GetObjectW")]
	public static extern int GetObject(nint h, int c, out BITMAP pv);

	[DllImport("gdi32.dll")]
	public static extern int GetDIBits(nint hdc, nint hbm, uint start, uint cLines, [Out] byte[] lpvBits, ref BITMAPINFO lpbmi, uint usage);

	[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
	public static extern int SHCreateItemFromParsingName(string pszPath, nint pbc, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out IShellItem? ppv);

	[DllImport("shell32.dll")]
	public static extern int SHCreateItemFromIDList(nint pidl, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out IShellItem? ppv);

	[DllImport("shell32.dll")]
	public static extern int SHGetNameFromIDList(nint pidl, uint sigdnName, out nint ppszName);
}
