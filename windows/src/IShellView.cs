using System.Runtime.InteropServices;

namespace DesktopDance;

[ComImport]
[Guid("000214E3-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellView
{
	[PreserveSig]
	int GetWindow(out nint phwnd);
}
