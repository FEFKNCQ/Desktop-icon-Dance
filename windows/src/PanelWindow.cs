using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DesktopDance;

public sealed class PanelWindow : Window
{
	public sealed class TestHandles
	{
		public Button Start;

		public Button Pause;

		public Button Restore;

		public ComboBox Mode;

		public ComboBox Ball;

		public ComboBox Language;

		public CheckBox Auto;

		public CheckBox FollowMusic;

		public CheckBox Names;

		public CheckBox Hide;

		public Slider Speed;

		public Slider Amp;

		public Slider Count;

		public TextBlock CountLabel;

		public TextBlock Status;

		public Settings Settings;
	}

	private readonly DanceController c;

	private readonly Settings settings;

	private readonly Button startBtn = new Button();

	private readonly Button pauseBtn = new Button();

	private readonly Button restoreBtn = new Button();

	private readonly ComboBox modeBox = new ComboBox();

	private readonly CheckBox autoChk = new CheckBox();

	private readonly ComboBox ballBox = new ComboBox();

	private readonly CheckBox sysChk = new CheckBox();

	private readonly Slider speedSlider = new Slider
	{
		Minimum = 0.5,
		Maximum = 2.0,
		Value = 1.0
	};

	private readonly Slider ampSlider = new Slider
	{
		Minimum = 0.3,
		Maximum = 2.0,
		Value = 1.0
	};

	private readonly Slider countSlider = new Slider
	{
		Minimum = 12.0,
		Maximum = 96.0,
		Value = 48.0,
		IsSnapToTickEnabled = true,
		TickFrequency = 1.0
	};

	private readonly TextBlock countLabel = new TextBlock
	{
		Text = "48",
		Width = 22.0,
		VerticalAlignment = VerticalAlignment.Center
	};

	private readonly CheckBox labelChk = new CheckBox();

	private readonly CheckBox hideChk = new CheckBox
	{
		FontSize = 11.0
	};

	private readonly ComboBox langBox = new ComboBox();

	private readonly TextBlock hint = new TextBlock
	{
		FontSize = 11.0,
		FontWeight = FontWeights.SemiBold,
		Margin = new Thickness(0.0, 0.0, 0.0, 8.0),
		TextWrapping = TextWrapping.Wrap
	};

	private readonly TextBlock status = new TextBlock
	{
		TextWrapping = TextWrapping.Wrap,
		FontSize = 11.0,
		Foreground = Brushes.DimGray
	};

	private readonly TextBlock sysText = new TextBlock
	{
		TextWrapping = TextWrapping.Wrap
	};

	private readonly TextBlock hideText = new TextBlock
	{
		TextWrapping = TextWrapping.Wrap
	};

	private readonly TextBlock capMode = Caption();

	private readonly TextBlock capBall = Caption();

	private readonly TextBlock capSpeed = Caption();

	private readonly TextBlock capAmp = Caption();

	private readonly TextBlock capCount = Caption();

	private readonly TextBlock capLang = Caption();

	private bool updating;

	private bool hideUnsupported;

	public PanelWindow(DanceController controller, Settings settings)
	{
		c = controller;
		this.settings = settings;
		base.Title = "Desktop-icon-Dance";
		base.Width = 340.0;
		base.SizeToContent = SizeToContent.Height;
		base.ResizeMode = ResizeMode.CanMinimize;
		base.Topmost = true;
		base.ShowInTaskbar = true;
		base.WindowStartupLocation = WindowStartupLocation.Manual;
		base.FontFamily = new FontFamily("Microsoft YaHei UI, Segoe UI");
		base.FontSize = 12.0;
		Build();
		ApplyLanguage();
		base.Loaded += delegate
		{
			PlaceBottomLeft();
			ApplyCaptureVisibility();
		};
		base.Closed += delegate
		{
			Log.Write("控制窗口被关闭，恢复桌面并退出");
			c.EmergencyRestore();
			Application.Current?.Shutdown();
		};
		c.Changed += Refresh;
		c.CatalogChanged += ReloadBallMenu;
		ReloadBallMenu();
		Refresh();
	}

	private static FrameworkElement Row(params UIElement[] children)
	{
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Margin = new Thickness(0.0, 0.0, 0.0, 9.0)
		};
		foreach (UIElement element in children)
		{
			stackPanel.Children.Add(element);
		}
		return stackPanel;
	}

	private static TextBlock Caption()
	{
		return new TextBlock
		{
			Width = 38.0,
			Foreground = Brushes.Gray,
			VerticalAlignment = VerticalAlignment.Center
		};
	}

	private static FrameworkElement SliderRow(TextBlock cap, Slider slider, params UIElement[] tail)
	{
		DockPanel dockPanel = new DockPanel
		{
			Margin = new Thickness(0.0, 0.0, 0.0, 9.0),
			LastChildFill = true
		};
		DockPanel.SetDock(cap, Dock.Left);
		dockPanel.Children.Add(cap);
		for (int num = tail.Length - 1; num >= 0; num--)
		{
			DockPanel.SetDock(tail[num], Dock.Right);
			dockPanel.Children.Add(tail[num]);
		}
		slider.VerticalAlignment = VerticalAlignment.Center;
		dockPanel.Children.Add(slider);
		return dockPanel;
	}

	private void Build()
	{
		Button[] array = new Button[3] { startBtn, pauseBtn, restoreBtn };
		foreach (Button obj in array)
		{
			obj.Padding = new Thickness(12.0, 4.0, 12.0, 4.0);
			obj.Margin = new Thickness(0.0, 0.0, 8.0, 0.0);
			obj.MinWidth = 76.0;
		}
		startBtn.IsDefault = true;
		startBtn.Click += delegate
		{
			c.Start();
		};
		pauseBtn.Click += delegate
		{
			c.TogglePause();
		};
		restoreBtn.Click += delegate
		{
			c.Stop(animated: true);
		};
		modeBox.Width = 110.0;
		modeBox.Margin = new Thickness(0.0, 0.0, 10.0, 0.0);
		modeBox.SelectionChanged += delegate
		{
			if (!updating)
			{
				int selectedIndex = modeBox.SelectedIndex;
				if (selectedIndex >= 0 && selectedIndex < DanceModes.All.Length)
				{
					c.Mode = DanceModes.All[selectedIndex];
				}
				Refresh();
			}
		};
		autoChk.VerticalAlignment = VerticalAlignment.Center;
		autoChk.Click += delegate
		{
			c.AutoSwitch = autoChk.IsChecked == true;
			Refresh();
		};
		ballBox.Width = 250.0;
		ballBox.SelectionChanged += delegate
		{
			if (!updating)
			{
				int selectedIndex = ballBox.SelectedIndex;
				c.BallName = ((selectedIndex <= 0) ? "" : ((ballBox.SelectedItem as string) ?? ""));
				settings.BallName = c.BallName;
				settings.Save();
			}
		};
		sysChk.Content = sysText;
		sysChk.Margin = new Thickness(0.0, 0.0, 0.0, 9.0);
		sysChk.Click += delegate
		{
			c.UseSystemAudio = sysChk.IsChecked == true;
			settings.FollowSystemAudio = c.UseSystemAudio;
			settings.Save();
			Refresh();
		};
		speedSlider.ValueChanged += delegate
		{
			if (!updating)
			{
				c.Speed = speedSlider.Value;
			}
		};
		ampSlider.ValueChanged += delegate
		{
			if (!updating)
			{
				c.Amp = ampSlider.Value;
			}
		};
		countSlider.ValueChanged += delegate
		{
			if (!updating)
			{
				int num = (int)Math.Round(countSlider.Value);
				if (num != c.TargetCount)
				{
					c.TargetCount = num;
					ReloadBallMenu();
				}
				countLabel.Text = num.ToString();
				Refresh();
			}
		};
		countLabel.Margin = new Thickness(6.0, 0.0, 8.0, 0.0);
		labelChk.VerticalAlignment = VerticalAlignment.Center;
		labelChk.Click += delegate
		{
			c.ShowLabels = labelChk.IsChecked == true;
			Refresh();
		};
		hideChk.Content = hideText;
		hideChk.Margin = new Thickness(0.0, 0.0, 0.0, 9.0);
		hideChk.IsChecked = settings.HidePanelFromCapture;
		hideChk.Click += delegate
		{
			settings.HidePanelFromCapture = hideChk.IsChecked == true;
			settings.Save();
			ApplyCaptureVisibility();
			Refresh();
		};
		langBox.Items.Add("中文");
		langBox.Items.Add("English");
		langBox.Width = 110.0;
		langBox.SelectionChanged += delegate
		{
			if (!updating)
			{
				SetLanguage((langBox.SelectedIndex == 1) ? Lang.En : Lang.Zh);
			}
		};
		StackPanel stackPanel = new StackPanel
		{
			Margin = new Thickness(16.0, 14.0, 16.0, 14.0)
		};
		stackPanel.Children.Add(Row(startBtn, pauseBtn, restoreBtn));
		stackPanel.Children.Add(Row(capMode, modeBox, autoChk));
		stackPanel.Children.Add(Row(capBall, ballBox));
		stackPanel.Children.Add(sysChk);
		stackPanel.Children.Add(SliderRow(capSpeed, speedSlider));
		stackPanel.Children.Add(SliderRow(capAmp, ampSlider));
		stackPanel.Children.Add(SliderRow(capCount, countSlider, countLabel, labelChk));
		stackPanel.Children.Add(hideChk);
		stackPanel.Children.Add(Row(capLang, langBox));
		stackPanel.Children.Add(hint);
		stackPanel.Children.Add(status);
		base.Content = stackPanel;
	}

	private void ApplyLanguage()
	{
		updating = true;
		try
		{
			bool flag = L.Current == Lang.Zh;
			base.Width = (flag ? 340 : 360);
			TextBlock[] array = new TextBlock[6] { capMode, capBall, capSpeed, capAmp, capCount, capLang };
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Width = (flag ? 38 : 62);
			}
			ballBox.Width = (flag ? 250 : 236);
			pauseBtn.Content = L.T("暂停", "Pause");
			restoreBtn.Content = L.T("恢复桌面", "Restore");
			autoChk.Content = L.T("自动切换", "Auto switch");
			labelChk.Content = L.T("图标名称", "Names");
			sysText.Text = L.T("跟着电脑正在播放的音乐跳", "Follow the music playing on this PC");
			hideText.Text = L.T("截屏 / 录屏时隐藏本窗口", "Hide this window from screenshots / recordings");
			hint.Text = L.T("Esc：立刻恢复桌面", "Press Esc to restore instantly");
			capMode.Text = L.T("模式", "Mode");
			capBall.Text = L.T("篮球", "Ball");
			capSpeed.Text = L.T("速度", "Speed");
			capAmp.Text = L.T("幅度", "Size");
			capCount.Text = L.T("数量", "Number");
			capLang.Text = L.T("语言", "Language");
			ballBox.ToolTip = L.T("「打篮球」模式里用哪个图标当篮球", "Which icon plays the ball in Basketball mode");
			sysChk.ToolTip = L.T("勾上：听电脑正在播的声音（网易云、抖音、浏览器都行）打拍子，不用麦克风。不勾或没声音：用内置 120 拍", "Ticked: follows the beat of whatever this PC is playing (music app, video, browser); no microphone is used. Unticked or silent: uses a built-in 120 BPM beat");
			hideChk.ToolTip = L.T("勾上后，本窗口不会出现在截图和录屏里（跳舞的图标不受影响）", "When ticked, this window is left out of screenshots and recordings (the dancing icons are not affected)");
			modeBox.Items.Clear();
			DanceMode[] all = DanceModes.All;
			foreach (DanceMode m in all)
			{
				modeBox.Items.Add(DanceModes.Title(m));
			}
			modeBox.SelectedIndex = Array.IndexOf(DanceModes.All, c.Mode);
			langBox.SelectedIndex = ((!flag) ? 1 : 0);
		}
		finally
		{
			updating = false;
		}
	}

	private void SetLanguage(Lang lang)
	{
		if (lang != L.Current)
		{
			L.Current = lang;
			settings.Language = L.Code(lang);
			settings.Save();
			Log.Write("界面语言改为：" + ((lang == Lang.Zh) ? "中文" : "English"));
			ApplyLanguage();
			ReloadBallMenu();
			Refresh();
			base.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(KeepOnScreen));
		}
	}

	public void SelfTestSwitchLanguage(Lang lang)
	{
		SetLanguage(lang);
	}

	public TestHandles SelfTestHandles()
	{
		return new TestHandles
		{
			Start = startBtn,
			Pause = pauseBtn,
			Restore = restoreBtn,
			Mode = modeBox,
			Ball = ballBox,
			Language = langBox,
			Auto = autoChk,
			FollowMusic = sysChk,
			Names = labelChk,
			Hide = hideChk,
			Speed = speedSlider,
			Amp = ampSlider,
			Count = countSlider,
			CountLabel = countLabel,
			Status = status,
			Settings = settings
		};
	}

	private void PlaceBottomLeft()
	{
		Rect workArea = SystemParameters.WorkArea;
		base.Left = workArea.Left + 16.0;
		base.Top = Math.Max(workArea.Top, workArea.Bottom - base.ActualHeight - 16.0);
	}

	private void KeepOnScreen()
	{
		try
		{
			if (base.WindowState == WindowState.Normal)
			{
				Rect workArea = SystemParameters.WorkArea;
				if (base.Top + base.ActualHeight > workArea.Bottom)
				{
					base.Top = Math.Max(workArea.Top, workArea.Bottom - base.ActualHeight - 16.0);
				}
				if (base.Left + base.ActualWidth > workArea.Right)
				{
					base.Left = Math.Max(workArea.Left, workArea.Right - base.ActualWidth - 16.0);
				}
			}
		}
		catch (Exception ex)
		{
			Log.Write("挪控制窗口出错：" + ex.Message);
		}
	}

	public void Refresh()
	{
		updating = true;
		try
		{
			DanceState state = c.State;
			startBtn.Content = ((state == DanceState.Paused) ? L.T("继续", "Resume") : L.T("开始蹦迪", "Start"));
			startBtn.IsEnabled = state == DanceState.Idle || state == DanceState.Paused;
			pauseBtn.IsEnabled = state == DanceState.Running;
			restoreBtn.IsEnabled = state != DanceState.Idle;
			modeBox.SelectedIndex = Array.IndexOf(DanceModes.All, c.Mode);
			autoChk.IsChecked = c.AutoSwitch;
			labelChk.IsChecked = c.ShowLabels;
			sysChk.IsChecked = c.UseSystemAudio;
			countLabel.Text = c.TargetCount.ToString();
			string text = c.StatusText;
			if (hideUnsupported)
			{
				text = text + "\n" + L.T("这台电脑的系统不支持「录屏时隐藏本窗口」（需要 Windows 10 2004 或更新）", "This version of Windows cannot hide this window from recordings (needs Windows 10 version 2004 or newer)");
			}
			status.Text = text;
		}
		finally
		{
			updating = false;
		}
	}

	private void ReloadBallMenu()
	{
		updating = true;
		try
		{
			ballBox.Items.Clear();
			ballBox.Items.Add(L.T("自动（第一个图标）", "Auto (first icon)"));
			List<string> list = c.CandidateNames();
			foreach (string item in list)
			{
				ballBox.Items.Add(item);
			}
			int selectedIndex = 0;
			string key = c.BallName;
			if (key.Length > 0)
			{
				int num = list.FindIndex((string n) => DesktopLoader.Matches(n, key));
				if (num >= 0)
				{
					selectedIndex = num + 1;
				}
				else
				{
					ballBox.Items.Add(key);
					selectedIndex = ballBox.Items.Count - 1;
				}
			}
			ballBox.SelectedIndex = selectedIndex;
		}
		finally
		{
			updating = false;
		}
	}

	private void ApplyCaptureVisibility()
	{
		try
		{
			nint handle = new WindowInteropHelper(this).Handle;
			if (handle != IntPtr.Zero)
			{
				bool hidePanelFromCapture = settings.HidePanelFromCapture;
				bool flag = Native.SetWindowDisplayAffinity(handle, hidePanelFromCapture ? 17u : 0u);
				hideUnsupported = hidePanelFromCapture && !flag;
				if (hidePanelFromCapture)
				{
					Log.Write("录屏时隐藏控制窗口：" + (flag ? "已开启" : "系统不支持"));
				}
			}
		}
		catch (Exception ex)
		{
			Log.Write("设置录屏隐藏出错：" + ex.Message);
		}
	}

	public BitmapSource SelfTestRender(Lang lang, List<string> notes)
	{
		L.Current = lang;
		ApplyLanguage();
		ReloadBallMenu();
		Refresh();
		StackPanel stackPanel = (StackPanel)base.Content;
		double num = base.Width - 16.0;
		double num2 = num - stackPanel.Margin.Left - stackPanel.Margin.Right;
		int num3 = 0;
		int num4 = 0;
		foreach (UIElement child in stackPanel.Children)
		{
			num3++;
			if (child is Panel)
			{
				child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				if (child.DesiredSize.Width > num2 + 0.5)
				{
					num4++;
					notes.Add("第 " + num3 + " 行需要 " + child.DesiredSize.Width.ToString("0") + " 宽，窗口里只有 " + num2.ToString("0"));
				}
			}
		}
		if (num4 == 0)
		{
			notes.Add("每一行都放得下（可用宽度 " + num2.ToString("0") + "）");
		}
		stackPanel.Background = Brushes.White;
		stackPanel.Measure(new Size(num, double.PositiveInfinity));
		double num5 = Math.Max(1.0, stackPanel.DesiredSize.Height);
		stackPanel.Arrange(new Rect(0.0, 0.0, num, num5));
		stackPanel.UpdateLayout();
		RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap((int)Math.Ceiling(num), (int)Math.Ceiling(num5), 96.0, 96.0, PixelFormats.Pbgra32);
		renderTargetBitmap.Render(stackPanel);
		renderTargetBitmap.Freeze();
		return renderTargetBitmap;
	}
}
