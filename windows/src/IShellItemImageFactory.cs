using System.Runtime.InteropServices;

namespace DesktopDance;

[ComImport]
[Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellItemImageFactory
{
	[PreserveSig]
	int GetImage(SIZE size, int flags, out nint phbm);
}
