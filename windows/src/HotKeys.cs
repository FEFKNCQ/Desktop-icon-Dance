using System;
using System.Threading;

namespace DesktopDance;

public sealed class HotKeys
{
	public const int IdEscape = 2;

	private const uint MsgEscOn = 32769u;

	private const uint MsgEscOff = 32770u;

	private const uint MsgQuit = 32771u;

	private readonly ManualResetEventSlim ready = new ManualResetEventSlim(initialState: false);

	private Thread? thread;

	private uint threadId;

	private volatile bool escOk;

	public Action<int>? Pressed;

	public bool EscapeRegistered => escOk;

	public void Start()
	{
		if (thread == null)
		{
			thread = new Thread(Loop)
			{
				IsBackground = true,
				Name = "hotkeys"
			};
			thread.Start();
			ready.Wait(3000);
		}
	}

	private void Loop()
	{
		try
		{
			threadId = Native.GetCurrentThreadId();
			Native.PeekMessage(out var _, IntPtr.Zero, 0u, 0u, 0u);
		}
		finally
		{
			ready.Set();
		}
		MSG lpMsg2;
		while (Native.GetMessage(out lpMsg2, IntPtr.Zero, 0u, 0u) > 0)
		{
			if (lpMsg2.message == 786)
			{
				int obj = (int)((IntPtr)lpMsg2.wParam).ToInt64();
				Log.Write("热键触发：Esc");
				try
				{
					Pressed?.Invoke(obj);
				}
				catch (Exception ex)
				{
					Log.Write("热键处理出错：" + ex.Message);
				}
			}
			else if (lpMsg2.message == 32769)
			{
				if (!escOk)
				{
					escOk = Native.RegisterHotKey(IntPtr.Zero, 2, 16384u, 27u);
					if (!escOk)
					{
						Log.Write("热键 Esc 注册失败（可能被别的软件占用），请点「恢复桌面」按钮或在桌面上点一下");
					}
				}
			}
			else if (lpMsg2.message == 32770)
			{
				if (escOk)
				{
					Native.UnregisterHotKey(IntPtr.Zero, 2);
					escOk = false;
				}
			}
			else if (lpMsg2.message == 32771)
			{
				break;
			}
		}
		if (escOk)
		{
			Native.UnregisterHotKey(IntPtr.Zero, 2);
		}
	}

	public void SetEscape(bool on)
	{
		if (threadId != 0)
		{
			Native.PostThreadMessage(threadId, on ? 32769u : 32770u, IntPtr.Zero, IntPtr.Zero);
		}
	}

	public void Stop()
	{
		if (threadId != 0)
		{
			Native.PostThreadMessage(threadId, 32771u, IntPtr.Zero, IntPtr.Zero);
		}
	}
}
