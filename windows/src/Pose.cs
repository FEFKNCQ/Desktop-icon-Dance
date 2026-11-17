namespace DesktopDance;

public struct Pose
{
	public Vec P;

	public double Rot;

	public double Sx;

	public double Sy;

	public double Alpha;

	public double Z;

	public Pose(Vec p, double rot = 0.0, double sx = 1.0, double sy = 1.0, double alpha = 1.0, double z = 0.0)
	{
		P = p;
		Rot = rot;
		Sx = sx;
		Sy = sy;
		Alpha = alpha;
		Z = z;
	}

	public static Pose Mix(Pose a, Pose b, double t)
	{
		return new Pose(M.Lerp(a.P, b.P, t), M.Lerp(a.Rot, b.Rot, t), M.Lerp(a.Sx, b.Sx, t), M.Lerp(a.Sy, b.Sy, t), M.Lerp(a.Alpha, b.Alpha, t), (t < 0.5) ? a.Z : b.Z);
	}
}
