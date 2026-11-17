using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DesktopDance;

public static class SelfTest
{
	private static readonly StringBuilder report = new StringBuilder();

	private static bool failed;

	private static void Line(string s)
	{
		report.AppendLine(s);
	}

	private static void Check(bool ok, string what)
	{
		Line((ok ? "[通过] " : "[失败] ") + what);
		if (!ok)
		{
			failed = true;
		}
	}

	public static int Run(string outDir)
	{
		try
		{
			Directory.CreateDirectory(outDir);
		}
		catch
		{
		}
		Line("桌面图标跳舞 Windows 版 · 自检 · " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
		Line("系统：" + Environment.OSVersion.VersionString);
		Line("");
		Line("== 一、计算部分 ==");
		MotionClip clip = null;
		try
		{
			clip = Logic();
		}
		catch (Exception ex)
		{
			failed = true;
			Line("[失败] 计算部分抛出异常：" + ex);
		}
		Line("");
		Line("== 二、探测这台机器（只记录，不算通过或失败）==");
		LoadResult res = null;
		try
		{
			res = Probe();
		}
		catch (Exception ex2)
		{
			Line("探测抛出异常：" + ex2);
		}
		try
		{
			Render(outDir, res, clip);
		}
		catch (Exception ex3)
		{
			Line("离屏渲染抛出异常：" + ex3);
		}
		try
		{
			PanelProbe(outDir, res);
		}
		catch (Exception ex4)
		{
			Line("控制窗口排版探测抛出异常：" + ex4);
		}
		L.Current = Lang.Zh;
		Line("");
		Line(failed ? "结论：计算部分有失败项。" : "结论：计算部分全部通过。窗口层级、热键、系统声音没有在这里测。");
		try
		{
			File.WriteAllText(Path.Combine(outDir, "selftest.txt"), report.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
		}
		catch
		{
		}
		return failed ? 1 : 0;
	}

	private static bool Finite(double v)
	{
		if (!double.IsNaN(v))
		{
			return !double.IsInfinity(v);
		}
		return false;
	}

	private static Pose[] GridHomes(int n, double w, double h)
	{
		Pose[] array = new Pose[n];
		int num = Math.Max(1, (int)((h - 60.0) / 100.0));
		for (int i = 0; i < n; i++)
		{
			array[i] = new Pose(new Vec(40 + i / num * 76, h - 60.0 - (double)(i % num * 100)), 0.0, 1.0, 1.0, 1.0, i);
		}
		return array;
	}

	private static MotionClip? Logic()
	{
		MotionClip motionClip = MotionClip.LoadEmbedded();
		Check(motionClip != null, "exe 里带着动作数据，并且能读出来");
		if (motionClip != null)
		{
			Line("    帧数 " + motionClip.FrameCount + "，时长 " + motionClip.Duration.ToString("0.00") + " 秒，最高点 " + motionClip.MaxY.ToString("0.000") + "，横向 " + motionClip.MaxAbsX.ToString("0.000") + "，肢体 " + motionClip.MaxLocalX.ToString("0.000") + "，走位 " + motionClip.MaxRootX.ToString("0.000"));
			Check(motionClip.Duration > 5.0 && motionClip.Duration < 120.0, "动作时长合理");
			Check(motionClip.MaxY > 1.0 && motionClip.MaxY < 2.5, "小人高度合理（最高点在 1～2.5 个身高之间）");
			bool ok = true;
			double num = 9.0;
			double num2 = -9.0;
			for (int i = 0; i < 400; i++)
			{
				MotionClip.Sample sample = motionClip.SampleAt(motionClip.Duration * 1.5 * (double)i / 400.0);
				Vec[] joints = sample.Joints;
				for (int j = 0; j < joints.Length; j++)
				{
					Vec vec = joints[j];
					if (!Finite(vec.X) || !Finite(vec.Y))
					{
						ok = false;
					}
				}
				if (!Finite(sample.Ball.X) || !Finite(sample.Ball.Y) || !Finite(sample.BallAlpha))
				{
					ok = false;
				}
				double y = sample.Joints[0].Y;
				num = Math.Min(num, y);
				num2 = Math.Max(num2, y);
			}
			Check(ok, "动作数据取样 400 次，全部是正常数字");
			Line("    鼻子高度范围 " + num.ToString("0.00") + " ～ " + num2.ToString("0.00") + "（单位：身高）");
			Check(num > 0.2 && num2 < 2.0, "头的位置合理");
		}
		(string, RectD)[] array = new(string, RectD)[2]
		{
			("全屏", new RectD(56.0, 40.0, 1808.0, 970.0)),
			("竖屏", new RectD(697.0, 40.0, 525.0, 970.0))
		};
		int[] array2 = new int[4] { 5, 12, 48, 96 };
		DanceMode[] all;
		for (int j = 0; j < array2.Length; j++)
		{
			int num3 = array2[j];
			(string, RectD)[] array3 = array;
			for (int k = 0; k < array3.Length; k++)
			{
				(string, RectD) tuple = array3[k];
				string item = tuple.Item1;
				RectD item2 = tuple.Item2;
				all = DanceModes.All;
				foreach (DanceMode m in all)
				{
					Choreographer choreographer = new Choreographer
					{
						Stage = item2,
						IconSize = 48.0,
						Clip = motionClip,
						BallIndex = 0
					};
					Pose[] array4 = GridHomes(num3, 1920.0, 1080.0);
					Vec[] array5 = new Vec[num3];
					for (int n = 0; n < num3; n++)
					{
						array5[n] = array4[n].P;
					}
					choreographer.Homes = array5;
					choreographer.GrooveUsesHomes = item == "全屏";
					choreographer.SetMode(m, 0.0, array4);
					BeatClock beatClock = new BeatClock();
					BeatAnalyzer.Features f = new BeatAnalyzer.Features();
					bool flag = true;
					double num4 = 1000000000.0;
					double num5 = -1000000000.0;
					double num6 = 1000000000.0;
					double num7 = -1000000000.0;
					double num8 = 0.0;
					for (int num9 = 0; num9 < 1500; num9++)
					{
						double num10 = 1.0 / 60.0;
						num8 += num10;
						double dBeat = beatClock.Update(num10, num8, f, 1.0);
						Pose[] array6 = choreographer.Poses(num3, new Tempo
						{
							Beat = beatClock.Beat,
							DBeat = dBeat,
							Dt = num10,
							Energy = beatClock.Energy,
							Pulse = beatClock.Pulse
						});
						if (array6.Length != num3)
						{
							flag = false;
						}
						Pose[] array7 = array6;
						for (int num11 = 0; num11 < array7.Length; num11++)
						{
							Pose pose = array7[num11];
							if (!Finite(pose.P.X) || !Finite(pose.P.Y) || !Finite(pose.Rot) || !Finite(pose.Sx) || !Finite(pose.Sy) || !Finite(pose.Alpha))
							{
								flag = false;
							}
							if (pose.Sx <= 0.0 || pose.Sy <= 0.0 || pose.Sx > 4.0 || pose.Sy > 4.0)
							{
								flag = false;
							}
							num4 = Math.Min(num4, pose.P.X);
							num5 = Math.Max(num5, pose.P.X);
							num6 = Math.Min(num6, pose.P.Y);
							num7 = Math.Max(num7, pose.P.Y);
						}
					}
					bool flag2 = num4 > -1500.0 && num5 < 3500.0 && num6 > -1500.0 && num7 < 2600.0;
					Check(flag && flag2, DanceModes.LogName(m) + " · " + item + " · " + num3 + " 个图标：25 秒，数字正常，范围 x " + num4.ToString("0") + "～" + num5.ToString("0") + "，y " + num6.ToString("0") + "～" + num7.ToString("0"));
				}
			}
		}
		Choreographer choreographer2 = new Choreographer
		{
			Stage = array[0].Item2,
			IconSize = 48.0,
			Clip = motionClip,
			BallIndex = 0
		};
		Pose[] current = GridHomes(48, 1920.0, 1080.0);
		choreographer2.SetMode(DanceMode.Rehearsal, 0.0, current);
		DanceMode[] array8 = new DanceMode[5]
		{
			DanceMode.Flash,
			DanceMode.Shiver,
			DanceMode.Moonwalk,
			DanceMode.Subject3,
			DanceMode.Figure
		};
		List<DanceMode> list = new List<DanceMode>();
		List<double> list2 = new List<double>();
		double[] array9 = new double[5] { 4.0, 33.333333333333336, 8.0, 8.0, 28.0 };
		double num12 = 0.0;
		double[] array10 = array9;
		foreach (double num13 in array10)
		{
			num12 += num13;
		}
		bool flag3 = true;
		DanceMode danceMode = (DanceMode)(-1);
		int num14 = (int)(num12 * 2.0 * 8.0);
		for (int num15 = 0; num15 < num14; num15++)
		{
			double num16 = (double)num15 * 0.125;
			Pose[] array7 = choreographer2.Poses(48, new Tempo
			{
				Beat = num16,
				DBeat = 0.125,
				Dt = 0.0625,
				Energy = 0.55,
				Pulse = 0.0,
				Accent = 0.0,
				Stall = 0.0
			});
			for (int j = 0; j < array7.Length; j++)
			{
				Pose pose2 = array7[j];
				if (!Finite(pose2.P.X) || !Finite(pose2.P.Y) || pose2.Sx <= 0.0 || pose2.Sy <= 0.0)
				{
					flag3 = false;
				}
			}
			DanceMode rehearsalSegment = choreographer2.RehearsalSegment;
			if (rehearsalSegment != danceMode)
			{
				list.Add(rehearsalSegment);
				list2.Add(num16);
				danceMode = rehearsalSegment;
			}
		}
		bool flag4 = list.Count == array8.Length * 2;
		for (int num17 = 0; num17 < list.Count && flag4; num17++)
		{
			if (list[num17] != array8[num17 % array8.Length])
			{
				flag4 = false;
			}
		}
		List<double> list3 = new List<double>();
		for (int num18 = 0; num18 < 2; num18++)
		{
			double num19 = (double)num18 * num12;
			for (int num20 = 0; num20 < array9.Length; num20++)
			{
				list3.Add(num19);
				num19 += array9[num20];
			}
		}
		bool flag5 = list2.Count == list3.Count;
		for (int num21 = 0; num21 < list2.Count && flag5; num21++)
		{
			if (Math.Abs(list2[num21] - list3[num21]) > 0.13)
			{
				flag5 = false;
			}
		}
		Check(flag3 && flag4 && flag5, "测试模式：闪身步→狗熊→MJ→科目三→打篮球 各一次且不重复，整轮 " + num12.ToString("0.0") + " 拍后从头循环（" + list.Count + " 段，起点 " + string.Join("/", list2.ConvertAll((double x) => x.ToString("0.0"))) + " 拍）");
		double[] array11 = new double[array8.Length];
		int num22 = 0;
		int num23 = 0;
		for (; num22 < array9.Length; num22++)
		{
			array11[num23++] = list3[num22];
		}
		bool ok2 = true;
		for (int num24 = 0; num24 < array8.Length; num24++)
		{
			Choreographer choreographer3 = new Choreographer();
			choreographer3.Stage = array[0].Item2;
			choreographer3.IconSize = 48.0;
			choreographer3.Clip = motionClip;
			choreographer3.BallIndex = 0;
			choreographer3.SetMode(array8[num24], 0.0, current);
			Tempo tempo = new Tempo
			{
				Beat = 0.0,
				DBeat = 0.0,
				Dt = 0.0,
				Energy = 0.55,
				Pulse = 0.0,
				Accent = 0.0,
				Stall = 0.0
			};
			Tempo tempo2 = new Tempo
			{
				Beat = array11[num24],
				DBeat = 0.0,
				Dt = 0.0,
				Energy = 0.55,
				Pulse = 0.0,
				Accent = 0.0,
				Stall = 0.0
			};
			Pose[] array12 = choreographer3.Poses(48, tempo);
			Pose[] array13 = choreographer2.Poses(48, tempo2);
			for (int num25 = 0; num25 < 48; num25++)
			{
				if (array12[num25].P.X != array13[num25].P.X || array12[num25].P.Y != array13[num25].P.Y || array12[num25].Sx != array13[num25].Sx || array12[num25].Rot != array13[num25].Rot)
				{
					ok2 = false;
				}
			}
		}
		Check(ok2, "测试模式：每段都从该动作的起势开始，不接上一段的残留姿势");
		DanceMode[] obj = new DanceMode[4]
		{
			DanceMode.Shiver,
			DanceMode.Flash,
			DanceMode.Moonwalk,
			DanceMode.Subject3
		};
		double num26 = 0.0;
		string text = "";
		all = obj;
		for (int j = 0; j < all.Length; j++)
		{
			DanceMode m2 = all[j];
			Choreographer choreographer4 = new Choreographer
			{
				Stage = array[0].Item2,
				IconSize = 48.0,
				Clip = motionClip,
				BallIndex = 0
			};
			choreographer4.SetMode(m2, 0.0, GridHomes(48, 1920.0, 1080.0));
			Pose[] array14 = null;
			for (int num27 = 0; num27 <= 1056; num27++)
			{
				double beat = (double)num27 / 16.0;
				Pose[] array15 = choreographer4.Poses(48, new Tempo
				{
					Beat = beat,
					DBeat = 0.0625,
					Dt = 1.0 / 32.0,
					Energy = 0.55,
					Pulse = 0.0,
					Accent = 0.0,
					Stall = 0.0
				});
				if (array14 != null)
				{
					for (int num28 = 0; num28 < array15.Length; num28++)
					{
						if (Part(array15[num28].Z) == Part(array14[num28].Z))
						{
							double num29 = array15[num28].P.X - array14[num28].P.X;
							double num30 = array15[num28].P.Y - array14[num28].P.Y;
							double num31 = Math.Sqrt(num29 * num29 + num30 * num30);
							if (num31 > num26)
							{
								num26 = num31;
								text = m2.ToString() + " 第 " + beat.ToString("0.00") + " 拍 图标#" + num28 + " z=" + array15[num28].Z.ToString("0") + " (" + array14[num28].P.X.ToString("0") + "," + array14[num28].P.Y.ToString("0") + ")→(" + array15[num28].P.X.ToString("0") + "," + array15[num28].P.Y.ToString("0") + ")";
							}
						}
					}
				}
				array14 = array15;
			}
		}
		Check(num26 < 320.0, "四个舞蹈的引擎都不跳帧（相邻帧最大位移 " + num26.ToString("0.0") + " px ≈ " + (num26 / 700.0).ToString("0.000") + " 身高，出现在 " + text + "）");
		if (motionClip != null)
		{
			(string, RectD)[] array3 = array;
			for (int j = 0; j < array3.Length; j++)
			{
				(string, RectD) tuple2 = array3[j];
				string item3 = tuple2.Item1;
				RectD item4 = tuple2.Item2;
				array10 = new double[3] { 0.3, 1.0, 2.0 };
				for (int k = 0; k < array10.Length; k++)
				{
					double amp = array10[k];
					Choreographer choreographer5 = new Choreographer
					{
						Stage = item4,
						IconSize = 48.0,
						Clip = motionClip,
						Amp = amp
					};
					Choreographer choreographer6 = new Choreographer
					{
						Stage = item4,
						IconSize = 48.0,
						Clip = motionClip,
						Amp = amp
					};
					Pose[] current2 = GridHomes(48, 1920.0, 1080.0);
					choreographer5.SetMode(DanceMode.Figure, 0.0, current2);
					choreographer6.SetMode(DanceMode.Figure, 0.0, current2);
					bool ok3 = true;
					double num32 = 0.0;
					for (int num33 = 0; num33 < 1200; num33++)
					{
						double dt = 1.0 / 60.0;
						Pose[] array16 = choreographer5.Poses(48, new Tempo
						{
							Beat = (double)num33 / 30.0,
							DBeat = 1.0 / 30.0,
							Dt = dt,
							Energy = 0.55,
							Pulse = 0.0
						});
						Pose[] array17 = choreographer6.Poses(48, new Tempo
						{
							Beat = (double)num33 / 30.0,
							DBeat = 1.0 / 30.0,
							Dt = dt,
							Energy = 1.0,
							Pulse = 1.0
						});
						for (int num34 = 0; num34 < 48; num34++)
						{
							if (array16[num34].P.X != array17[num34].P.X || array16[num34].P.Y != array17[num34].P.Y || array16[num34].Sx != array17[num34].Sx || array16[num34].Rot != array17[num34].Rot)
							{
								ok3 = false;
							}
							if (num34 != 0)
							{
								double val = Math.Max(Math.Max(item4.MinX - array16[num34].P.X, array16[num34].P.X - item4.MaxX), Math.Max(item4.MinY - array16[num34].P.Y, array16[num34].P.Y - item4.MaxY));
								num32 = Math.Max(num32, val);
							}
						}
					}
					Check(ok3, "打篮球 · " + item3 + " · 幅度 " + amp.ToString("0.0") + "：有没有鼓点，画面完全一样（不跟拍）");
					Check(num32 < 400.0, "打篮球 · " + item3 + " · 幅度 " + amp.ToString("0.0") + "：小人基本待在舞台里（身体最多超出舞台 " + num32.ToString("0") + " 点）");
				}
			}
		}
		BeatAnalyzer beatAnalyzer = new BeatAnalyzer();
		BeatClock beatClock2 = new BeatClock();
		int num35 = 480;
		float[] array18 = new float[num35];
		Random random = new Random(7);
		double num36 = 15.0 / 32.0;
		long num37 = 672000L;
		for (long num38 = 0L; num38 < num37; num38 += num35)
		{
			for (int num39 = 0; num39 < num35; num39++)
			{
				double num40 = (double)(num38 + num39) / 48000.0 % num36;
				double num41 = 0.6 * Math.Exp((0.0 - num40) / 0.09) * Math.Sin(Math.PI * 200.0 * num40);
				array18[num39] = (float)(num41 + (random.NextDouble() - 0.5) * 0.01);
			}
			double startTime = (double)num38 / 48000.0;
			double now = (double)(num38 + num35) / 48000.0;
			beatAnalyzer.Process(array18, num35, 48000.0, startTime);
			beatClock2.Update((double)num35 / 48000.0, now, beatAnalyzer.Drain(now), 1.0);
		}
		Line("    合成 128 BPM 底鼓 14 秒 → 检出 " + beatClock2.Bpm.ToString("0.0") + " BPM，" + beatAnalyzer.DebugSummary());
		Check(Math.Abs(beatClock2.Bpm - 128.0) < 6.0, "节拍检测能跟上 128 BPM");
		Check(beatClock2.Locked, "有声音时处于「跟随」状态");
		BeatClock beatClock3 = new BeatClock();
		BeatAnalyzer.Features f2 = new BeatAnalyzer.Features();
		for (int num42 = 0; num42 < 600; num42++)
		{
			beatClock3.Update(1.0 / 60.0, (double)num42 / 60.0, f2, 1.0);
		}
		Check(Math.Abs(beatClock3.Beat - 20.0) < 0.01 && Math.Abs(beatClock3.Bpm - 120.0) < 0.01, "没声音时是 120 BPM 的内置节拍（10 秒走 20 拍）");
		BeatClock beatClock4 = new BeatClock();
		BeatAnalyzer.Features features = new BeatAnalyzer.Features
		{
			Active = true,
			Loud = true,
			Level = 0.8f
		};
		features.Onsets.Add(new BeatAnalyzer.Onset
		{
			Time = 0.02,
			Strength = 0.9f
		});
		double num43 = 0.0;
		for (int num44 = 0; num44 < 180; num44++)
		{
			beatClock4.Update(1.0 / 60.0, (double)num44 / 60.0, features, 1.0);
			num43 = Math.Max(num43, beatClock4.Accent);
			features.Onsets.Clear();
		}
		Check(num43 > 0.7, "真实鼓点能抬起明显的重音（Accent 峰值 " + num43.ToString("0.00") + "）");
		BeatAnalyzer.Features f3 = new BeatAnalyzer.Features
		{
			Active = true,
			Loud = false,
			Level = 0f
		};
		for (int num45 = 0; num45 < 24; num45++)
		{
			beatClock4.Update(1.0 / 60.0, 3.0 + (double)num45 / 60.0, f3, 1.0);
		}
		double beat2 = beatClock4.Beat;
		double num46 = 0.0;
		for (int num47 = 0; num47 < 12; num47++)
		{
			beatClock4.Update(1.0 / 60.0, 3.4 + (double)num47 / 60.0, f3, 1.0);
			num46 = Math.Max(num46, beatClock4.Stall);
		}
		double num48 = beatClock4.Beat - beat2;
		Check(num46 > 0.9 && num48 < 0.02, "音乐突然暂停：拍子停住、动作一起定格（定格期间只走了 " + num48.ToString("0.000") + " 拍）");
		double beat3 = beatClock4.Beat;
		for (int num49 = 0; num49 < 90; num49++)
		{
			beatClock4.Update(1.0 / 60.0, 3.6 + (double)num49 / 60.0, f3, 1.0);
		}
		Check(beatClock4.Stall < 0.01 && beatClock4.Beat - beat3 > 2.0, "暂停过后：拍子自己继续走，不用手动恢复");
		Lang current3 = L.Current;
		bool flag6 = true;
		bool ok4 = true;
		HashSet<string> hashSet = new HashSet<string>();
		Lang[] array19 = new Lang[2]
		{
			Lang.Zh,
			Lang.En
		};
		for (int j = 0; j < array19.Length; j++)
		{
			L.Current = array19[j];
			all = DanceModes.All;
			foreach (DanceMode danceMode2 in all)
			{
				string text2 = DanceModes.Title(danceMode2);
				if (string.IsNullOrWhiteSpace(text2) || !hashSet.Add(text2))
				{
					flag6 = false;
				}
				if (DanceModes.LogName(danceMode2) != danceMode2 switch
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
				})
				{
					ok4 = false;
				}
			}
		}
		L.Current = Lang.En;
		bool flag7 = DanceModes.Title(DanceMode.Figure) == "Basketball" && L.T("中", "en") == "en" && new Msg("中", "en").Text == "en";
		L.Current = Lang.Zh;
		bool flag8 = DanceModes.Title(DanceMode.Figure) == "打篮球" && L.T("中", "en") == "中" && new Msg("中", "en").Text == "中";
		L.Current = current3;
		Check(flag6 && flag7 && flag8, "界面语言：每个模式名中英文各一套，切换语言后立刻换过来");
		Check(ok4, "界面语言：日志里用的模式名始终是中文，不随界面语言变");
		Check(L.Parse("zh") == Lang.Zh && L.Parse("EN") == Lang.En && !L.Parse("").HasValue && !L.Parse("fr").HasValue && !L.Parse(null).HasValue && L.Code(Lang.Zh) == "zh" && L.Code(Lang.En) == "en", "界面语言：设置文件里的 zh / en 能读能写，别的值当作没选过");
		return motionClip;
		static int Part(double z)
		{
			if (!(z >= 1100.0))
			{
				if (!(z >= 1000.0))
				{
					if (!(z >= 900.0))
					{
						if (!(z >= 400.0))
						{
							return (z >= 250.0) ? 1 : 0;
						}
						return 2;
					}
					return 3;
				}
				return 4;
			}
			return 5;
		}
	}

	private static void PanelProbe(string outDir, LoadResult? res)
	{
		DanceController danceController = new DanceController(Dispatcher.CurrentDispatcher);
		PanelWindow panelWindow = new PanelWindow(danceController, new Settings
		{
			NoSave = true
		});
		if (res != null)
		{
			NameCatalog nameCatalog = new NameCatalog();
			foreach (DancerItem item in res.Items)
			{
				(item.IsGuest ? nameCatalog.Guests : nameCatalog.Desktop).Add(item.Name);
			}
			danceController.SelfTestSetCatalog(nameCatalog);
		}
		Lang[] array = new Lang[2]
		{
			Lang.Zh,
			Lang.En
		};
		foreach (Lang lang in array)
		{
			List<string> list = new List<string>();
			BitmapSource bitmapSource = panelWindow.SelfTestRender(lang, list);
			string text = "panel-" + L.Code(lang) + ".png";
			PngBitmapEncoder pngBitmapEncoder = new PngBitmapEncoder();
			pngBitmapEncoder.Frames.Add(BitmapFrame.Create(bitmapSource));
			using (FileStream stream = File.Create(Path.Combine(outDir, text)))
			{
				pngBitmapEncoder.Save(stream);
			}
			Line("  控制窗口（" + ((lang == Lang.Zh) ? "中文" : "English") + "）：" + bitmapSource.PixelWidth + "x" + bitmapSource.PixelHeight + "，" + string.Join("；", list) + "，图片 " + text);
		}
		L.Current = Lang.Zh;
	}

	private static LoadResult? Probe()
	{
		LoadResult res = null;
		Exception err = null;
		Thread thread = new Thread((ThreadStart)delegate
		{
			try
			{
				res = DesktopLoader.Load(48, "");
			}
			catch (Exception ex)
			{
				err = ex;
			}
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.IsBackground = true;
		thread.Start();
		if (!thread.Join(TimeSpan.FromSeconds(90.0)))
		{
			Line("读桌面 90 秒没返回，放弃");
			return null;
		}
		if (err != null)
		{
			Line("读桌面抛出异常：" + err);
			return null;
		}
		if (res == null)
		{
			return null;
		}
		foreach (string item in res.Diag)
		{
			Line("  " + item);
		}
		int count = res.Items.FindAll((DancerItem it) => it.Icon != null).Count;
		Line("  共 " + res.Items.Count + " 个图标（桌面 " + res.DesktopCount + "，借用 " + (res.Items.Count - res.DesktopCount) + "），取到图片 " + count + " 个");
		Line("  图标位置：" + (res.PositionsFromShell ? "从资源管理器读到了" : "没读到"));
		Line("  " + ((res.CurtainNote.Length > 0) ? res.CurtainNote : "遮罩：无"));
		for (int num = 0; num < res.Items.Count && num < 8; num++)
		{
			DancerItem dancerItem = res.Items[num];
			Line("    " + (dancerItem.IsGuest ? "借用 " : "桌面 ") + dancerItem.Name + (dancerItem.HasPos ? ("  中心 (" + dancerItem.PxX.ToString("0") + "," + dancerItem.PxY.ToString("0") + ")") : "") + ((dancerItem.Icon != null) ? ("  图片 " + dancerItem.Icon.PixelWidth + "x" + dancerItem.Icon.PixelHeight + " " + dancerItem.Icon.Format.ToString()) : "  没有图片"));
		}
		return res;
	}

	private static void Render(string outDir, LoadResult? res, MotionClip? clip)
	{
		double num = res?.Screen.Scale ?? 1.0;
		double num2 = ((res != null) ? ((double)res.Screen.MonW / num) : 1920.0);
		double num3 = ((res != null) ? ((double)res.Screen.MonH / num) : 1080.0);
		double num4 = res?.IconDip ?? 48.0;
		List<DancerItem> list = new List<DancerItem>();
		if (res != null)
		{
			list.AddRange(res.Items);
		}
		for (int i = list.Count; i < 48; i++)
		{
			list.Add(new DancerItem
			{
				Name = "图标 " + (i + 1),
				IsGuest = true
			});
		}
		Pose[] array = GridHomes(list.Count, num2, num3);
		for (int j = 0; j < list.Count; j++)
		{
			if (list[j].HasPos)
			{
				array[j].P = list[j].Home;
			}
		}
		Vec[] array2 = new Vec[list.Count];
		for (int k = 0; k < list.Count; k++)
		{
			array2[k] = array[k].P;
		}
		StageHost stageHost = new StageHost(num2, num3, num4, (res?.CellW ?? 76.0) - 6.0, 1.0, list);
		stageHost.SetCurtain(res?.Curtain, res?.CurtainColor ?? Color.FromRgb(32, 36, 48));
		stageHost.SetLabelsVisible(on: true, -1);
		stageHost.Apply(array);
		Save(stageHost, Path.Combine(outDir, "0-home.jpg"));
		RectD stage = new RectD(56.0, 88.0, num2 - 112.0, num3 - 48.0 - 70.0);
		int num5 = 1;
		DanceMode[] all = DanceModes.All;
		for (int l = 0; l < all.Length; l++)
		{
			DanceMode danceMode = all[l];
			Choreographer choreographer = new Choreographer
			{
				Stage = stage,
				IconSize = num4,
				Clip = clip,
				Homes = array2
			};
			choreographer.SetMode(danceMode, 0.0, array);
			BeatClock beatClock = new BeatClock();
			BeatAnalyzer.Features f = new BeatAnalyzer.Features();
			Pose[] poses = array;
			double num6 = 0.0;
			for (int m = 0; m < 840; m++)
			{
				double num7 = 1.0 / 60.0;
				num6 += num7;
				double dBeat = beatClock.Update(num7, num6, f, 1.0);
				poses = choreographer.Poses(list.Count, new Tempo
				{
					Beat = beatClock.Beat,
					DBeat = dBeat,
					Dt = num7,
					Energy = beatClock.Energy,
					Pulse = beatClock.Pulse
				});
			}
			stageHost.SetLabelsVisible(danceMode != DanceMode.Figure, -1);
			stageHost.Apply(poses);
			Save(stageHost, Path.Combine(outDir, num5 + "-" + danceMode.ToString().ToLowerInvariant() + ".jpg"));
			if (danceMode == DanceMode.Shiver || danceMode == DanceMode.Flash || danceMode == DanceMode.Moonwalk || danceMode == DanceMode.Subject3)
			{
				string text = danceMode.ToString().ToLowerInvariant();
				for (int n = 0; n < 8; n++)
				{
					Choreographer choreographer2 = new Choreographer
					{
						Stage = stage,
						IconSize = num4,
						Clip = clip,
						Homes = array2
					};
					choreographer2.SetMode(danceMode, 0.0, array);
					BeatClock beatClock2 = new BeatClock();
					BeatAnalyzer.Features f2 = new BeatAnalyzer.Features();
					Pose[] poses2 = choreographer2.Poses(list.Count, new Tempo
					{
						Beat = 0.0,
						DBeat = 0.0,
						Dt = 0.0,
						Energy = 0.55,
						Pulse = 0.0,
						Accent = 0.0,
						Stall = 0.0
					});
					double num8 = 0.0;
					for (int num9 = 0; num9 < n * 30; num9++)
					{
						double num10 = 1.0 / 60.0;
						num8 += num10;
						double dBeat2 = beatClock2.Update(num10, num8, f2, 1.0);
						poses2 = choreographer2.Poses(list.Count, new Tempo
						{
							Beat = beatClock2.Beat,
							DBeat = dBeat2,
							Dt = num10,
							Energy = beatClock2.Energy,
							Pulse = 0.0,
							Accent = 0.0,
							Stall = 0.0
						});
					}
					stageHost.Apply(poses2);
					Save(stageHost, Path.Combine(outDir, "seq-" + text + "-" + n + ".jpg"));
				}
			}
			if (danceMode == DanceMode.Shiver)
			{
				double[] array3 = new double[10] { 0.0, 36.0, 78.0, 84.0, 180.0, 240.0, 300.0, 396.0, 480.0, 534.0 };
				string[] array4 = new string[10] { "ready", "shout", "clap", "chop", "circles", "cross1", "cross2", "push", "peak", "hold" };
				for (int num11 = 0; num11 < array3.Length; num11++)
				{
					Choreographer choreographer3 = new Choreographer();
					choreographer3.Stage = stage;
					choreographer3.IconSize = num4;
					choreographer3.Clip = clip;
					choreographer3.Homes = array2;
					choreographer3.SetMode(danceMode, 0.0, array);
					Pose[] poses3 = choreographer3.Poses(list.Count, new Tempo
					{
						Beat = array3[num11] / 18.0,
						DBeat = 0.0,
						Dt = 0.0,
						Energy = 0.55,
						Pulse = 0.0,
						Accent = 0.0,
						Stall = 0.0
					});
					stageHost.Apply(poses3);
					Save(stageHost, Path.Combine(outDir, "key-shiver-" + (num11 + 1) + "-" + array4[num11] + ".jpg"));
				}
			}
			if (danceMode == DanceMode.Rehearsal)
			{
				double[] array5 = new double[5] { 1.0, 6.0, 14.0, 22.0, 40.0 };
				string[] array6 = new string[5] { "flash", "shiver", "moonwalk", "subject3", "figure" };
				for (int num12 = 0; num12 < array5.Length; num12++)
				{
					Choreographer choreographer4 = new Choreographer();
					choreographer4.Stage = stage;
					choreographer4.IconSize = num4;
					choreographer4.Clip = clip;
					choreographer4.Homes = array2;
					choreographer4.SetMode(danceMode, 0.0, array);
					Pose[] poses4 = choreographer4.Poses(list.Count, new Tempo
					{
						Beat = array5[num12],
						DBeat = 0.0,
						Dt = 0.0,
						Energy = 0.55,
						Pulse = 0.0,
						Accent = 0.0,
						Stall = 0.0
					});
					stageHost.Apply(poses4);
					Save(stageHost, Path.Combine(outDir, "seq-rehearsal-" + (num12 + 1) + "-" + array6[num12] + ".jpg"));
				}
			}
			num5++;
		}
		Line("  离屏渲染：已画出原位、每种队形，以及四个新增模式的逐拍造型序列 seq-*.jpg（" + (int)num2 + "x" + (int)num3 + "）");
	}

	private static void Save(StageHost host, string path)
	{
		RenderTargetBitmap source = host.RenderToBitmap();
		JpegBitmapEncoder jpegBitmapEncoder = new JpegBitmapEncoder
		{
			QualityLevel = 85
		};
		jpegBitmapEncoder.Frames.Add(BitmapFrame.Create(source));
		using FileStream stream = File.Create(path);
		jpegBitmapEncoder.Save(stream);
	}
}
