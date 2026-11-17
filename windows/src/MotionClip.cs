using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace DesktopDance;

public sealed class MotionClip
{
	public struct Sample
	{
		public Vec[] Joints;

		public Vec Ball;

		public double BallAlpha;
	}

	public const int JointCount = 17;

	private static readonly string[] JointNames = new string[17]
	{
		"nose", "neck", "lsho", "rsho", "lelb", "relb", "lwri", "rwri", "root", "lhip",
		"rhip", "lkne", "rkne", "lank", "rank", "lear", "rear"
	};

	private readonly double dt;

	private readonly int count;

	private readonly Vec[][] tracks;

	private readonly Vec[] ballTrack;

	private readonly double[] ballVisible;

	private readonly int[] cutFrames;

	public double Duration { get; }

	public int FrameCount => count;

	public double MaxAbsX { get; private set; } = 0.5;

	public double MaxY { get; private set; } = 1.0;

	public double MaxRootX { get; private set; }

	public double MaxLocalX { get; private set; } = 0.3;

	public static MotionClip? LoadDefault()
	{
		try
		{
			if (File.Exists(AppPaths.MotionOverride))
			{
				using FileStream stream = File.OpenRead(AppPaths.MotionOverride);
				MotionClip motionClip = TryParse(stream, "外部文件 " + AppPaths.MotionOverride);
				if (motionClip != null)
				{
					return motionClip;
				}
			}
		}
		catch (Exception ex)
		{
			Log.Write("读外部动作数据失败：" + ex.Message);
		}
		return LoadEmbedded();
	}

	public static MotionClip? LoadEmbedded()
	{
		try
		{
			using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("basketball-motion.json");
			if (stream == null)
			{
				Log.Write("exe 里没有找到动作数据");
				return null;
			}
			return TryParse(stream, "内置动作数据");
		}
		catch (Exception ex)
		{
			Log.Write("读内置动作数据失败：" + ex.Message);
			return null;
		}
	}

	public static MotionClip? TryParse(Stream stream, string what)
	{
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(stream);
			return new MotionClip(jsonDocument.RootElement);
		}
		catch (Exception ex)
		{
			Log.Write(what + " 用不了：" + ex.Message);
			return null;
		}
	}

	private MotionClip(JsonElement root)
	{
		JsonElement property = root.GetProperty("frames");
		int n = property.GetArrayLength();
		if (n < 8)
		{
			throw new InvalidDataException("帧数太少");
		}
		double num = root.GetProperty("aspect").GetDouble();
		double[] array = new double[n];
		Vec?[][] J = new Vec?[17][];
		for (int i = 0; i < 17; i++)
		{
			J[i] = new Vec?[n];
		}
		Vec?[] array2 = new Vec?[n];
		int num2 = 0;
		foreach (JsonElement item6 in property.EnumerateArray())
		{
			array[num2] = item6.GetProperty("t").GetDouble();
			if (item6.TryGetProperty("j", out var value) && value.ValueKind == JsonValueKind.Object)
			{
				for (int j = 0; j < 17; j++)
				{
					if (value.TryGetProperty(JointNames[j], out var value2) && value2.ValueKind == JsonValueKind.Array && value2.GetArrayLength() >= 3 && value2[2].GetDouble() > 0.25)
					{
						J[j][num2] = new Vec(value2[0].GetDouble() * num, value2[1].GetDouble());
					}
				}
			}
			if (item6.TryGetProperty("ball", out var value3) && value3.ValueKind == JsonValueKind.Array && value3.GetArrayLength() >= 4 && value3[3].GetDouble() > 0.3)
			{
				array2[num2] = new Vec(value3[0].GetDouble() * num, value3[1].GetDouble());
			}
			num2++;
		}
		count = n;
		double num3 = array[n - 1] - array[0];
		if (num3 <= 0.0)
		{
			throw new InvalidDataException("时间戳不对");
		}
		dt = num3 / (double)(n - 1);
		Duration = num3 + dt;
		List<int> list = new List<int> { 0 };
		if (root.TryGetProperty("cuts", out var value4) && value4.ValueKind == JsonValueKind.Array)
		{
			List<double> list2 = new List<double>();
			foreach (JsonElement item7 in value4.EnumerateArray())
			{
				list2.Add(item7.GetDouble());
			}
			list2.Sort();
			foreach (double c in list2)
			{
				int num4 = Array.FindIndex(array, (double t) => t >= c - 1E-06);
				if (num4 >= 0 && num4 - list[list.Count - 1] >= 5 && n - num4 >= 5)
				{
					list.Add(num4);
				}
			}
		}
		cutFrames = list.Skip(1).ToArray();
		List<(int, int)> list3 = new List<(int, int)>();
		for (int num5 = 0; num5 < list.Count; num5++)
		{
			list3.Add((list[num5], (num5 + 1 < list.Count) ? list[num5 + 1] : n));
		}
		for (int num6 = 0; num6 < 17; num6++)
		{
			foreach (var (a, b) in list3)
			{
				Fill(J[num6], a, b, int.MaxValue);
			}
		}
		Vec?[] array3 = J[1];
		Vec?[] source = J[8];
		if (array3.Any((Vec? p) => !p.HasValue) || source.Any((Vec? p) => !p.HasValue))
		{
			throw new InvalidDataException("脖子或胯的数据不全");
		}
		Vec[] neck = array3.Select((Vec? p) => p.Value).ToArray();
		Vec[] rootT = source.Select((Vec? p) => p.Value).ToArray();
		(Joint, Joint, double)[] array4 = new(Joint, Joint, double)[10]
		{
			(Joint.Neck, Joint.Root, 0.29),
			(Joint.LHip, Joint.LKne, 0.245),
			(Joint.RHip, Joint.RKne, 0.245),
			(Joint.LKne, Joint.LAnk, 0.246),
			(Joint.RKne, Joint.RAnk, 0.246),
			(Joint.LSho, Joint.LElb, 0.172),
			(Joint.RSho, Joint.RElb, 0.172),
			(Joint.LElb, Joint.LWri, 0.157),
			(Joint.RElb, Joint.RWri, 0.157),
			(Joint.LSho, Joint.RSho, 0.2)
		};
		double[] array5 = new double[n];
		List<double> list4 = new List<double>();
		for (int num7 = 0; num7 < n; num7++)
		{
			list4.Clear();
			(Joint, Joint, double)[] array6 = array4;
			for (int num8 = 0; num8 < array6.Length; num8++)
			{
				(Joint, Joint, double) tuple2 = array6[num8];
				Joint item = tuple2.Item1;
				Joint item2 = tuple2.Item2;
				double item3 = tuple2.Item3;
				Vec? vec = J[(int)item][num7];
				Vec? vec2 = J[(int)item2][num7];
				if (vec.HasValue && vec2.HasValue)
				{
					list4.Add(M.Hypot(vec.Value.X - vec2.Value.X, vec.Value.Y - vec2.Value.Y) / item3);
				}
			}
			list4.Sort((double x, double y) => y.CompareTo(x));
			array5[num7] = ((list4.Count >= 3) ? list4[2] : ((list4.Count > 0) ? list4[list4.Count - 1] : 0.5));
		}
		int num9 = Math.Max(1, (int)M.Round(1.0 / dt));
		double[] unit = new double[n];
		for (int num10 = 0; num10 < n; num10++)
		{
			int num11 = Math.Max(0, num10 - num9);
			double[] array7 = new double[Math.Min(n - 1, num10 + num9) - num11 + 1];
			Array.Copy(array5, num11, array7, 0, array7.Length);
			Array.Sort(array7);
			unit[num10] = Math.Max(array7[Math.Min(array7.Length - 1, array7.Length * 8 / 10)], 0.02);
		}
		unit = BoxSmooth(unit, num9 / 2);
		Vec[] lsho = Or(Joint.LSho, (int num39) => new Vec(neck[num39].X + unit[num39] * 0.09, neck[num39].Y));
		Vec[] rsho = Or(Joint.RSho, (int num39) => new Vec(neck[num39].X - unit[num39] * 0.09, neck[num39].Y));
		Vec[] lelb = Or(Joint.LElb, (int num39) => new Vec(lsho[num39].X, lsho[num39].Y - unit[num39] * 0.15));
		Vec[] relb = Or(Joint.RElb, (int num39) => new Vec(rsho[num39].X, rsho[num39].Y - unit[num39] * 0.15));
		Vec[] array8 = Or(Joint.LWri, (int num39) => new Vec(lelb[num39].X, lelb[num39].Y - unit[num39] * 0.15));
		Vec[] array9 = Or(Joint.RWri, (int num39) => new Vec(relb[num39].X, relb[num39].Y - unit[num39] * 0.15));
		Vec[] lhip = Or(Joint.LHip, (int num39) => new Vec(rootT[num39].X + unit[num39] * 0.05, rootT[num39].Y));
		Vec[] rhip = Or(Joint.RHip, (int num39) => new Vec(rootT[num39].X - unit[num39] * 0.05, rootT[num39].Y));
		Vec[] lkne = Or(Joint.LKne, (int num39) => new Vec(lhip[num39].X, lhip[num39].Y - unit[num39] * 0.21));
		Vec[] rkne = Or(Joint.RKne, (int num39) => new Vec(rhip[num39].X, rhip[num39].Y - unit[num39] * 0.21));
		Vec[] array10 = Or(Joint.LAnk, (int num39) => new Vec(lkne[num39].X, lkne[num39].Y - unit[num39] * 0.21));
		Vec[] array11 = Or(Joint.RAnk, (int num39) => new Vec(rkne[num39].X, rkne[num39].Y - unit[num39] * 0.21));
		Vec[] nose = Or(Joint.Nose, (int num39) => new Vec(neck[num39].X, neck[num39].Y + unit[num39] * 0.11));
		Vec[] array12 = Or(Joint.LEar, (int num39) => nose[num39]);
		Vec[] array13 = Or(Joint.REar, (int num39) => nose[num39]);
		Vec[][] array14 = new Vec[17][]
		{
			nose, neck, lsho, rsho, lelb, relb, array8, array9, rootT, lhip,
			rhip, lkne, rkne, array10, array11, array12, array13
		};
		foreach (var (a2, b2) in list3)
		{
			Fill(array2, a2, b2, 8);
		}
		double[] array15 = new double[n];
		Vec[] array16 = new Vec[n];
		for (int num12 = 0; num12 < n; num12++)
		{
			array15[num12] = (array2[num12].HasValue ? 1 : 0);
			array16[num12] = array2[num12] ?? new Vec(array9[num12].X, array9[num12].Y - unit[num12] * 0.07);
		}
		Vec? vec3 = null;
		for (int num13 = 0; num13 < n; num13++)
		{
			if (array2[num13].HasValue)
			{
				vec3 = array16[num13];
			}
			else if (vec3.HasValue)
			{
				array16[num13] = vec3.Value;
			}
		}
		double[] array17 = new double[n];
		for (int num14 = 0; num14 < n; num14++)
		{
			double num15 = 0.0;
			for (int num16 = -2; num16 <= 2; num16++)
			{
				num15 += array15[Math.Min(Math.Max(num14 + num16, 0), n - 1)];
			}
			array17[num14] = num15 / 5.0;
		}
		array15 = array17;
		for (int num17 = 0; num17 < 17; num17++)
		{
			Joint joint = (Joint)num17;
			bool flag = joint == Joint.LWri || joint == Joint.RWri || joint == Joint.LAnk || joint == Joint.RAnk;
			foreach (var (a3, b3) in list3)
			{
				array14[num17] = Smoothed(array14[num17], a3, b3, flag ? 1 : 2);
			}
		}
		foreach (var item8 in list3)
		{
			int item4 = item8.Item1;
			int item5 = item8.Item2;
			array16 = Smoothed(array16, item4, item5, 1);
		}
		double[] array18 = new double[n];
		for (int num18 = 0; num18 < n; num18++)
		{
			array18[num18] = Math.Min(array14[13][num18].Y, array14[14][num18].Y);
		}
		double[] array19 = new double[n];
		for (int num19 = 0; num19 < n; num19++)
		{
			double num20 = double.MaxValue;
			for (int num21 = Math.Max(0, num19 - num9); num21 <= Math.Min(n - 1, num19 + num9); num21++)
			{
				num20 = Math.Min(num20, array18[num21]);
			}
			array19[num19] = num20;
		}
		array19 = BoxSmooth(array19, num9 / 2);
		double num22 = ((array3.Length != 0) ? (from p in source
			select p.Value.X into x
			orderby x
			select x).ElementAt(n / 2) : 0.0);
		for (int num23 = 0; num23 < n; num23++)
		{
			double num24 = unit[num23];
			double num25 = array19[num23] - num24 * 0.03;
			for (int num26 = 0; num26 < 17; num26++)
			{
				array14[num26][num23] = new Vec((array14[num26][num23].X - num22) / num24, (array14[num26][num23].Y - num25) / num24);
			}
			array16[num23] = new Vec((array16[num23].X - num22) / num24, (array16[num23].Y - num25) / num24);
		}
		double[] array20 = new double[n];
		for (int num27 = 0; num27 < n; num27++)
		{
			array20[num27] = (array14[0][num27].Y + array14[15][num27].Y + array14[16][num27].Y) / 3.0;
		}
		double num28 = Pct(array20, 0.9) + 0.085;
		if (num28 > 0.3)
		{
			double num29 = 1.0 / num28;
			for (int num30 = 0; num30 < 17; num30++)
			{
				for (int num31 = 0; num31 < n; num31++)
				{
					array14[num30][num31] = array14[num30][num31] * num29;
				}
			}
			for (int num32 = 0; num32 < n; num32++)
			{
				array16[num32] *= num29;
			}
		}
		tracks = array14;
		ballTrack = array16;
		ballVisible = array15;
		Vec[] array21 = tracks[8];
		double[] array22 = new double[n];
		double[] array23 = new double[n];
		double[] array24 = new double[n];
		double num33 = 0.0;
		for (int num34 = 0; num34 < n; num34++)
		{
			double num35 = 0.0;
			double num36 = 0.0;
			double num37 = 0.0;
			for (int num38 = 0; num38 < 17; num38++)
			{
				Vec vec4 = tracks[num38][num34];
				num35 = Math.Max(num35, vec4.Y);
				num36 = Math.Max(num36, Math.Abs(vec4.X - array21[num34].X));
				num37 = Math.Max(num37, Math.Abs(vec4.X));
			}
			if (array15[num34] > 0.5)
			{
				num36 = Math.Max(num36, Math.Abs(array16[num34].X - array21[num34].X));
				num37 = Math.Max(num37, Math.Abs(array16[num34].X));
			}
			array22[num34] = num35;
			array23[num34] = num36;
			array24[num34] = num37;
			num33 = Math.Max(num33, Math.Abs(array21[num34].X));
		}
		MaxY = Math.Max(array22.Max(), 1.0) + 0.1;
		MaxAbsX = Pct(array24, 0.97) + 0.08;
		MaxLocalX = Pct(array23, 0.97) + 0.08;
		MaxRootX = num33;
		Vec[] Or(Joint joint2, Func<int, Vec> fb)
		{
			Vec?[] array25 = J[(int)joint2];
			Vec[] array26 = new Vec[n];
			for (int k = 0; k < n; k++)
			{
				array26[k] = array25[k] ?? fb(k);
			}
			return array26;
		}
	}

	private static void Fill(Vec?[] pts, int a, int b, int maxGap)
	{
		List<int> list = new List<int>();
		for (int i = a; i < b; i++)
		{
			if (pts[i].HasValue)
			{
				list.Add(i);
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		Vec?[] array = (Vec?[])pts.Clone();
		for (int j = a; j < b; j++)
		{
			if (array[j].HasValue)
			{
				continue;
			}
			int num = -1;
			int num2 = -1;
			foreach (int item in list)
			{
				if (item < j)
				{
					num = item;
				}
				else if (item > j)
				{
					num2 = item;
					break;
				}
			}
			if (num >= 0 && num2 >= 0)
			{
				if (num2 - num - 1 <= maxGap)
				{
					pts[j] = M.Lerp(array[num].Value, array[num2].Value, (double)(j - num) / (double)(num2 - num));
				}
			}
			else if (maxGap == int.MaxValue)
			{
				pts[j] = array[(num >= 0) ? num : num2];
			}
		}
	}

	private static Vec[] Smoothed(Vec[] t, int a, int b, int radius)
	{
		Vec[] array = (Vec[])t.Clone();
		if (radius <= 0)
		{
			return array;
		}
		for (int i = a; i < b; i++)
		{
			double num = 0.0;
			double num2 = 0.0;
			double num3 = 0.0;
			for (int j = -radius; j <= radius; j++)
			{
				int num4 = Math.Min(Math.Max(i + j, a), b - 1);
				double num5 = radius + 1 - Math.Abs(j);
				num += t[num4].X * num5;
				num2 += t[num4].Y * num5;
				num3 += num5;
			}
			array[i] = new Vec(num / num3, num2 / num3);
		}
		return array;
	}

	private static double[] BoxSmooth(double[] v, int r)
	{
		double[] array = new double[v.Length];
		for (int i = 0; i < v.Length; i++)
		{
			int num = Math.Max(0, i - r);
			int num2 = Math.Min(v.Length - 1, i + r);
			double num3 = 0.0;
			for (int j = num; j <= num2; j++)
			{
				num3 += v[j];
			}
			array[i] = num3 / (double)(num2 - num + 1);
		}
		return array;
	}

	private static double Pct(double[] v, double p)
	{
		double[] array = (double[])v.Clone();
		Array.Sort(array);
		return array[Math.Min(array.Length - 1, (int)((double)array.Length * p))];
	}

	private Sample Frame(double f)
	{
		double num = Math.Min(Math.Max(f, 0.0), count - 1);
		int num2 = (int)num;
		int num3 = Math.Min(num2 + 1, count - 1);
		if (Array.IndexOf(cutFrames, num3) >= 0)
		{
			num3 = num2;
		}
		double t = num - (double)num2;
		Vec[] array = new Vec[17];
		for (int i = 0; i < 17; i++)
		{
			array[i] = M.Lerp(tracks[i][num2], tracks[i][num3], t);
		}
		return new Sample
		{
			Joints = array,
			Ball = M.Lerp(ballTrack[num2], ballTrack[num3], t),
			BallAlpha = M.Lerp(ballVisible[num2], ballVisible[num3], t)
		};
	}

	private static Sample Blend(Sample a, Sample b, double w)
	{
		Vec ball = ((a.BallAlpha < 0.05) ? b.Ball : ((b.BallAlpha < 0.05) ? a.Ball : M.Lerp(a.Ball, b.Ball, w)));
		Vec[] array = new Vec[17];
		for (int i = 0; i < 17; i++)
		{
			array[i] = M.Lerp(a.Joints[i], b.Joints[i], w);
		}
		return new Sample
		{
			Joints = array,
			Ball = ball,
			BallAlpha = M.Lerp(a.BallAlpha, b.BallAlpha, w)
		};
	}

	public Sample SampleAt(double time)
	{
		double num = time % Duration;
		if (num < 0.0)
		{
			num += Duration;
		}
		double num2 = num / dt;
		Sample sample = Frame(num2);
		double num3 = 0.2 / dt;
		int num4 = -1;
		int[] array = cutFrames;
		foreach (int num5 in array)
		{
			if ((double)num5 <= num2)
			{
				num4 = num5;
			}
		}
		if (num4 >= 0 && num2 - (double)num4 < num3)
		{
			return Blend(Frame(num4 - 1), sample, M.Smooth((num2 - (double)num4) / num3));
		}
		if (num > Duration - 0.2)
		{
			return Blend(sample, Frame(0.0), M.Smooth((num - (Duration - 0.2)) / 0.2));
		}
		return sample;
	}
}
