using System.Runtime.InteropServices;

namespace DesktopDance;

[ComImport]
[Guid("70629033-e363-4a28-a567-0db78006e6d7")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IEnumShellItems
{
	[PreserveSig]
	int Next(uint celt, [MarshalAs(UnmanagedType.Interface)] out IShellItem? rgelt, out uint pceltFetched);

	[PreserveSig]
	int Skip(uint celt);

	[PreserveSig]
	int Reset();

	[PreserveSig]
	int Clone([MarshalAs(UnmanagedType.Interface)] out IEnumShellItems? ppenum);
}
