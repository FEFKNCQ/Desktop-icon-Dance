using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DesktopDance;

public sealed class LoadResult
{
	public List<DancerItem> Items = new List<DancerItem>();

	public int DesktopCount;

	public ScreenInfo Screen = new ScreenInfo();

	public double IconDip = 48.0;

	public double CellW = 76.0;

	public double CellH = 100.0;

	public bool PositionsFromShell;

	public int OffScreenCount;

	public BitmapSource? Curtain;

	public Color CurtainColor = Color.FromRgb(20, 20, 20);

	public string CurtainNote = "";

	public Msg CurtainProblem = Msg.None;

	public Dictionary<string, POINT> Snapshot = new Dictionary<string, POINT>();

	public List<string> Diag = new List<string>();
}
