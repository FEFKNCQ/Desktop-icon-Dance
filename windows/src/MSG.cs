namespace DesktopDance;

public struct MSG
{
	public nint hwnd;

	public uint message;

	public nint wParam;

	public nint lParam;

	public uint time;

	public POINT pt;

	public uint lPrivate;
}
public readonly struct Msg
{
	public readonly string Zh;

	public readonly string En;

	public static readonly Msg None = new Msg("", "");

	public bool IsEmpty => string.IsNullOrEmpty(Zh);

	public string Text => L.T(Zh ?? "", En ?? "");

	public Msg(string zh, string en)
	{
		Zh = zh ?? "";
		En = en ?? "";
	}
}
