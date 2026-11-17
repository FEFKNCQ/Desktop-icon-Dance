using System;

namespace DesktopDance;

internal static class ShellGuids
{
	public static readonly Guid CLSID_ShellWindows = new Guid("9BA05972-F6A8-11CF-A442-00A0C90A8F39");

	public static readonly Guid SID_STopLevelBrowser = new Guid("4C96BE40-915C-11CF-99D3-00AA004AE837");

	public static readonly Guid BHID_EnumItems = new Guid("94f60519-2850-4924-aa5a-d15e84868039");

	public const int SWC_DESKTOP = 8;

	public const int SWFO_NEEDDISPATCH = 1;

	public const uint SVGIO_ALLVIEW = 2u;

	public const uint SIGDN_NORMALDISPLAY = 0u;

	public const uint SIGDN_DESKTOPABSOLUTEPARSING = 2147647488u;

	public const int SIIGBF_RESIZETOFIT = 0;

	public const int SIIGBF_ICONONLY = 4;
}
