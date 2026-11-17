using System;
using System.Threading;
using System.Windows.Threading;

namespace DesktopDance;

public static class StaWorker
{
	public static void Run(Action work)
	{
		Thread thread = new Thread((ThreadStart)delegate
		{
			try
			{
				work();
			}
			catch (Exception ex)
			{
				Log.Write("后台任务出错：" + ex);
			}
			finally
			{
				try
				{
					Dispatcher.CurrentDispatcher.InvokeShutdown();
				}
				catch
				{
				}
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.IsBackground = true;
		thread.Name = "shell-worker";
		thread.Start();
	}
}
