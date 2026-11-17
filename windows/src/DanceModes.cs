namespace DesktopDance;

public static class DanceModes
{
	public static readonly DanceMode[] All = new DanceMode[9]
	{
		DanceMode.Figure,
		DanceMode.Shiver,
		DanceMode.Flash,
		DanceMode.Moonwalk,
		DanceMode.Subject3,
		DanceMode.Rehearsal,
		DanceMode.Groove,
		DanceMode.Snake,
		DanceMode.Free
	};

	public static string Title(DanceMode m)
	{
		return m switch
		{
			DanceMode.Snake => L.T("列队", "Line"), 
			DanceMode.Figure => L.T("打篮球", "Basketball"), 
			DanceMode.Groove => L.T("律动", "Groove"), 
			DanceMode.Free => L.T("自由", "Free"), 
			DanceMode.Shiver => L.T("狗熊哆嗦毛", "Bear Shiver"), 
			DanceMode.Flash => L.T("闪身步", "Flash Step"), 
			DanceMode.Moonwalk => L.T("MJ 太空步", "MJ Moonwalk"), 
			DanceMode.Subject3 => L.T("科目三", "Subject Three"), 
			_ => L.T("测试模式", "Test Reel"), 
		};
	}

	public static string LogName(DanceMode m)
	{
		return m switch
		{
			DanceMode.Snake => "列队", 
			DanceMode.Figure => "打篮球", 
			DanceMode.Groove => "律动", 
			DanceMode.Free => "自由", 
			DanceMode.Shiver => "狗熊哆嗦毛", 
			DanceMode.Flash => "闪身步", 
			DanceMode.Moonwalk => "MJ 太空步", 
			DanceMode.Subject3 => "科目三", 
			_ => "测试模式", 
		};
	}

	public static double AutoBeats(DanceMode m)
	{
		return m switch
		{
			DanceMode.Snake => 44.0, 
			DanceMode.Figure => 32.0, 
			DanceMode.Shiver => 16.0, 
			DanceMode.Flash => 16.0, 
			DanceMode.Moonwalk => 24.0, 
			DanceMode.Subject3 => 16.0, 
			DanceMode.Rehearsal => 1000000000.0, 
			_ => 16.0, 
		};
	}
}
