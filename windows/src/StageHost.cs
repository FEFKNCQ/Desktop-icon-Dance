using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DesktopDance;

public sealed class StageHost : FrameworkElement
{
	private sealed class Node
	{
		public readonly ContainerVisual Root = new ContainerVisual();

		public readonly MatrixTransform Xf = new MatrixTransform();

		public DrawingVisual? Label;

		public double LastAlpha = -1.0;
	}

	private readonly VisualCollection visuals;

	private readonly DrawingVisual curtain = new DrawingVisual();

	private readonly ContainerVisual iconLayer = new ContainerVisual();

	private readonly DrawingVisual guide = new DrawingVisual();

	private readonly List<Node> nodes = new List<Node>();

	private readonly double stageW;

	private readonly double stageH;

	private int[] order = Array.Empty<int>();

	private double[] zs = Array.Empty<double>();

	public int Count => nodes.Count;

	protected override int VisualChildrenCount => visuals.Count;

	public StageHost(double width, double height, double iconDip, double labelWidth, double pixelsPerDip, IReadOnlyList<DancerItem> items)
	{
		stageW = width;
		stageH = height;
		base.Width = width;
		base.Height = height;
		visuals = new VisualCollection(this);
		visuals.Add(curtain);
		visuals.Add(iconLayer);
		visuals.Add(guide);
		Typeface typeface = new Typeface(SystemFonts.IconFontFamily ?? new FontFamily("Segoe UI"), SystemFonts.IconFontStyle, SystemFonts.IconFontWeight, FontStretches.Normal);
		double num = SystemFonts.IconFontSize;
		if (double.IsNaN(num) || num < 8.0 || num > 40.0)
		{
			num = 12.0;
		}
		SolidColorBrush solidColorBrush = new SolidColorBrush(Color.FromArgb(120, 0, 0, 0));
		solidColorBrush.Freeze();
		SolidColorBrush solidColorBrush2 = new SolidColorBrush(Color.FromArgb(200, 150, 150, 150));
		solidColorBrush2.Freeze();
		Vector[] array = new Vector[6]
		{
			new Vector(0.0, 1.0),
			new Vector(1.0, 1.0),
			new Vector(-1.0, 1.0),
			new Vector(1.0, 0.0),
			new Vector(-1.0, 0.0),
			new Vector(0.0, 2.0)
		};
		for (int i = 0; i < items.Count; i++)
		{
			DancerItem dancerItem = items[i];
			Node node = new Node();
			node.Root.Transform = node.Xf;
			DrawingVisual drawingVisual = new DrawingVisual();
			RenderOptions.SetBitmapScalingMode(drawingVisual, BitmapScalingMode.LowQuality);
			using (DrawingContext drawingContext = drawingVisual.RenderOpen())
			{
				BitmapSource icon = dancerItem.Icon;
				if (icon != null && icon.PixelWidth > 0 && icon.PixelHeight > 0)
				{
					double num2 = Math.Min(iconDip / (double)icon.PixelWidth, iconDip / (double)icon.PixelHeight);
					double num3 = (double)icon.PixelWidth * num2;
					double num4 = (double)icon.PixelHeight * num2;
					drawingContext.DrawImage(icon, new Rect((0.0 - num3) / 2.0, (0.0 - num4) / 2.0, num3, num4));
				}
				else
				{
					drawingContext.DrawRoundedRectangle(solidColorBrush2, null, new Rect((0.0 - iconDip) / 2.0, (0.0 - iconDip) / 2.0, iconDip, iconDip), iconDip * 0.18, iconDip * 0.18);
				}
			}
			node.Root.Children.Add(drawingVisual);
			if (!string.IsNullOrEmpty(dancerItem.Name))
			{
				try
				{
					FormattedText formattedText = new FormattedText(dancerItem.Name, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, num, Brushes.White, pixelsPerDip)
					{
						MaxTextWidth = Math.Max(40.0, labelWidth),
						MaxLineCount = 2,
						Trimming = TextTrimming.CharacterEllipsis,
						TextAlignment = TextAlignment.Center
					};
					DrawingVisual drawingVisual2 = new DrawingVisual();
					using (DrawingContext drawingContext2 = drawingVisual2.RenderOpen())
					{
						Point point = new Point((0.0 - formattedText.MaxTextWidth) / 2.0, iconDip / 2.0 + 3.0);
						formattedText.SetForegroundBrush(solidColorBrush);
						Vector[] array2 = array;
						foreach (Vector vector in array2)
						{
							drawingContext2.DrawText(formattedText, point + vector);
						}
						formattedText.SetForegroundBrush(Brushes.White);
						drawingContext2.DrawText(formattedText, point);
					}
					node.Root.Children.Add(drawingVisual2);
					node.Label = drawingVisual2;
				}
				catch (Exception ex)
				{
					Log.Write("画图标名字失败 " + dancerItem.Name + "：" + ex.Message);
				}
			}
			node.Root.Opacity = 0.0;
			nodes.Add(node);
		}
		order = new int[nodes.Count];
		zs = new double[nodes.Count];
		for (int k = 0; k < nodes.Count; k++)
		{
			order[k] = k;
			iconLayer.Children.Add(nodes[k].Root);
		}
	}

	protected override Visual GetVisualChild(int index)
	{
		return visuals[index];
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		return new Size(stageW, stageH);
	}

	public void SetCurtain(BitmapSource? image, Color background)
	{
		using DrawingContext drawingContext = curtain.RenderOpen();
		SolidColorBrush solidColorBrush = new SolidColorBrush(background);
		solidColorBrush.Freeze();
		drawingContext.DrawRectangle(solidColorBrush, null, new Rect(0.0, 0.0, stageW, stageH));
		if (image != null)
		{
			drawingContext.DrawImage(image, new Rect(0.0, 0.0, stageW, stageH));
		}
	}

	public void SetGuide(RectD? rect)
	{
		using DrawingContext drawingContext = guide.RenderOpen();
		if (rect.HasValue)
		{
			RectD value = rect.Value;
			Pen pen = new Pen(new SolidColorBrush(Color.FromArgb(140, byte.MaxValue, byte.MaxValue, byte.MaxValue)), 2.0);
			pen.Freeze();
			double item = stageH - value.MaxY;
			double item2 = stageH - value.MinY;
			(double, double, double, double)[] array = new(double, double, double, double)[4]
			{
				(value.MinX, item, 1.0, 1.0),
				(value.MaxX, item, -1.0, 1.0),
				(value.MinX, item2, 1.0, -1.0),
				(value.MaxX, item2, -1.0, -1.0)
			};
			for (int i = 0; i < array.Length; i++)
			{
				var (num, num2, num3, num4) = array[i];
				drawingContext.DrawLine(pen, new Point(num + num3 * 26.0, num2), new Point(num, num2));
				drawingContext.DrawLine(pen, new Point(num, num2), new Point(num, num2 + num4 * 26.0));
			}
		}
	}

	public void SetLabelsVisible(bool on, int hiddenIndex)
	{
		for (int i = 0; i < nodes.Count; i++)
		{
			DrawingVisual label = nodes[i].Label;
			if (label != null)
			{
				label.Opacity = ((on && i != hiddenIndex) ? 1 : 0);
			}
		}
	}

	public void Apply(Pose[] poses)
	{
		int num = Math.Min(nodes.Count, poses.Length);
		bool flag = false;
		for (int i = 0; i < num; i++)
		{
			Pose pose = poses[i];
			Node node = nodes[i];
			if (!double.IsNaN(pose.P.X) && !double.IsNaN(pose.P.Y) && !double.IsNaN(pose.Rot) && !double.IsNaN(pose.Sx) && !double.IsNaN(pose.Sy) && !double.IsInfinity(pose.P.X) && !double.IsInfinity(pose.P.Y))
			{
				Matrix identity = Matrix.Identity;
				identity.Scale(pose.Sx, pose.Sy);
				identity.Rotate((0.0 - pose.Rot) * 180.0 / Math.PI);
				identity.Translate(pose.P.X, stageH - pose.P.Y);
				node.Xf.Matrix = identity;
				double num2 = M.Clamp(pose.Alpha, 0.0, 1.0);
				if (num2 != node.LastAlpha)
				{
					node.Root.Opacity = num2;
					node.LastAlpha = num2;
				}
				if (zs[i] != pose.Z)
				{
					zs[i] = pose.Z;
					flag = true;
				}
			}
		}
		if (flag)
		{
			Reorder();
		}
	}

	private void Reorder()
	{
		bool flag = true;
		for (int i = 1; i < order.Length; i++)
		{
			if (zs[order[i - 1]] > zs[order[i]])
			{
				flag = false;
				break;
			}
		}
		if (!flag)
		{
			int[] array = new int[order.Length];
			for (int j = 0; j < array.Length; j++)
			{
				array[j] = j;
			}
			Array.Sort(array, delegate(int a, int b)
			{
				int num2 = zs[a].CompareTo(zs[b]);
				return (num2 == 0) ? a.CompareTo(b) : num2;
			});
			order = array;
			iconLayer.Children.Clear();
			int[] array2 = order;
			foreach (int index in array2)
			{
				iconLayer.Children.Add(nodes[index].Root);
			}
		}
	}

	public RenderTargetBitmap RenderToBitmap()
	{
		Measure(new Size(stageW, stageH));
		Arrange(new Rect(0.0, 0.0, stageW, stageH));
		RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(Math.Max(1, (int)Math.Ceiling(stageW)), Math.Max(1, (int)Math.Ceiling(stageH)), 96.0, 96.0, PixelFormats.Pbgra32);
		renderTargetBitmap.Render(this);
		return renderTargetBitmap;
	}
}
