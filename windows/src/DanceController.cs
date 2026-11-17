using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;

namespace DesktopDance;

public sealed class DanceController
{
	private readonly Dispatcher dispatcher;

	public readonly HotKeys HotKeys = new HotKeys();

	private DanceMode mode = DanceMode.Figure;

	private bool portrait;

	private bool showGuide = true;

	private bool showLabels = true;

	private bool autoSwitch;

	private bool useSystemAudio = true;

	private double amp = 1.0;

	private int targetCount = 48;

	private string ballName = "";

	public double Speed = 1.0;

	private static readonly Msg AudioBuiltIn = new Msg("内置节拍", "Built-in beat");

	private static readonly Msg AudioConnecting = new Msg("正在连接系统声音…", "Connecting to system audio…");

	private static readonly Msg AudioFollowing = new Msg("跟随系统声音", "Following system audio");

	private Msg note = Msg.None;

	private Msg audioNote = AudioBuiltIn;

	private string curtainNote = "";

	private Msg curtainProblem = Msg.None;

	private Msg verifyNote = Msg.None;

	private Msg layerNote = Msg.None;

	private StageWindow? stage;

	private StageWindow? staleStage;

	private double staleDeadline;

	private List<DancerItem> items = new List<DancerItem>();

	private LoadResult? load;

	private int dancerCount;

	private int bystanders;

	private int loadedTarget;

	private bool reloadForced;

	private bool reloading;

	private DispatcherTimer? reloadTimer;

	private readonly Choreographer choreo = new Choreographer();

	private readonly BeatAnalyzer analyzer = new BeatAnalyzer();

	private readonly BeatClock clock = new BeatClock();

	private readonly SystemAudioCapture sysAudio;

	private Task audioChain = Task.CompletedTask;

	private int prepareToken;

	private bool ticking;

	private volatile bool stageUp;

	private RectD visibleLocal;

	private double stageW;

	private double stageH;

	private double lastTick;

	private double modeStartBeat;

	private double tempoRate = 1.0;

	private Pose[] lastPoses = Array.Empty<Pose>();

	private Pose[]? transFrom;

	private double transStart;

	private double transDur = 0.6;

	private double transStagger;

	private int frames;

	private double fpsStamp;

	public DanceState State { get; private set; }

	public DanceMode Mode
	{
		get
		{
			return mode;
		}
		set
		{
			if (mode != value)
			{
				mode = value;
				ModeChanged();
			}
		}
	}

	public bool Portrait
	{
		get
		{
			return portrait;
		}
		set
		{
			if (portrait != value)
			{
				portrait = value;
				LayoutStage(animated: true);
			}
		}
	}

	public bool ShowGuide
	{
		get
		{
			return showGuide;
		}
		set
		{
			if (showGuide != value)
			{
				showGuide = value;
				LayoutStage(animated: false);
			}
		}
	}

	public bool ShowLabels
	{
		get
		{
			return showLabels;
		}
		set
		{
			if (showLabels != value)
			{
				showLabels = value;
				RefreshLabels();
			}
		}
	}

	public bool AutoSwitch
	{
		get
		{
			return autoSwitch;
		}
		set
		{
			autoSwitch = value;
			modeStartBeat = clock.Beat;
		}
	}

	public double Amp
	{
		get
		{
			return amp;
		}
		set
		{
			amp = value;
			choreo.Amp = value;
		}
	}

	public int TargetCount
	{
		get
		{
			return targetCount;
		}
		set
		{
			if (targetCount != value)
			{
				targetCount = value;
				ScheduleReload();
			}
		}
	}

	public string BallName
	{
		get
		{
			return ballName;
		}
		set
		{
			ballName = value ?? "";
			UpdateBall();
			if (IsActive && ballName.Length > 0 && !BallIsDancing())
			{
				reloadForced = true;
				ScheduleReload();
			}
		}
	}

	public bool UseSystemAudio
	{
		get
		{
			return useSystemAudio;
		}
		set
		{
			if (useSystemAudio != value)
			{
				useSystemAudio = value;
				if (IsActive)
				{
					RestartAudio();
				}
			}
		}
	}

	public int Fps { get; private set; }

	public int DesktopCount { get; private set; }

	public NameCatalog Catalog { get; private set; } = new NameCatalog();

	public bool CatalogLoaded { get; private set; }

	public bool IsActive
	{
		get
		{
			if (State != DanceState.Running)
			{
				return State == DanceState.Paused;
			}
			return true;
		}
	}

	public bool StageUp => stageUp;

	public int Bpm => (int)M.Round(clock.Bpm);

	public string StageLayerNote => stage?.LayerNote ?? "";

	public int DancerCount => dancerCount;

	public int Bystanders => bystanders;

	public double BeatNow => clock.Beat;

	public string BallInUse
	{
		get
		{
			if (choreo.BallIndex < 0 || choreo.BallIndex >= items.Count)
			{
				return "";
			}
			return items[choreo.BallIndex].Name;
		}
	}

	public Pose[] CurrentPoses => lastPoses;

	public int GuestCount => Math.Max(0, items.Count - DesktopCount);

	private bool NeedsReload
	{
		get
		{
			if (!reloadForced)
			{
				return loadedTarget != targetCount;
			}
			return true;
		}
	}

	public string StatusText
	{
		get
		{
			List<string> list = new List<string>();
			switch (State)
			{
			case DanceState.Idle:
			{
				if (!CatalogLoaded)
				{
					list.Add(L.T("就绪 · 正在数桌面图标…", "Ready · counting desktop icons…"));
					break;
				}
				int count = Catalog.Desktop.Count;
				list.Add(L.T("就绪 · 桌面上有 " + count + " 个图标", "Ready · " + count + " icons on the desktop"));
				if (count < TargetCount)
				{
					list.Add(L.T("不足 " + TargetCount + " 个，开始时借已安装应用的图标补位（只显示，不建文件）", "Fewer than " + TargetCount + ": icons of installed apps will fill in (display only, no files are created)"));
				}
				break;
			}
			case DanceState.Preparing:
				list.Add(L.T("准备中…（读图标和壁纸，通常 1～3 秒）", "Preparing… (reading icons and wallpaper, usually 1–3 seconds)"));
				break;
			default:
				list.Add((State == DanceState.Paused) ? L.T("已暂停", "Paused") : ((State == DanceState.Stopping) ? L.T("正在回到原位…", "Returning to place…") : L.T("跳舞中", "Dancing")));
				list.Add((bystanders > 0) ? L.T(dancerCount + " 个图标在跳，其余 " + bystanders + " 个留在原位", dancerCount + " icons dancing, the other " + bystanders + " stay in place") : L.T("桌面图标 " + DesktopCount + " + 补位 " + GuestCount, "Desktop icons " + DesktopCount + " + borrowed " + GuestCount));
				if (reloading)
				{
					list.Add(L.T("正在按新的数量重新排队…", "Rearranging for the new number…"));
				}
				list.Add(audioNote.Text + " · " + Bpm + " BPM · " + Fps + " fps");
				if (mode == DanceMode.Figure && Math.Abs(tempoRate - 1.0) > 0.02)
				{
					list.Add(L.T("跟着音乐：动作速度 " + tempoRate.ToString("0.00") + " 倍", "Following the music: speed ×" + tempoRate.ToString("0.00")));
				}
				if (useSystemAudio && sysAudio.Running && !sysAudio.Hearing)
				{
					list.Add(L.T("现在没听到声音，先按内置节拍跳。电脑一放歌就会自动跟上", "No sound right now, so it is dancing to the built-in beat. It will follow as soon as music plays"));
				}
				if (!curtainProblem.IsEmpty)
				{
					list.Add(curtainProblem.Text);
				}
				if (!layerNote.IsEmpty)
				{
					list.Add(layerNote.Text);
				}
				if (!HotKeys.EscapeRegistered)
				{
					list.Add(L.T("Esc 被别的软件占用了，恢复请点「恢复桌面」或在桌面上点一下", "Esc is taken by another app; click Restore or click the desktop instead"));
				}
				break;
			}
			if (!note.IsEmpty)
			{
				list.Add(note.Text);
			}
			if (State == DanceState.Idle && !verifyNote.IsEmpty)
			{
				list.Add(verifyNote.Text);
			}
			return string.Join("\n", list);
		}
	}

	public string AudioDebug => analyzer.DebugSummary() + "，" + (clock.Locked ? "已跟上声音" : "没有声音");

	public event Action? Changed;

	public event Action? CatalogChanged;

	public DanceController(Dispatcher dispatcher)
	{
		this.dispatcher = dispatcher;
		sysAudio = new SystemAudioCapture(analyzer);
	}

	private void SetState(DanceState s)
	{
		State = s;
		this.Changed?.Invoke();
	}

	public void Start()
	{
		if (State == DanceState.Paused)
		{
			TogglePause();
		}
		else if (State == DanceState.Idle)
		{
			note = Msg.None;
			verifyNote = Msg.None;
			SetState(DanceState.Preparing);
			int token = ++prepareToken;
			int target = targetCount;
			string text = ballName;
			Log.Write("开始准备：目标 " + target + " 个图标，篮球「" + ((text.Length == 0) ? "第一个图标" : text) + "」");
			LoadAsync(target, text, token, reload: false);
		}
	}

	private void LoadAsync(int target, string ball, int token, bool reload)
	{
		StaWorker.Run(delegate
		{
			LoadResult res;
			try
			{
				res = DesktopLoader.Load(target, ball);
			}
			catch (Exception ex)
			{
				Log.Write("读取桌面出错：" + ex);
				res = new LoadResult();
				res.Diag.Add("读取桌面出错：" + ex.Message);
			}
			dispatcher.BeginInvoke((Action)delegate
			{
				OnLoaded(res, token, target, reload);
			});
		});
	}

	private void OnLoaded(LoadResult res, int token, int target, bool reload)
	{
		if (token != prepareToken)
		{
			return;
		}
		if (reload)
		{
			reloading = false;
			if (State != DanceState.Running)
			{
				this.Changed?.Invoke();
				return;
			}
		}
		else if (State != DanceState.Preparing)
		{
			return;
		}
		foreach (string item in res.Diag)
		{
			Log.Write("  " + item);
		}
		if (res.Items.Count == 0)
		{
			note = new Msg("没有可用的图标（详情见日志）", "No icons available (see the log for details)");
			if (reload)
			{
				loadedTarget = target;
				this.Changed?.Invoke();
			}
			else
			{
				SetState(DanceState.Idle);
			}
			return;
		}
		if (reload && load != null && load.PositionsFromShell && res.PositionsFromShell)
		{
			res.Snapshot = load.Snapshot;
		}
		loadedTarget = target;
		load = res;
		items = res.Items;
		DesktopCount = res.DesktopCount;
		dancerCount = Math.Min(items.Count, Math.Max(1, target));
		int num = ((ballName.Length > 0) ? items.FindIndex((DancerItem it) => DesktopLoader.Matches(it.Name, ballName)) : (-1));
		if (num >= dancerCount)
		{
			DancerItem value = items[num];
			items[num] = items[dancerCount - 1];
			items[dancerCount - 1] = value;
		}
		bystanders = 0;
		for (int num2 = dancerCount; num2 < items.Count; num2++)
		{
			if (!items[num2].IsGuest)
			{
				bystanders++;
			}
		}
		ScreenInfo screen = res.Screen;
		double scale = screen.Scale;
		stageW = (double)screen.MonW / scale;
		stageH = (double)screen.MonH / scale;
		visibleLocal = new RectD((double)(screen.WorkX - screen.MonX) / scale, (double)(screen.MonY + screen.MonH - screen.WorkY - screen.WorkH) / scale, (double)screen.WorkW / scale, (double)screen.WorkH / scale);
		AssignHomes(res);
		curtainNote = res.CurtainNote;
		curtainProblem = res.CurtainProblem;
		try
		{
			StageHost stageHost = new StageHost(stageW, stageH, res.IconDip, res.CellW - 6.0, scale, items);
			stageHost.SetCurtain(res.Curtain, res.CurtainColor);
			StageWindow stageWindow = new StageWindow(screen, stageHost);
			stageWindow.Clicked += delegate
			{
				Log.Write("点击桌面，停止");
				Stop(animated: true);
			};
			Begin(stageWindow, res, reload);
		}
		catch (Exception ex)
		{
			Log.Write("搭舞台出错：" + ex);
			Teardown(new Msg("搭舞台出错：" + ex.Message, "Could not set up the stage: " + ex.Message));
		}
	}

	private void AssignHomes(LoadResult res)
	{
		List<Vec> list = new List<Vec>();
		foreach (DancerItem item in items)
		{
			if (item.HasPos)
			{
				list.Add(item.Home);
			}
		}
		int num = 0;
		foreach (DancerItem item2 in items)
		{
			if (!item2.HasPos)
			{
				Vec p = GridHome(num, res);
				int num2 = 0;
				while (num2 < 600 && list.Exists((Vec t) => Math.Abs(t.X - p.X) < res.CellW * 0.9 && Math.Abs(t.Y - p.Y) < res.CellH * 0.9))
				{
					num++;
					num2++;
					p = GridHome(num, res);
				}
				item2.Home = p;
				list.Add(p);
				num++;
			}
		}
	}

	private Vec GridHome(int slot, LoadResult res)
	{
		RectD rectD = visibleLocal;
		int num = Math.Max(1, (int)((rectD.H - 10.0) / res.CellH));
		int num2 = slot / num;
		int num3 = slot % num;
		return new Vec(rectD.MinX + res.CellW / 2.0 + (double)num2 * res.CellW, rectD.MaxY - 6.0 - res.IconDip / 2.0 - (double)num3 * res.CellH);
	}

	private void Begin(StageWindow wnd, LoadResult res, bool reload)
	{
		StageWindow old = stage;
		if (old != null)
		{
			CloseStaleStage();
			staleStage = old;
			staleDeadline = Clock.Now + 1.0;
			wnd.ContentRendered += delegate
			{
				if (staleStage == old)
				{
					staleDeadline = Math.Min(staleDeadline, Clock.Now + 0.1);
				}
			};
		}
		stage = wnd;
		Vec[] array = new Vec[items.Count];
		for (int num = 0; num < items.Count; num++)
		{
			array[num] = items[num].Home;
		}
		choreo.Homes = array;
		choreo.Amp = amp;
		choreo.IconSize = res.IconDip;
		choreo.GridW = res.CellW;
		choreo.GridH = res.CellH;
		choreo.Clip = MotionClip.LoadDefault();
		UpdateBall();
		LayoutStage(animated: false);
		if (!reload)
		{
			clock.Reset();
			analyzer.Reset();
			tempoRate = 1.0;
		}
		Pose[] array2 = HomePoses(0.0);
		choreo.SetMode(mode, clock.Beat, Dancing(array2));
		wnd.Host.Apply(array2);
		lastPoses = array2;
		RefreshLabels();
		wnd.Open();
		if (staleStage != null)
		{
			staleDeadline = Clock.Now + 1.0;
		}
		stageUp = true;
		layerNote = wnd.LayerProblem;
		BeginTransition(array2, 1.1, 0.5);
		HotKeys.SetEscape(on: true);
		modeStartBeat = clock.Beat;
		frames = 0;
		lastTick = Clock.Now;
		fpsStamp = lastTick;
		if (!ticking)
		{
			CompositionTarget.Rendering += OnRendering;
			ticking = true;
		}
		note = Msg.None;
		SetState(DanceState.Running);
		if (!reload)
		{
			StartAudio();
		}
		if (NeedsReload)
		{
			ScheduleReload();
		}
		Log.Write("打篮球动作：" + ((choreo.Clip == null) ? "手写动作（没有动作数据）" : ("动作数据 " + choreo.Clip.Duration.ToString("0.0") + " 秒")));
		Log.Write((reload ? "重新排队：" : "开始：") + "跳舞 " + dancerCount + " 个，留在原位 " + bystanders + " 个；桌面图标 " + DesktopCount + " 个，补位 " + GuestCount + " 个，模式 " + DanceModes.LogName(mode) + "，" + curtainNote + "，图标位置" + (res.PositionsFromShell ? "来自资源管理器" : "未知（从默认网格出发）"));
	}

	public void TogglePause()
	{
		if (State == DanceState.Running)
		{
			SetState(DanceState.Paused);
		}
		else if (State == DanceState.Paused)
		{
			lastTick = Clock.Now;
			SetState(DanceState.Running);
			if (NeedsReload)
			{
				ScheduleReload();
			}
		}
	}

	private void ScheduleReload()
	{
		reloadTimer?.Stop();
		if (State != DanceState.Running)
		{
			return;
		}
		if (reloadTimer == null)
		{
			DispatcherTimer t = new DispatcherTimer(DispatcherPriority.Normal, dispatcher)
			{
				Interval = TimeSpan.FromSeconds(0.35)
			};
			t.Tick += delegate
			{
				t.Stop();
				ReloadItems();
			};
			reloadTimer = t;
		}
		reloadTimer.Start();
	}

	private void ReloadItems()
	{
		if (State == DanceState.Running)
		{
			int target = targetCount;
			int token = ++prepareToken;
			reloading = false;
			if (!NeedsReload)
			{
				this.Changed?.Invoke();
				return;
			}
			reloadForced = false;
			reloading = true;
			this.Changed?.Invoke();
			Log.Write("数量改为 " + target + "，重新排队");
			LoadAsync(target, ballName, token, reload: true);
		}
	}

	private void CloseStaleStage()
	{
		StageWindow stageWindow = staleStage;
		staleStage = null;
		if (stageWindow == null)
		{
			return;
		}
		try
		{
			stageWindow.Close();
		}
		catch (Exception ex)
		{
			Log.Write("关旧舞台窗口出错：" + ex.Message);
		}
	}

	public void Stop(bool animated)
	{
		if (State != DanceState.Idle)
		{
			if (State == DanceState.Running && animated)
			{
				StopAudio();
				State = DanceState.Stopping;
				BeginTransition(lastPoses, 0.45, 0.1);
				this.Changed?.Invoke();
			}
			else
			{
				Teardown();
			}
		}
	}

	public void EmergencyRestore(Msg? why = null)
	{
		if (State != DanceState.Idle || stage != null)
		{
			Teardown(why);
		}
	}

	private void Teardown(Msg? problem = null)
	{
		prepareToken++;
		if (ticking)
		{
			CompositionTarget.Rendering -= OnRendering;
			ticking = false;
		}
		StopAudio();
		reloadTimer?.Stop();
		reloadForced = false;
		reloading = false;
		StageWindow stageWindow = stage;
		stage = null;
		if (stageWindow != null)
		{
			try
			{
				stageWindow.Close();
			}
			catch (Exception ex)
			{
				Log.Write("关舞台窗口出错：" + ex.Message);
			}
		}
		CloseStaleStage();
		stageUp = false;
		HotKeys.SetEscape(on: false);
		Dictionary<string, POINT> dictionary = ((load != null && load.PositionsFromShell) ? load.Snapshot : null);
		load = null;
		items = new List<DancerItem>();
		lastPoses = Array.Empty<Pose>();
		transFrom = null;
		Fps = 0;
		note = problem ?? Msg.None;
		SetState(DanceState.Idle);
		Log.Write("已恢复桌面" + (problem.HasValue ? ("（" + problem.Value.Zh + "）") : ""));
		if (dictionary != null && dictionary.Count > 0)
		{
			VerifyPositions(dictionary);
		}
		RefreshCatalog();
	}

	private void VerifyPositions(Dictionary<string, POINT> before)
	{
		StaWorker.Run(delegate
		{
			Dictionary<string, POINT> dictionary = DesktopLoader.ReadPositions();
			Msg shown = Msg.None;
			string text;
			if (dictionary == null)
			{
				text = "";
			}
			else
			{
				int num = 0;
				int num2 = 0;
				foreach (KeyValuePair<string, POINT> item in before)
				{
					if (dictionary.TryGetValue(item.Key, out var value))
					{
						if (value.X == item.Value.X && value.Y == item.Value.Y)
						{
							num2++;
						}
						else
						{
							num++;
						}
					}
				}
				if (num > 0)
				{
					shown = new Msg("注意：有 " + num + " 个桌面图标的位置和开始前不一样（本程序不会移动图标，可能是系统或你自己动的）", "Note: " + num + " desktop icon(s) are not where they were before the start (this app never moves icons; the system or you may have moved them)");
				}
				text = ((num == 0) ? ("已复核：" + num2 + " 个桌面图标的位置和开始前一样") : ("注意：有 " + num + " 个桌面图标的位置和开始前不一样（本程序不会移动图标，可能是系统或你自己动的）"));
			}
			if (text.Length > 0)
			{
				Log.Write(text);
			}
			dispatcher.BeginInvoke((Action)delegate
			{
				verifyNote = shown;
				this.Changed?.Invoke();
			});
		});
	}

	private void StartAudio()
	{
		analyzer.Reset();
		if (!useSystemAudio)
		{
			audioNote = AudioBuiltIn;
			return;
		}
		audioNote = AudioConnecting;
		audioChain = audioChain.ContinueWith(delegate
		{
			Msg? err = sysAudio.Start();
			dispatcher.BeginInvoke((Action)delegate
			{
				if (!IsActive || !useSystemAudio)
				{
					StopAudio();
				}
				else
				{
					if (err.HasValue)
					{
						audioNote = AudioBuiltIn;
						note = new Msg("接不上系统声音（" + err.Value.Zh + "），先用内置节拍", "Could not connect to system audio (" + err.Value.En + "); using the built-in beat");
					}
					else
					{
						audioNote = AudioFollowing;
					}
					this.Changed?.Invoke();
				}
			});
		}, TaskScheduler.Default);
	}

	private void StopAudio()
	{
		audioChain = audioChain.ContinueWith(delegate
		{
			try
			{
				sysAudio.Stop();
			}
			catch (Exception ex)
			{
				Log.Write("停止系统声音出错：" + ex.Message);
			}
		}, TaskScheduler.Default);
	}

	private void RestartAudio()
	{
		StopAudio();
		StartAudio();
		this.Changed?.Invoke();
	}

	private void UpdateBall()
	{
		int ballIndex = 0;
		if (ballName.Length > 0)
		{
			int num = items.FindIndex((DancerItem it) => DesktopLoader.Matches(it.Name, ballName));
			if (num >= 0 && num < dancerCount)
			{
				ballIndex = num;
			}
		}
		choreo.BallIndex = ballIndex;
		RefreshLabels();
	}

	private bool BallIsDancing()
	{
		int num = items.FindIndex((DancerItem it) => DesktopLoader.Matches(it.Name, ballName));
		if (num >= 0)
		{
			return num < dancerCount;
		}
		return false;
	}

	private void RefreshLabels()
	{
		stage?.Host.SetLabelsVisible(showLabels, (mode == DanceMode.Figure) ? choreo.BallIndex : (-1));
	}

	public void RefreshCatalog()
	{
		StaWorker.Run(delegate
		{
			NameCatalog cat = DesktopLoader.ListNames();
			dispatcher.BeginInvoke((Action)delegate
			{
				Catalog = cat;
				CatalogLoaded = true;
				this.CatalogChanged?.Invoke();
				this.Changed?.Invoke();
			});
		});
	}

	public void SelfTestSetCatalog(NameCatalog cat)
	{
		Catalog = cat;
		CatalogLoaded = true;
		this.CatalogChanged?.Invoke();
		this.Changed?.Invoke();
	}

	public List<string> CandidateNames()
	{
		List<string> list = new List<string>(Catalog.Desktop);
		int num = Math.Max(0, TargetCount - Catalog.Desktop.Count);
		for (int i = 0; i < Catalog.Guests.Count && i < num; i++)
		{
			list.Add(Catalog.Guests[i]);
		}
		return list;
	}

	private Pose[] HomePoses(double guestAlpha)
	{
		Pose[] array = new Pose[items.Count];
		for (int i = 0; i < items.Count; i++)
		{
			array[i] = new Pose(items[i].Home, 0.0, 1.0, 1.0, items[i].IsGuest ? guestAlpha : 1.0, i);
		}
		return array;
	}

	private Pose[] Dancing(Pose[] all)
	{
		if (all.Length <= dancerCount)
		{
			return all;
		}
		Pose[] array = new Pose[dancerCount];
		Array.Copy(all, array, dancerCount);
		return array;
	}

	private Pose[] WithBystanders(Pose[] dancing)
	{
		if (dancing.Length >= items.Count)
		{
			return dancing;
		}
		Pose[] array = new Pose[items.Count];
		Array.Copy(dancing, array, dancing.Length);
		for (int i = dancing.Length; i < array.Length; i++)
		{
			array[i] = new Pose(items[i].Home, 0.0, 1.0, 1.0, (!items[i].IsGuest) ? 1 : 0, i - 10000);
		}
		return array;
	}

	private void BeginTransition(Pose[] startPoses, double dur, double stagger)
	{
		transFrom = startPoses;
		transStart = Clock.Now;
		transDur = dur;
		transStagger = stagger;
	}

	private void ModeChanged()
	{
		if (IsActive)
		{
			choreo.SetMode(mode, clock.Beat, Dancing(lastPoses));
			modeStartBeat = clock.Beat;
			BeginTransition(lastPoses, 0.7, 0.25);
			RefreshLabels();
			this.Changed?.Invoke();
		}
	}

	private void LayoutStage(bool animated)
	{
		StageWindow stageWindow = stage;
		if (stageWindow != null)
		{
			RectD rectD = visibleLocal;
			RectD rectD2 = rectD;
			RectD? rectD3 = null;
			if (portrait)
			{
				double num = Math.Min(rectD.W, M.Round(rectD.H * 9.0 / 16.0));
				rectD2 = new RectD(M.Round(rectD.MidX - num / 2.0), rectD.MinY, num, rectD.H);
				rectD3 = rectD2.Inset(1.0, 1.0);
			}
			RectD rectD4 = rectD2.Inset(portrait ? 30 : 56, 0.0);
			rectD4.Y += 40.0;
			rectD4.H -= 70.0;
			choreo.Stage = rectD4;
			choreo.GrooveUsesHomes = !portrait;
			stageWindow.Host.SetGuide((showGuide && portrait) ? rectD3 : ((RectD?)null));
			if (animated && State == DanceState.Running)
			{
				BeginTransition(lastPoses, 0.6, 0.2);
			}
		}
	}

	private void OnRendering(object? sender, EventArgs e)
	{
		double now = Clock.Now;
		double num = Math.Min(Math.Max(now - lastTick, 0.0), 0.05);
		if (num <= 0.0)
		{
			return;
		}
		lastTick = now;
		StageWindow stageWindow = stage;
		if (staleStage != null && now >= staleDeadline)
		{
			CloseStaleStage();
		}
		if ((State != DanceState.Running && State != DanceState.Stopping) || stageWindow == null)
		{
			return;
		}
		frames++;
		if (now - fpsStamp >= 1.0)
		{
			int num2 = (int)M.Round((double)frames / (now - fpsStamp));
			frames = 0;
			fpsStamp = now;
			bool num3 = num2 != Fps;
			Fps = num2;
			if (num3 || State == DanceState.Running)
			{
				this.Changed?.Invoke();
			}
		}
		Pose[] array;
		if (State == DanceState.Stopping)
		{
			array = HomePoses(0.0);
		}
		else
		{
			BeatAnalyzer.Features f = analyzer.Drain(now);
			double dBeat = clock.Update(num, now, f, Speed);
			double num4 = ((useSystemAudio && sysAudio.Running && sysAudio.Hearing && clock.Locked) ? Math.Min(Math.Max(clock.Bpm / 100.0, 0.75), 1.4) : 1.0);
			tempoRate += (num4 - tempoRate) * Math.Min(1.0, num / 2.0);
			choreo.ClipRate = Speed * tempoRate;
			if (autoSwitch && mode != DanceMode.Rehearsal && clock.Beat - modeStartBeat >= DanceModes.AutoBeats(mode))
			{
				DanceMode[] all = DanceModes.All;
				Mode = all[(Array.IndexOf(all, mode) + 1) % all.Length];
			}
			array = WithBystanders(choreo.Poses(dancerCount, new Tempo
			{
				Beat = clock.Beat,
				DBeat = dBeat,
				Dt = num,
				Energy = clock.Energy,
				Pulse = clock.Pulse,
				Accent = clock.Accent,
				Stall = clock.Stall
			}));
		}
		Pose[] array2 = transFrom;
		if (array2 != null && array2.Length == array.Length)
		{
			double num5 = now - transStart;
			int num6 = Math.Max(array.Length, 1);
			for (int i = 0; i < array.Length && i < dancerCount; i++)
			{
				double t = (num5 - transStagger * (double)i / (double)num6) / transDur;
				array[i] = Pose.Mix(array2[i], array[i], M.Smooth(t));
			}
			if (num5 >= transDur + transStagger)
			{
				transFrom = null;
				if (State == DanceState.Stopping)
				{
					Teardown();
					return;
				}
			}
		}
		else if (State == DanceState.Stopping)
		{
			Teardown();
			return;
		}
		RectD rectD = visibleLocal.Inset(22.0, 22.0);
		for (int j = 0; j < array.Length && j < dancerCount; j++)
		{
			Vec p = array[j].P;
			array[j].P = new Vec(M.Clamp(p.X, rectD.MinX, rectD.MaxX), M.Clamp(p.Y, rectD.MinY, rectD.MaxY));
		}
		stageWindow.Host.Apply(array);
		lastPoses = array;
	}
}
