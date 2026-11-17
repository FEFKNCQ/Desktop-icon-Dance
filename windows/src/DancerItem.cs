using System.Windows.Media.Imaging;

namespace DesktopDance;

public sealed class DancerItem
{
	public string Name = "";

	public string Key = "";

	public bool IsGuest;

	public bool HasPos;

	public double PxX;

	public double PxY;

	public Vec Home;

	public BitmapSource? Icon;
}
