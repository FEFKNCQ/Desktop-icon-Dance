using System.Runtime.InteropServices;

namespace DesktopDance;

[ComImport]
[Guid("85CB6900-4D95-11CF-960C-0080C7F4EE85")]
[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
internal interface IShellWindows
{
	[return: MarshalAs(UnmanagedType.IDispatch)]
	object? FindWindowSW([MarshalAs(UnmanagedType.Struct)] ref object? pvarLoc, [MarshalAs(UnmanagedType.Struct)] ref object? pvarLocRoot, int swClass, out int phwnd, int swfwOptions);
}
