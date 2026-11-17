using System.Diagnostics;

namespace DesktopDance;

public static class Clock
{
	private static readonly Stopwatch sw = Stopwatch.StartNew();

	public static double Now => sw.Elapsed.TotalSeconds;
}
