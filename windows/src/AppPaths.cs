using System;
using System.IO;

namespace DesktopDance;

public static class AppPaths
{
	private static string? dir;

	public static string Dir
	{
		get
		{
			if (dir == null)
			{
				string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesktopDance");
				try
				{
					Directory.CreateDirectory(path);
				}
				catch
				{
				}
				dir = path;
			}
			return dir;
		}
	}

	public static string LogFile => Path.Combine(Dir, "desktop-dance.log");

	public static string SettingsFile => Path.Combine(Dir, "settings.json");

	public static string MotionOverride => Path.Combine(Dir, "basketball-motion.json");
}
