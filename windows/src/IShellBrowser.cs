using System.Runtime.InteropServices;

namespace DesktopDance;

[ComImport]
[Guid("000214E2-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellBrowser
{
	[PreserveSig]
	int GetWindow(out nint phwnd);

	void _ContextSensitiveHelp();

	void _InsertMenusSB();

	void _SetMenuSB();

	void _RemoveMenusSB();

	void _SetStatusTextSB();

	void _EnableModelessSB();

	void _TranslateAcceleratorSB();

	void _BrowseObject();

	void _GetViewStateStream();

	void _GetControlWindow();

	void _SendControlMsg();

	[PreserveSig]
	int QueryActiveShellView([MarshalAs(UnmanagedType.IUnknown)] out object? ppshv);
}
