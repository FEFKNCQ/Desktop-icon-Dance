using System;
using System.IO;
using System.Text;

namespace DesktopDance;

public static class Log
{
	private static readonly object gate = new object();

	public static void StartSession(string header)
	{
		try
		{
			FileInfo fileInfo = new FileInfo(AppPaths.LogFile);
			if (fileInfo.Exists && fileInfo.Length > 524288)
			{
				string destFileName = AppPaths.LogFile + ".old";
				File.Copy(AppPaths.LogFile, destFileName, overwrite: true);
				File.WriteAllText(AppPaths.LogFile, "", Encoding.UTF8);
			}
		}
		catch
		{
		}
		Write("======== " + header + " ========");
	}

	public static void Write(string s)
	{
		string contents = "[" + DateTime.Now.ToString("MM-dd HH:mm:ss.fff") + "] " + s + Environment.NewLine;
		try
		{
			lock (gate)
			{
				File.AppendAllText(AppPaths.LogFile, contents, Encoding.UTF8);
			}
		}
		catch
		{
		}
	}
}
