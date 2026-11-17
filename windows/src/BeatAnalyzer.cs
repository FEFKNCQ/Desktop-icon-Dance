using System;
using System.Collections.Generic;

namespace DesktopDance;

public sealed class BeatAnalyzer
{
	public struct Onset
	{
		public double Time;

		public float Strength;
	}

	public sealed class Features
	{
		public List<Onset> Onsets = new List<Onset>();

		public float Level;

		public bool Active;

		public bool Loud;
	}

	private readonly object gate = new object();

	private readonly List<Onset> pending = new List<Onset>();

	private float levelOut;

	private double lastLoud = -100.0;

	private int dbgCalls;

	private int dbgOnsets;

	private float dbgMaxFull;

	private const int Hop = 512;

	private float lp;

	private float accLow;

	private float accFull;

	private int accCount;

	private float prevLow;

	private float prevFull;

	private readonly float[] fluxHist = new float[64];

	private int fluxIdx;

	private float peak = 0.0001f;

	private float smoothed;

	private double lastOnset = -100.0;

	private readonly List<Onset> found = new List<Onset>();

	public string DebugSummary()
	{
		lock (gate)
		{
			return "收到音频 " + dbgCalls + " 段，检出鼓点 " + dbgOnsets + " 个，最大音量 " + dbgMaxFull.ToString("0.0000");
		}
	}

	public void Reset()
	{
		lock (gate)
		{
			pending.Clear();
			levelOut = 0f;
			lastLoud = -100.0;
		}
	}

	public void Process(float[] samples, int count, double sampleRate, double startTime)
	{
		if (count <= 0 || sampleRate <= 0.0)
		{
			return;
		}
		float num = (float)Math.Exp(Math.PI * -320.0 / sampleRate);
		found.Clear();
		double num2 = -1.0;
		for (int i = 0; i < count; i++)
		{
			float num3 = samples[i];
			lp = num * lp + (1f - num) * num3;
			accLow += lp * lp;
			accFull += num3 * num3;
			accCount++;
			if (accCount >= 512)
			{
				float num4 = (float)Math.Sqrt(accLow / 512f);
				float num5 = (float)Math.Sqrt(accFull / 512f);
				accLow = 0f;
				accFull = 0f;
				accCount = 0;
				double num6 = startTime + (double)i / sampleRate;
				float num7 = Math.Max(0f, num4 - prevLow) + 0.4f * Math.Max(0f, num5 - prevFull);
				prevLow = num4;
				prevFull = num5;
				float num8 = 0f;
				for (int j = 0; j < fluxHist.Length; j++)
				{
					num8 += fluxHist[j];
				}
				num8 /= (float)fluxHist.Length;
				float num9 = 0f;
				for (int k = 0; k < fluxHist.Length; k++)
				{
					num9 += (fluxHist[k] - num8) * (fluxHist[k] - num8);
				}
				float num10 = (float)Math.Sqrt(num9 / (float)fluxHist.Length);
				fluxHist[fluxIdx] = num7;
				fluxIdx = (fluxIdx + 1) % fluxHist.Length;
				if (num7 > num8 * 1.5f + num10 * 1.4f + 0.004f && num6 - lastOnset > 0.24)
				{
					lastOnset = num6;
					found.Add(new Onset
					{
						Time = num6,
						Strength = Math.Min(1f, num7 / (num8 + num10 * 3f + 0.02f))
					});
				}
				peak = Math.Max(num5, peak * 0.9997f);
				float num11 = Math.Min(1f, num5 / (peak + 1E-05f));
				smoothed += (num11 - smoothed) * ((num11 > smoothed) ? 0.5f : 0.08f);
				if (num5 > 0.003f)
				{
					num2 = num6;
				}
			}
		}
		lock (gate)
		{
			dbgCalls++;
			dbgOnsets += found.Count;
			dbgMaxFull = Math.Max(dbgMaxFull, peak);
			pending.AddRange(found);
			if (pending.Count > 64)
			{
				pending.RemoveRange(0, pending.Count - 64);
			}
			levelOut = smoothed;
			if (num2 > 0.0)
			{
				lastLoud = num2;
			}
		}
	}

	public Features Drain(double now)
	{
		Features features = new Features();
		lock (gate)
		{
			int num = 0;
			while (num < pending.Count)
			{
				if (pending[num].Time <= now)
				{
					features.Onsets.Add(pending[num]);
					pending.RemoveAt(num);
				}
				else
				{
					num++;
				}
			}
			features.Level = levelOut;
			features.Active = now - lastLoud < 1.2;
			features.Loud = now - lastLoud < 0.18;
			return features;
		}
	}
}
