using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;

namespace DesktopDance;

public static class Program
{
	[STAThread]
	public static int Main(string[] args)
	{
		if (args.Length >= 1 && args[0] == "--selftest")
		{
			return SelfTest.Run((args.Length >= 2) ? args[1] : "selftest-out");
		}
		string outDir = ((args.Length >= 2) ? args[1] : "livetest-out");
		if (args.Length >= 1 && args[0] == "--livetest-after")
		{
			return LiveTest.AfterKill(outDir);
		}
		bool flag = args.Length >= 1 && args[0] == "--livetest-hold";
		bool flag2 = flag || (args.Length >= 1 && args[0] == "--livetest");
		Settings settings = Settings.Load();
		if (flag2)
		{
			settings.NoSave = true;
		}
		string text = "系统语言";
		Lang lang = L.SystemDefault();
		Lang? lang2 = L.Parse(settings.Language);
		if (lang2.HasValue)
		{
			Lang valueOrDefault = lang2.GetValueOrDefault();
			lang = valueOrDefault;
			text = "上次选的";
		}
		for (int i = 0; i + 1 < args.Length; i++)
		{
			if (args[i] == "--lang")
			{
				lang2 = L.Parse(args[i + 1]);
				if (lang2.HasValue)
				{
					Lang valueOrDefault2 = lang2.GetValueOrDefault();
					lang = valueOrDefault2;
					text = "命令行";
				}
			}
		}
		L.Current = lang;
		bool createdNew;
		using (new Mutex(initiallyOwned: true, "Local\\DesktopDance-single-instance-7d0c1b0e", out createdNew))
		{
			if (!createdNew)
			{
				if (flag2)
				{
					return 4;
				}
				MessageBox.Show(L.T("Desktop-icon-Dance 已经在运行了，看看屏幕左下角或任务栏。", "Desktop-icon-Dance is already running. Look at the bottom-left corner of the screen, or the taskbar."), "Desktop-icon-Dance", MessageBoxButton.OK, MessageBoxImage.Asterisk);
				return 0;
			}
			string text2 = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "?";
			Log.StartSession("桌面图标跳舞 Windows 版 " + text2 + " · " + Environment.OSVersion.VersionString + " · " + RuntimeInformation.FrameworkDescription);
			Log.Write("界面语言：" + ((lang == Lang.Zh) ? "中文" : "English") + "（" + text + "）");
			Application app = new Application
			{
				ShutdownMode = ShutdownMode.OnExplicitShutdown
			};
			DanceController controller = new DanceController(app.Dispatcher);
			controller.BallName = settings.BallName;
			controller.UseSystemAudio = settings.FollowSystemAudio;
			app.DispatcherUnhandledException += delegate(object s, DispatcherUnhandledExceptionEventArgs e)
			{
				Log.Write("界面线程出错：" + e.Exception);
				try
				{
					controller.EmergencyRestore(new Msg("程序出了点错，已停止并恢复桌面（详情见日志）", "Something went wrong. Stopped and restored the desktop (details are in the log)"));
				}
				catch
				{
				}
				e.Handled = true;
			};
			AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e)
			{
				Log.Write("未处理的错误：" + e.ExceptionObject);
			};
			controller.HotKeys.Pressed = delegate
			{
				if (app.Dispatcher.BeginInvoke(DispatcherPriority.Send, (Action)delegate
				{
					controller.EmergencyRestore();
				}).Wait(TimeSpan.FromSeconds(3.0)) != DispatcherOperationStatus.Completed && controller.StageUp)
				{
					Log.Write("界面 3 秒没有响应，直接结束进程（舞台窗口随进程消失，桌面不受影响）");
					Process.GetCurrentProcess().Kill();
				}
			};
			controller.HotKeys.Start();
			SystemEvents.DisplaySettingsChanged += delegate
			{
				app.Dispatcher.BeginInvoke((Action)delegate
				{
					if (controller.State != DanceState.Idle)
					{
						controller.EmergencyRestore(new Msg("屏幕设置变了，已停止并恢复桌面", "Display settings changed. Stopped and restored the desktop"));
					}
				});
			};
			PanelWindow panelWindow = new PanelWindow(controller, settings);
			panelWindow.Show();
			controller.RefreshCatalog();
			LiveTest liveTest = (flag2 ? LiveTest.Attach(app, controller, panelWindow, outDir, flag) : null);
			int num = app.Run();
			controller.HotKeys.Stop();
			Log.Write("退出");
			return liveTest?.ExitCode ?? num;
		}
	}
}
