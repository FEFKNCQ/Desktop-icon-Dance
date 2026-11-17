using System;

namespace DesktopDance;

public static class M
{
	public static double Lerp(double a, double b, double t)
	{
		return a + (b - a) * t;
	}

	public static Vec Lerp(Vec a, Vec b, double t)
	{
		return new Vec(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
	}

	public static double Clamp(double v, double lo, double hi)
	{
		return Math.Min(Math.Max(v, lo), hi);
	}

	public static double Smooth(double t)
	{
		double num = Clamp(t, 0.0, 1.0);
		return num * num * (3.0 - 2.0 * num);
	}

	public static double Hypot(double x, double y)
	{
		return Math.Sqrt(x * x + y * y);
	}

	public static double Round(double v)
	{
		return Math.Round(v, MidpointRounding.AwayFromZero);
	}

	public static double Hash01(int a, int b)
	{
		long num = a * -7046029254386353131L + b * -4658895280553007687L;
		long num2 = (num ^ (num >>> 30)) * -4658895280553007687L;
		long num3 = (num2 ^ (num2 >>> 27)) * -7723592293110705685L;
		return (double)((ulong)(num3 ^ (num3 >>> 31)) >> 11) / 9007199254740992.0;
	}
}
