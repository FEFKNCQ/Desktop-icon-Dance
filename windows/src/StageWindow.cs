using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace DesktopDance;

public sealed class StageWindow : Window
{
	private readonly ScreenInfo screen;

	private nint hwnd;

	private bool pinnedAboveDesktop;

	public StageHost Host { get; }

	public string LayerNote { get; private set; } = "";

	public Msg LayerProblem { get; private set; } = Msg.None;

	public event Action? Clicked;

	public StageWindow(ScreenInfo sc, StageHost host)
	{
		screen = sc;
		Host = host;
		base.Title = "DesktopDance Stage";
		base.WindowStyle = WindowStyle.None;
		base.ResizeMode = ResizeMode.NoResize;
		base.ShowInTaskbar = false;
		base.ShowActivated = false;
		base.Topmost = false;
		base.Focusable = false;
		base.WindowStartupLocation = WindowStartupLocation.Manual;
		base.Left = (double)sc.MonX / sc.Scale;
		base.Top = (double)sc.MonY / sc.Scale;
		base.Width = (double)sc.MonW / sc.Scale;
		base.Height = (double)sc.MonH / sc.Scale;
		base.Background = Brushes.Black;
		host.HorizontalAlignment = HorizontalAlignment.Left;
		host.VerticalAlignment = VerticalAlignment.Top;
		base.Content = host;
		base.MouseDown += delegate(object s, MouseButtonEventArgs e)
		{
			e.Handled = true;
			this.Clicked?.Invoke();
		};
	}

	public void Open()
	{
		WindowInteropHelper windowInteropHelper = new WindowInteropHelper(this);
		hwnd = windowInteropHelper.EnsureHandle();
		long num = ((IntPtr)Native.GetWindowLongPtr(hwnd, -20)).ToInt64();
		Native.SetWindowLongPtr(hwnd, -20, new IntPtr(num | 0x8000000 | 0x80));
		HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);
		ToBottom();
		Show();
		ToBottom();
		CheckLayer();
	}

	private void ToBottom()
	{
		if (hwnd != IntPtr.Zero)
		{
			Native.SetWindowPos(hwnd, Native.HWND_BOTTOM, screen.MonX, screen.MonY, screen.MonW, screen.MonH, 16u);
		}
	}

	private static nint FindDesktopHost()
	{
		nint num = Native.FindWindow("Progman", null);
		if (num != IntPtr.Zero && Native.FindWindowEx(num, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero)
		{
			return num;
		}
		nint num2 = IntPtr.Zero;
		for (int i = 0; i < 200; i++)
		{
			num2 = Native.FindWindowEx(IntPtr.Zero, num2, "WorkerW", null);
			if (num2 == IntPtr.Zero)
			{
				break;
			}
			if (Native.FindWindowEx(num2, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero)
			{
				return num2;
			}
		}
		return IntPtr.Zero;
	}

	private static bool IsAbove(nint a, nint b)
	{
		nint num = a;
		for (int i = 0; i < 5000; i++)
		{
			if (num == IntPtr.Zero)
			{
				break;
			}
			num = Native.GetWindow(num, 2u);
			if (num == b)
			{
				return true;
			}
		}
		return false;
	}

	private void CheckLayer()
	{
		try
		{
			nint num = FindDesktopHost();
			if (num == IntPtr.Zero)
			{
				LayerNote = "没找到桌面窗口，无法确认层级";
				LayerProblem = new Msg("舞台层级：" + LayerNote, "Stage layer: desktop window not found, so the layer could not be confirmed");
			}
			else if (!IsAbove(num, hwnd))
			{
				LayerNote = "在桌面图标之上（正常）";
				LayerProblem = Msg.None;
			}
			else
			{
				nint window = Native.GetWindow(num, 3u);
				if (window != IntPtr.Zero && window != hwnd)
				{
					pinnedAboveDesktop = true;
					Native.SetWindowPos(hwnd, window, 0, 0, 0, 0, 19u);
				}
				bool flag = IsAbove(num, hwnd);
				LayerNote = (flag ? "被桌面盖住了，挪不上去（看到的会是真实桌面）" : "被桌面盖住过，已挪到桌面图标正上方");
				LayerProblem = new Msg("舞台层级：" + LayerNote, flag ? "Stage layer: covered by the desktop and could not be moved up (you will see the real desktop)" : "Stage layer: was covered by the desktop; moved to just above the desktop icons");
			}
		}
		catch (Exception ex)
		{
			LayerNote = "检查层级出错：" + ex.Message;
			LayerProblem = new Msg("舞台层级：" + LayerNote, "Stage layer: the check failed: " + ex.Message);
		}
		Log.Write("舞台窗口层级：" + LayerNote);
	}

	private nint WndProc(nint h, int msg, nint wParam, nint lParam, ref bool handled)
	{
		if (msg == 70 && lParam != IntPtr.Zero)
		{
			WINDOWPOS structure = Marshal.PtrToStructure<WINDOWPOS>(lParam);
			if (!pinnedAboveDesktop && (structure.flags & 4) == 0 && structure.hwndInsertAfter != Native.HWND_BOTTOM)
			{
				structure.hwndInsertAfter = Native.HWND_BOTTOM;
				Marshal.StructureToPtr(structure, lParam, fDeleteOld: false);
			}
		}
		else if (msg == 33)
		{
			handled = true;
			return new IntPtr(3);
		}
		return IntPtr.Zero;
	}
}
