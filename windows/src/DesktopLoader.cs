using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace DesktopDance;

public static class DesktopLoader
{
	private sealed class ViewItem
	{
		public string Name = "";

		public string Key = "";

		public nint Pidl;

		public POINT Pos;
	}

	private sealed class DesktopView
	{
		public List<ViewItem> Items = new List<ViewItem>();

		public POINT Spacing;

		public RECT ListRect;

		public bool HasList;

		public bool IconsVisible = true;

		public void FreePidls()
		{
			foreach (ViewItem item in Items)
			{
				if (item.Pidl != IntPtr.Zero)
				{
					Marshal.FreeCoTaskMem(item.Pidl);
					item.Pidl = IntPtr.Zero;
				}
			}
		}
	}

	private sealed class GuestEntry
	{
		public string Name = "";

		public IShellItem? Item;

		public string Path = "";
	}

	public const int MaxDesktopItems = 140;

	private static readonly string[] SkipWords = new string[20]
	{
		"uninstall", "卸载", "readme", "read me", "help", "帮助", "manual", "手册", "documentation", "文档",
		"license", "许可", "website", "网站", "release notes", "更新日志", "反馈", "feedback", "修复", "repair"
	};

	public static LoadResult Load(int target, string ballName)
	{
		LoadResult loadResult = new LoadResult();
		loadResult.Screen = ScreenInfo.Read();
		ScreenInfo screen = loadResult.Screen;
		double scale = screen.Scale;
		loadResult.Diag.Add(screen.ToString());
		int num = ReadDesktopIconSize();
		loadResult.IconDip = M.Clamp((num > 0) ? num : 48, 24.0, 128.0);
		int num2 = (int)Math.Round(loadResult.IconDip * scale);
		int num3 = Math.Min(256, Math.Max(32, (int)Math.Ceiling((double)num2 * 1.5)));
		loadResult.Diag.Add("桌面图标大小：注册表 " + ((num > 0) ? num.ToString() : "读不到，按 48") + "，屏幕上约 " + num2 + " 像素");
		loadResult.CellW = loadResult.IconDip + 28.0;
		loadResult.CellH = loadResult.IconDip + 52.0;
		HashSet<string> hashSet = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
		DesktopView desktopView = ReadDesktopView(loadResult.Diag);
		try
		{
			if (desktopView != null && desktopView.IconsVisible)
			{
				loadResult.PositionsFromShell = true;
				if (desktopView.Spacing.X > 8 && desktopView.Spacing.Y > 8)
				{
					loadResult.CellW = (double)desktopView.Spacing.X / scale;
					loadResult.CellH = (double)desktopView.Spacing.Y / scale;
				}
				int num4 = (desktopView.HasList ? desktopView.ListRect.Left : screen.VirtX);
				int num5 = (desktopView.HasList ? desktopView.ListRect.Top : screen.VirtY);
				loadResult.Diag.Add("桌面视图：" + desktopView.Items.Count + " 个图标，格子 " + desktopView.Spacing.X + "x" + desktopView.Spacing.Y + "，列表窗口原点 (" + num4 + "," + num5 + ")");
				foreach (ViewItem item in desktopView.Items)
				{
					if (item.Key.Length > 0)
					{
						loadResult.Snapshot[item.Key] = item.Pos;
					}
					double num6 = (double)(num4 + item.Pos.X) + (double)num2 / 2.0;
					double num7 = (double)(num5 + item.Pos.Y) + (double)num2 / 2.0;
					if (!(num6 >= (double)screen.MonX) || !(num6 < (double)(screen.MonX + screen.MonW)) || !(num7 >= (double)screen.MonY) || !(num7 < (double)(screen.MonY + screen.MonH)))
					{
						loadResult.OffScreenCount++;
					}
					else
					{
						if (loadResult.Items.Count >= 140)
						{
							continue;
						}
						DancerItem dancerItem = new DancerItem
						{
							Name = item.Name,
							Key = item.Key,
							IsGuest = false,
							HasPos = true,
							PxX = num6,
							PxY = num7
						};
						dancerItem.Home = new Vec((num6 - (double)screen.MonX) / scale, ((double)(screen.MonY + screen.MonH) - num7) / scale);
						IShellItem ppv = null;
						try
						{
							Guid riid = typeof(IShellItem).GUID;
							if (Native.SHCreateItemFromIDList(item.Pidl, ref riid, out ppv) == 0 && ppv != null)
							{
								dancerItem.Icon = LoadImage(ppv, num3, iconOnly: false);
							}
						}
						catch (Exception ex)
						{
							loadResult.Diag.Add("取图标失败 " + item.Name + "：" + ex.Message);
						}
						finally
						{
							Release(ppv);
						}
						loadResult.Items.Add(dancerItem);
						hashSet.Add(dancerItem.Name);
					}
				}
				if (loadResult.OffScreenCount > 0)
				{
					loadResult.Diag.Add("有 " + loadResult.OffScreenCount + " 个桌面图标在别的显示器上，不参加");
				}
			}
			else if (desktopView != null)
			{
				loadResult.Diag.Add("桌面图标被设成了不显示（右键桌面 → 查看 → 显示桌面图标 没勾），按空桌面处理");
			}
			else
			{
				loadResult.Diag.Add("问不到桌面视图，改为直接读桌面文件夹（没有真实位置）");
				foreach (string item2 in DesktopFiles())
				{
					if (loadResult.Items.Count >= 140)
					{
						break;
					}
					IShellItem shellItem = ItemFromPath(item2);
					if (shellItem == null)
					{
						continue;
					}
					try
					{
						DancerItem dancerItem2 = new DancerItem
						{
							Name = DisplayName(shellItem),
							Key = item2,
							IsGuest = false,
							HasPos = false
						};
						if (dancerItem2.Name.Length == 0)
						{
							dancerItem2.Name = Path.GetFileNameWithoutExtension(item2);
						}
						dancerItem2.Icon = LoadImage(shellItem, num3, iconOnly: false);
						loadResult.Items.Add(dancerItem2);
						hashSet.Add(dancerItem2.Name);
					}
					finally
					{
						Release(shellItem);
					}
				}
			}
		}
		finally
		{
			desktopView?.FreePidls();
		}
		loadResult.DesktopCount = loadResult.Items.Count;
		int num8 = Math.Max(0, target - loadResult.DesktopCount);
		string ballKey = (ballName ?? "").Trim();
		bool flag = ballKey.Length == 0 || loadResult.Items.Exists((DancerItem it) => Matches(it.Name, ballKey));
		if (num8 > 0 || !flag)
		{
			List<GuestEntry> list = ListGuests(loadResult.Diag);
			try
			{
				int num9 = 0;
				foreach (GuestEntry item3 in list)
				{
					if (num9 >= num8)
					{
						break;
					}
					if (!hashSet.Contains(item3.Name))
					{
						DancerItem dancerItem3 = MakeGuest(item3, num3);
						if (dancerItem3 != null)
						{
							loadResult.Items.Add(dancerItem3);
							hashSet.Add(dancerItem3.Name);
							num9++;
						}
					}
				}
				if (ballKey.Length > 0 && !loadResult.Items.Exists((DancerItem it) => Matches(it.Name, ballKey)))
				{
					GuestEntry guestEntry = list.Find((GuestEntry x) => Matches(x.Name, ballKey));
					DancerItem dancerItem4 = ((guestEntry != null) ? MakeGuest(guestEntry, num3) : null);
					if (dancerItem4 != null)
					{
						loadResult.Items.Add(dancerItem4);
					}
					else
					{
						loadResult.Diag.Add("没找到名字里带「" + ballKey + "」的图标，篮球改用第一个图标");
					}
				}
				loadResult.Diag.Add("可借用的应用图标 " + list.Count + " 个，用了 " + (loadResult.Items.Count - loadResult.DesktopCount) + " 个");
			}
			finally
			{
				foreach (GuestEntry item4 in list)
				{
					Release(item4.Item);
					item4.Item = null;
				}
			}
		}
		int count = loadResult.Items.FindAll((DancerItem it) => it.Icon == null).Count;
		if (count > 0)
		{
			loadResult.Diag.Add("有 " + count + " 个图标没取到图片，用灰色方块代替");
		}
		try
		{
			Wallpaper.Compose(loadResult);
		}
		catch (Exception ex2)
		{
			loadResult.Curtain = null;
			loadResult.CurtainNote = "壁纸处理失败，用纯色盖住真实图标";
			loadResult.CurtainProblem = new Msg(loadResult.CurtainNote, "Could not process the wallpaper; a plain color covers the real icons");
			loadResult.Diag.Add("壁纸处理失败：" + ex2.GetType().Name + " " + ex2.Message);
		}
		return loadResult;
	}

	public static NameCatalog ListNames()
	{
		NameCatalog nameCatalog = new NameCatalog();
		List<string> list = new List<string>();
		DesktopView desktopView = ReadDesktopView(list);
		try
		{
			if (desktopView != null && desktopView.IconsVisible)
			{
				foreach (ViewItem item in desktopView.Items)
				{
					if (item.Name.Length > 0)
					{
						nameCatalog.Desktop.Add(item.Name);
					}
				}
			}
			else if (desktopView == null)
			{
				foreach (string item2 in DesktopFiles())
				{
					IShellItem shellItem = ItemFromPath(item2);
					if (shellItem == null)
					{
						continue;
					}
					try
					{
						string text = DisplayName(shellItem);
						if (text.Length > 0)
						{
							nameCatalog.Desktop.Add(text);
						}
					}
					finally
					{
						Release(shellItem);
					}
				}
			}
		}
		finally
		{
			desktopView?.FreePidls();
		}
		List<GuestEntry> list2 = ListGuests(list);
		HashSet<string> hashSet = new HashSet<string>(nameCatalog.Desktop, StringComparer.CurrentCultureIgnoreCase);
		foreach (GuestEntry item3 in list2)
		{
			if (hashSet.Add(item3.Name))
			{
				nameCatalog.Guests.Add(item3.Name);
			}
			Release(item3.Item);
			item3.Item = null;
		}
		foreach (string item4 in list)
		{
			Log.Write("  " + item4);
		}
		return nameCatalog;
	}

	public static Dictionary<string, POINT>? ReadPositions()
	{
		DesktopView desktopView = ReadDesktopView(new List<string>());
		if (desktopView == null)
		{
			return null;
		}
		try
		{
			Dictionary<string, POINT> dictionary = new Dictionary<string, POINT>();
			foreach (ViewItem item in desktopView.Items)
			{
				if (item.Key.Length > 0)
				{
					dictionary[item.Key] = item.Pos;
				}
			}
			return dictionary;
		}
		finally
		{
			desktopView.FreePidls();
		}
	}

	public static bool Matches(string name, string key)
	{
		if (key.Length > 0)
		{
			return name.IndexOf(key, StringComparison.CurrentCultureIgnoreCase) >= 0;
		}
		return false;
	}

	private static DesktopView? ReadDesktopView(List<string> diag)
	{
		object obj = null;
		object obj2 = null;
		object ppvObject = null;
		object ppshv = null;
		try
		{
			Type typeFromCLSID = Type.GetTypeFromCLSID(ShellGuids.CLSID_ShellWindows, throwOnError: false);
			if (typeFromCLSID == null)
			{
				diag.Add("桌面视图：系统里没有 ShellWindows");
				return null;
			}
			obj = Activator.CreateInstance(typeFromCLSID);
			IShellWindows shellWindows = (IShellWindows)obj;
			object pvarLoc = 0;
			int phwnd;
			try
			{
				object pvarLocRoot = new object();
				obj2 = shellWindows.FindWindowSW(ref pvarLoc, ref pvarLocRoot, 8, out phwnd, 1);
			}
			catch (Exception ex)
			{
				diag.Add("桌面视图：第一次查找失败（" + ex.GetType().Name + " " + ex.Message + "），换一种参数再试");
				object pvarLocRoot2 = null;
				pvarLoc = 0;
				obj2 = shellWindows.FindWindowSW(ref pvarLoc, ref pvarLocRoot2, 8, out phwnd, 1);
			}
			if (obj2 == null)
			{
				diag.Add("桌面视图：没找到桌面窗口（资源管理器没在运行？）");
				return null;
			}
			IComServiceProvider obj3 = (IComServiceProvider)obj2;
			Guid guidService = ShellGuids.SID_STopLevelBrowser;
			Guid riid = typeof(IShellBrowser).GUID;
			int num = obj3.QueryService(ref guidService, ref riid, out ppvObject);
			if (num != 0 || ppvObject == null)
			{
				diag.Add("桌面视图：QueryService 失败 0x" + num.ToString("X8"));
				return null;
			}
			num = ((IShellBrowser)ppvObject).QueryActiveShellView(out ppshv);
			if (num != 0 || ppshv == null)
			{
				diag.Add("桌面视图：QueryActiveShellView 失败 0x" + num.ToString("X8"));
				return null;
			}
			DesktopView desktopView = new DesktopView();
			nint phwnd2 = IntPtr.Zero;
			try
			{
				((IShellView)ppshv).GetWindow(out phwnd2);
			}
			catch
			{
			}
			if (phwnd2 != IntPtr.Zero)
			{
				nint num2 = Native.FindWindowEx(phwnd2, IntPtr.Zero, "SysListView32", null);
				if (num2 != IntPtr.Zero)
				{
					desktopView.HasList = Native.GetWindowRect(num2, out desktopView.ListRect);
					desktopView.IconsVisible = Native.IsWindowVisible(num2);
				}
			}
			IFolderView folderView = (IFolderView)ppshv;
			num = folderView.ItemCount(2u, out var pcItems);
			if (num != 0)
			{
				diag.Add("桌面视图：ItemCount 失败 0x" + num.ToString("X8"));
				return null;
			}
			folderView.GetSpacing(out desktopView.Spacing);
			for (int i = 0; i < pcItems && i < 2000; i++)
			{
				if (folderView.Item(i, out var ppidl) != 0 || ppidl == IntPtr.Zero)
				{
					continue;
				}
				ViewItem viewItem = new ViewItem
				{
					Pidl = ppidl
				};
				if (folderView.GetItemPosition(ppidl, out viewItem.Pos) != 0)
				{
					Marshal.FreeCoTaskMem(ppidl);
					continue;
				}
				viewItem.Name = NameFromPidl(ppidl, 0u);
				viewItem.Key = NameFromPidl(ppidl, 2147647488u);
				if (viewItem.Key.Length == 0)
				{
					viewItem.Key = "名字:" + viewItem.Name;
				}
				desktopView.Items.Add(viewItem);
			}
			return desktopView;
		}
		catch (Exception ex2)
		{
			diag.Add("桌面视图：出错 " + ex2.GetType().Name + " " + ex2.Message);
			return null;
		}
		finally
		{
			Release(ppshv);
			Release(ppvObject);
			Release(obj2);
			Release(obj);
		}
	}

	private static string NameFromPidl(nint pidl, uint sigdn)
	{
		try
		{
			if (Native.SHGetNameFromIDList(pidl, sigdn, out var ppszName) != 0 || ppszName == IntPtr.Zero)
			{
				return "";
			}
			try
			{
				return Marshal.PtrToStringUni(ppszName) ?? "";
			}
			finally
			{
				Marshal.FreeCoTaskMem(ppszName);
			}
		}
		catch
		{
			return "";
		}
	}

	private static string DisplayName(IShellItem item)
	{
		try
		{
			if (item.GetDisplayName(0u, out var ppszName) != 0 || ppszName == IntPtr.Zero)
			{
				return "";
			}
			try
			{
				return Marshal.PtrToStringUni(ppszName) ?? "";
			}
			finally
			{
				Marshal.FreeCoTaskMem(ppszName);
			}
		}
		catch
		{
			return "";
		}
	}

	private static IShellItem? ItemFromPath(string path)
	{
		try
		{
			Guid riid = typeof(IShellItem).GUID;
			IShellItem ppv;
			return (Native.SHCreateItemFromParsingName(path, IntPtr.Zero, ref riid, out ppv) == 0) ? ppv : null;
		}
		catch
		{
			return null;
		}
	}

	private static void Release(object? o)
	{
		try
		{
			if (o != null && Marshal.IsComObject(o))
			{
				Marshal.ReleaseComObject(o);
			}
		}
		catch
		{
		}
	}

	private static List<string> DesktopFiles()
	{
		List<string> list = new List<string>();
		string[] array = new string[2]
		{
			Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
			Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)
		};
		foreach (string text in array)
		{
			try
			{
				if (string.IsNullOrEmpty(text) || !Directory.Exists(text))
				{
					continue;
				}
				foreach (string item in Directory.EnumerateFileSystemEntries(text))
				{
					try
					{
						if ((File.GetAttributes(item) & (FileAttributes.Hidden | FileAttributes.System)) == 0)
						{
							list.Add(item);
						}
					}
					catch
					{
					}
				}
			}
			catch
			{
			}
		}
		list.Sort(StringComparer.CurrentCultureIgnoreCase);
		return list;
	}

	private static int ReadDesktopIconSize()
	{
		try
		{
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\Shell\\Bags\\1\\Desktop");
			if (registryKey?.GetValue("IconSize") is int num && num >= 16 && num <= 256)
			{
				return num;
			}
		}
		catch
		{
		}
		return 0;
	}

	private static bool GoodGuestName(string name)
	{
		if (string.IsNullOrWhiteSpace(name) || name.Length > 60)
		{
			return false;
		}
		string text = name.ToLowerInvariant();
		string[] skipWords = SkipWords;
		foreach (string value in skipWords)
		{
			if (text.Contains(value))
			{
				return false;
			}
		}
		if (text.Contains("http") || text.EndsWith(".txt") || text.EndsWith(".chm") || text.EndsWith(".html") || text.EndsWith(".url"))
		{
			return false;
		}
		return true;
	}

	private static List<GuestEntry> ListGuests(List<string> diag)
	{
		List<GuestEntry> list = new List<GuestEntry>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
		IShellItem shellItem = null;
		object ppv = null;
		try
		{
			shellItem = ItemFromPath("shell:AppsFolder");
			if (shellItem != null)
			{
				Guid bhid = ShellGuids.BHID_EnumItems;
				Guid riid = typeof(IEnumShellItems).GUID;
				int num = shellItem.BindToHandler(IntPtr.Zero, ref bhid, ref riid, out ppv);
				if (num == 0 && ppv != null)
				{
					IEnumShellItems enumShellItems = (IEnumShellItems)ppv;
					for (int i = 0; i < 4000; i++)
					{
						if (enumShellItems.Next(1u, out IShellItem rgelt, out uint pceltFetched) != 0)
						{
							break;
						}
						if (pceltFetched == 0)
						{
							break;
						}
						if (rgelt == null)
						{
							break;
						}
						string text = DisplayName(rgelt);
						if (GoodGuestName(text) && hashSet.Add(text))
						{
							list.Add(new GuestEntry
							{
								Name = text,
								Item = rgelt
							});
						}
						else
						{
							Release(rgelt);
						}
					}
				}
				else
				{
					diag.Add("应用列表：枚举失败 0x" + num.ToString("X8"));
				}
			}
			else
			{
				diag.Add("应用列表：打不开 shell:AppsFolder");
			}
		}
		catch (Exception ex)
		{
			diag.Add("应用列表：出错 " + ex.GetType().Name + " " + ex.Message);
		}
		finally
		{
			Release(ppv);
			Release(shellItem);
		}
		if (list.Count < 8)
		{
			int count = list.Count;
			string[] obj = new string[2]
			{
				Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
				Environment.GetFolderPath(Environment.SpecialFolder.Programs)
			};
			EnumerationOptions enumerationOptions = new EnumerationOptions
			{
				RecurseSubdirectories = true,
				IgnoreInaccessible = true
			};
			string[] array = obj;
			foreach (string text2 in array)
			{
				try
				{
					if (string.IsNullOrEmpty(text2) || !Directory.Exists(text2))
					{
						continue;
					}
					foreach (string item in Directory.EnumerateFiles(text2, "*.lnk", enumerationOptions))
					{
						string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(item);
						if (GoodGuestName(fileNameWithoutExtension) && hashSet.Add(fileNameWithoutExtension))
						{
							list.Add(new GuestEntry
							{
								Name = fileNameWithoutExtension,
								Path = item
							});
						}
					}
				}
				catch
				{
				}
			}
			diag.Add("应用列表：从开始菜单快捷方式补了 " + (list.Count - count) + " 个");
		}
		list.Sort((GuestEntry a, GuestEntry b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
		return list;
	}

	private static DancerItem? MakeGuest(GuestEntry g, int bmpPx)
	{
		IShellItem shellItem = g.Item;
		bool flag = false;
		if (shellItem == null && g.Path.Length > 0)
		{
			shellItem = ItemFromPath(g.Path);
			flag = true;
		}
		if (shellItem == null)
		{
			return null;
		}
		try
		{
			BitmapSource bitmapSource = LoadImage(shellItem, bmpPx, iconOnly: true);
			if (bitmapSource == null)
			{
				return null;
			}
			return new DancerItem
			{
				Name = g.Name,
				Key = "guest:" + g.Name,
				IsGuest = true,
				HasPos = false,
				Icon = bitmapSource
			};
		}
		catch
		{
			return null;
		}
		finally
		{
			if (flag)
			{
				Release(shellItem);
			}
		}
	}

	private static BitmapSource? LoadImage(IShellItem item, int px, bool iconOnly)
	{
		IShellItemImageFactory shellItemImageFactory;
		try
		{
			shellItemImageFactory = (IShellItemImageFactory)item;
		}
		catch
		{
			return null;
		}
		SIZE size = new SIZE
		{
			CX = px,
			CY = px
		};
		nint phbm = IntPtr.Zero;
		int num = -1;
		if (!iconOnly)
		{
			try
			{
				num = shellItemImageFactory.GetImage(size, 0, out phbm);
			}
			catch
			{
				num = -1;
				phbm = IntPtr.Zero;
			}
		}
		if (num != 0 || phbm == IntPtr.Zero)
		{
			try
			{
				num = shellItemImageFactory.GetImage(size, 4, out phbm);
			}
			catch
			{
				num = -1;
				phbm = IntPtr.Zero;
			}
		}
		if (num != 0 || phbm == IntPtr.Zero)
		{
			return null;
		}
		try
		{
			return FromHBitmap(phbm);
		}
		finally
		{
			Native.DeleteObject(phbm);
		}
	}

	private static BitmapSource? FromHBitmap(nint hbm)
	{
		if (Native.GetObject(hbm, Marshal.SizeOf<BITMAP>(), out var pv) == 0)
		{
			return null;
		}
		int bmWidth = pv.bmWidth;
		int num = Math.Abs(pv.bmHeight);
		if (bmWidth <= 0 || num <= 0 || bmWidth > 4096 || num > 4096)
		{
			return null;
		}
		BITMAPINFO lpbmi = new BITMAPINFO
		{
			biSize = 40u,
			biWidth = bmWidth,
			biHeight = -num,
			biPlanes = 1,
			biBitCount = 32,
			biCompression = 0u
		};
		byte[] array = new byte[bmWidth * num * 4];
		nint dC = Native.GetDC(IntPtr.Zero);
		int dIBits;
		try
		{
			dIBits = Native.GetDIBits(dC, hbm, 0u, (uint)num, array, ref lpbmi, 0u);
		}
		finally
		{
			Native.ReleaseDC(IntPtr.Zero, dC);
		}
		if (dIBits == 0)
		{
			return null;
		}
		bool flag = false;
		bool flag2 = false;
		for (int i = 0; i < array.Length; i += 4)
		{
			byte b = array[i + 3];
			if (b != 0)
			{
				flag = true;
			}
			if (array[i] > b + 1 || array[i + 1] > b + 1 || array[i + 2] > b + 1)
			{
				flag2 = true;
			}
		}
		PixelFormat pixelFormat;
		if (!flag)
		{
			for (int j = 3; j < array.Length; j += 4)
			{
				array[j] = byte.MaxValue;
			}
			pixelFormat = PixelFormats.Bgr32;
		}
		else
		{
			pixelFormat = (flag2 ? PixelFormats.Bgra32 : PixelFormats.Pbgra32);
		}
		BitmapSource bitmapSource = BitmapSource.Create(bmWidth, num, 96.0, 96.0, pixelFormat, null, array, bmWidth * 4);
		bitmapSource.Freeze();
		return bitmapSource;
	}
}
