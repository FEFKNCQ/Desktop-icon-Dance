namespace DesktopDance;

public readonly struct Vec
{
	public readonly double X;

	public readonly double Y;

	public static readonly Vec Zero = new Vec(0.0, 0.0);

	public Vec(double x, double y)
	{
		X = x;
		Y = y;
	}

	public static Vec operator +(Vec a, Vec b)
	{
		return new Vec(a.X + b.X, a.Y + b.Y);
	}

	public static Vec operator -(Vec a, Vec b)
	{
		return new Vec(a.X - b.X, a.Y - b.Y);
	}

	public static Vec operator *(Vec a, double k)
	{
		return new Vec(a.X * k, a.Y * k);
	}
}
