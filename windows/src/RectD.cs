namespace DesktopDance;

public struct RectD
{
	public double X;

	public double Y;

	public double W;

	public double H;

	public double MinX => X;

	public double MaxX => X + W;

	public double MidX => X + W / 2.0;

	public double MinY => Y;

	public double MaxY => Y + H;

	public double MidY => Y + H / 2.0;

	public RectD(double x, double y, double w, double h)
	{
		X = x;
		Y = y;
		W = w;
		H = h;
	}

	public RectD Inset(double dx, double dy)
	{
		return new RectD(X + dx, Y + dy, W - 2.0 * dx, H - 2.0 * dy);
	}
}
