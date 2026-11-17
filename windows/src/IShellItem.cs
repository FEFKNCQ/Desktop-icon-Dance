using System;
using System.Runtime.InteropServices;

namespace DesktopDance;

[ComImport]
[Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellItem
{
	[PreserveSig]
	int BindToHandler(nint pbc, ref Guid bhid, ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object? ppv);

	[PreserveSig]
	int GetParent([MarshalAs(UnmanagedType.Interface)] out IShellItem? ppsi);

	[PreserveSig]
	int GetDisplayName(uint sigdnName, out nint ppszName);

	[PreserveSig]
	int GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);

	[PreserveSig]
	int Compare([MarshalAs(UnmanagedType.Interface)] IShellItem psi, uint hint, out int piOrder);
}
