using System;
using System.Runtime.InteropServices;

namespace DesktopDance;

public sealed class ScreenInfo
{
	public int MonX;

	public int MonY;

	public int MonW = 1920;

	public int MonH = 1080;

	public int WorkX;

	public int WorkY;

	public int WorkW = 1920;

	public int WorkH = 1040;

	public int VirtX;

	public int VirtY;

	public int VirtW = 1920;

	public int VirtH = 1080;

	public double Scale = 1.0;

	public static ScreenInfo Read()
	{
		ScreenInfo screenInfo = new ScreenInfo();
		try
		{
			nint num = Native.MonitorFromPoint(new POINT
			{
				X = 0,
				Y = 0
			}, 1u);
			MONITORINFO lpmi = new MONITORINFO
			{
				cbSize = Marshal.SizeOf<MONITORINFO>()
			};
			if (num != IntPtr.Zero && Native.GetMonitorInfo(num, ref lpmi))
			{
				screenInfo.MonX = lpmi.rcMonitor.Left;
				screenInfo.MonY = lpmi.rcMonitor.Top;
				screenInfo.MonW = lpmi.rcMonitor.Right - lpmi.rcMonitor.Left;
				screenInfo.MonH = lpmi.rcMonitor.Bottom - lpmi.rcMonitor.Top;
				screenInfo.WorkX = lpmi.rcWork.Left;
				screenInfo.WorkY = lpmi.rcWork.Top;
				screenInfo.WorkW = lpmi.rcWork.Right - lpmi.rcWork.Left;
				screenInfo.WorkH = lpmi.rcWork.Bottom - lpmi.rcWork.Top;
				if (Native.GetDpiForMonitor(num, 0, out var dpiX, out var _) == 0 && dpiX >= 48)
				{
					screenInfo.Scale = (double)dpiX / 96.0;
				}
			}
			screenInfo.VirtX = Native.GetSystemMetrics(76);
			screenInfo.VirtY = Native.GetSystemMetrics(77);
			screenInfo.VirtW = Native.GetSystemMetrics(78);
			screenInfo.VirtH = Native.GetSystemMetrics(79);
			if (screenInfo.VirtW <= 0 || screenInfo.VirtH <= 0)
			{
				screenInfo.VirtX = screenInfo.MonX;
				screenInfo.VirtY = screenInfo.MonY;
				screenInfo.VirtW = screenInfo.MonW;
				screenInfo.VirtH = screenInfo.MonH;
			}
		}
		catch (Exception ex)
		{
			Log.Write("读屏幕信息失败：" + ex.Message);
		}
		return screenInfo;
	}

	public override string ToString()
	{
		return "主屏 " + MonW + "x" + MonH + " @(" + MonX + "," + MonY + ")，工作区 " + WorkW + "x" + WorkH + " @(" + WorkX + "," + WorkY + ")，缩放 " + (Scale * 100.0).ToString("0") + "%，全部屏幕 " + VirtW + "x" + VirtH + " @(" + VirtX + "," + VirtY + ")";
	}
}
