using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DesktopDance;

public sealed class LiveTest
{
	private sealed class Report
	{
		private readonly string path;

		private readonly object gate = new object();

		public bool Failed { get; private set; }

		public Report(string path)
		{
			this.path = path;
			try
			{
				File.WriteAllText(path, "", new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
			}
			catch
			{
			}
		}

		public void Line(string s)
		{
			lock (gate)
			{
				try
				{
					File.AppendAllText(path, s + Environment.NewLine, Encoding.UTF8);
				}
				catch
				{
				}
			}
			Log.Write("[实跑测试] " + s);
		}

		public void Check(bool ok, string what)
		{
			if (!ok)
			{
				Failed = true;
			}
			Line((ok ? "[通过] " : "[失败] ") + what);
		}

		public void Fail(string what)
		{
			Failed = true;
			Line("[失败] " + what);
		}

		public void Soft(bool ok, string what)
		{
			Line((ok ? "[记录·是] " : "[记录·否] ") + what);
		}

		public void Note(string what)
		{
			Line("    " + what);
		}
	}

	private sealed class Probe
	{
		public POINT Pt;

		public nint Root;

		public string Class = "";

		public bool IsDesktop
		{
			get
			{
				if (!(Class == "Progman"))
				{
					return Class == "WorkerW";
				}
				return true;
			}
		}
	}

	private static class TestNative
	{
		[DllImport("user32.dll")]
		public static extern int GetSystemMetrics(int nIndex);

		[DllImport("user32.dll")]
		public static extern nint WindowFromPoint(POINT pt);

		[DllImport("user32.dll")]
		public static extern nint GetAncestor(nint hwnd, uint gaFlags);

		[DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetClassNameW")]
		public static extern int GetClassName(nint hWnd, StringBuilder lpClassName, int nMaxCount);

		[DllImport("user32.dll")]
		public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, nuint dwExtraInfo);

		[DllImport("user32.dll")]
		public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, nuint dwExtraInfo);

		[DllImport("user32.dll")]
		public static extern bool SetCursorPos(int x, int y);

		[DllImport("user32.dll")]
		public static extern bool GetCursorPos(out POINT lpPoint);

		[DllImport("dwmapi.dll")]
		public static extern int DwmGetWindowAttribute(nint hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

		[DllImport("gdi32.dll")]
		public static extern nint CreateCompatibleDC(nint hdc);

		[DllImport("gdi32.dll")]
		public static extern nint CreateCompatibleBitmap(nint hdc, int cx, int cy);

		[DllImport("gdi32.dll")]
		public static extern nint SelectObject(nint hdc, nint h);

		[DllImport("gdi32.dll")]
		public static extern bool BitBlt(nint hdc, int x, int y, int cx, int cy, nint hdcSrc, int x1, int y1, uint rop);

		[DllImport("gdi32.dll")]
		public static extern bool DeleteDC(nint hdc);
	}

	private const string StageTitle = "DesktopDance Stage";

	private readonly Application app;

	private readonly DanceController c;

	private readonly PanelWindow panel;

	private readonly string outDir;

	private readonly bool hold;

	private readonly Report rep;

	private PanelWindow.TestHandles ui;

	private RECT workArea;

	private int realClicks;

	private int fallbackClicks;

	public int ExitCode { get; private set; } = 2;

	private LiveTest(Application app, DanceController c, PanelWindow panel, string outDir, bool hold)
	{
		this.app = app;
		this.c = c;
		this.panel = panel;
		this.outDir = outDir;
		this.hold = hold;
		rep = new Report(Path.Combine(outDir, hold ? "livetest-hold.txt" : "livetest.txt"));
	}

	public static LiveTest Attach(Application app, DanceController c, PanelWindow panel, string outDir, bool hold)
	{
		try
		{
			Directory.CreateDirectory(outDir);
		}
		catch
		{
		}
		LiveTest t = new LiveTest(app, c, panel, outDir, hold);
		Thread thread = new Thread((ThreadStart)delegate
		{
			Thread.Sleep(hold ? 200000 : 270000);
			t.rep.Fail("超时：测试脚本没有在规定时间内跑完，直接结束进程");
			CopyLog(outDir);
			Environment.Exit(2);
		});
		thread.IsBackground = true;
		thread.Name = "livetest-watchdog";
		thread.Start();
		app.Dispatcher.BeginInvoke((Action)async delegate
		{
			_ = 1;
			try
			{
				if (!hold)
				{
					await t.RunFull();
				}
				else
				{
					await t.RunHold();
				}
			}
			catch (Exception ex)
			{
				t.rep.Fail("测试脚本抛出异常：" + ex);
				try
				{
					c.EmergencyRestore();
				}
				catch
				{
				}
				t.ExitCode = 1;
			}
			CopyLog(outDir);
			app.Shutdown();
		});
		return t;
	}

	private async Task RunFull()
	{
		rep.Line("桌面图标跳舞 Windows 版 · 实跑测试 · " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
		rep.Line("系统：" + Environment.OSVersion.VersionString + "，界面语言：" + ((L.Current == Lang.Zh) ? "中文" : "English"));
		RECT work = WorkArea();
		int systemMetrics = TestNative.GetSystemMetrics(0);
		int systemMetrics2 = TestNative.GetSystemMetrics(1);
		rep.Line("主屏 " + systemMetrics + "x" + systemMetrics2 + "，工作区 " + (work.Right - work.Left) + "x" + (work.Bottom - work.Top));
		rep.Line("");
		await WaitUntil(() => c.CatalogLoaded, 40.0);
		rep.Note("桌面上有 " + c.Catalog.Desktop.Count + " 个图标（控制窗口里的数）");
		await Task.Delay(800);
		await PanelShots();
		RECT panelRect = PanelRect();
		Dictionary<string, POINT> before = await ReadPositions();
		if (before == null)
		{
			rep.Note("读不到桌面图标的位置：后面「位置没变」这一项没法核对");
		}
		else
		{
			rep.Note("开始前读到 " + before.Count + " 个桌面图标的位置");
		}
		BitmapSource shotBefore = Shot("1-before.png");
		List<Probe> probesBefore = ProbePoints(work, panelRect);
		int count = probesBefore.FindAll((Probe p) => p.IsDesktop).Count;
		rep.Note("取了 " + probesBefore.Count + " 个探测点，其中 " + count + " 个点下面是桌面，其余是别的窗口：" + Summary(probesBefore));
		rep.Line("");
		rep.Line("== 第一轮：逐行操作控制窗口（按钮和勾选项用鼠标真的点；下拉和滑块直接选 / 拖到某个值），最后按 Esc ==");
		ui = panel.SelfTestHandles();
		workArea = work;
		rep.Check(ui.Start.IsEnabled && !ui.Pause.IsEnabled && !ui.Restore.IsEnabled, "就绪时：「开始」能点，「暂停」「恢复桌面」是灰的");
		rep.Check(ui.Status.Text.Contains(L.T("就绪", "Ready")), "就绪时：状态文字写着「就绪」");
		rep.Check(ui.CountLabel.Text == c.TargetCount.ToString() && (int)Math.Round(ui.Count.Value) == c.TargetCount, "就绪时：「数量」滑块、旁边的数字和后台的数量一致（" + c.TargetCount + "）");
		bool flag = await Press(ui.Start, null, () => c.State != DanceState.Idle);
		if (flag)
		{
			flag = await WaitUntil(() => c.State == DanceState.Running, 45.0);
		}
		bool flag2 = flag;
		rep.Check(flag2, "点「开始」后进入跳舞状态");
		if (!flag2)
		{
			rep.Note("状态文字：" + OneLine(c.StatusText));
			c.EmergencyRestore();
			ExitCode = 1;
			return;
		}
		await Task.Delay(3500);
		nint num = Native.FindWindow(null, "DesktopDance Stage");
		rep.Check(num != IntPtr.Zero && Native.IsWindowVisible(num), "舞台窗口已经打开");
		rep.Note("舞台窗口层级（程序自己的检查）：" + c.StageLayerNote);
		rep.Note("帧率 " + c.Fps + " fps，Esc 热键" + (c.HotKeys.EscapeRegistered ? "已注册" : "没注册上") + "，" + c.AudioDebug);
		rep.Note("状态文字：" + OneLine(c.StatusText));
		rep.Check(!ui.Start.IsEnabled && ui.Pause.IsEnabled && ui.Restore.IsEnabled, "跳舞时：「开始」变灰，「暂停」「恢复桌面」能点");
		rep.Check(ui.Status.Text.Contains(L.T("跳舞中", "Dancing")), "跳舞时：状态文字写着「跳舞中」");
		rep.Check(c.DancerCount == c.TargetCount, "跳舞时：场上跳舞的图标数（" + c.DancerCount + "）等于「数量」（" + c.TargetCount + "）；桌面图标 " + c.DesktopCount + " 个，借来 " + c.GuestCount + " 个");
		LayerCheck(probesBefore, num, "打篮球");
		BitmapSource bitmapSource = Shot("2-basketball.png");
		if (shotBefore != null && bitmapSource != null)
		{
			double num2 = Diff(shotBefore, bitmapSource, work, panelRect);
			rep.Soft(num2 > 0.002, "跳舞时的画面和开始前不一样（工作区里 " + (num2 * 100.0).ToString("0.00") + "% 的像素变了；一点没变说明舞台没露出来）");
		}
		await RowMode();
		await RowAutoSwitch();
		await RowBall();
		await RowFollowMusicSpeedAmp();
		await RowCount();
		await RowLanguage();
		await RowPauseNamesHide();
		rep.Soft(fallbackClicks == 0, "鼠标点击：" + realClicks + " 次真的点中并生效" + ((fallbackClicks > 0) ? ("，另有 " + fallbackClicks + " 次没点中、改成直接触发那个控件的点击事件") : ""));
		TestNative.keybd_event(27, 0, 0u, UIntPtr.Zero);
		TestNative.keybd_event(27, 0, 2u, UIntPtr.Zero);
		bool flag3 = await WaitUntil(() => c.State == DanceState.Idle, 4.0);
		rep.Soft(flag3, "模拟按下 Esc 后停止并恢复（走的是全局热键；测试机不接受模拟按键时这一项会不成立）");
		if (!flag3)
		{
			c.EmergencyRestore();
		}
		await AfterRestore(before, "按 Esc 后");
		await Task.Delay(700);
		BitmapSource bitmapSource2 = Shot("11-after-esc.png");
		if (shotBefore != null && bitmapSource2 != null)
		{
			double num3 = Diff(shotBefore, bitmapSource2, work, panelRect, PanelRect());
			rep.Soft(num3 < 0.01, "恢复后的画面和开始前一样（工作区里只有 " + (num3 * 100.0).ToString("0.00") + "% 的像素不同，控制窗口那一块不算）");
		}
		rep.Line("");
		rep.Line("== 第二轮：用「恢复桌面」停止 ==");
		flag = await Press(ui.Start, null, () => c.State != DanceState.Idle);
		if (flag)
		{
			flag = await WaitUntil(() => c.State == DanceState.Running, 45.0);
		}
		flag2 = flag;
		rep.Check(flag2, "第二次点「开始」后进入跳舞状态");
		await Task.Delay(2500);
		await Press(ui.Restore, null, () => c.State == DanceState.Stopping || c.State == DanceState.Idle);
		bool flag4 = await WaitUntil(() => c.State == DanceState.Idle, 8.0);
		rep.Check(flag4, "点「恢复桌面」后图标飞回原位，回到就绪状态");
		rep.Check(ui.Start.IsEnabled && !ui.Pause.IsEnabled && !ui.Restore.IsEnabled && Txt(ui.Start.Content) == L.T("开始蹦迪", "Start"), "恢复后：按钮回到就绪时的样子");
		if (!flag4)
		{
			c.EmergencyRestore();
		}
		await AfterRestore(before, "「恢复桌面」后");
		rep.Line("");
		rep.Line("== 第三轮：在桌面上点一下停止 ==");
		flag = await Press(ui.Start, null, () => c.State != DanceState.Idle);
		if (flag)
		{
			flag = await WaitUntil(() => c.State == DanceState.Running, 45.0);
		}
		flag2 = flag;
		rep.Check(flag2, "第三次开始后进入跳舞状态");
		await Task.Delay(2500);
		num = Native.FindWindow(null, "DesktopDance Stage");
		Probe target = null;
		foreach (Probe item in ProbePoints(work, panelRect))
		{
			if (item.Root == num && num != IntPtr.Zero)
			{
				target = item;
				break;
			}
		}
		if (target == null)
		{
			rep.Soft(ok: false, "找不到一个最上面就是舞台的点，没法试「点桌面停止」");
		}
		else
		{
			TestNative.GetCursorPos(out var old);
			TestNative.SetCursorPos(target.Pt.X, target.Pt.Y);
			await Task.Delay(120);
			TestNative.mouse_event(2u, 0u, 0u, 0u, UIntPtr.Zero);
			TestNative.mouse_event(4u, 0u, 0u, 0u, UIntPtr.Zero);
			bool ok = await WaitUntil(() => c.State == DanceState.Idle, 8.0);
			TestNative.SetCursorPos(old.X, old.Y);
			rep.Soft(ok, "在舞台上 (" + target.Pt.X + "," + target.Pt.Y + ") 模拟点一下后停止并恢复");
		}
		if (c.State != DanceState.Idle)
		{
			c.Stop(animated: false);
		}
		await WaitUntil(() => c.State == DanceState.Idle, 5.0);
		await AfterRestore(before, "点桌面后");
		await Task.Delay(700);
		BitmapSource bitmapSource3 = Shot("12-after.png");
		if (shotBefore != null && bitmapSource3 != null)
		{
			double num4 = Diff(shotBefore, bitmapSource3, work, panelRect, PanelRect());
			rep.Soft(num4 < 0.01, "全部结束后的画面和开始前一样（工作区里只有 " + (num4 * 100.0).ToString("0.00") + "% 的像素不同）");
		}
		rep.Line("");
		rep.Line(rep.Failed ? "结论：有失败项。" : "结论：安全相关的项全部通过（能开始、能恢复、恢复后舞台窗口没了、图标位置没变）。带「记录」的项见上，不算通过或失败。");
		ExitCode = (rep.Failed ? 1 : 0);
	}

	private static string Txt(object? content)
	{
		return (content as string) ?? (content as TextBlock)?.Text ?? "";
	}

	private async Task MouseClick(FrameworkElement el)
	{
		Point point = ((el is ToggleButton) ? new Point(8.0, el.ActualHeight / 2.0) : new Point(el.ActualWidth / 2.0, el.ActualHeight / 2.0));
		Point point2 = el.PointToScreen(point);
		TestNative.SetCursorPos((int)Math.Round(point2.X), (int)Math.Round(point2.Y));
		await Task.Delay(150);
		TestNative.mouse_event(2u, 0u, 0u, 0u, UIntPtr.Zero);
		await Task.Delay(40);
		TestNative.mouse_event(4u, 0u, 0u, 0u, UIntPtr.Zero);
		await Task.Delay(120);
	}

	private async Task<bool> Press(ButtonBase b, bool? wantChecked, Func<bool> took, double wait = 2.0)
	{
		for (int attempt = 0; attempt < 2; attempt++)
		{
			if (!b.IsEnabled)
			{
				break;
			}
			await MouseClick(b);
			if (await WaitUntil(took, wait))
			{
				realClicks++;
				return true;
			}
		}
		fallbackClicks++;
		if (b is ToggleButton toggleButton && wantChecked.HasValue)
		{
			toggleButton.IsChecked = wantChecked;
		}
		b.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
		return await WaitUntil(took, wait);
	}

	private async Task RowMode()
	{
		rep.Line("-- 模式 --");
		int shotNo = 3;
		for (int i = 0; i < DanceModes.All.Length; i++)
		{
			DanceMode m = DanceModes.All[i];
			if (m != DanceMode.Figure)
			{
				ui.Mode.SelectedIndex = i;
				await Task.Delay(2600);
				rep.Check(c.Mode == m && c.State == DanceState.Running, "「模式」选「" + DanceModes.LogName(m) + "」：后台换成了这个模式，还在跳（" + c.Fps + " fps）");
				Shot(shotNo + "-" + m.ToString().ToLowerInvariant() + ".png");
				shotNo++;
			}
		}
		ui.Mode.SelectedIndex = Array.IndexOf(DanceModes.All, DanceMode.Figure);
		await Task.Delay(1200);
		rep.Check(c.Mode == DanceMode.Figure, "「模式」选回「打篮球」：后台换回来了");
	}

	private async Task RowAutoSwitch()
	{
		rep.Line("-- 自动切换 --");
		ui.Mode.SelectedIndex = Array.IndexOf(DanceModes.All, DanceMode.Groove);
		await Task.Delay(500);
		bool ok = await Press(ui.Auto, true, () => c.AutoSwitch);
		rep.Check(ok, "勾上「自动切换」：后台开了自动切换");
		double t0 = Clock.Now;
		bool ok2 = await WaitUntil(() => c.Mode != DanceMode.Groove, 20.0);
		rep.Check(ok2, "勾上之后不用管它，" + (Clock.Now - t0).ToString("0") + " 秒后自己从「律动」换到了「" + DanceModes.LogName(c.Mode) + "」");
		await Task.Delay(400);
		rep.Check(ui.Mode.SelectedIndex == Array.IndexOf(DanceModes.All, c.Mode), "自动换了模式后：「模式」下拉显示的也跟着变了");
		bool ok3 = await Press(ui.Auto, false, () => !c.AutoSwitch);
		rep.Check(ok3, "去掉「自动切换」的勾：后台关了");
		ui.Mode.SelectedIndex = Array.IndexOf(DanceModes.All, DanceMode.Figure);
		await Task.Delay(1200);
	}

	private async Task RowBall()
	{
		rep.Line("-- 篮球 --");
		if (ui.Ball.Items.Count < 3)
		{
			rep.Note("「篮球」下拉里可选的图标不到两个，这一行没法试");
			return;
		}
		string was = c.BallInUse;
		ui.Ball.SelectedIndex = 2;
		string want = (ui.Ball.SelectedItem as string) ?? "";
		bool ok = await WaitUntil(() => c.BallName == want && DesktopLoader.Matches(c.BallInUse, want), 8.0);
		rep.Check(ok, "「篮球」选「" + want + "」：场上当球的图标换成了「" + c.BallInUse + "」（原来是「" + was + "」）");
		ui.Ball.SelectedIndex = 0;
		ok = await WaitUntil(() => c.BallName.Length == 0, 3.0);
		await Task.Delay(600);
		rep.Check(ok, "「篮球」选回「自动」：后台改回自动，现在当球的是「" + c.BallInUse + "」");
	}

	private async Task<double> BeatRate(int ms)
	{
		double b0 = c.BeatNow;
		double t0 = Clock.Now;
		await Task.Delay(ms);
		return (c.BeatNow - b0) / Math.Max(0.001, Clock.Now - t0);
	}

	private async Task<double> Motion(double seconds)
	{
		int n = c.DancerCount;
		if (n <= 0)
		{
			return 0.0;
		}
		Vec[] lo = new Vec[n];
		Vec[] hi = new Vec[n];
		for (int i = 0; i < n; i++)
		{
			lo[i] = new Vec(1000000000.0, 1000000000.0);
			hi[i] = new Vec(-1000000000.0, -1000000000.0);
		}
		double end = Clock.Now + seconds;
		while (Clock.Now < end)
		{
			Pose[] currentPoses = c.CurrentPoses;
			for (int j = 0; j < n && j < currentPoses.Length; j++)
			{
				Vec p = currentPoses[j].P;
				lo[j] = new Vec(Math.Min(lo[j].X, p.X), Math.Min(lo[j].Y, p.Y));
				hi[j] = new Vec(Math.Max(hi[j].X, p.X), Math.Max(hi[j].Y, p.Y));
			}
			await Task.Delay(30);
		}
		double num = 0.0;
		int num2 = 0;
		for (int k = 0; k < n; k++)
		{
			if (!(hi[k].X < lo[k].X))
			{
				num += hi[k].X - lo[k].X + (hi[k].Y - lo[k].Y);
				num2++;
			}
		}
		return (num2 == 0) ? 0.0 : (num / (double)num2);
	}

	private async Task RowFollowMusicSpeedAmp()
	{
		rep.Line("-- 跟着电脑正在播放的音乐跳 --");
		bool was = c.UseSystemAudio;
		bool ok = await Press(ui.FollowMusic, !was, () => c.UseSystemAudio == !was);
		rep.Check(ok, (was ? "去掉" : "勾上") + "「跟着音乐跳」：后台" + (was ? "不再听系统声音" : "开始听系统声音"));
		if (c.UseSystemAudio)
		{
			await Press(ui.FollowMusic, false, () => !c.UseSystemAudio);
		}
		await Task.Delay(900);
		rep.Check(ui.Status.Text.Contains(L.T("内置节拍", "Built-in beat")), "不跟音乐时：状态文字写着「内置节拍」");
		rep.Line("-- 速度 --");
		ui.Speed.Value = 1.0;
		await Task.Delay(400);
		double r1 = await BeatRate(2500);
		ui.Speed.Value = 2.0;
		await Task.Delay(400);
		double num = await BeatRate(2500);
		rep.Check(Math.Abs(c.Speed - 2.0) < 1E-06 && r1 > 0.5 && num / r1 > 1.6 && num / r1 < 2.4, "「速度」拖到最右（2 倍）：节拍真的快了一倍（每秒 " + r1.ToString("0.00") + " 拍 → " + num.ToString("0.00") + " 拍）");
		ui.Speed.Value = 0.5;
		await Task.Delay(400);
		double num2 = await BeatRate(2500);
		rep.Check(Math.Abs(c.Speed - 0.5) < 1E-06 && r1 > 0.5 && num2 / r1 > 0.4 && num2 / r1 < 0.62, "「速度」拖到最左（0.5 倍）：节拍真的慢了一半（每秒 " + num2.ToString("0.00") + " 拍）");
		ui.Speed.Value = 1.0;
		rep.Line("-- 幅度 --");
		ui.Mode.SelectedIndex = Array.IndexOf(DanceModes.All, DanceMode.Groove);
		await Task.Delay(1800);
		ui.Amp.Value = 0.3;
		await Task.Delay(500);
		double m1 = await Motion(2.5);
		ui.Amp.Value = 2.0;
		await Task.Delay(500);
		double num3 = await Motion(2.5);
		rep.Check(Math.Abs(c.Amp - 2.0) < 1E-06 && num3 > m1 * 1.5, "「幅度」从最左拖到最右（律动模式下量）：图标的动作真的变大了（平均摆动范围 " + m1.ToString("0.0") + " 点 → " + num3.ToString("0.0") + " 点）");
		Shot("6-groove-amp-max.png");
		ui.Amp.Value = 1.0;
		ui.Mode.SelectedIndex = Array.IndexOf(DanceModes.All, DanceMode.Figure);
		await Task.Delay(1200);
		if (was)
		{
			ok = await Press(ui.FollowMusic, true, () => c.UseSystemAudio);
			rep.Check(ok, "重新勾上「跟着音乐跳」：后台又开始听系统声音");
		}
	}

	private async Task RowCount()
	{
		rep.Line("-- 数量（上次真机测出来的问题就在这一行：拖了滑块，场上的个数没跟着变）--");
		int[] array = new int[3] { 12, 96, 48 };
		for (int i = 0; i < array.Length; i++)
		{
			int n = array[i];
			ui.Count.Value = n;
			bool ok = await WaitUntil(() => c.TargetCount == n && c.DancerCount == n && c.State == DanceState.Running, 12.0);
			await Task.Delay(1500);
			int num = Math.Max(0, c.DesktopCount - n);
			rep.Check(ok && c.Bystanders == num, "「数量」拖到 " + n + "：场上真的是 " + c.DancerCount + " 个在跳，" + c.Bystanders + " 个桌面图标留在原位不动（桌面图标共 " + c.DesktopCount + " 个，借来的在跳 " + Math.Max(0, c.DancerCount - Math.Min(c.DesktopCount, c.DancerCount)) + " 个）");
			rep.Check(ui.CountLabel.Text == n.ToString(), "「数量」拖到 " + n + "：滑块旁边的数字显示 " + ui.CountLabel.Text);
			rep.Note("状态文字：" + OneLine(ui.Status.Text));
			Shot("7-count-" + n + ".png");
		}
	}

	private async Task RowLanguage()
	{
		rep.Line("-- 语言 --");
		Lang original = L.Current;
		Lang other = ((original == Lang.Zh) ? Lang.En : Lang.Zh);
		ui.Language.SelectedIndex = ((other != Lang.Zh) ? 1 : 0);
		await Task.Delay(1500);
		rep.Check(L.Current == other && c.State == DanceState.Running && c.Fps > 0, "「语言」选" + ((other == Lang.Zh) ? "中文" : " English") + "：换过去了，还在跳（" + c.Fps + " fps）");
		rep.Check(Txt(ui.Pause.Content) == ((other == Lang.Zh) ? "暂停" : "Pause") && Txt(ui.Restore.Content) == ((other == Lang.Zh) ? "恢复桌面" : "Restore") && ui.Status.Text.Contains((other == Lang.Zh) ? "跳舞中" : "Dancing") && ((ui.Mode.SelectedItem as string) ?? "") == ((other == Lang.Zh) ? "打篮球" : "Basketball"), "换语言后：按钮、模式名、状态文字都换成了" + ((other == Lang.Zh) ? "中文" : "英文"));
		rep.Note("换语言后的状态文字：" + OneLine(ui.Status.Text));
		Shot("8-language-switched.png");
		ui.Language.SelectedIndex = ((original != Lang.Zh) ? 1 : 0);
		await Task.Delay(800);
		rep.Check(L.Current == original && Txt(ui.Pause.Content) == ((original == Lang.Zh) ? "暂停" : "Pause"), "「语言」选回原来的：字换回来了");
	}

	private async Task RowPauseNamesHide()
	{
		rep.Line("-- 暂停 / 继续 --");
		BitmapSource r1 = Capture();
		await Task.Delay(700);
		BitmapSource bitmapSource = Capture();
		double moving = ((r1 != null && bitmapSource != null) ? Diff(r1, bitmapSource, workArea, PanelRect()) : (-1.0));
		bool ok = await Press(ui.Pause, null, () => c.State == DanceState.Paused);
		rep.Check(ok, "点「暂停」：后台进入暂停");
		await Task.Delay(500);
		rep.Check(Txt(ui.Start.Content) == L.T("继续", "Resume") && ui.Start.IsEnabled && !ui.Pause.IsEnabled, "暂停时：「开始」按钮变成了「继续」并且能点，「暂停」变灰");
		rep.Check(ui.Status.Text.Contains(L.T("已暂停", "Paused")), "暂停时：状态文字写着「已暂停」");
		BitmapSource p0 = Capture();
		await Task.Delay(800);
		BitmapSource p1 = Capture();
		await Task.Delay(800);
		BitmapSource p2 = Capture();
		if (p0 != null && p1 != null && p2 != null)
		{
			RECT rECT = PanelRect();
			RECT box;
			double num = DiffBox(p1, p2, workArea, out box, rECT);
			rep.Check(num < 0.0005, "暂停时画面真的定住了（隔 0.8 秒的两张截屏只有 " + (num * 100.0).ToString("0.000") + "% 的像素不同；跳舞时隔 0.7 秒是 " + (moving * 100.0).ToString("0.00") + "%）" + ((num > 0.0) ? ("，不同的地方在屏幕 (" + box.Left + "," + box.Top + ")-(" + box.Right + "," + box.Bottom + ")") : ""));
			RECT box2;
			double num2 = DiffBox(p0, p1, workArea, out box2, rECT);
			rep.Check(num2 < 0.005, "点下暂停半秒后图标就不再动了（和 0.8 秒后的截屏比，只有 " + (num2 * 100.0).ToString("0.000") + "% 的像素不同）");
			if (num2 > 0.0)
			{
				rep.Note("这点差别是图标名字的笔画：文字停下来后系统会把它重新描清楚，图标本身没动。差别落在屏幕 (" + box2.Left + "," + box2.Top + ")-(" + box2.Right + "," + box2.Bottom + ")");
			}
			if (num2 >= 0.005 || num >= 0.0005)
			{
				Save(p0, Path.Combine(outDir, "9a-paused-0.5s.png"));
				Save(p1, Path.Combine(outDir, "9b-paused-1.3s.png"));
				Save(p2, Path.Combine(outDir, "9c-paused-2.1s.png"));
			}
		}
		else
		{
			rep.Note("截不到屏，没法核对「暂停时画面定住」");
		}
		rep.Line("-- 图标名称（趁暂停画面不动时比对）--");
		bool namesWere = c.ShowLabels;
		ok = await Press(ui.Names, !namesWere, () => c.ShowLabels == !namesWere);
		rep.Check(ok, (namesWere ? "去掉" : "勾上") + "「图标名称」：后台照办了");
		await Task.Delay(500);
		BitmapSource bitmapSource2 = Capture();
		if (p2 != null && bitmapSource2 != null)
		{
			double num3 = Diff(p2, bitmapSource2, workArea, PanelRect());
			rep.Check(num3 > 0.0003, (namesWere ? "去掉" : "勾上") + "「图标名称」后：画面上的名字真的" + (namesWere ? "没了" : "出来了") + "（" + (num3 * 100.0).ToString("0.00") + "% 的像素变了）");
		}
		Shot("9-paused-names-toggled.png");
		ok = await Press(ui.Names, namesWere, () => c.ShowLabels == namesWere);
		await Task.Delay(500);
		BitmapSource bitmapSource3 = Capture();
		if (p2 != null && bitmapSource3 != null)
		{
			double num4 = Diff(p2, bitmapSource3, workArea, PanelRect());
			rep.Check(ok && num4 < 0.0005, "把「图标名称」改回去：画面和刚才一样（只有 " + (num4 * 100.0).ToString("0.000") + "% 的像素不同）");
		}
		rep.Line("-- 截屏 / 录屏时隐藏本窗口 --");
		RECT pr = VisiblePanelRect();
		BitmapSource a = Capture();
		ok = await Press(ui.Hide, true, () => ui.Settings.HidePanelFromCapture);
		rep.Check(ok, "勾上「截屏 / 录屏时隐藏本窗口」：后台照办了");
		await Task.Delay(700);
		BitmapSource bitmapSource4 = Capture();
		if (a != null && bitmapSource4 != null)
		{
			double num5 = Diff(a, bitmapSource4, pr);
			rep.Soft(num5 > 0.5, "勾上后截屏：控制窗口不在截屏里了（它那一块有 " + (num5 * 100.0).ToString("0") + "% 的像素变了；系统太旧不支持时这一项不成立）");
			Save(bitmapSource4, Path.Combine(outDir, "10-panel-hidden-from-capture.png"));
		}
		ok = await Press(ui.Hide, false, () => !ui.Settings.HidePanelFromCapture);
		await Task.Delay(700);
		BitmapSource bitmapSource5 = Capture();
		if (a != null && bitmapSource5 != null)
		{
			double num6 = Diff(a, bitmapSource5, pr);
			rep.Check(ok && num6 < 0.1, "去掉这个勾：控制窗口又能截到了");
		}
		ok = await Press(ui.Start, null, () => c.State == DanceState.Running);
		rep.Check(ok, "点「继续」：接着跳");
		await Task.Delay(800);
		rep.Check(Txt(ui.Start.Content) == L.T("开始蹦迪", "Start") && !ui.Start.IsEnabled && ui.Pause.IsEnabled, "继续后：按钮回到跳舞时的样子");
	}

	private async Task PanelShots()
	{
		Lang original = L.Current;
		try
		{
			Lang[] array = new Lang[2]
			{
				original,
				(original == Lang.Zh) ? Lang.En : Lang.Zh
			};
			foreach (Lang lg in array)
			{
				if (lg != L.Current)
				{
					panel.SelfTestSwitchLanguage(lg);
					await Task.Delay(900);
				}
				BitmapSource bitmapSource = Capture();
				RECT rECT = VisiblePanelRect();
				if (bitmapSource == null)
				{
					rep.Note("截不到屏，没有控制窗口的图");
					break;
				}
				int num = Math.Max(0, rECT.Left);
				int num2 = Math.Max(0, rECT.Top);
				int num3 = Math.Min(bitmapSource.PixelWidth, rECT.Right) - num;
				int num4 = Math.Min(bitmapSource.PixelHeight, rECT.Bottom) - num2;
				if (num3 < 50 || num4 < 50)
				{
					rep.Note("控制窗口不在主屏里（" + rECT.Left + "," + rECT.Top + "," + rECT.Right + "," + rECT.Bottom + "），没有它的图");
					break;
				}
				CroppedBitmap croppedBitmap = new CroppedBitmap(bitmapSource, new Int32Rect(num, num2, num3, num4));
				croppedBitmap.Freeze();
				string text = "panel-window-" + L.Code(lg) + ".png";
				Save(croppedBitmap, Path.Combine(outDir, text));
				rep.Note("控制窗口（" + ((lg == Lang.Zh) ? "中文" : "English") + "）真窗口截图 " + num3 + "x" + num4 + "：" + text + "，状态文字：" + OneLine(c.StatusText));
			}
		}
		catch (Exception ex)
		{
			rep.Note("截控制窗口出错：" + ex.Message);
		}
		if (L.Current != original)
		{
			panel.SelfTestSwitchLanguage(original);
			await Task.Delay(700);
		}
	}

	private async Task AfterRestore(Dictionary<string, POINT>? before, string when)
	{
		rep.Check(c.State == DanceState.Idle, when + "：程序回到就绪状态");
		bool ok = await WaitUntil(() => Native.FindWindow(null, "DesktopDance Stage") == IntPtr.Zero, 4.0);
		rep.Check(ok, when + "：舞台窗口已经没了");
		if (before != null)
		{
			Dictionary<string, POINT> dictionary = await ReadPositions();
			if (dictionary == null)
			{
				rep.Soft(ok: false, when + "：这次没读到图标位置，没法核对");
			}
			else
			{
				rep.Check(SamePositions(before, dictionary, out string detail), when + "：" + detail);
			}
		}
	}

	private async Task RunHold()
	{
		rep.Line("桌面图标跳舞 Windows 版 · 实跑测试（等着被强制结束）· " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
		await WaitUntil(() => c.CatalogLoaded, 40.0);
		await Task.Delay(800);
		RECT panelRect = PanelRect();
		Dictionary<string, POINT> dictionary = await ReadPositions();
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("#panel\t" + panelRect.Left + "\t" + panelRect.Top + "\t" + panelRect.Right + "\t" + panelRect.Bottom);
		stringBuilder.AppendLine("#positions\t" + ((dictionary == null) ? "none" : dictionary.Count.ToString()));
		if (dictionary != null)
		{
			foreach (KeyValuePair<string, POINT> item in dictionary)
			{
				stringBuilder.AppendLine(item.Key.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ') + "\t" + item.Value.X + "\t" + item.Value.Y);
			}
		}
		File.WriteAllText(Path.Combine(outDir, "positions-before.tsv"), stringBuilder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		Shot("k0-before.png");
		List<Probe> probes = ProbePoints(WorkArea(), panelRect);
		rep.Note("取了 " + probes.Count + " 个探测点：" + Summary(probes));
		c.Start();
		bool flag = await WaitUntil(() => c.State == DanceState.Running, 45.0);
		rep.Check(flag, "开始后进入跳舞状态");
		if (!flag)
		{
			c.EmergencyRestore();
			ExitCode = 1;
			return;
		}
		await Task.Delay(3000);
		rep.Note("舞台窗口层级（程序自己的检查）：" + c.StageLayerNote);
		LayerCheck(probes, Native.FindWindow(null, "DesktopDance Stage"), "强制结束前");
		Shot("k1-dancing.png");
		rep.Line("正在跳舞，等着被强制结束…");
		File.WriteAllText(Path.Combine(outDir, "hold-ready.txt"), "ready");
		await Task.Delay(150000);
		rep.Fail("等了 150 秒没有被结束，自己恢复桌面并退出");
		c.EmergencyRestore();
		ExitCode = 3;
	}

	public static int AfterKill(string outDir)
	{
		try
		{
			Directory.CreateDirectory(outDir);
		}
		catch
		{
		}
		Report report = new Report(Path.Combine(outDir, "livetest-kill.txt"));
		try
		{
			report.Line("桌面图标跳舞 Windows 版 · 强制结束后的复核 · " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
			bool flag = false;
			for (int i = 0; i < 50; i++)
			{
				if (flag)
				{
					break;
				}
				flag = Native.FindWindow(null, "DesktopDance Stage") == IntPtr.Zero;
				if (!flag)
				{
					Thread.Sleep(100);
				}
			}
			report.Check(flag, "进程被强制结束后：舞台窗口已经没了（没有任何东西留在桌面上）");
			Dictionary<string, POINT> dictionary = new Dictionary<string, POINT>();
			bool flag2 = false;
			RECT rECT = default(RECT);
			string path = Path.Combine(outDir, "positions-before.tsv");
			if (File.Exists(path))
			{
				string[] array = File.ReadAllLines(path, Encoding.UTF8);
				foreach (string text in array)
				{
					string[] array2 = text.Split('\t');
					int result;
					int result2;
					if (array2.Length >= 5 && array2[0] == "#panel")
					{
						int.TryParse(array2[1], out rECT.Left);
						int.TryParse(array2[2], out rECT.Top);
						int.TryParse(array2[3], out rECT.Right);
						int.TryParse(array2[4], out rECT.Bottom);
					}
					else if (array2.Length >= 2 && array2[0] == "#positions")
					{
						flag2 = array2[1] != "none";
					}
					else if (array2.Length >= 3 && !text.StartsWith("#") && int.TryParse(array2[^2], out result) && int.TryParse(array2[^1], out result2))
					{
						dictionary[string.Join("\t", array2, 0, array2.Length - 2)] = new POINT
						{
							X = result,
							Y = result2
						};
					}
				}
			}
			else
			{
				report.Fail("找不到开始前记下的图标位置（positions-before.tsv）");
			}
			if (flag2)
			{
				Dictionary<string, POINT> dictionary2 = DesktopLoader.ReadPositions();
				if (dictionary2 == null)
				{
					report.Soft(ok: false, "强制结束后没读到图标位置，没法核对");
				}
				else
				{
					report.Check(SamePositions(dictionary, dictionary2, out string detail), "进程被强制结束后：" + detail);
				}
			}
			else
			{
				report.Note("开始前就没读到图标位置，这一项没法核对");
			}
			Thread.Sleep(600);
			BitmapSource bitmapSource = Capture();
			if (bitmapSource != null)
			{
				Save(bitmapSource, Path.Combine(outDir, "k2-after-kill.png"));
			}
			string path2 = Path.Combine(outDir, "k0-before.png");
			if (bitmapSource != null && File.Exists(path2))
			{
				double num = Diff(Load(path2), bitmapSource, WorkArea(), rECT);
				report.Soft(num < 0.01, "强制结束后的画面和开始前一样（工作区里只有 " + (num * 100.0).ToString("0.00") + "% 的像素不同，控制窗口那一块不算）");
			}
			report.Line(report.Failed ? "结论：有失败项。" : "结论：强制结束后桌面是原样。");
		}
		catch (Exception ex)
		{
			report.Fail("复核抛出异常：" + ex);
		}
		CopyLog(outDir);
		return report.Failed ? 1 : 0;
	}

	private static string OneLine(string s)
	{
		return s.Replace("\r", "").Replace("\n", " | ");
	}

	private static async Task<bool> WaitUntil(Func<bool> cond, double seconds)
	{
		double deadline = Clock.Now + seconds;
		while (!cond())
		{
			if (Clock.Now >= deadline)
			{
				return false;
			}
			await Task.Delay(100);
		}
		return true;
	}

	private static async Task<Dictionary<string, POINT>?> ReadPositions()
	{
		TaskCompletionSource<Dictionary<string, POINT>?> tcs = new TaskCompletionSource<Dictionary<string, POINT>>();
		Thread thread = new Thread((ThreadStart)delegate
		{
			try
			{
				tcs.TrySetResult(DesktopLoader.ReadPositions());
			}
			catch (Exception ex)
			{
				Log.Write("[实跑测试] 读图标位置出错：" + ex.Message);
				tcs.TrySetResult(null);
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.IsBackground = true;
		thread.Start();
		return (await Task.WhenAny(tcs.Task, Task.Delay(30000)) == tcs.Task) ? tcs.Task.Result : null;
	}

	private static bool SamePositions(Dictionary<string, POINT> before, Dictionary<string, POINT> now, out string detail)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		string text = "";
		foreach (KeyValuePair<string, POINT> item in before)
		{
			if (!now.TryGetValue(item.Key, out var value))
			{
				num3++;
				continue;
			}
			if (value.X == item.Value.X && value.Y == item.Value.Y)
			{
				num++;
				continue;
			}
			num2++;
			if (text.Length == 0)
			{
				text = "（例如 " + item.Key + "：" + item.Value.X + "," + item.Value.Y + " → " + value.X + "," + value.Y + "）";
			}
		}
		int num4 = now.Count - num - num2;
		bool flag = num2 == 0 && num3 == 0 && num4 == 0;
		detail = (flag ? (num + " 个桌面图标的位置和开始前一模一样，没多没少") : ("图标和开始前不一样：位置变了 " + num2 + " 个" + text + "，少了 " + num3 + " 个，多了 " + num4 + " 个"));
		return flag;
	}

	private RECT PanelRect()
	{
		try
		{
			nint handle = new WindowInteropHelper(panel).Handle;
			if (handle != IntPtr.Zero && Native.GetWindowRect(handle, out var lpRect))
			{
				return lpRect;
			}
		}
		catch
		{
		}
		return default(RECT);
	}

	private RECT VisiblePanelRect()
	{
		try
		{
			nint handle = new WindowInteropHelper(panel).Handle;
			if (handle != IntPtr.Zero && TestNative.DwmGetWindowAttribute(handle, 9, out var pvAttribute, Marshal.SizeOf<RECT>()) == 0 && pvAttribute.Right > pvAttribute.Left)
			{
				return pvAttribute;
			}
		}
		catch
		{
		}
		return PanelRect();
	}

	private static RECT WorkArea()
	{
		try
		{
			MONITORINFO lpmi = new MONITORINFO
			{
				cbSize = Marshal.SizeOf<MONITORINFO>()
			};
			nint num = Native.MonitorFromPoint(new POINT
			{
				X = 0,
				Y = 0
			}, 1u);
			if (num != IntPtr.Zero && Native.GetMonitorInfo(num, ref lpmi))
			{
				return lpmi.rcWork;
			}
		}
		catch
		{
		}
		return new RECT
		{
			Left = 0,
			Top = 0,
			Right = TestNative.GetSystemMetrics(0),
			Bottom = TestNative.GetSystemMetrics(1)
		};
	}

	private static List<Probe> ProbePoints(RECT work, RECT panelRect)
	{
		List<Probe> list = new List<Probe>();
		int num = work.Right - work.Left;
		int num2 = work.Bottom - work.Top;
		for (int i = 0; i < 5; i++)
		{
			for (int j = 0; j < 7; j++)
			{
				POINT pt = new POINT
				{
					X = work.Left + (int)((double)num * (0.08 + 0.84 * (double)j / 6.0)),
					Y = work.Top + (int)((double)num2 * (0.08 + 0.84 * (double)i / 4.0))
				};
				if (pt.X < panelRect.Left - 12 || pt.X > panelRect.Right + 12 || pt.Y < panelRect.Top - 12 || pt.Y > panelRect.Bottom + 12)
				{
					nint num3 = TestNative.WindowFromPoint(pt);
					nint num4 = ((num3 == IntPtr.Zero) ? IntPtr.Zero : TestNative.GetAncestor(num3, 2u));
					StringBuilder stringBuilder = new StringBuilder(128);
					if (num4 != IntPtr.Zero)
					{
						TestNative.GetClassName(num4, stringBuilder, stringBuilder.Capacity);
					}
					list.Add(new Probe
					{
						Pt = pt,
						Root = num4,
						Class = stringBuilder.ToString()
					});
				}
			}
		}
		return list;
	}

	private static string Summary(List<Probe> probes)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		foreach (Probe probe in probes)
		{
			string key = ((probe.Class.Length == 0) ? "（无）" : probe.Class);
			dictionary[key] = ((!dictionary.TryGetValue(key, out var value)) ? 1 : (value + 1));
		}
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, int> item in dictionary)
		{
			list.Add(item.Key + " × " + item.Value);
		}
		return string.Join("，", list);
	}

	private void LayerCheck(List<Probe> before, nint stage, string when)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		List<string> list = new List<string>();
		foreach (Probe item in before)
		{
			nint num5 = TestNative.WindowFromPoint(item.Pt);
			nint num6 = ((num5 == IntPtr.Zero) ? IntPtr.Zero : TestNative.GetAncestor(num5, 2u));
			if (item.IsDesktop)
			{
				num++;
				if (num6 == stage && stage != IntPtr.Zero)
				{
					num2++;
				}
				else if (list.Count < 3)
				{
					list.Add("(" + item.Pt.X + "," + item.Pt.Y + ") 最上面是 " + ClassOf(num6));
				}
				continue;
			}
			num3++;
			if (num6 == item.Root)
			{
				num4++;
			}
			else if (list.Count < 3)
			{
				list.Add("(" + item.Pt.X + "," + item.Pt.Y + ") 原来是 " + item.Class + "，现在是 " + ((num6 == stage) ? "舞台" : ClassOf(num6)));
			}
		}
		if (num > 0)
		{
			rep.Soft(num2 == num, when + "：原来露着桌面的 " + num + " 个点里，现在有 " + num2 + " 个最上面是舞台（应该全部都是）");
		}
		else
		{
			rep.Note(when + "：没有一个探测点露着桌面（屏幕被别的窗口占满了），没法核对「舞台盖住了桌面」");
		}
		if (num3 > 0)
		{
			rep.Soft(num4 == num3, when + "：原来被别的窗口盖着的 " + num3 + " 个点里，有 " + num4 + " 个还是原来那个窗口（应该全部都是：舞台不能盖到别的窗口上面）");
		}
		foreach (string item2 in list)
		{
			rep.Note("对不上的点：" + item2);
		}
	}

	private static string ClassOf(nint h)
	{
		if (h == IntPtr.Zero)
		{
			return "（无）";
		}
		StringBuilder stringBuilder = new StringBuilder(128);
		TestNative.GetClassName(h, stringBuilder, stringBuilder.Capacity);
		if (stringBuilder.Length != 0)
		{
			return stringBuilder.ToString();
		}
		return "（无名）";
	}

	private BitmapSource? Shot(string name)
	{
		try
		{
			BitmapSource bitmapSource = Capture();
			if (bitmapSource == null)
			{
				rep.Note("截屏失败：" + name);
				return null;
			}
			Save(bitmapSource, Path.Combine(outDir, name));
			return bitmapSource;
		}
		catch (Exception ex)
		{
			rep.Note("截屏 " + name + " 出错：" + ex.Message);
			return null;
		}
	}

	private static BitmapSource? Capture()
	{
		int systemMetrics = TestNative.GetSystemMetrics(0);
		int systemMetrics2 = TestNative.GetSystemMetrics(1);
		if (systemMetrics <= 0 || systemMetrics2 <= 0)
		{
			return null;
		}
		nint dC = Native.GetDC(IntPtr.Zero);
		if (dC == IntPtr.Zero)
		{
			return null;
		}
		nint num = IntPtr.Zero;
		nint num2 = IntPtr.Zero;
		try
		{
			num = TestNative.CreateCompatibleDC(dC);
			num2 = TestNative.CreateCompatibleBitmap(dC, systemMetrics, systemMetrics2);
			if (num == IntPtr.Zero || num2 == IntPtr.Zero)
			{
				return null;
			}
			nint h = TestNative.SelectObject(num, num2);
			bool num3 = TestNative.BitBlt(num, 0, 0, systemMetrics, systemMetrics2, dC, 0, 0, 1087111200u);
			TestNative.SelectObject(num, h);
			if (!num3)
			{
				return null;
			}
			FormatConvertedBitmap formatConvertedBitmap = new FormatConvertedBitmap(Imaging.CreateBitmapSourceFromHBitmap(num2, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()), PixelFormats.Bgr32, null, 0.0);
			formatConvertedBitmap.Freeze();
			return formatConvertedBitmap;
		}
		finally
		{
			if (num2 != IntPtr.Zero)
			{
				Native.DeleteObject(num2);
			}
			if (num != IntPtr.Zero)
			{
				TestNative.DeleteDC(num);
			}
			Native.ReleaseDC(IntPtr.Zero, dC);
		}
	}

	private static void Save(BitmapSource b, string path)
	{
		PngBitmapEncoder pngBitmapEncoder = new PngBitmapEncoder();
		pngBitmapEncoder.Frames.Add(BitmapFrame.Create(b));
		using FileStream stream = File.Create(path);
		pngBitmapEncoder.Save(stream);
	}

	private static BitmapSource Load(string path)
	{
		BitmapImage bitmapImage = new BitmapImage();
		bitmapImage.BeginInit();
		bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
		bitmapImage.UriSource = new Uri(Path.GetFullPath(path));
		bitmapImage.EndInit();
		FormatConvertedBitmap formatConvertedBitmap = new FormatConvertedBitmap(bitmapImage, PixelFormats.Bgr32, null, 0.0);
		formatConvertedBitmap.Freeze();
		return formatConvertedBitmap;
	}

	private static double Diff(BitmapSource a, BitmapSource b, RECT area, params RECT[] exclude)
	{
		RECT box;
		return DiffBox(a, b, area, out box, exclude);
	}

	private static double DiffBox(BitmapSource a, BitmapSource b, RECT area, out RECT box, params RECT[] exclude)
	{
		box = default(RECT);
		int num = int.MaxValue;
		int num2 = int.MaxValue;
		int num3 = -1;
		int num4 = -1;
		if (a.PixelWidth != b.PixelWidth || a.PixelHeight != b.PixelHeight)
		{
			return 1.0;
		}
		int pixelWidth = a.PixelWidth;
		int pixelHeight = a.PixelHeight;
		int num5 = pixelWidth * 4;
		byte[] array = new byte[num5 * pixelHeight];
		byte[] array2 = new byte[num5 * pixelHeight];
		new FormatConvertedBitmap(a, PixelFormats.Bgr32, null, 0.0).CopyPixels(array, num5, 0);
		new FormatConvertedBitmap(b, PixelFormats.Bgr32, null, 0.0).CopyPixels(array2, num5, 0);
		int num6 = Math.Max(0, area.Left);
		int num7 = Math.Max(0, area.Top);
		int num8 = Math.Min(pixelWidth, area.Right);
		int num9 = Math.Min(pixelHeight, area.Bottom);
		long num10 = 0L;
		long num11 = 0L;
		for (int i = num7; i < num9; i++)
		{
			for (int j = num6; j < num8; j++)
			{
				bool flag = false;
				for (int k = 0; k < exclude.Length; k++)
				{
					RECT rECT = exclude[k];
					if (j >= rECT.Left - 8 && j < rECT.Right + 8 && i >= rECT.Top - 8 && i < rECT.Bottom + 8)
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					continue;
				}
				int num12 = i * num5 + j * 4;
				int num13 = Math.Abs(array[num12] - array2[num12]) + Math.Abs(array[num12 + 1] - array2[num12 + 1]) + Math.Abs(array[num12 + 2] - array2[num12 + 2]);
				num10++;
				if (num13 > 48)
				{
					num11++;
					if (j < num)
					{
						num = j;
					}
					if (i < num2)
					{
						num2 = i;
					}
					if (j > num3)
					{
						num3 = j;
					}
					if (i > num4)
					{
						num4 = i;
					}
				}
			}
		}
		if (num3 >= 0)
		{
			box = new RECT
			{
				Left = num,
				Top = num2,
				Right = num3 + 1,
				Bottom = num4 + 1
			};
		}
		if (num10 != 0L)
		{
			return (double)num11 / (double)num10;
		}
		return 0.0;
	}

	private static void CopyLog(string outDir)
	{
		try
		{
			File.Copy(AppPaths.LogFile, Path.Combine(outDir, "desktop-dance.log"), overwrite: true);
		}
		catch
		{
		}
	}
}
