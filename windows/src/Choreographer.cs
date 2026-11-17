using System;
using System.Collections.Generic;

namespace DesktopDance;

public sealed class Choreographer
{
	private sealed class ClipMeans
	{
		public Vec Root;

		public Vec Ball;

		public Vec[] Offs = Array.Empty<Vec>();
	}

	private struct Skeleton
	{
		public Vec Head;

		public Vec Neck;

		public Vec Hip;

		public Vec ShP;

		public Vec ElbP;

		public Vec HandP;

		public Vec ShM;

		public Vec ElbM;

		public Vec HandM;

		public Vec HipP;

		public Vec KneeP;

		public Vec FootP;

		public Vec HipM;

		public Vec KneeM;

		public Vec FootM;

		public Vec Ball;

		public double Lean;

		public double BallAlpha;

		public double FootRotP;

		public double FootRotM;
	}

	private struct Body
	{
		public double Lean;

		public double HipX;

		public double Dip;

		public double Torso;

		public double Head;

		public double Shrug;

		public Vec HandP;

		public Vec HandM;

		public Vec FootP;

		public Vec FootM;

		public double ElbP;

		public double ElbM;

		public double KneP;

		public double KneM;

		public double FootRotP;

		public double FootRotM;

		public double Rod;
	}

	private struct BKey
	{
		public double F;

		public Body B;
	}

	private struct FootKey
	{
		public double F;

		public double LX;

		public double LY;

		public double RX;

		public double RY;
	}

	private struct ShiverKey
	{
		public double F;

		public double Lean;

		public double Head;

		public double ShL;

		public double ShR;

		public double AUL;

		public double AFL;

		public double AUR;

		public double AFR;

		public double HL;

		public double HR;

		public double Knee;

		public double Trem;

		public double Freq;
	}

	private struct ShiverFoot
	{
		public double F;

		public double LX;

		public double LY;

		public double RX;

		public double RY;

		public int Step;
	}

	private struct ShiverPose
	{
		public double Lean;

		public double Head;

		public double ShL;

		public double ShR;

		public double AUL;

		public double AFL;

		public double AUR;

		public double AFR;

		public double HL;

		public double HR;

		public double Knee;

		public double Trem;

		public double Freq;

		public double X;

		public double FLx;

		public double FLy;

		public double FRx;

		public double FRy;

		public double Lift;

		public int Step;
	}

	public RectD Stage;

	public double IconSize = 48.0;

	public double Amp = 1.0;

	public Vec[] Homes = Array.Empty<Vec>();

	public bool GrooveUsesHomes = true;

	public int BallIndex;

	public double GridW = 76.0;

	public double GridH = 100.0;

	public double ClipRate = 1.0;

	private MotionClip? clipData;

	private ClipMeans? clipMeans;

	private readonly Dictionary<int, (double y, double x)> fitCache = new Dictionary<int, (double, double)>();

	private double modeBeat0;

	private double clipTime;

	private DanceMode rehearsalSegment = DanceMode.Flash;

	private Vec[] freePos = Array.Empty<Vec>();

	private Vec[] freeVel = Array.Empty<Vec>();

	private double figPhase;

	private bool figInit;

	private Vec handA = Vec.Zero;

	private Vec handP = Vec.Zero;

	private double leanS;

	private double hipS;

	private const double EngineFramesPerBeat = 18.0;

	private const double SnapFrames = 8.0;

	private static readonly ShiverKey[] ShiverKeys = new ShiverKey[23]
	{
		SK(0.0, 0.0, 0.0, 0.0, 0.0, -11.0, 9.0, 11.0, -9.0, 0.35, 0.35, 0.25, 0.0, 4.5),
		SK(18.0, 0.0, 1.0, 0.0, 0.0, -11.0, 9.0, 11.0, -9.0, 0.35, 0.35, 0.33, 0.0, 4.5),
		SK(36.0, 0.0, -3.0, 0.0, 0.0, -16.0, 12.0, 16.0, -12.0, 0.35, 0.35, 0.2, 0.0, 4.5),
		SK(54.0, 0.0, 1.0, 0.0, 0.0, -11.0, 9.0, 11.0, -9.0, 0.35, 0.35, 0.38, 0.0, 4.5),
		SK(72.0, 0.0, 0.0, 0.0, 0.0, -16.0, 14.0, 16.0, -14.0, 0.35, 0.35, 0.3, 0.0, 4.5),
		SK(78.0, 0.0, 0.0, 0.0, 0.0, 180.0, -45.0, -180.0, 45.0, 0.95, 0.95, 0.18, 0.0, 4.5),
		SK(84.0, 3.0, 0.0, 0.0, 0.0, 70.0, -85.0, -70.0, 85.0, 0.7, 0.7, 0.66, 0.0, 4.5),
		SK(96.0, 0.0, 0.0, 0.0, 0.0, 70.0, -85.0, -70.0, 85.0, 0.35, 0.35, 0.3, 0.0, 4.5),
		SK(108.0, 0.0, 0.0, 0.0, 0.0, -16.0, 14.0, 16.0, -14.0, 0.35, 0.35, 0.3, 0.0, 4.5),
		SK(114.0, 0.0, 0.0, 0.0, 0.0, 180.0, -45.0, -180.0, 45.0, 0.95, 0.95, 0.18, 0.0, 4.5),
		SK(120.0, -3.0, 0.0, 0.0, 0.0, 70.0, -85.0, -70.0, 85.0, 0.7, 0.7, 0.66, 0.0, 4.5),
		SK(132.0, 0.0, 0.0, 0.0, 0.0, 70.0, -85.0, -70.0, 85.0, 0.35, 0.35, 0.3, 0.0, 4.5),
		SK(144.0, 0.0, 0.0, 0.0, 0.0, -11.0, 9.0, 11.0, -9.0, 0.35, 0.35, 0.42, 0.05, 4.6),
		SK(216.0, 0.0, 0.0, 0.0, 0.0, -11.0, 9.0, 11.0, -9.0, 0.35, 0.35, 0.44, 0.2, 5.2),
		SK(288.0, 0.0, 0.0, 0.0, 0.0, -11.0, 9.0, 11.0, -9.0, 0.35, 0.35, 0.46, 0.3, 5.8),
		SK(360.0, 0.0, 0.0, 0.0, 0.0, -90.0, -6.0, 90.0, 6.0, 1.0, 1.0, 0.54, 0.48, 6.6),
		SK(432.0, 0.0, 0.0, 0.0, 0.0, -90.0, -6.0, 90.0, 6.0, 1.0, 1.0, 0.68, 0.88, 8.4),
		SK(480.0, 0.0, 0.0, 0.0, 0.0, -90.0, -6.0, 90.0, 6.0, 1.0, 1.0, 0.76, 1.15, 9.4),
		SK(504.0, 0.0, 0.0, 0.0, 0.0, -11.0, 9.0, 11.0, -9.0, 0.35, 0.35, 0.72, 0.95, 8.6),
		SK(522.0, 0.0, 0.0, 0.0, 0.0, -24.0, 12.0, 24.0, -12.0, 0.4, 0.4, 0.56, 0.28, 6.2),
		SK(534.0, 0.0, 0.0, 0.0, 0.0, 70.0, -85.0, -70.0, 85.0, 0.35, 0.35, 0.82, 0.06, 5.0),
		SK(552.0, 0.0, 0.0, 0.0, 0.0, -11.0, 9.0, 11.0, -9.0, 0.35, 0.35, 0.35, 0.0, 4.5),
		SK(600.0, 0.0, 0.0, 0.0, 0.0, -11.0, 9.0, 11.0, -9.0, 0.35, 0.35, 0.25, 0.0, 4.5)
	};

	private static readonly ShiverFoot[] ShiverFootKeys = new ShiverFoot[12]
	{
		SF(0.0, -36.0, 118.0, 36.0, 118.0, 0),
		SF(216.0, -36.0, 118.0, 36.0, 118.0, 1),
		SF(234.0, -8.0, 106.0, 36.0, 118.0, 2),
		SF(252.0, -8.0, 106.0, 2.0, 112.0, 1),
		SF(270.0, -52.0, 118.0, 2.0, 112.0, 2),
		SF(288.0, -52.0, 118.0, 44.0, 128.0, 1),
		SF(306.0, -8.0, 116.0, 44.0, 128.0, 2),
		SF(324.0, -8.0, 116.0, 10.0, 106.0, 1),
		SF(342.0, -46.0, 118.0, 10.0, 106.0, 2),
		SF(360.0, -46.0, 118.0, 48.0, 126.0, 1),
		SF(378.0, -36.0, 118.0, 36.0, 118.0, 0),
		SF(600.0, -36.0, 118.0, 36.0, 118.0, 0)
	};

	private const double ArmUpUL = 180.0;

	private const double ArmUpFL = -45.0;

	private const double ArmPushUL = -90.0;

	private static readonly double[] ShiverPhase = BuildPhase(600, (double f) => ShiverPoseAt(f).Freq);

	private const double FlashFrames = 540.0;

	private static readonly double[] FlashPhase = BuildPhase(540, (double f) => FlashFreqAt(f));

	private const double MoonFrames = 600.0;

	private static readonly double[] MoonPhase = BuildPhase(600, (double f) => 5.0);

	private const double Subj3Frames = 540.0;

	private const double FlowerFastHz = 3.75;

	private const double FlowerAmp = 32.0;

	private const double FlowerFastAmp = 55.0;

	private const double FlowerRadius = 15.0;

	private const int FlowerHotS = 318;

	private const int FlowerHotE = 366;

	private static readonly double[] Subj3Phase = BuildPhase(540, (double f) => (f >= 318.0 && f < 366.0) ? 3.75 : 2.5);

	private static readonly DanceMode[] ReelModes = new DanceMode[5]
	{
		DanceMode.Flash,
		DanceMode.Shiver,
		DanceMode.Moonwalk,
		DanceMode.Subject3,
		DanceMode.Figure
	};

	private static readonly double[] ReelBeats = new double[5] { 4.0, 33.333333333333336, 8.0, 8.0, 28.0 };

	public DanceMode Mode { get; private set; }

	public MotionClip? Clip
	{
		get
		{
			return clipData;
		}
		set
		{
			clipData = value;
			clipMeans = null;
			fitCache.Clear();
		}
	}

	public DanceMode RehearsalSegment => rehearsalSegment;

	public double BaseScale
	{
		get
		{
			if (!(Stage.H > Stage.W))
			{
				return 1.0;
			}
			return 0.78;
		}
	}

	public void SetMode(DanceMode m, double beat, Pose[] current)
	{
		Mode = m;
		modeBeat0 = beat;
		freePos = new Vec[current.Length];
		for (int i = 0; i < current.Length; i++)
		{
			freePos[i] = current[i].P;
		}
		freeVel = new Vec[current.Length];
		figPhase = 0.0;
		figInit = false;
		clipTime = 0.0;
	}

	public Pose[] Poses(int n, Tempo tempo)
	{
		if (n <= 0)
		{
			return Array.Empty<Pose>();
		}
		if (tempo.Stall > 0.0)
		{
			tempo.Dt *= 1.0 - tempo.Stall;
			tempo.Pulse *= 1.0 - tempo.Stall;
			tempo.Accent *= 1.0 - tempo.Stall;
		}
		Pose[] array = Mode switch
		{
			DanceMode.Snake => Snake(n, tempo), 
			DanceMode.Figure => Figure(n, tempo), 
			DanceMode.Groove => Groove(n, tempo), 
			DanceMode.Shiver => Shiver(n, tempo), 
			DanceMode.Flash => Flash(n, tempo), 
			DanceMode.Moonwalk => Moonwalk(n, tempo), 
			DanceMode.Subject3 => Subject3(n, tempo), 
			DanceMode.Rehearsal => Rehearsal(n, tempo), 
			_ => Free(n, tempo), 
		};
		if (Mode != DanceMode.Figure && (Mode != DanceMode.Rehearsal || rehearsalSegment != DanceMode.Figure))
		{
			ApplyBeatGroove(array, tempo);
		}
		return array;
	}

	private void ApplyBeatGroove(Pose[] poses, Tempo T)
	{
		double num = Math.Min(Amp, 1.6);
		double num2 = M.Clamp(T.Accent * 0.85 + T.Pulse * 0.35, 0.0, 1.25);
		if (!(num2 < 0.002))
		{
			double num3 = Math.Floor(T.Beat / 4.0);
			double num4 = ((num3 - Math.Floor(num3 / 2.0) * 2.0 == 0.0) ? 1.0 : (-1.0));
			double num5 = IconSize * BaseScale;
			double num6 = 1.0 + 0.22 * num * num2;
			double num7 = 1.0 - 0.12 * num * num2;
			double num8 = num5 * 0.16 * num * num2;
			double num9 = num5 * 0.12 * num * num2 * num4;
			double num10 = 0.045 * num * num2;
			double num11 = 0.08 * num * num2 * num4;
			RectD stage = Stage;
			double midX = stage.MidX;
			double midY = stage.MidY;
			for (int i = 0; i < poses.Length; i++)
			{
				Pose pose = poses[i];
				double x = (pose.P.X - midX) * (1.0 + num10) + midX + num9;
				double y = (pose.P.Y - midY) * (1.0 + num10) + midY - num8;
				poses[i] = new Pose(new Vec(x, y), pose.Rot + num11, pose.Sx * num6, pose.Sy * num7, pose.Alpha, pose.Z);
			}
		}
	}

	private Pose[] Snake(int n, Tempo T)
	{
		RectD stage = Stage;
		double num2;
		double num = (num2 = T.Beat - modeBeat0);
		double energy = T.Energy;
		double pulse = T.Pulse;
		double midX = stage.MidX;
		double midY = stage.MidY;
		bool flag = stage.H > stage.W;
		double baseScale = BaseScale;
		double num3 = IconSize * 0.55 * baseScale;
		double num4 = Amp * IconSize * baseScale * (0.45 + 0.9 * energy);
		double a = Math.Min(stage.H * 0.28, (double)n * IconSize * 0.15);
		double b = stage.H - num3 * 2.0;
		double t = M.Smooth((num - 4.0) / 4.0);
		double t2 = M.Smooth((num - 8.0) / 3.0);
		double t3 = 0.0;
		double num5 = 1.0;
		double num6 = num - 11.0;
		double m;
		if (num6 > 0.0)
		{
			m = num6 % 48.0;
			t3 = Bump(14.0, 16.0, 22.0, 24.0) + Bump(38.0, 40.0, 46.0, 48.0);
			if (m >= 19.0 && m < 43.0)
			{
				num5 = -1.0;
			}
		}
		double num7 = M.Lerp(a, b, t);
		int num8 = Math.Max(1, (int)((double)n * 0.42));
		int num9 = n - num8;
		int num10 = Math.Max(1, (num9 + 1) / 2);
		Vec a2 = new Vec(midX, midY - stage.H * 0.06);
		double num11 = Math.Min(stage.W * 0.4, stage.H * 0.46) * (1.0 + 0.06 * Math.Sin(Math.PI * num2));
		double num12 = num2 * 0.1;
		Pose[] array = new Pose[n];
		for (int i = 0; i < n; i++)
		{
			double num13 = ((n > 1) ? ((double)i / (double)(n - 1)) : 0.5);
			double num14 = num13 + 0.01 * Amp * Math.Sin(Math.PI * 2.0 * (num2 / 4.0 - num13 * 2.0));
			double num15 = Math.PI * num2 - 10.053096491487338 * num14;
			double num16 = num4 * (0.45 * Math.Sin(Math.PI * num2) + 0.75 * Math.Sin(num15));
			Vec a3 = new Vec(midX + num16 * M.Lerp(0.55, 1.0, t), midY + (0.5 - num14) * num7);
			Vec b2 = ((!flag) ? new Vec(stage.MinX + num3 + num14 * (stage.W - num3 * 2.0), midY + num5 * stage.H * 0.3 * Math.Sin(7.853981633974483 * num14 + num12) + num16 * 0.8) : new Vec(midX + num5 * stage.W * 0.3 * Math.Sin(Math.PI * 3.0 * num14 + num12) + num16 * 0.8, stage.MaxY - num3 - num14 * (stage.H - num3 * 2.0)));
			Vec vec;
			if (i >= num9)
			{
				vec = M.Lerp(t: (double)(i - num9 + 1) / (double)num8, a: a2, b: new Vec(midX, stage.MinY + num3));
				vec = new Vec(vec.X + num16 * 0.6, vec.Y);
			}
			else
			{
				double num17 = ((i % 2 != 0) ? 1 : (-1));
				vec = M.Lerp(t: 1.0 - (double)(i / 2) / (double)num10, a: a2, b: new Vec(midX + num17 * num11, stage.MaxY - num3));
				vec = new Vec(vec.X + num16 * 0.6, vec.Y + num16 * 0.25 * num17);
			}
			Vec vec2 = M.Lerp(M.Lerp(a3, b2, t2), vec, t3);
			vec2 = new Vec(vec2.X, vec2.Y + 5.0 * pulse * Amp);
			double rot = -0.2 * Amp * Math.Cos(num15) * (0.6 + 0.6 * energy);
			double num18 = baseScale * (1.0 + 0.1 * pulse + 0.04 * Math.Sin(num15));
			array[i] = new Pose(vec2, rot, num18, num18, 1.0, n - i);
		}
		return array;
		double Bump(double num19, double num20, double c1, double d)
		{
			return M.Smooth((m - num19) / (num20 - num19)) * (1.0 - M.Smooth((m - c1) / (d - c1)));
		}
	}

	private static void Ik(Vec a, Vec target, double l1, double l2, double bend, out Vec joint, out Vec end)
	{
		Vec vec = target - a;
		double num = M.Hypot(vec.X, vec.Y);
		double num2 = (l1 + l2) * 0.999;
		if (num > num2)
		{
			vec *= num2 / num;
			num = num2;
		}
		if (num < 0.0001)
		{
			joint = new Vec(a.X + l1, a.Y);
			end = a;
			return;
		}
		double num3 = (l1 * l1 - l2 * l2 + num * num) / (2.0 * num);
		double num4 = Math.Sqrt(Math.Max(0.0, l1 * l1 - num3 * num3));
		double num5 = vec.X / num;
		double num6 = vec.Y / num;
		joint = new Vec(a.X + num5 * num3 - num6 * num4 * bend, a.Y + num6 * num3 + num5 * num4 * bend);
		end = a + vec;
	}

	private static Vec SamplePolyline(Vec[] pts, double t)
	{
		if (pts.Length == 0)
		{
			return Vec.Zero;
		}
		if (pts.Length == 1)
		{
			return pts[0];
		}
		double[] array = new double[pts.Length - 1];
		double num = 0.0;
		for (int i = 1; i < pts.Length; i++)
		{
			num += (array[i - 1] = M.Hypot(pts[i].X - pts[i - 1].X, pts[i].Y - pts[i - 1].Y));
		}
		double num2 = M.Clamp(t, 0.0, 1.0) * num;
		for (int j = 0; j < array.Length; j++)
		{
			if (num2 <= array[j] || j == array.Length - 1)
			{
				return M.Lerp(pts[j], pts[j + 1], (array[j] > 0.0) ? (num2 / array[j]) : 0.0);
			}
			num2 -= array[j];
		}
		return pts[^1];
	}

	private static int Sum(int[] a)
	{
		int num = 0;
		foreach (int num2 in a)
		{
			num += num2;
		}
		return num;
	}

	private static int[] Allocate(int total, double[] weights)
	{
		double num = 0.0;
		foreach (double num2 in weights)
		{
			num += num2;
		}
		int[] array = new int[weights.Length];
		for (int j = 0; j < weights.Length; j++)
		{
			array[j] = Math.Max(1, (int)M.Round((double)total * weights[j] / num));
		}
		int num3 = 0;
		while (Sum(array) > total && num3 < 500)
		{
			int num4 = -1;
			for (int k = 0; k < array.Length; k++)
			{
				if (array[k] > 1 && (num4 < 0 || (double)array[num4] / weights[num4] < (double)array[k] / weights[k]))
				{
					num4 = k;
				}
			}
			if (num4 < 0)
			{
				break;
			}
			array[num4]--;
			num3++;
		}
		while (Sum(array) < total && num3 < 1000)
		{
			int num5 = 0;
			for (int l = 1; l < array.Length; l++)
			{
				if ((double)array[l] / weights[l] < (double)array[num5] / weights[num5])
				{
					num5 = l;
				}
			}
			array[num5]++;
			num3++;
		}
		return array;
	}

	private Pose[] Ring(int n, Tempo T)
	{
		RectD stage = Stage;
		double num = Math.Min(stage.W, stage.H) * 0.3;
		Pose[] array = new Pose[n];
		for (int i = 0; i < n; i++)
		{
			double num2 = Math.PI * 2.0 * (double)i / (double)n - T.Beat * 0.4;
			double num3 = BaseScale * (1.0 + 0.15 * T.Pulse);
			array[i] = new Pose(new Vec(stage.MidX + num * Math.Cos(num2), stage.MidY + num * Math.Sin(num2)), 0.0, num3, num3);
		}
		return array;
	}

	private static Skeleton FromSample(MotionClip.Sample s)
	{
		Vec neck = J(Joint.Neck);
		Vec hip = J(Joint.Root);
		Vec head = (J(Joint.Nose) + J(Joint.LEar) + J(Joint.REar)) * (1.0 / 3.0);
		double lean = Math.Atan2(neck.X - hip.X, Math.Max(neck.Y - hip.Y, 0.01));
		return new Skeleton
		{
			Head = head,
			Neck = neck,
			Hip = hip,
			ShP = J(Joint.LSho),
			ElbP = J(Joint.LElb),
			HandP = J(Joint.LWri),
			ShM = J(Joint.RSho),
			ElbM = J(Joint.RElb),
			HandM = J(Joint.RWri),
			HipP = J(Joint.LHip),
			KneeP = J(Joint.LKne),
			FootP = J(Joint.LAnk),
			HipM = J(Joint.RHip),
			KneeM = J(Joint.RKne),
			FootM = J(Joint.RAnk),
			Ball = s.Ball,
			Lean = lean,
			BallAlpha = s.BallAlpha
		};
		Vec J(Joint k)
		{
			return s.Joints[(int)k];
		}
	}

	private ClipMeans Means(MotionClip clip)
	{
		if (clipMeans != null)
		{
			return clipMeans;
		}
		int num = Math.Max(2, (int)(clip.Duration * 20.0));
		Vec zero = Vec.Zero;
		Vec zero2 = Vec.Zero;
		Vec[] array = new Vec[17];
		double num2 = 0.0;
		for (int i = 0; i < num; i++)
		{
			MotionClip.Sample sample = clip.SampleAt(clip.Duration * (double)i / (double)num);
			Vec vec = sample.Joints[8];
			zero += vec;
			for (int j = 0; j < array.Length; j++)
			{
				array[j] += sample.Joints[j] - vec;
			}
			if (sample.BallAlpha > 0.5)
			{
				zero2 += sample.Ball - vec;
				num2 += 1.0;
			}
		}
		double num3 = 1.0 / (double)num;
		for (int k = 0; k < array.Length; k++)
		{
			array[k] *= num3;
		}
		return clipMeans = new ClipMeans
		{
			Root = zero * num3,
			Offs = array,
			Ball = ((num2 > 0.0) ? (zero2 * (1.0 / num2)) : Vec.Zero)
		};
	}

	private void Growth(MotionClip clip, double ex, out double growY, out double growX)
	{
		int key = (int)M.Round(ex * 50.0);
		if (fitCache.TryGetValue(key, out (double, double) value))
		{
			(growY, growX) = value;
			return;
		}
		ClipMeans m = Means(clip);
		Extent(1.0, out var maxY, out var maxX);
		Extent(ex, out var maxY2, out var maxX2);
		growY = Math.Max(1.0, maxY2 / maxY);
		growX = Math.Max(1.0, maxX2 / maxX);
		fitCache[key] = (growY, growX);
		void Extent(double e, out double reference, out double reference2)
		{
			reference = 0.001;
			reference2 = 0.001;
			int num = Math.Max(2, (int)(clip.Duration * 20.0));
			for (int i = 0; i < num; i++)
			{
				MotionClip.Sample sample = clip.SampleAt(clip.Duration * (double)i / (double)num);
				Vec vec = sample.Joints[8];
				Vec vec2 = new Vec(vec.X, m.Root.Y + (vec.Y - m.Root.Y) * e);
				for (int j = 0; j < sample.Joints.Length; j++)
				{
					Vec vec3 = vec2 + m.Offs[j] + (sample.Joints[j] - vec - m.Offs[j]) * e;
					reference = Math.Max(reference, vec3.Y);
					reference2 = Math.Max(reference2, Math.Abs(vec3.X - vec2.X));
				}
			}
		}
	}

	private Pose[] Figure(int n, Tempo T)
	{
		if (n < 8)
		{
			return Ring(n, T);
		}
		RectD stage = Stage;
		double num = 0.0;
		MotionClip clip = Clip;
		Skeleton skeleton;
		double H;
		double rot;
		if (clip != null)
		{
			clipTime += T.Dt * ClipRate;
			double num2 = ((Amp <= 1.0) ? Math.Max(Amp, 0.3) : (1.0 + (Math.Min(Amp, 2.0) - 1.0) * 0.7));
			double growY = 1.0;
			double growX = 1.0;
			if (num2 > 1.01)
			{
				Growth(clip, num2, out growY, out growX);
			}
			double num3 = clip.MaxY * growY;
			double num4 = clip.MaxLocalX * growX;
			double num5 = stage.H * 0.98 / num3;
			double num6 = stage.W * 0.5;
			double num7 = 1.0;
			if ((clip.MaxRootX + num4) * num5 > num6)
			{
				num7 = ((clip.MaxRootX > 0.001) ? M.Clamp((num6 / num5 - num4) / clip.MaxRootX, 0.0, 1.0) : 0.0);
				if (num4 * num5 > num6)
				{
					num5 = num6 / num4;
				}
			}
			MotionClip.Sample s = clip.SampleAt(clipTime);
			if (Math.Abs(num2 - 1.0) > 0.01)
			{
				ClipMeans clipMeans = Means(clip);
				Vec vec = s.Joints[8];
				Vec vec2 = new Vec(vec.X, clipMeans.Root.Y + (vec.Y - clipMeans.Root.Y) * num2);
				Vec ball = vec2 + clipMeans.Ball + (s.Ball - vec - clipMeans.Ball) * num2;
				for (int i = 0; i < s.Joints.Length; i++)
				{
					s.Joints[i] = vec2 + clipMeans.Offs[i] + (s.Joints[i] - vec - clipMeans.Offs[i]) * num2;
				}
				s.Ball = ball;
			}
			if (num7 < 1.0)
			{
				double num8 = s.Joints[8].X * (num7 - 1.0);
				for (int j = 0; j < s.Joints.Length; j++)
				{
					s.Joints[j] = new Vec(s.Joints[j].X + num8, s.Joints[j].Y);
				}
				s.Ball = new Vec(s.Ball.X + num8, s.Ball.Y);
			}
			skeleton = FromSample(s);
			H = num5;
			num = Math.Max(0.0, (stage.H * 0.98 - num3 * num5) / 2.0);
			rot = (0.0 - clipTime) * 5.0;
		}
		else
		{
			skeleton = ProceduralSkeleton(T);
			H = Math.Min(stage.H * 0.96, stage.W * 1.45);
			rot = (0.0 - figPhase) * 2.4;
		}
		Vec origin = new Vec(stage.MidX, stage.MinY + stage.H * 0.02 + num);
		double num9 = ((clip == null) ? (0.06 * Math.Sin(Math.PI * figPhase)) : 0.0);
		double lean = skeleton.Lean;
		Vec head = skeleton.Head;
		Vec neck = skeleton.Neck;
		Vec hip = skeleton.Hip;
		Vec ball2 = skeleton.Ball;
		double[] array = new double[6]
		{
			Math.PI * 17.0 / 100.0,
			0.3,
			0.4,
			0.4,
			0.43,
			0.43
		};
		int[] array2 = Allocate(n - 1, array);
		double num10 = 0.0;
		double[] array3 = array;
		foreach (double num11 in array3)
		{
			num10 += num11;
		}
		double num12 = num10 * H / (double)(n - 1);
		double k2 = M.Clamp(num12 / IconSize * 1.25, 0.34, 1.0);
		List<Pose> list = new List<Pose>(n + 4);
		double num13 = Math.Min(k2 * 1.7, 1.25);
		list.Add(new Pose(Scr(ball2), rot, num13, num13, skeleton.BallAlpha, 2000.0));
		int num14 = array2[0];
		for (int l = 0; l < num14; l++)
		{
			double num15 = Math.PI * 2.0 * (double)l / (double)num14 + Math.PI / 2.0;
			Vec p = ((num14 < 3) ? head : (head + new Vec(Math.Cos(num15), Math.Sin(num15)) * 0.085));
			list.Add(new Pose(Scr(p), lean * 0.6, k2, k2, 1.0, 900 + l));
		}
		AddLine(new Vec[2] { neck, hip }, array2[1], 500.0, lean * 0.6);
		AddLine(new Vec[4] { neck, skeleton.ShP, skeleton.ElbP, skeleton.HandP }, array2[2], 1000.0, num9);
		AddLine(new Vec[4] { neck, skeleton.ShM, skeleton.ElbM, skeleton.HandM }, array2[3], 1100.0, 0.0 - num9);
		AddLine(new Vec[3] { skeleton.HipP, skeleton.KneeP, skeleton.FootP }, array2[4], 300.0, 0.0);
		AddLine(new Vec[3] { skeleton.HipM, skeleton.KneeM, skeleton.FootM }, array2[5], 200.0, 0.0);
		while (list.Count < n)
		{
			list.Add(new Pose(Scr(hip), 0.0, k2, k2));
		}
		int num16 = Math.Min(Math.Max(BallIndex, 0), n - 1);
		Pose[] array4 = new Pose[n];
		int num17 = 1;
		for (int m = 0; m < n; m++)
		{
			if (m == num16)
			{
				array4[m] = list[0];
				continue;
			}
			array4[m] = list[num17];
			num17++;
		}
		return array4;
		void AddLine(Vec[] pts, int count, double z, double rot2)
		{
			for (int num18 = 0; num18 < count; num18++)
			{
				Vec p2 = SamplePolyline(pts, ((double)num18 + 0.5) / (double)count);
				list.Add(new Pose(Scr(p2), rot2, k2, k2, 1.0, z + (double)num18));
			}
		}
		Vec Scr(Vec vec3)
		{
			return origin + vec3 * H;
		}
	}

	private Pose[] FigureFromSkeleton(int n, Skeleton sk, double wob)
	{
		RectD stage = Stage;
		double H = Math.Min(stage.H * 0.94, stage.W * 1.3);
		Vec origin = new Vec(stage.MidX, stage.MinY + stage.H * 0.02);
		double lean = sk.Lean;
		double[] array = new double[6]
		{
			Math.PI * 17.0 / 100.0,
			0.3,
			0.4,
			0.4,
			0.43,
			0.43
		};
		int[] array2 = Allocate(n, array);
		double num = 0.0;
		double[] array3 = array;
		foreach (double num2 in array3)
		{
			num += num2;
		}
		double num3 = num * H / (double)n;
		double k = M.Clamp(num3 / IconSize * 1.25, 0.34, 1.0);
		List<Pose> list = new List<Pose>(n + 4);
		int num4 = array2[0];
		for (int j = 0; j < num4; j++)
		{
			double num5 = Math.PI * 2.0 * (double)j / (double)num4 + Math.PI / 2.0;
			Vec p = ((num4 < 3) ? sk.Head : (sk.Head + new Vec(Math.Cos(num5), Math.Sin(num5)) * 0.085));
			list.Add(new Pose(Scr(p), lean * 0.6, k, k, 1.0, 900 + j));
		}
		AddLine(new Vec[2] { sk.Neck, sk.Hip }, array2[1], 500.0, lean * 0.6);
		AddLine(new Vec[4] { sk.Neck, sk.ShP, sk.ElbP, sk.HandP }, array2[2], 1000.0, wob);
		AddLine(new Vec[4] { sk.Neck, sk.ShM, sk.ElbM, sk.HandM }, array2[3], 1100.0, 0.0 - wob);
		AddLine(new Vec[3] { sk.HipP, sk.KneeP, sk.FootP }, array2[4], 300.0, sk.FootRotP);
		AddLine(new Vec[3] { sk.HipM, sk.KneeM, sk.FootM }, array2[5], 200.0, sk.FootRotM);
		while (list.Count < n)
		{
			list.Add(new Pose(Scr(sk.Hip), 0.0, k, k));
		}
		Pose[] array4 = new Pose[n];
		for (int l = 0; l < n; l++)
		{
			array4[l] = list[l];
		}
		return array4;
		void AddLine(Vec[] pts, int count, double z, double rot)
		{
			for (int m = 0; m < count; m++)
			{
				Vec p2 = SamplePolyline(pts, ((double)m + 0.5) / (double)count);
				list.Add(new Pose(Scr(p2), rot, k, k, 1.0, z + (double)m));
			}
		}
		Vec Scr(Vec vec)
		{
			return origin + vec * H;
		}
	}

	private static Vec V(double x, double y)
	{
		return new Vec(x, y);
	}

	private static Body B(double lean, double hipX, double dip, double torso, double head, double shrug, Vec hp, Vec hm, Vec fp, Vec fm)
	{
		return new Body
		{
			Lean = lean,
			HipX = hipX,
			Dip = dip,
			Torso = torso,
			Head = head,
			Shrug = shrug,
			HandP = hp,
			HandM = hm,
			FootP = fp,
			FootM = fm,
			ElbP = 1.0,
			ElbM = -1.0,
			KneP = 1.0,
			KneM = -1.0,
			FootRotP = 0.0,
			FootRotM = 0.0,
			Rod = 0.0
		};
	}

	private static Body Rodify(Body b)
	{
		double c = Math.Cos(b.Lean);
		double s = Math.Sin(b.Lean);
		double hx = b.HipX;
		b.HandP = Rot(b.HandP);
		b.HandM = Rot(b.HandM);
		b.Rod = 1.0;
		return b;
		Vec Rot(Vec p)
		{
			double num = p.X - hx;
			double y = p.Y;
			return V(hx + num * c + y * s, (0.0 - num) * s + y * c);
		}
	}

	private static Body Mirror(Body b)
	{
		Body result = b;
		result.Lean = 0.0 - b.Lean;
		result.HipX = 0.0 - b.HipX;
		result.Torso = 0.0 - b.Torso;
		result.Head = 0.0 - b.Head;
		result.HandP = V(0.0 - b.HandM.X, b.HandM.Y);
		result.HandM = V(0.0 - b.HandP.X, b.HandP.Y);
		result.FootP = V(0.0 - b.FootM.X, b.FootM.Y);
		result.FootM = V(0.0 - b.FootP.X, b.FootP.Y);
		result.ElbP = 0.0 - b.ElbM;
		result.ElbM = 0.0 - b.ElbP;
		result.KneP = 0.0 - b.KneM;
		result.KneM = 0.0 - b.KneP;
		result.FootRotP = 0.0 - b.FootRotM;
		result.FootRotM = 0.0 - b.FootRotP;
		return result;
	}

	private Skeleton BuildSkeleton(Body b)
	{
		double num = M.Clamp(b.Lean, -0.95, 0.95);
		double num2 = M.Clamp(b.Dip, 0.0, 0.24);
		double num3 = M.Clamp(b.HipX, -0.42, 0.42);
		Vec vec = V(Math.Sin(num), Math.Cos(num));
		Vec vec2 = V(Math.Cos(num), 0.0 - Math.Sin(num));
		double t = M.Clamp(b.Rod, 0.0, 1.0);
		Vec vec3 = V(num3, 0.0) + vec * (0.4 - num2);
		Vec vec4 = V(M.Lerp(num3, vec3.X, t), M.Lerp(0.4 - num2, vec3.Y, t));
		Vec vec5 = vec4 + vec * 0.3;
		double num4 = num + M.Clamp(b.Torso, -0.6, 0.6);
		Vec vec6 = V(Math.Sin(num4), Math.Cos(num4));
		Vec vec7 = V(Math.Cos(num4), 0.0 - Math.Sin(num4));
		double num5 = num + M.Clamp(b.Head, -0.5, 0.5);
		Vec head = vec5 + V(Math.Sin(num5), Math.Cos(num5)) * 0.115;
		double num6 = M.Clamp(b.Shrug, -0.05, 0.06);
		Vec vec8 = vec5 + vec7 * 0.09 - vec6 * (0.015 - num6);
		Vec vec9 = vec5 - vec7 * 0.09 - vec6 * (0.015 - num6);
		Ik(vec8, b.HandP, 0.155, 0.155, b.ElbP, out var joint, out var end);
		Ik(vec9, b.HandM, 0.155, 0.155, b.ElbM, out var joint2, out var end2);
		Vec vec10 = vec4 + vec2 * 0.05;
		Vec vec11 = vec4 - vec2 * 0.05;
		Ik(vec10, b.FootP, 0.215, 0.215, b.KneP, out var joint3, out var end3);
		Ik(vec11, b.FootM, 0.215, 0.215, b.KneM, out var joint4, out end3);
		return new Skeleton
		{
			Head = head,
			Neck = vec5,
			Hip = vec4,
			ShP = vec8,
			ElbP = joint,
			HandP = end,
			ShM = vec9,
			ElbM = joint2,
			HandM = end2,
			HipP = vec10,
			KneeP = joint3,
			FootP = b.FootP,
			HipM = vec11,
			KneeM = joint4,
			FootM = b.FootM,
			Ball = Vec.Zero,
			Lean = num,
			BallAlpha = 0.0,
			FootRotP = b.FootRotP,
			FootRotM = b.FootRotM
		};
	}

	private static double Crm(double p0, double p1, double p2, double p3, double t)
	{
		double num = t * t;
		double num2 = num * t;
		return 0.5 * (2.0 * p1 + (p2 - p0) * t + (2.0 * p0 - 5.0 * p1 + 4.0 * p2 - p3) * num + (3.0 * p1 - p0 - 3.0 * p2 + p3) * num2);
	}

	private static Body Blend(Body a, Body b, Body c, Body d, double t)
	{
		return new Body
		{
			Lean = F(a.Lean, b.Lean, c.Lean, d.Lean),
			HipX = F(a.HipX, b.HipX, c.HipX, d.HipX),
			Dip = F(a.Dip, b.Dip, c.Dip, d.Dip),
			Torso = F(a.Torso, b.Torso, c.Torso, d.Torso),
			Head = F(a.Head, b.Head, c.Head, d.Head),
			Shrug = F(a.Shrug, b.Shrug, c.Shrug, d.Shrug),
			HandP = G(a.HandP, b.HandP, c.HandP, d.HandP),
			HandM = G(a.HandM, b.HandM, c.HandM, d.HandM),
			FootP = G(a.FootP, b.FootP, c.FootP, d.FootP),
			FootM = G(a.FootM, b.FootM, c.FootM, d.FootM),
			ElbP = F(a.ElbP, b.ElbP, c.ElbP, d.ElbP),
			ElbM = F(a.ElbM, b.ElbM, c.ElbM, d.ElbM),
			KneP = F(a.KneP, b.KneP, c.KneP, d.KneP),
			KneM = F(a.KneM, b.KneM, c.KneM, d.KneM),
			FootRotP = F(a.FootRotP, b.FootRotP, c.FootRotP, d.FootRotP),
			FootRotM = F(a.FootRotM, b.FootRotM, c.FootRotM, d.FootRotM),
			Rod = F(a.Rod, b.Rod, c.Rod, d.Rod)
		};
		double F(double p0, double p1, double p2, double p3)
		{
			return Crm(p0, p1, p2, p3, t);
		}
		Vec G(Vec p0, Vec p1, Vec p2, Vec p3)
		{
			return V(F(p0.X, p1.X, p2.X, p3.X), F(p0.Y, p1.Y, p2.Y, p3.Y));
		}
	}

	private static double EaseInOut(double t)
	{
		return t * t * (3.0 - 2.0 * t);
	}

	private static double EaseSnap(double t)
	{
		return 1.0 - Math.Pow(1.0 - t, 2.2);
	}

	private static double EaseOutQuart(double t)
	{
		return 1.0 - Math.Pow(1.0 - t, 4.0);
	}

	// 帧轴：beatFrames = 该动画每拍多少帧，total = 整轮总帧数
	private double EngineFrame(Tempo T, double beatFrames, double total)
	{
		double num = (T.Beat - modeBeat0) * beatFrames;
		num -= Math.Floor(num / total) * total;
		if (num < 0.0)
		{
			num += total;
		}
		return num;
	}

	private double EngineFrame(Tempo T, double period)
	{
		double num = (T.Beat - modeBeat0) * 18.0;
		num -= Math.Floor(num / period) * period;
		if (num < 0.0)
		{
			num += period;
		}
		return num;
	}
	private static BKey K(double f, Body b)
	{
		return new BKey
		{
			F = f,
			B = b
		};
	}

	private static FootKey FK(double f, double lx, double ly, double rx, double ry)
	{
		return new FootKey
		{
			F = f,
			LX = lx,
			LY = ly,
			RX = rx,
			RY = ry
		};
	}

	private static BKey[] Keys(Body[] sheet, double perBeat)
	{
		double num = 18.0 / perBeat;
		BKey[] array = new BKey[sheet.Length];
		for (int i = 0; i < sheet.Length; i++)
		{
			array[i] = K((double)i * num, sheet[i]);
		}
		return array;
	}

	private static BKey[] KeysMirror(Body[] sheet, double perBeat)
	{
		double num = 18.0 / perBeat;
		double num2 = (double)sheet.Length * num;
		BKey[] array = new BKey[sheet.Length * 2];
		for (int i = 0; i < sheet.Length; i++)
		{
			array[i] = K((double)i * num, sheet[i]);
			array[i + sheet.Length] = K(num2 + (double)i * num, Mirror(sheet[i]));
		}
		return array;
	}

	private static Body BodyLerp(Body a, Body b, double t)
	{
		return new Body
		{
			Lean = F(a.Lean, b.Lean),
			HipX = F(a.HipX, b.HipX),
			Dip = F(a.Dip, b.Dip),
			Torso = F(a.Torso, b.Torso),
			Head = F(a.Head, b.Head),
			Shrug = F(a.Shrug, b.Shrug),
			HandP = G(a.HandP, b.HandP),
			HandM = G(a.HandM, b.HandM),
			FootP = G(a.FootP, b.FootP),
			FootM = G(a.FootM, b.FootM),
			ElbP = F(a.ElbP, b.ElbP),
			ElbM = F(a.ElbM, b.ElbM),
			KneP = F(a.KneP, b.KneP),
			KneM = F(a.KneM, b.KneM),
			FootRotP = F(a.FootRotP, b.FootRotP),
			FootRotM = F(a.FootRotM, b.FootRotM),
			Rod = F(a.Rod, b.Rod)
		};
		double F(double x, double y)
		{
			return x + (y - x) * t;
		}
		Vec G(Vec x, Vec y)
		{
			return V(F(x.X, y.X), F(x.Y, y.Y));
		}
	}

	private static Body KeysAt(BKey[] keys, double f, double period)
	{
		int num = keys.Length;
		int i;
		for (i = 0; i < num - 1 && keys[i + 1].F <= f; i++)
		{
		}
		BKey bKey = keys[i];
		BKey bKey2 = keys[(i + 1) % num];
		double num2 = ((i == num - 1) ? period : bKey2.F);
		double t = ((num2 == bKey.F) ? 0.0 : M.Clamp((f - bKey.F) / (num2 - bKey.F), 0.0, 1.0));
		if (num2 - bKey.F <= 8.0)
		{
			return BodyLerp(bKey.B, bKey2.B, EaseSnap(t));
		}
		return Blend(keys[(i - 1 + num) % num].B, bKey.B, bKey2.B, keys[(i + 2) % num].B, t);
	}

	private static void FeetAt(FootKey[] keys, double f, double period, out double lx, out double ly, out double rx, out double ry)
	{
		int num = keys.Length;
		int i;
		for (i = 0; i < num - 1 && keys[i + 1].F <= f; i++)
		{
		}
		FootKey footKey = keys[i];
		FootKey footKey2 = keys[(i + 1) % num];
		double num2 = ((i == num - 1) ? period : footKey2.F);
		double t = ((num2 == footKey.F) ? 0.0 : M.Clamp((f - footKey.F) / (num2 - footKey.F), 0.0, 1.0));
		double t2 = ((num2 - footKey.F <= 8.0) ? EaseSnap(t) : EaseInOut(t));
		lx = M.Lerp(footKey.LX, footKey2.LX, t2);
		ly = M.Lerp(footKey.LY, footKey2.LY, t2);
		rx = M.Lerp(footKey.RX, footKey2.RX, t2);
		ry = M.Lerp(footKey.RY, footKey2.RY, t2);
	}

	private static double[] BuildPhase(int frames, Func<double, double> freqAt)
	{
		double[] array = new double[frames + 1];
		double num = 0.0;
		for (int i = 1; i <= frames; i++)
		{
			num = (array[i] = num + Math.PI * 2.0 * freqAt(i - 1) / 30.0);
		}
		return array;
	}

	private static double Wave(double[] phase, double f, double lag, double seed, double amp)
	{
		int num = (int)f;
		if (num < 0)
		{
			num = 0;
		}
		if (num >= phase.Length)
		{
			num = phase.Length - 1;
		}
		double num2 = phase[num] - lag * 1.7 * Math.PI + seed * 0.8;
		return amp * (Math.Sin(num2) + 0.38 * Math.Sin(num2 * 1.87 + 1.1));
	}

	private static void Break(Pose[] ps, double[] phase, double f, double amp)
	{
		if (!(amp < 0.0001))
		{
			for (int i = 0; i < ps.Length; i++)
			{
				double z = ps[i].Z;
				double num = ((z >= 1000.0) ? 0.88 : ((z >= 900.0) ? 1.0 : ((z >= 400.0) ? 0.56 : 0.1)));
				num += M.Hash01(i, 311) * 0.08;
				double num2 = M.Hash01(i, 293) * 0.8;
				double num3 = 1.0 + num * 2.4;
				double num4 = Wave(phase, f, num, num2, amp);
				double num5 = num4 * num3 * 0.00526;
				double num6 = Wave(phase, f, num + 0.17, num2 + 1.7, amp) * num3 * 0.55 * 0.00526;
				ps[i] = new Pose(new Vec(ps[i].P.X + num5, ps[i].P.Y + num6), ps[i].Rot + num4 * 2.6 * Math.PI / 180.0, ps[i].Sx * (1.0 + num4 * 0.035), ps[i].Sy * (1.0 + num4 * 0.035), ps[i].Alpha, ps[i].Z);
			}
		}
	}

	private Skeleton ProceduralSkeleton(Tempo T)
	{
		double energy = T.Energy;
		figPhase += T.DBeat * M.Lerp(0.5, 1.0, M.Smooth((energy - 0.15) / 0.45));
		double num = figPhase % 16.0;
		bool num2 = num >= 8.0;
		double num3 = (num2 ? (num - 8.0) : num);
		double num4 = ((!num2) ? 1 : (-1));
		double num5 = num3 - Math.Floor(num3);
		double num6 = 0.04 * Math.Sin(Math.PI * num3);
		double num7 = 0.012 * Math.Sin(Math.PI * num3);
		double num8 = 0.03 * (0.5 - 0.5 * Math.Cos(Math.PI * 2.0 * num3));
		Vec vec = new Vec(0.26, 0.4);
		Vec vec2 = new Vec(-0.2, 0.33);
		Vec vec3 = new Vec(0.26, 0.385);
		if (num3 < 4.0)
		{
			vec = new Vec(0.26, 0.4 + 0.045 * Math.Cos(Math.PI * 2.0 * num5));
			vec3 = new Vec(0.262, 0.06 + 0.325 * Math.Abs(Math.Cos(Math.PI * num5)));
			vec2 = new Vec(-0.2 - 0.02 * Math.Sin(Math.PI * num3), 0.33 + 0.025 * Math.Sin(Math.PI * num3));
			num6 += 0.06;
		}
		else if (num3 < 6.0)
		{
			double num9 = (num3 - 4.0) / 2.0;
			vec3 = new Vec(M.Lerp(0.26, -0.26, num9), 0.385 + 2.34 * num9 * (1.0 - num9));
			Vec b = new Vec(vec3.X, vec3.Y - 0.054);
			vec = M.Lerp(new Vec(0.21, 0.35), b, 1.0 - M.Smooth((num9 - 0.22) / 0.2));
			vec2 = M.Lerp(new Vec(-0.25, 0.36 + 0.22 * M.Smooth((num9 - 0.25) / 0.4)), b, M.Smooth((num9 - 0.62) / 0.25));
			num6 = 0.1 * (1.0 - 2.0 * num9);
			num8 = 0.02;
		}
		else
		{
			double num10 = Math.Sin(Math.PI * (num3 - 6.0));
			double num11 = -0.07 * num10;
			vec3 = new Vec(-0.26 + num11, 0.385);
			vec2 = new Vec(-0.26 + num11, 0.331);
			vec = new Vec(0.22 + 0.07 * Math.Abs(num10) + num11, 0.35 + 0.12 * Math.Abs(num10));
			num6 = -0.24 * num10;
			num7 = 0.045 * num10;
			num8 = 0.035 * Math.Abs(num10);
		}
		vec = new Vec(vec.X * num4, vec.Y);
		vec2 = new Vec(vec2.X * num4, vec2.Y);
		vec3 = new Vec(vec3.X * num4, vec3.Y);
		num6 *= num4;
		num7 *= num4;
		double t = (figInit ? (1.0 - Math.Exp((0.0 - T.Dt) / 0.06)) : 1.0);
		figInit = true;
		handA = M.Lerp(handA, vec, t);
		handP = M.Lerp(handP, vec2, t);
		leanS = M.Lerp(leanS, num6, t);
		hipS = M.Lerp(hipS, num7, t);
		double num12 = leanS;
		Vec vec4 = new Vec(hipS, 0.4 - num8);
		Vec vec5 = new Vec(Math.Sin(num12), Math.Cos(num12));
		Vec vec6 = new Vec(Math.Cos(num12), 0.0 - Math.Sin(num12));
		Vec vec7 = vec4 + vec5 * 0.3;
		Vec head = vec7 + new Vec(Math.Sin(num12 * 1.3), Math.Cos(num12 * 1.3)) * 0.115;
		Vec vec8 = vec7 + vec6 * 0.09 - vec5 * 0.015;
		Vec vec9 = vec7 - vec6 * 0.09 - vec5 * 0.015;
		Vec target = ((num4 > 0.0) ? handA : handP);
		Vec target2 = ((num4 > 0.0) ? handP : handA);
		Ik(vec8, target, 0.155, 0.155, 1.0, out var joint, out var end);
		Ik(vec9, target2, 0.155, 0.155, -1.0, out var joint2, out var end2);
		Vec vec10 = vec4 + new Vec(0.05, 0.0);
		Vec vec11 = vec4 + new Vec(-0.05, 0.0);
		Vec vec12 = new Vec(0.12, 0.0);
		Vec vec13 = new Vec(-0.12, 0.0);
		Ik(vec10, vec12, 0.215, 0.215, 1.0, out var joint3, out var end3);
		Ik(vec11, vec13, 0.215, 0.215, -1.0, out var joint4, out end3);
		return new Skeleton
		{
			Head = head,
			Neck = vec7,
			Hip = vec4,
			ShP = vec8,
			ElbP = joint,
			HandP = end,
			ShM = vec9,
			ElbM = joint2,
			HandM = end2,
			HipP = vec10,
			KneeP = joint3,
			FootP = vec12,
			HipM = vec11,
			KneeM = joint4,
			FootM = vec13,
			Ball = vec3,
			Lean = num12,
			BallAlpha = 1.0
		};
	}

	private Vec[] GrooveSpots(int n, out int cols)
	{
		if (GrooveUsesHomes && Homes.Length >= n)
		{
			cols = 0;
			Vec[] array = new Vec[n];
			Array.Copy(Homes, array, n);
			return array;
		}
		RectD stage = Stage;
		double baseScale = BaseScale;
		cols = Math.Max(1, (int)Math.Ceiling(Math.Sqrt((double)n * stage.W / Math.Max(stage.H, 1.0))));
		int num = (int)Math.Ceiling((double)n / (double)cols);
		double num2 = Math.Min(stage.W / (double)cols, IconSize * 1.8 * baseScale);
		double num3 = Math.Min(stage.H * 0.86 / (double)Math.Max(num, 1), IconSize * 1.8 * baseScale);
		Vec[] array2 = new Vec[n];
		for (int i = 0; i < n; i++)
		{
			int num4 = i % cols;
			int num5 = i / cols;
			array2[i] = new Vec(stage.MidX + ((double)num4 - (double)(cols - 1) / 2.0) * num2, stage.MidY + ((double)(num - 1) / 2.0 - (double)num5) * num3);
		}
		return array2;
	}

	private Pose[] Groove(int n, Tempo T)
	{
		int cols;
		Vec[] array = GrooveSpots(n, out cols);
		double baseScale = BaseScale;
		double beat = T.Beat;
		double energy = T.Energy;
		double pulse = T.Pulse;
		double num = Math.Min(Amp, 1.6);
		Pose[] array2 = new Pose[n];
		for (int i = 0; i < n; i++)
		{
			int num2 = ((cols > 0) ? (i % cols) : ((int)(array[i].X / Math.Max(GridW, 1.0))));
			int num3 = ((cols > 0) ? (i / cols) : ((int)(array[i].Y / Math.Max(GridH, 1.0))));
			double num4 = (((num2 + num3) % 2 == 0) ? 0.0 : Math.PI);
			double num5 = IconSize * baseScale * 0.24 * Amp * (0.4 + energy) * Math.Abs(Math.Sin(Math.PI * beat));
			double num6 = 1.0 - 0.26 * pulse * num;
			double num7 = 1.0 + 0.2 * pulse * num;
			double x = array[i].X + IconSize * baseScale * 0.1 * Amp * Math.Sin(Math.PI * beat / 2.0 + (double)num3 * 0.7);
			double y = array[i].Y + num5 - IconSize * baseScale * 0.5 * (1.0 - num6);
			double rot = 0.17 * Amp * (0.5 + energy) * Math.Sin(Math.PI * beat + num4);
			array2[i] = new Pose(new Vec(x, y), rot, baseScale * num7, baseScale * num6, 1.0, i);
		}
		return array2;
	}

	private static ShiverKey SK(double f, double lean, double head, double sl, double sr, double ul, double fl, double ur, double fr, double hl, double hr, double knee, double trem, double freq)
	{
		return new ShiverKey
		{
			F = f,
			Lean = lean,
			Head = head,
			ShL = sl,
			ShR = sr,
			AUL = ul,
			AFL = fl,
			AUR = ur,
			AFR = fr,
			HL = hl,
			HR = hr,
			Knee = knee,
			Trem = trem,
			Freq = freq
		};
	}

	private static ShiverFoot SF(double f, double lx, double ly, double rx, double ry, int step)
	{
		return new ShiverFoot
		{
			F = f,
			LX = lx,
			LY = ly,
			RX = rx,
			RY = ry,
			Step = step
		};
	}

	private static ShiverPose ShiverPoseAt(double f)
	{
		int i;
		for (i = 0; i < ShiverKeys.Length - 2 && ShiverKeys[i + 1].F <= f; i++)
		{
		}
		ShiverKey shiverKey = ShiverKeys[i];
		ShiverKey shiverKey2 = ShiverKeys[Math.Min(i + 1, ShiverKeys.Length - 1)];
		double t = ((shiverKey2.F == shiverKey.F) ? 0.0 : M.Clamp((f - shiverKey.F) / (shiverKey2.F - shiverKey.F), 0.0, 1.0));
		double t2 = ((shiverKey2.F - shiverKey.F <= 12.0) ? EaseSnap(t) : EaseInOut(t));
		ShiverPose result = new ShiverPose
		{
			Lean = M.Lerp(shiverKey.Lean, shiverKey2.Lean, t2),
			Head = M.Lerp(shiverKey.Head, shiverKey2.Head, t2),
			ShL = M.Lerp(shiverKey.ShL, shiverKey2.ShL, t2),
			ShR = M.Lerp(shiverKey.ShR, shiverKey2.ShR, t2),
			AUL = M.Lerp(shiverKey.AUL, shiverKey2.AUL, t2),
			AFL = M.Lerp(shiverKey.AFL, shiverKey2.AFL, t2),
			AUR = M.Lerp(shiverKey.AUR, shiverKey2.AUR, t2),
			AFR = M.Lerp(shiverKey.AFR, shiverKey2.AFR, t2),
			HL = M.Lerp(shiverKey.HL, shiverKey2.HL, t2),
			HR = M.Lerp(shiverKey.HR, shiverKey2.HR, t2),
			Knee = M.Lerp(shiverKey.Knee, shiverKey2.Knee, t2),
			Trem = M.Lerp(shiverKey.Trem, shiverKey2.Trem, t2),
			Freq = M.Lerp(shiverKey.Freq, shiverKey2.Freq, t2),
			Step = 0,
			Lift = 0.0
		};
		int j;
		for (j = 0; j < ShiverFootKeys.Length - 2 && ShiverFootKeys[j + 1].F <= f; j++)
		{
		}
		ShiverFoot shiverFoot = ShiverFootKeys[j];
		ShiverFoot shiverFoot2 = ShiverFootKeys[Math.Min(j + 1, ShiverFootKeys.Length - 1)];
		double num = ((shiverFoot2.F == shiverFoot.F) ? 0.0 : M.Clamp((f - shiverFoot.F) / (shiverFoot2.F - shiverFoot.F), 0.0, 1.0));
		double t3 = EaseInOut(num);
		result.FLx = M.Lerp(shiverFoot.LX, shiverFoot2.LX, t3);
		result.FLy = M.Lerp(shiverFoot.LY, shiverFoot2.LY, t3);
		result.FRx = M.Lerp(shiverFoot.RX, shiverFoot2.RX, t3);
		result.FRy = M.Lerp(shiverFoot.RY, shiverFoot2.RY, t3);
		result.Step = shiverFoot.Step;
		if (result.Step != 0)
		{
			result.Lift = Math.Sin(Math.PI * num);
		}
		if (f >= 144.0 && f < 216.0)
		{
			double num2 = Ramp(f - 144.0) * Ramp(216.0 - f);
			double num3 = Math.PI * 2.0 * ((f - 144.0) / 36.0);
			result.Lean += num2 * 8.0 * Math.Sin(num3);
			result.ShL += num2 * 9.0 * Math.Cos(num3);
			result.ShR += num2 * 9.0 * Math.Cos(num3 + 1.9477874452256718);
			result.AUL += num2 * 32.0 * Math.Sin(num3);
			result.AUR += num2 * 32.0 * Math.Sin(num3 + Math.PI);
			result.AFL += num2 * 10.0 * Math.Cos(num3);
			result.AFR -= num2 * 10.0 * Math.Cos(num3);
			result.Knee += num2 * 0.06 * (1.0 + Math.Cos(num3));
		}
		if (f >= 216.0 && f < 360.0)
		{
			double num4 = Ramp(f - 216.0) * Ramp(360.0 - f);
			int num5 = (int)Math.Floor((f - 216.0) / 18.0);
			double num6 = (f - 216.0) % 18.0 / 18.0;
			bool num7 = num5 % 2 == 0;
			double b = (num7 ? 128 : (-28));
			double b2 = (num7 ? 18 : (-12));
			double b3 = (num7 ? 28 : (-128));
			double b4 = (num7 ? 12 : (-18));
			double a = (num7 ? (-28) : 128);
			double a2 = (num7 ? (-12) : 18);
			double a3 = (num7 ? (-128) : 28);
			double a4 = (num7 ? (-18) : 12);
			double t4 = M.Smooth(M.Clamp(num6 / 0.45, 0.0, 1.0));
			double num8 = ((num6 < 0.45) ? M.Smooth(num6 / 0.45) : ((num6 < 0.55) ? 1.0 : (1.0 - M.Smooth((num6 - 0.55) / 0.3))));
			result.AUL = M.Lerp(result.AUL, M.Lerp(M.Lerp(a, b, t4), 180.0, num8 * 0.82), num4);
			result.AFL = M.Lerp(result.AFL, M.Lerp(M.Lerp(a2, b2, t4), -45.0, num8 * 0.82), num4);
			result.AUR = M.Lerp(result.AUR, M.Lerp(M.Lerp(a3, b3, t4), -180.0, num8 * 0.82), num4);
			result.AFR = M.Lerp(result.AFR, M.Lerp(M.Lerp(a4, b4, t4), 45.0, num8 * 0.82), num4);
			result.Lean += num4 * (double)((num5 % 4 == 1) ? 5 : (-5)) * Math.Sin(Math.PI * num6);
			result.X += num4 * (result.FLx + result.FRx) * 0.1;
		}
		if (f >= 360.0 && f < 432.0)
		{
			double num9 = Ramp(f - 360.0) * Ramp(432.0 - f);
			double num10 = 0.86 + 0.14 * Math.Sin(Math.PI * 2.0 * (f - 360.0) / 36.0);
			result.AUL = M.Lerp(result.AUL, -90.0 * num10, num9);
			result.AUR = M.Lerp(result.AUR, 90.0 * num10, num9);
			result.Lean += num9 * 2.5 * Math.Sin(Math.PI * 2.0 * (f - 360.0) / 72.0);
		}
		if (f >= 432.0 && f < 522.0)
		{
			double num11 = Ramp(f - 432.0) * Ramp(522.0 - f);
			double num12 = Math.Sin(Math.PI * 2.0 * (f - 432.0) / 24.0);
			result.AUL += num11 * num12 * 9.0;
			result.AUR -= num11 * num12 * 9.0;
			result.Head += num11 * num12 * 2.5;
		}
		if (f >= 36.0 && f < 534.0)
		{
			double num13 = Ramp(f - 36.0) * Ramp(534.0 - f);
			double num14 = ((f - 36.0) % 18.0 + 18.0) % 18.0 / 18.0;
			double num15 = Math.Pow(Math.Max(0.0, 1.0 - num14 * 2.2), 2.0);
			result.Knee += num13 * num15 * 0.17 * (0.5 + result.Trem);
		}
		if (result.Step == 1)
		{
			result.X += 6.0 * result.Lift;
		}
		else if (result.Step == 2)
		{
			result.X -= 6.0 * result.Lift;
		}
		return result;
		static double Ramp(double d)
		{
			return M.Clamp(d / 8.0, 0.0, 1.0);
		}
	}

	private Pose[] Shiver(int n, Tempo T)
	{
		if (n < 8)
		{
			return Ring(n, T);
		}
		double num = (T.Beat - modeBeat0) * 18.0;
		num -= Math.Floor(num / 600.0) * 600.0;
		if (num < 0.0)
		{
			num += 600.0;
		}
		ShiverPose shiverPose = ShiverPoseAt(num);
		double num2 = Math.Min(Amp, 1.6);
		double energy = T.Energy;
		double num3 = 0.075 + (shiverPose.Knee - 0.25) * 0.17;
		double num4 = M.Clamp(T.Accent * 0.8 + T.Pulse * 0.6, 0.0, 1.2);
		num3 += 0.018 * num2 * num4;
		double amp = shiverPose.Trem * (0.8 + 0.45 * energy) * num2;
		double shY = 0.7 - num3;
		double num5 = shiverPose.X / 190.0;
		Body b = new Body
		{
			Lean = shiverPose.Lean * (Math.PI / 180.0),
			HipX = num5,
			Dip = num3,
			Torso = (shiverPose.ShL - shiverPose.ShR) / 190.0 * 0.6,
			Head = shiverPose.Head * (Math.PI / 180.0),
			Shrug = (0.0 - (shiverPose.ShL + shiverPose.ShR)) / 2.0 / 190.0,
			HandP = ShiverHand(num5 + 0.09, shY, shiverPose.AUR, shiverPose.AFR),
			HandM = ShiverHand(num5 - 0.09, shY, shiverPose.AUL, shiverPose.AFL),
			FootP = V(shiverPose.FRx / 190.0, ShiverFootY(shiverPose.FRy, shiverPose.Step == 2, shiverPose.Lift)),
			FootM = V(shiverPose.FLx / 190.0, ShiverFootY(shiverPose.FLy, shiverPose.Step == 1, shiverPose.Lift))
		};
		Skeleton sk = BuildSkeleton(b);
		Pose[] array = FigureFromSkeleton(n, sk, 0.0);
		Break(array, ShiverPhase, num, amp);
		return array;
	}

	private static Vec ShiverHand(double shX, double shY, double a1, double a2)
	{
		double num = Math.PI / 180.0;
		double num2 = Math.Sin(a1 * num) * 44.0 + Math.Sin((a1 + a2) * num) * 40.0;
		double num3 = Math.Cos(a1 * num) * 44.0 + Math.Cos((a1 + a2) * num) * 40.0;
		double num4 = 0.0036842105263157894;
		return new Vec(shX + num2 * num4, shY - num3 * num4);
	}

	private static double ShiverFootY(double ly, bool stepping, double lift)
	{
		return Math.Max(0.0, (118.0 - ly) * 0.5 / 190.0) + (stepping ? (lift * 15.0 / 190.0) : 0.0);
	}

	// ---------- 网页姿态翻译层：flash-step / mj / subject-three 三个网页引擎共用 ----------
	// 网页骨架（190 世界单位高，脚在 y=118，骨盆 y=2+knee*14，臂 44+40，腿 62+60）的字段：
	//   x/y 骨盆偏移、rootX 整体横移、rootRot 绕脚踝整根旋转、lean 上半身倾、knee 下蹲、
	//   shL/shR 肩高低、aUL/aFL/aUR/aFR 手臂极坐标（度）、footRoll 足内外翻、heelLift 脚跟抬起。
	// rootRot 我们没有独立自由度：并入 Lean 且 Rod=1（hip 沿倾方向位移、脚钉地）。

	private const double WebRad = Math.PI / 180.0;

	private struct WebPose
	{
		public double X;

		public double Y;

		public double RootX;

		public double RootRot;

		public double Lean;

		public double Head;

		public double ShL;

		public double ShR;

		public double AUL;

		public double AFL;

		public double AUR;

		public double AFR;

		public double WristPhaseL;

		public double WristPhaseR;

		public double WristAmp;

		public double Knee;

		public double FootRollL;

		public double FootRollR;

		public double HeelLiftL;

		public double HeelLiftR;

		public double Trem;

		public double FLx;

		public double FLy;

		public double FRx;

		public double FRy;
	}

	private struct RobotPose
	{
		public double AUL;

		public double AFL;

		public double AUR;

		public double AFR;

		public double Head;

		public double ShL;

		public double ShR;
	}

	// 网页基准站姿：手臂自然下垂、双脚与肩同宽
	private static WebPose WebBase(double flx, double frx)
	{
		return new WebPose
		{
			AUL = -11.0,
			AFL = 9.0,
			AUR = 11.0,
			AFR = -9.0,
			Knee = 0.25,
			FLx = flx,
			FRx = frx,
			FLy = 118.0,
			FRy = 118.0
		};
	}

	// 网页手臂极坐标（度）→ 我们的手位（身高单位，y 向上），叠加腕部圆锥绕圈
	private static Vec WebHand(double shX, double shY, double a1, double a2, double phaseDeg, double ampUnits)
	{
		double dx = Math.Sin(a1 * WebRad) * 44.0 + Math.Sin((a1 + a2) * WebRad) * 40.0;
		double dy = Math.Cos(a1 * WebRad) * 44.0 + Math.Cos((a1 + a2) * WebRad) * 40.0;
		double s = 0.7 / 190.0;
		double w = ampUnits / 190.0;
		double ph = phaseDeg * WebRad;
		return new Vec(shX + dx * s + Math.Cos(ph) * w, shY - dy * s + Math.Sin(ph) * w * 0.62);
	}

	private static double WebFootY(double ly, double heel)
	{
		return Math.Max(0.0, (118.0 - ly) * 0.5 / 190.0) + heel * 0.03;
	}

	// 网页姿态 → 我们骨架。a = 动作幅度（只缩「表情」通道，脚位是走位不缩）
	private static Body WebBody(WebPose p, double a)
	{
		double dip = 0.075 + (p.Knee - 0.25) * 0.17 - p.Y / 190.0;
		double hipX = (p.X + p.RootX) / 190.0;
		double shY = 0.7 - dip;
		return new Body
		{
			Lean = (p.Lean + p.RootRot) * WebRad * a,
			Rod = M.Clamp(Math.Abs(p.RootRot) / 3.0, 0.0, 1.0),
			HipX = hipX * a,
			Dip = 0.075 + (dip - 0.075) * a,
			Torso = (p.ShL - p.ShR) / 190.0 * 0.6 * a,
			Head = p.Head * WebRad * a,
			Shrug = (0.0 - (p.ShL + p.ShR) / 2.0) / 190.0 * a,
			HandP = WebHand(hipX + 0.09, shY, p.AUR, p.AFR, p.WristPhaseR, p.WristAmp * a),
			HandM = WebHand(hipX - 0.09, shY, p.AUL, p.AFL, p.WristPhaseL, p.WristAmp * a),
			FootP = V((p.FRx + p.RootX) / 190.0, WebFootY(p.FRy, p.HeelLiftR)),
			FootM = V((p.FLx + p.RootX) / 190.0, WebFootY(p.FLy, p.HeelLiftL)),
			FootRotP = (p.FootRollR + p.HeelLiftR * 22.0) * WebRad * a,
			FootRotM = (p.FootRollL - p.HeelLiftL * 22.0) * WebRad * a
		};
	}

	// 左右整体镜像（网页 mirror）：角度取反互换、倾角取反、脚位互换取反；rootX 不镜像
	private static WebPose WebMirror(WebPose p)
	{
		return new WebPose
		{
			X = p.X,
			Y = p.Y,
			RootX = p.RootX,
			AUL = 0.0 - p.AUR,
			AFL = 0.0 - p.AFR,
			AUR = 0.0 - p.AUL,
			AFR = 0.0 - p.AFL,
			WristPhaseL = p.WristPhaseR,
			WristPhaseR = p.WristPhaseL,
			WristAmp = p.WristAmp,
			ShL = p.ShR,
			ShR = p.ShL,
			Lean = 0.0 - p.Lean,
			Head = 0.0 - p.Head,
			RootRot = 0.0 - p.RootRot,
			Knee = p.Knee,
			Trem = p.Trem,
			FootRollL = 0.0 - p.FootRollR,
			FootRollR = 0.0 - p.FootRollL,
			HeelLiftL = p.HeelLiftR,
			HeelLiftR = p.HeelLiftL,
			FLx = 0.0 - p.FRx,
			FLy = p.FRy,
			FRx = 0.0 - p.FLx,
			FRy = p.FLy
		};
	}

	private delegate WebPose WebPoseFn(double f);

	private static WebPose WebLerp(WebPose a, WebPose b, double t)
	{
		return new WebPose
		{
			X = M.Lerp(a.X, b.X, t),
			Y = M.Lerp(a.Y, b.Y, t),
			RootX = M.Lerp(a.RootX, b.RootX, t),
			RootRot = M.Lerp(a.RootRot, b.RootRot, t),
			Lean = M.Lerp(a.Lean, b.Lean, t),
			Head = M.Lerp(a.Head, b.Head, t),
			ShL = M.Lerp(a.ShL, b.ShL, t),
			ShR = M.Lerp(a.ShR, b.ShR, t),
			AUL = M.Lerp(a.AUL, b.AUL, t),
			AFL = M.Lerp(a.AFL, b.AFL, t),
			AUR = M.Lerp(a.AUR, b.AUR, t),
			AFR = M.Lerp(a.AFR, b.AFR, t),
			WristPhaseL = M.Lerp(a.WristPhaseL, b.WristPhaseL, t),
			WristPhaseR = M.Lerp(a.WristPhaseR, b.WristPhaseR, t),
			WristAmp = M.Lerp(a.WristAmp, b.WristAmp, t),
			Knee = M.Lerp(a.Knee, b.Knee, t),
			FootRollL = M.Lerp(a.FootRollL, b.FootRollL, t),
			FootRollR = M.Lerp(a.FootRollR, b.FootRollR, t),
			HeelLiftL = M.Lerp(a.HeelLiftL, b.HeelLiftL, t),
			HeelLiftR = M.Lerp(a.HeelLiftR, b.HeelLiftR, t),
			Trem = M.Lerp(a.Trem, b.Trem, t),
			FLx = M.Lerp(a.FLx, b.FLx, t),
			FLy = M.Lerp(a.FLy, b.FLy, t),
			FRx = M.Lerp(a.FRx, b.FRx, t),
			FRy = M.Lerp(a.FRy, b.FRy, t)
		};
	}

	// 段边界渐入：网页每段是硬切，图标小人一帧换个手位就是 0.9 身高的瞬移，
	// 所以边界后 blends[i] 帧内把「上一段末姿态」平滑融进本段——连续处几乎等于原样。
	// bounds 与 blends 一一对应：普通段边界 6 帧渐入，机器人内部卡点 3 帧快切（保住机械感）。
	private static WebPose WebMorph(double f, double[] bounds, double[] blends, WebPoseFn raw)
	{
		for (int i = 0; i < bounds.Length; i++)
		{
			double b = bounds[i];
			double blend = blends[i];
			if (f >= b && f < b + blend)
			{
				WebPose prev = raw(b - 0.0001);
				WebPose curr = raw(f);
				return WebLerp(prev, curr, M.Smooth((f - b) / blend));
			}
		}
		return raw(f);
	}

	// ---------- 闪身步：flash-step.html 帧驱动引擎逐段翻译 ----------
	// 540 帧 @30fps、每拍 18 帧 = 30 拍：预备 →（假动作→蓄力→突射→急停→回位）左一遍 →
	// 右一遍（镜像）→ 左右连闪 → 收势。横移曲线 RX 与五段 stagePose 按网页公式原样搬。

	private static double FlashFreqAt(double f)
	{
		if (f >= 72.0 && f < 114.0)
		{
			return 6.0;
		}
		if (f >= 114.0 && f < 150.0)
		{
			return 8.0;
		}
		if (f >= 150.0 && f < 180.0)
		{
			return 9.0;
		}
		return 4.5;
	}

	private static double FlashRootXAt(double f)
	{
		if (f < 36.0)
		{
			return 0.0;
		}
		if (f < 72.0)
		{
			return M.Lerp(0.0, 16.0, EaseInOut((f - 36.0) / 36.0));
		}
		if (f < 114.0)
		{
			return M.Lerp(16.0, 30.0, EaseInOut((f - 72.0) / 42.0));
		}
		if (f < 150.0)
		{
			return M.Lerp(30.0, -95.0, EaseOutQuart((f - 114.0) / 36.0));
		}
		if (f < 216.0)
		{
			return -95.0;
		}
		if (f < 252.0)
		{
			return M.Lerp(-95.0, -111.0, EaseInOut((f - 216.0) / 36.0));
		}
		if (f < 294.0)
		{
			return M.Lerp(-111.0, -125.0, EaseInOut((f - 252.0) / 42.0));
		}
		if (f < 330.0)
		{
			return M.Lerp(-125.0, 95.0, EaseOutQuart((f - 294.0) / 36.0));
		}
		if (f < 396.0)
		{
			return 95.0;
		}
		if (f < 468.0)
		{
			return M.Lerp(95.0, 0.0, EaseOutQuart((f - 396.0) / 72.0));
		}
		return 0.0;
	}

	// 五阶段姿态（以「向左闪」为基准）：0 预备 / 1 假动作 / 2 蓄力 / 3 突射 / 4 急停 / 5 回位 / 6 连闪 / 7 收势
	private static WebPose FlashStage(int id, double t)
	{
		double e = EaseInOut(t);
		double q = EaseOutQuart(t);
		WebPose p = WebBase(-36.0, 36.0);
		switch (id)
		{
		case 0:
			p.Knee = 0.3 + 0.04 * Math.Sin(Math.PI * 2.0 * t);
			p.AUL = -26.0;
			p.AFL = 42.0;
			p.AUR = 26.0;
			p.AFR = -42.0;
			break;
		case 1:
			p.AUL = M.Lerp(-26.0, 56.0, e);
			p.AFL = M.Lerp(42.0, 8.0, e);
			p.AUR = M.Lerp(26.0, 30.0, e);
			p.AFR = M.Lerp(-42.0, -14.0, e);
			p.Lean = 9.0 * e;
			p.Head = 4.0 * e;
			p.RootRot = 3.0 * e;
			p.Knee = 0.34 + 0.06 * e;
			p.FRx = 36.0 + 9.0 * e;
			p.FRy = 118.0 + 9.0 * e;
			p.ShL = -3.0 * e;
			p.ShR = 4.0 * e;
			break;
		case 2:
			p.AUL = M.Lerp(56.0, -48.0, e);
			p.AFL = M.Lerp(8.0, 72.0, e);
			p.AUR = M.Lerp(30.0, 48.0, e);
			p.AFR = M.Lerp(-14.0, -72.0, e);
			p.Lean = M.Lerp(9.0, 15.0, e);
			p.RootRot = M.Lerp(3.0, 7.0, e);
			p.Knee = 0.4 + 0.26 * e;
			p.ShL = -8.0;
			p.ShR = 8.0;
			p.Trem = 0.12 * e;
			p.FRx = 45.0;
			p.FRy = 127.0;
			break;
		case 3:
			p.AUL = M.Lerp(-48.0, -104.0, q);
			p.AFL = M.Lerp(72.0, -8.0, q);
			p.AUR = M.Lerp(48.0, 32.0, q);
			p.AFR = M.Lerp(-72.0, -8.0, q);
			p.Lean = M.Lerp(15.0, -8.0, q);
			p.RootRot = M.Lerp(7.0, -5.0, q);
			p.Knee = M.Lerp(0.66, 0.28, q);
			p.ShL = M.Lerp(-8.0, 5.0, q);
			p.ShR = M.Lerp(8.0, -5.0, q);
			p.Trem = 0.25 * (1.0 - q);
			p.FLx = M.Lerp(-36.0, -58.0, q);
			p.FLy = M.Lerp(118.0, 106.0, q);
			p.FRx = M.Lerp(45.0, 30.0, q);
			p.FRy = M.Lerp(127.0, 118.0, q);
			break;
		case 4:
		{
			double ft = ((t < 0.27) ? 0.0 : ((t - 0.27) / 0.73));
			double b = EaseInOut(ft);
			p.AUL = M.Lerp(-104.0, -86.0, b);
			p.AFL = M.Lerp(-8.0, 6.0, b);
			p.AUR = M.Lerp(32.0, -28.0, b);
			p.AFR = M.Lerp(-8.0, -4.0, b);
			p.Lean = M.Lerp(-8.0, 7.0, b);
			p.RootRot = M.Lerp(-5.0, 5.0, b);
			p.Knee = M.Lerp(0.28, 0.58, b);
			p.Trem = 0.34 * (1.0 - b);
			p.FLx = M.Lerp(-58.0, -36.0, b);
			p.FLy = M.Lerp(106.0, 118.0, b);
			p.FRx = M.Lerp(30.0, 36.0, b);
			p.FRy = 118.0;
			break;
		}
		case 5:
			p.AUL = M.Lerp(-86.0, -11.0, e);
			p.AFL = M.Lerp(6.0, 9.0, e);
			p.AUR = M.Lerp(-28.0, 11.0, e);
			p.AFR = M.Lerp(-4.0, -9.0, e);
			p.Lean = M.Lerp(7.0, 0.0, e);
			p.RootRot = M.Lerp(5.0, 0.0, e);
			p.Knee = M.Lerp(0.58, 0.25, e);
			break;
		case 6:
		{
			double w = Math.Sin(Math.PI * 2.0 * t * 2.0);
			p.RootX = 34.0 * w * (1.0 - t * 0.65);
			p.X = 5.0 * w;
			p.Lean = -7.0 * w;
			p.RootRot = -3.0 * w;
			p.AUL = -40.0 - 28.0 * Math.Max(0.0, w);
			p.AFL = 16.0;
			p.AUR = 40.0 + 28.0 * Math.Max(0.0, 0.0 - w);
			p.AFR = -16.0;
			p.Knee = 0.34 + 0.1 * Math.Abs(w);
			p.Trem = 0.08;
			break;
		}
		case 7:
			p.AUL = -18.0;
			p.AFL = 26.0;
			p.AUR = 18.0;
			p.AFR = -26.0;
			p.Knee = 0.28;
			break;
		}
		return p;
	}

	private static WebPose FlashRawAt(double f)
	{
		int[] segS = new int[13] { 0, 36, 72, 114, 150, 180, 216, 252, 294, 330, 360, 396, 468 };
		double[] segL = new double[13]
		{
			36.0, 36.0, 42.0, 36.0, 30.0, 36.0, 36.0, 42.0, 36.0, 30.0, 36.0, 72.0, 72.0
		};
		int[] segId = new int[13] { 0, 1, 2, 3, 4, 5, 1, 2, 3, 4, 5, 6, 7 };
		bool[] segM = new bool[13]
		{
			false, false, false, false, false, false, true, true, true, true, true, false, false
		};
		int i = segS.Length - 1;
		for (int k = 0; k < segS.Length; k++)
		{
			if (f >= (double)segS[k])
			{
				i = k;
			}
		}
		WebPose p = FlashStage(segId[i], (f - (double)segS[i]) / segL[i]);
		if (segM[i])
		{
			p = WebMirror(p);
		}
		p.RootX = ((i == 11) ? (p.RootX + M.Lerp(95.0, 0.0, EaseOutQuart((f - 396.0) / 72.0))) : FlashRootXAt(f));
		return p;
	}

	private static readonly double[] FlashBounds = new double[12]
	{
		36.0, 72.0, 114.0, 150.0, 180.0, 216.0, 252.0, 294.0, 330.0, 360.0, 396.0, 468.0
	};

	private static readonly double[] FlashBlends = new double[12]
	{
		6.0, 6.0, 6.0, 6.0, 6.0, 6.0, 6.0, 6.0, 6.0, 6.0, 6.0, 6.0
	};

	private static WebPose FlashPoseAt(double f)
	{
		return WebMorph(f, FlashBounds, FlashBlends, FlashRawAt);
	}

	private Pose[] Flash(int n, Tempo T)
	{
		if (n < 8)
		{
			return Ring(n, T);
		}
		double f = EngineFrame(T, 18.0, FlashFrames);
		double a = Math.Min(Amp, 1.6);
		WebPose webPose = FlashPoseAt(f);
		Body b = WebBody(webPose, a);
		Skeleton sk = BuildSkeleton(b);
		Pose[] array = FigureFromSkeleton(n, sk, 0.0);
		Break(array, FlashPhase, f, webPose.Trem * (0.8 + 0.45 * T.Energy) * a * 1.6);
		return array;
	}

	// ---------- MJ 太空步：mj.html 帧驱动引擎逐段翻译 ----------
	// 600 帧 @30fps、每拍 15 帧 = 40 拍：压帽檐 → 肩律动 → 月球漫步×4 → 脚尖点地 →
	// 机器人（10 帧一卡）→ 45° 反重力前倾（从脚踝整根倾）→ 转圈 → 单脚定格 → 侧滑踢腿 →
	// 回滑×2 → 终场造型。转圈的「横向压缩」图标小人没有对应自由度，用 rootRot 摆动带出转势。

	private static WebPose MoonSlide(WebPose p, int step, double t, int sgn)
	{
		double e = EaseInOut(t);
		p.RootX = (double)sgn * (90.0 - (double)step * 45.0 - 45.0 * e);
		bool slideLeft = step % 2 == 0;
		if (slideLeft)
		{
			p.FLx = M.Lerp(-30.0, 8.0, e);
			p.FLy = 118.0;
			p.HeelLiftL = 0.0;
			p.FootRollL = 0.0;
			p.FRx = 30.0;
			p.FRy = 116.0;
			p.HeelLiftR = 0.9;
			p.FootRollR = -10.0;
		}
		else
		{
			p.FRx = M.Lerp(30.0, -8.0, e);
			p.FRy = 118.0;
			p.HeelLiftR = 0.0;
			p.FootRollR = 0.0;
			p.FLx = -30.0;
			p.FLy = 116.0;
			p.HeelLiftL = 0.9;
			p.FootRollL = 10.0;
		}
		p.Knee = 0.3 + 0.06 * Math.Sin(Math.PI * t);
		p.Lean = (double)sgn * 3.0;
		p.RootRot = (double)sgn * 2.0;
		p.Head = (double)sgn * 4.0 * Math.Sin(Math.PI * t);
		p.ShL = -6.0 * (slideLeft ? 1.0 : -1.0);
		p.ShR = 6.0 * (slideLeft ? 1.0 : -1.0);
		p.AUL = -46.0;
		p.AFL = 28.0;
		p.AUR = 46.0;
		p.AFR = -28.0;
		p.Y = 1.5 * Math.Sin(Math.PI * t);
		p.Trem = 0.05;
		return p;
	}

	private static readonly RobotPose[] MoonRobot = new RobotPose[6]
	{
		new RobotPose
		{
			AUL = -90.0,
			AFL = 0.0,
			AUR = 90.0,
			AFR = 0.0,
			Head = 0.0,
			ShL = 0.0,
			ShR = 0.0
		},
		new RobotPose
		{
			AUL = -90.0,
			AFL = -85.0,
			AUR = 20.0,
			AFR = 30.0,
			Head = -10.0,
			ShL = -8.0,
			ShR = 4.0
		},
		new RobotPose
		{
			AUL = -150.0,
			AFL = 30.0,
			AUR = 90.0,
			AFR = -85.0,
			Head = 10.0,
			ShL = 6.0,
			ShR = -8.0
		},
		new RobotPose
		{
			AUL = -20.0,
			AFL = 10.0,
			AUR = 150.0,
			AFR = -30.0,
			Head = 0.0,
			ShL = 0.0,
			ShR = 10.0
		},
		new RobotPose
		{
			AUL = -95.0,
			AFL = -70.0,
			AUR = 95.0,
			AFR = 70.0,
			Head = -6.0,
			ShL = -6.0,
			ShR = -6.0
		},
		new RobotPose
		{
			AUL = -12.0,
			AFL = 8.0,
			AUR = 12.0,
			AFR = -8.0,
			Head = 0.0,
			ShL = 0.0,
			ShR = 0.0
		}
	};

	private static WebPose MoonRawAt(double f)
	{
		f -= Math.Floor(f / MoonFrames) * MoonFrames;
		if (f < 0.0)
		{
			f += MoonFrames;
		}
		WebPose p = WebBase(-30.0, 30.0);
		if (f < 30.0)
		{
			p.AUR = 150.0;
			p.AFR = 22.0;
			p.AUL = -18.0;
			p.AFL = 24.0;
			p.Knee = 0.3;
			p.Head = 4.0;
		}
		else if (f < 60.0)
		{
			double pulse = Math.Pow(Math.Max(0.0, 1.0 - (f - 30.0) % 15.0 / 15.0 * 2.2), 2.0);
			p.AUR = 150.0;
			p.AFR = 22.0;
			p.AUL = -22.0;
			p.AFL = 26.0;
			p.ShL = -10.0 * pulse;
			p.ShR = 10.0 * pulse;
			p.Head = 5.0 * pulse;
			p.Knee = 0.3 + 0.08 * pulse;
		}
		else if (f < 180.0)
		{
			p = MoonSlide(p, (int)Math.Floor((f - 60.0) / 30.0), (f - 60.0) % 30.0 / 30.0, 1);
		}
		else if (f < 210.0)
		{
			p.HeelLiftL = 1.0;
			p.HeelLiftR = 1.0;
			p.FLx = -22.0;
			p.FRx = 22.0;
			p.Knee = 0.16;
			p.AUL = -72.0;
			p.AFL = -14.0;
			p.AUR = 72.0;
			p.AFR = 14.0;
			p.RootRot = -2.0;
		}
		else if (f < 270.0)
		{
			RobotPose robotPose = MoonRobot[Math.Min(MoonRobot.Length - 1, (int)Math.Floor((f - 210.0) / 10.0))];
			p.AUL = robotPose.AUL;
			p.AFL = robotPose.AFL;
			p.AUR = robotPose.AUR;
			p.AFR = robotPose.AFR;
			p.Head = robotPose.Head;
			p.ShL = robotPose.ShL;
			p.ShR = robotPose.ShR;
			p.Knee = 0.28;
		}
		else if (f < 330.0)
		{
			double q1 = EaseOutQuart(Math.Min(1.0, (f - 270.0) / 60.0 * 2.2));
			double q2 = EaseOutQuart((f - 270.0) / 60.0);
			p.RootRot = 45.0 * q1;
			p.Lean = 4.0 * q2;
			p.AUL = -14.0;
			p.AFL = 10.0;
			p.AUR = 118.0;
			p.AFR = 40.0;
			p.Knee = 0.2;
			p.Head = -3.0;
		}
		else if (f < 390.0)
		{
			double a2 = Math.PI * 2.0 * 2.0 * (f - 330.0) / 60.0;
			p.RootRot = 6.0 * Math.Sin(a2);
			p.AUL = -70.0;
			p.AFL = 40.0;
			p.AUR = 70.0;
			p.AFR = -40.0;
			p.Knee = 0.24;
			p.Trem = 0.2;
		}
		else if (f < 420.0)
		{
			p.HeelLiftL = 1.0;
			p.HeelLiftR = 0.35;
			p.FRx = 44.0;
			p.FRy = 106.0;
			p.Knee = 0.22;
			p.AUL = -100.0;
			p.AFL = -8.0;
			p.AUR = 130.0;
			p.AFR = 30.0;
			p.RootRot = -3.0;
		}
		else if (f < 480.0)
		{
			int s2 = (int)Math.Floor((f - 420.0) / 30.0);
			double t2 = (f - 420.0) % 30.0 / 30.0;
			p.RootX = -50.0 + (double)s2 * 50.0 + 50.0 * EaseInOut(t2);
			p.FRx = 30.0 + 26.0 * Math.Sin(Math.PI * t2);
			p.FRy = 118.0 - 22.0 * Math.Sin(Math.PI * t2);
			p.AUL = -80.0;
			p.AFL = -12.0;
			p.AUR = 84.0;
			p.AFR = 12.0;
			p.ShL = -8.0 * Math.Sin(Math.PI * t2);
			p.ShR = 8.0 * Math.Sin(Math.PI * t2);
			p.Knee = 0.3;
			p.Lean = -4.0 * Math.Sin(Math.PI * t2);
		}
		else if (f < 540.0)
		{
			int step2 = (int)Math.Floor((f - 480.0) / 30.0);
			double st2 = (f - 480.0) % 30.0 / 30.0;
			p = MoonSlide(WebBase(-30.0, 30.0), step2, st2, -1);
			p.RootX = -45.0 + (double)step2 * 22.0 + 22.0 * EaseInOut(st2);
		}
		else
		{
			p.AUR = 152.0;
			p.AFR = 20.0;
			p.AUL = -88.0;
			p.AFL = -10.0;
			p.Knee = 0.38;
			p.Lean = -5.0;
			p.Head = 6.0;
		}
		return p;
	}

	private static readonly double[] MoonBounds = new double[16]
	{
		0.0, 30.0, 60.0, 180.0, 210.0, 220.0, 230.0, 240.0, 250.0, 260.0, 270.0, 330.0, 390.0, 420.0, 480.0, 540.0
	};

	private static readonly double[] MoonBlends = new double[16]
	{
		6.0, 6.0, 6.0, 6.0, 6.0, 3.0, 3.0, 3.0, 3.0, 3.0, 6.0, 6.0, 6.0, 6.0, 6.0, 6.0
	};

	private static WebPose MoonPoseAt(double f)
	{
		return WebMorph(f, MoonBounds, MoonBlends, MoonRawAt);
	}

	private Pose[] Moonwalk(int n, Tempo T)
	{
		if (n < 8)
		{
			return Ring(n, T);
		}
		double f = EngineFrame(T, 15.0, MoonFrames);
		double a = Math.Min(Amp, 1.6);
		WebPose webPose = MoonPoseAt(f);
		Body b = WebBody(webPose, a);
		Skeleton sk = BuildSkeleton(b);
		Pose[] array = FigureFromSkeleton(n, sk, 0.0);
		bool tilting = f >= 270.0 && f < 330.0;
		Break(array, MoonPhase, f, (tilting ? 0.0 : (webPose.Trem + 0.04)) * a * 1.4);
		return array;
	}

	// ---------- 科目三：subject-three.html 帧驱动引擎逐段翻译 ----------
	// 540 帧 @30fps、每拍 15 帧 = 36 拍：抱拳鞠躬开场 2 拍 + 十步套路（每步 48 帧）+ 定格 2 拍。
	// 下身全程程序化：左右交替踮脚外翻 + 双膝同向弯 + 胯画 8 字。
	// 摇花手：腕部圆锥绕圈，基础 2.50Hz/±32°，第⑦步高潮 3.75Hz/±55°，半径 8.7→15.0，左右反相。

	private static void Subj3Arms(int n, double t, ref WebPose p)
	{
		switch (n)
		{
		case 1:
		{
			double w1 = Math.Sin(Math.PI * 2.0 * 4.0 * t);
			p.AUL = -55.0 - 35.0 * w1;
			p.AFL = 12.0;
			p.AUR = 55.0 - 35.0 * w1;
			p.AFR = -12.0;
			break;
		}
		case 2:
		{
			double a2 = Math.PI * 2.0 * 2.0 * t;
			p.AUL = 70.0 + 62.0 * Math.Cos(a2);
			p.AFL = 42.0 * Math.Sin(a2);
			p.AUR = -70.0 + 62.0 * Math.Cos(a2 + Math.PI);
			p.AFR = -42.0 * Math.Sin(a2 + Math.PI);
			break;
		}
		case 3:
		{
			double h3 = ((t < 0.5) ? (t / 0.5) : ((t - 0.5) / 0.5));
			p.AUL = -30.0;
			p.AFL = 18.0;
			p.AUR = ((t < 0.5) ? M.Lerp(20.0, -75.0, EaseInOut(h3)) : M.Lerp(-75.0, 155.0, EaseInOut(h3)));
			p.AFR = ((t < 0.5) ? (-18.0) : (-30.0));
			break;
		}
		case 4:
		{
			int k4 = (int)Math.Min(4.0, Math.Floor(t * 5.0));
			double u4 = t * 5.0 - (double)k4;
			double[,] seq = new double[5, 4]
			{
				{ 20.0, 10.0, -20.0, -10.0 },
				{ -60.0, 150.0, 60.0, -150.0 },
				{ -92.0, -4.0, 92.0, 4.0 },
				{ 62.0, -78.0, -62.0, 78.0 },
				{ -88.0, -6.0, 88.0, 6.0 }
			};
			int k4n = Math.Min(4, k4 + 1);
			double q4 = EaseInOut(u4);
			p.AUL = M.Lerp(seq[k4, 0], seq[k4n, 0], q4);
			p.AFL = M.Lerp(seq[k4, 1], seq[k4n, 1], q4);
			p.AUR = M.Lerp(seq[k4, 2], seq[k4n, 2], q4);
			p.AFR = M.Lerp(seq[k4, 3], seq[k4n, 3], q4);
			break;
		}
		case 5:
		{
			double u5 = ((t < 0.45) ? (t / 0.45) : ((t - 0.45) / 0.55));
			if (t < 0.45)
			{
				p.AUL = M.Lerp(-20.0, 55.0, EaseInOut(u5));
				p.AFL = M.Lerp(10.0, -70.0, EaseInOut(u5));
				p.AUR = M.Lerp(20.0, -55.0, EaseInOut(u5));
				p.AFR = M.Lerp(-10.0, 70.0, EaseInOut(u5));
			}
			else
			{
				p.AUL = M.Lerp(55.0, 165.0, EaseInOut(u5));
				p.AFL = M.Lerp(-70.0, 18.0, EaseInOut(u5));
				p.AUR = M.Lerp(-55.0, -165.0, EaseInOut(u5));
				p.AFR = M.Lerp(70.0, -18.0, EaseInOut(u5));
			}
			break;
		}
		case 6:
		{
			double lo6;
			double hi6;
			double cyc6;
			if (t < 0.28)
			{
				lo6 = 0.0;
				hi6 = 0.28;
				cyc6 = 3.0;
			}
			else if (t < 0.72)
			{
				lo6 = 0.28;
				hi6 = 0.72;
				cyc6 = 1.0;
			}
			else
			{
				lo6 = 0.72;
				hi6 = 1.0;
				cyc6 = 3.0;
			}
			double w6 = Math.Sin(Math.PI * 2.0 * cyc6 * ((t - lo6) / (hi6 - lo6)));
			p.AUL = -30.0 + 55.0 * w6;
			p.AFL = 26.0;
			p.AUR = 30.0 + 55.0 * w6;
			p.AFR = -26.0;
			break;
		}
		case 7:
		{
			double u7 = EaseInOut(Math.Min(1.0, t * 1.3));
			p.AUL = M.Lerp(62.0, 168.0, u7);
			p.AFL = M.Lerp(-72.0, 12.0, u7);
			p.AUR = M.Lerp(-62.0, -168.0, u7);
			p.AFR = M.Lerp(72.0, -12.0, u7);
			break;
		}
		case 8:
		{
			double a8 = Math.PI * 2.0 * 2.0 * t;
			p.AUL = -96.0;
			p.AFL = -6.0;
			p.AUR = 40.0 + 75.0 * Math.Cos(a8);
			p.AFR = 60.0 * Math.Sin(a8);
			break;
		}
		case 9:
		{
			double w9 = Math.Sin(Math.PI * 2.0 * 2.0 * t);
			double up = Math.Max(0.0, w9);
			double dn = Math.Max(0.0, 0.0 - w9);
			p.AUL = -96.0 + 20.0 * dn;
			p.AFL = -6.0 - 60.0 * up;
			p.AUR = 96.0 - 20.0 * up;
			p.AFR = 6.0 + 60.0 * dn;
			break;
		}
		case 10:
			p.AUL = 70.0;
			p.AFL = -85.0;
			p.AUR = -70.0;
			p.AFR = 85.0;
			break;
		}
	}

	private static WebPose Subj3RawAt(double f)
	{
		WebPose p = WebBase(-30.0, 30.0);
		double hip = Math.PI * 2.0 * 1.25 * f / 30.0;
		double sw = Math.Sin(hip);
		double sw2 = Math.Sin(2.0 * hip);
		p.X = 11.0 * sw;
		p.Lean = 7.0 * sw;
		p.Head = -2.0 * sw;
		p.Knee = 0.3 + 0.1 * Math.Abs(sw);
		p.Y = 2.0 * Math.Abs(sw);
		p.ShL = -4.0 * sw;
		p.ShR = 4.0 * sw;
		p.HeelLiftL = Math.Max(0.0, sw);
		p.HeelLiftR = Math.Max(0.0, 0.0 - sw);
		p.FootRollL = -38.0 * Math.Max(0.0, sw);
		p.FootRollR = 38.0 * Math.Max(0.0, 0.0 - sw);
		p.FLx = -30.0 - 7.0 * sw;
		p.FLy = 118.0;
		p.FRx = 30.0 - 7.0 * sw;
		p.FRy = 118.0;
		p.RootX = 5.0 * sw2;
		if (f < 30.0)
		{
			double s0 = Math.Sin(Math.PI * f / 30.0);
			p.AUL = 70.0;
			p.AFL = -85.0;
			p.AUR = -70.0;
			p.AFR = 85.0;
			p.Lean = 12.0 * s0;
			p.RootRot = 4.0 * s0;
			p.Head = 8.0 * s0;
		}
		else if (f < 510.0)
		{
			Subj3Arms((int)((f - 30.0) / 48.0) + 1, (f - 30.0) % 48.0 / 48.0, ref p);
		}
		else
		{
			p.AUL = 70.0;
			p.AFL = -85.0;
			p.AUR = -70.0;
			p.AFR = 85.0;
		}
		bool hot = f >= (double)FlowerHotS && f < (double)FlowerHotE;
		double ampDeg = (hot ? FlowerFastAmp : FlowerAmp);
		p.WristAmp = FlowerRadius * ampDeg / FlowerFastAmp;
		double phase = Subj3Phase[(int)Math.Min(f, Subj3Frames - 1.0)] * 180.0 / Math.PI;
		p.WristPhaseL = phase;
		p.WristPhaseR = phase + 180.0;
		p.Trem = (hot ? 0.14 : 0.05);
		return p;
	}

	private static readonly double[] Subj3Bounds = new double[11]
	{
		30.0, 78.0, 126.0, 174.0, 222.0, 270.0, 318.0, 366.0, 414.0, 462.0, 510.0
	};

	private static readonly double[] Subj3Blends = new double[11]
	{
		6.0, 6.0, 6.0, 6.0, 6.0, 6.0, 6.0, 6.0, 6.0, 6.0, 6.0
	};

	private static WebPose Subj3PoseAt(double f)
	{
		return WebMorph(f, Subj3Bounds, Subj3Blends, Subj3RawAt);
	}

	private Pose[] Subject3(int n, Tempo T)
	{
		if (n < 8)
		{
			return Ring(n, T);
		}
		double f = EngineFrame(T, 15.0, Subj3Frames);
		double a = Math.Min(Amp, 1.6);
		WebPose webPose = Subj3PoseAt(f);
		Body b = WebBody(webPose, a);
		Skeleton sk = BuildSkeleton(b);
		Pose[] array = FigureFromSkeleton(n, sk, 0.0);
		Break(array, Subj3Phase, f, webPose.Trem * (0.9 + 0.6 * T.Energy) * a * 1.6);
		return array;
	}

	private Pose[] Rehearsal(int n, Tempo T)
	{
		double num = 0.0;
		double[] reelBeats = ReelBeats;
		foreach (double num2 in reelBeats)
		{
			num += num2;
		}
		double num3 = T.Beat - modeBeat0;
		double num4 = num3 - Math.Floor(num3 / num) * num;
		if (num4 < 0.0)
		{
			num4 += num;
		}
		int num5 = ReelModes.Length - 1;
		double num6 = num4;
		for (int j = 0; j < ReelBeats.Length; j++)
		{
			if (num6 < ReelBeats[j] || j == ReelBeats.Length - 1)
			{
				num5 = j;
				break;
			}
			num6 -= ReelBeats[j];
		}
		num6 = M.Clamp(num6, 0.0, ReelBeats[num5]);
		DanceMode danceMode = (rehearsalSegment = ReelModes[num5]);
		double num7 = modeBeat0;
		double num8 = clipTime;
		double num9 = figPhase;
		modeBeat0 = T.Beat - num6;
		Pose[] result;
		switch (danceMode)
		{
		case DanceMode.Flash:
			result = Flash(n, T);
			break;
		case DanceMode.Shiver:
			result = Shiver(n, T);
			break;
		case DanceMode.Moonwalk:
			result = Moonwalk(n, T);
			break;
		case DanceMode.Subject3:
			result = Subject3(n, T);
			break;
		default:
		{
			double num10 = num6 / ReelBeats[num5];
			if (Clip != null)
			{
				clipTime = num10 * Clip.Duration;
			}
			else
			{
				figPhase = num10 * 16.0;
			}
			result = Figure(n, T);
			break;
		}
		}
		modeBeat0 = num7;
		clipTime = num8;
		figPhase = num9;
		return result;
	}

	private Pose[] Free(int n, Tempo T)
	{
		RectD stage = Stage;
		double baseScale = BaseScale;
		double beat = T.Beat;
		double energy = T.Energy;
		double pulse = T.Pulse;
		if (freePos.Length != n)
		{
			freePos = new Vec[n];
			for (int i = 0; i < n; i++)
			{
				freePos[i] = ((i < Homes.Length) ? Homes[i] : new Vec(stage.MidX, stage.MidY));
			}
			freeVel = new Vec[n];
		}
		double num = Math.Min(stage.W, stage.H) * 0.26;
		double num2 = ((stage.H > stage.W) ? 1.25 : 0.85);
		double num3 = Math.Min(Math.Max(T.Dt, 0.0), 1.0 / 30.0);
		Pose[] array = new Pose[n];
		for (int j = 0; j < n; j++)
		{
			double num4 = num * (0.3 + 0.7 * Math.Sqrt(M.Hash01(j, 11)));
			double num5 = 0.0 - (0.22 + 0.55 * M.Hash01(j, 23));
			double num6 = Math.PI * 2.0 * M.Hash01(j, 37) + num5 * beat;
			double num7 = 1.0 + pulse * (0.7 + 1.0 * M.Hash01(j, 53)) * Amp * (0.5 + 0.8 * energy);
			Vec vec = new Vec(stage.MidX + num4 * num7 * Math.Cos(num6), stage.MidY + num4 * num7 * Math.Sin(num6) * num2);
			if (num3 > 0.0)
			{
				Vec vec2 = (vec - freePos[j]) * 95.0 - freeVel[j] * 9.5;
				freeVel[j] += vec2 * num3;
				freePos[j] += freeVel[j] * num3;
			}
			else
			{
				freePos[j] = vec;
			}
			double rot = M.Clamp(freeVel[j].X / 1800.0, -0.35, 0.35);
			double num8 = baseScale * (0.9 + 0.14 * pulse);
			array[j] = new Pose(freePos[j], rot, num8, num8, 1.0, j);
		}
		return array;
	}
}
