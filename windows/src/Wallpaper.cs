using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace DesktopDance;

public static class Wallpaper
{
	public static void Compose(LoadResult r)
	{
		ScreenInfo screen = r.Screen;
		Color color = Color.FromRgb(0, 0, 0);
		try
		{
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Control Panel\\Colors");
			string[] array = ((registryKey?.GetValue("Background") as string) ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (array.Length == 3 && byte.TryParse(array[0], out var result) && byte.TryParse(array[1], out var result2) && byte.TryParse(array[2], out var result3))
			{
				color = Color.FromRgb(result, result2, result3);
			}
			else
			{
				uint sysColor = Native.GetSysColor(1);
				color = Color.FromRgb((byte)(sysColor & 0xFF), (byte)((sysColor >> 8) & 0xFF), (byte)((sysColor >> 16) & 0xFF));
			}
		}
		catch
		{
		}
		r.CurtainColor = color;
		string text = "";
		int num = 10;
		bool flag = false;
		try
		{
			StringBuilder stringBuilder = new StringBuilder(1024);
			if (Native.SystemParametersInfo(115u, (uint)stringBuilder.Capacity, stringBuilder, 0u))
			{
				text = stringBuilder.ToString();
			}
		}
		catch
		{
		}
		try
		{
			using RegistryKey registryKey2 = Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop");
			if (registryKey2 != null)
			{
				if (string.IsNullOrEmpty(text) || !File.Exists(text))
				{
					text = (registryKey2.GetValue("WallPaper") as string) ?? text;
				}
				if (int.TryParse(registryKey2.GetValue("WallpaperStyle") as string, out var result4))
				{
					num = result4;
				}
				flag = registryKey2.GetValue("TileWallpaper") as string == "1";
			}
		}
		catch
		{
		}
		if (string.IsNullOrEmpty(text) || !File.Exists(text))
		{
			string text2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft\\Windows\\Themes\\TranscodedWallpaper");
			if (string.IsNullOrEmpty(text) && !File.Exists(text2))
			{
				r.Curtain = null;
				r.CurtainNote = "遮罩：纯色背景（系统没有设壁纸图片）";
				List<string> diag = r.Diag;
				Color color2 = color;
				diag.Add("壁纸：没有图片，背景色 " + color2.ToString());
				return;
			}
			if (File.Exists(text2))
			{
				text = text2;
			}
		}
		if (!File.Exists(text))
		{
			r.Curtain = null;
			r.CurtainNote = "遮罩：找不到壁纸文件，用纯色盖住真实图标";
			r.CurtainProblem = new Msg(r.CurtainNote, "No wallpaper file found; a plain color covers the real icons");
			r.Diag.Add("壁纸：文件不存在 " + text);
			return;
		}
		BitmapSource bitmapSource;
		using (FileStream bitmapStream = new FileStream(text, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
		{
			bitmapSource = BitmapDecoder.Create(bitmapStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0];
			bitmapSource.Freeze();
		}
		double num2 = bitmapSource.PixelWidth;
		double num3 = bitmapSource.PixelHeight;
		if (num2 < 1.0 || num3 < 1.0)
		{
			throw new InvalidDataException("壁纸尺寸为 0");
		}
		double num4 = screen.MonW;
		double num5 = screen.MonH;
		DrawingVisual drawingVisual = new DrawingVisual();
		RenderOptions.SetBitmapScalingMode(drawingVisual, BitmapScalingMode.HighQuality);
		string text3;
		using (DrawingContext drawingContext = drawingVisual.RenderOpen())
		{
			SolidColorBrush brush = new SolidColorBrush(color);
			Rect rect = new Rect(0.0, 0.0, num4, num5);
			drawingContext.DrawRectangle(brush, null, rect);
			drawingContext.PushClip(new RectangleGeometry(rect));
			if (flag)
			{
				text3 = "平铺";
				ImageBrush brush2 = new ImageBrush(bitmapSource)
				{
					TileMode = TileMode.Tile,
					Stretch = Stretch.Fill,
					ViewportUnits = BrushMappingMode.Absolute,
					Viewport = new Rect(screen.VirtX - screen.MonX, screen.VirtY - screen.MonY, num2, num3)
				};
				drawingContext.DrawRectangle(brush2, null, rect);
			}
			else
			{
				switch (num)
				{
				case 22:
				{
					text3 = "跨区";
					double num10 = Math.Max((double)screen.VirtW / num2, (double)screen.VirtH / num3);
					double num11 = num2 * num10;
					double num12 = num3 * num10;
					double x = ((double)screen.VirtW - num11) / 2.0 + (double)(screen.VirtX - screen.MonX);
					double y = ((double)screen.VirtH - num12) / 2.0 + (double)(screen.VirtY - screen.MonY);
					drawingContext.DrawImage(bitmapSource, new Rect(x, y, num11, num12));
					break;
				}
				case 2:
					text3 = "拉伸";
					drawingContext.DrawImage(bitmapSource, rect);
					break;
				case 0:
					text3 = "居中";
					drawingContext.DrawImage(bitmapSource, new Rect((num4 - num2) / 2.0, (num5 - num3) / 2.0, num2, num3));
					break;
				default:
				{
					bool num6 = num == 6;
					text3 = (num6 ? "适应" : "填充");
					double num7 = (num6 ? Math.Min(num4 / num2, num5 / num3) : Math.Max(num4 / num2, num5 / num3));
					double num8 = num2 * num7;
					double num9 = num3 * num7;
					drawingContext.DrawImage(bitmapSource, new Rect((num4 - num8) / 2.0, (num5 - num9) / 2.0, num8, num9));
					break;
				}
				}
			}
			drawingContext.Pop();
		}
		RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(screen.MonW, screen.MonH, 96.0, 96.0, PixelFormats.Pbgra32);
		renderTargetBitmap.Render(drawingVisual);
		renderTargetBitmap.Freeze();
		r.Curtain = renderTargetBitmap;
		r.CurtainNote = "遮罩：壁纸文件（" + text3 + "）";
		r.Diag.Add("壁纸：" + text + "，" + (int)num2 + "x" + (int)num3 + "，铺法 " + text3 + "（WallpaperStyle=" + num + "，Tile=" + (flag ? 1 : 0) + "）");
	}
}
