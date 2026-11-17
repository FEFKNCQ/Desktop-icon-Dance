using System;
using System.Runtime.InteropServices;

namespace DesktopDance;

[ComImport]
[Guid("cde725b0-ccc9-4519-917e-325d72fab4ce")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IFolderView
{
	[PreserveSig]
	int GetCurrentViewMode(out uint pViewMode);

	void _SetCurrentViewMode();

	[PreserveSig]
	int GetFolder(ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object? ppv);

	[PreserveSig]
	int Item(int iItemIndex, out nint ppidl);

	[PreserveSig]
	int ItemCount(uint uFlags, out int pcItems);

	[PreserveSig]
	int Items(uint uFlags, ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object? ppv);

	[PreserveSig]
	int GetSelectionMarkedItem(out int piItem);

	[PreserveSig]
	int GetFocusedItem(out int piItem);

	[PreserveSig]
	int GetItemPosition(nint pidl, out POINT ppt);

	[PreserveSig]
	int GetSpacing(out POINT ppt);

	[PreserveSig]
	int GetDefaultSpacing(out POINT ppt);

	[PreserveSig]
	int GetAutoArrange();

	void _SelectItem();

	void _SelectAndPositionItems();
}
