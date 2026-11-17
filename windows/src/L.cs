using System;
using System.Globalization;

namespace DesktopDance;

public static class L
{
	public static volatile Lang Current;

	public static string T(string zh, string en)
	{
		if (Current != Lang.Zh)
		{
			return en;
		}
		return zh;
	}

	public static Lang SystemDefault()
	{
		try
		{
			return (!CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)) ? Lang.En : Lang.Zh;
		}
		catch
		{
			return Lang.En;
		}
	}

	public static Lang? Parse(string? code)
	{
		if (string.Equals(code, "zh", StringComparison.OrdinalIgnoreCase))
		{
			return Lang.Zh;
		}
		if (string.Equals(code, "en", StringComparison.OrdinalIgnoreCase))
		{
			return Lang.En;
		}
		return null;
	}

	public static string Code(Lang l)
	{
		if (l != Lang.Zh)
		{
			return "en";
		}
		return "zh";
	}
}
