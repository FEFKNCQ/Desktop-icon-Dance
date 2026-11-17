using System;
using System.Collections.Generic;

namespace DesktopDance;

public sealed class BeatClock
{
	private double lastOnset = -100.0;

	private readonly List<double> iois = new List<double>();

	private double strength = 0.6;

	private double loudTime;

	private double quietTime;

	private double stallLeft;

	public double Bpm { get; private set; } = 120.0;

	public double Beat { get; private set; }

	public double Energy { get; private set; } = 0.55;

	public double Pulse { get; private set; }

	public double Accent { get; private set; }

	public double Stall { get; private set; }

	public bool Locked { get; private set; }

	public void Reset()
	{
		Beat = 0.0;
		Pulse = 0.0;
		Energy = 0.55;
		iois.Clear();
		lastOnset = -100.0;
		Locked = false;
		strength = 0.6;
		Accent = 0.0;
		Stall = 0.0;
		loudTime = 0.0;
		quietTime = 0.0;
		stallLeft = 0.0;
	}

	public double Update(double dt, double now, BeatAnalyzer.Features f, double speed)
	{
		Locked = f.Active;
		if (f.Loud || (double)f.Level > 0.12)
		{
			loudTime += dt;
			quietTime = 0.0;
		}
		else
		{
			quietTime += dt;
		}
		if (loudTime > 0.8 && quietTime >= 0.3 && quietTime - dt < 0.3)
		{
			stallLeft = 0.55;
			loudTime = 0.0;
		}
		if (stallLeft > 0.0)
		{
			stallLeft = Math.Max(0.0, stallLeft - dt);
		}
		Stall = ((stallLeft > 0.0) ? Math.Min(1.0, stallLeft / 0.18) : 0.0);
		if (f.Active)
		{
			if (Stall < 0.01)
			{
				Energy += ((double)f.Level - Energy) * Math.Min(1.0, dt * 8.0);
			}
			foreach (BeatAnalyzer.Onset onset in f.Onsets)
			{
				strength += ((double)onset.Strength - strength) * 0.3;
				Accent = Math.Max(Accent, 0.55 + 0.45 * (double)onset.Strength);
				if (iois.Count < 4)
				{
					Pulse = Math.Max(Pulse, 0.55 + 0.45 * (double)onset.Strength);
				}
				double num = onset.Time - lastOnset;
				lastOnset = onset.Time;
				if (num > 0.2 && num < 2.0)
				{
					double num2;
					for (num2 = num; num2 < 0.4; num2 *= 2.0)
					{
					}
					while (num2 >= 0.8)
					{
						num2 /= 2.0;
					}
					iois.Add(num2);
					if (iois.Count > 16)
					{
						iois.RemoveAt(0);
					}
					if (iois.Count >= 4)
					{
						List<double> list = new List<double>(iois);
						list.Sort();
						double num3 = list[list.Count / 2];
						Bpm += (60.0 / num3 - Bpm) * 0.2;
					}
				}
				if (Math.Abs(speed - 1.0) < 0.06)
				{
					double num4 = Beat - (now - onset.Time) * Bpm / 60.0 * speed;
					double num5 = num4 - M.Round(num4);
					if (Math.Abs(num5) < 0.3)
					{
						Beat -= num5 * 0.25;
					}
				}
			}
		}
		else if (Stall < 0.01)
		{
			Energy += (0.55 - Energy) * Math.Min(1.0, dt * 2.0);
		}
		double beat = Beat;
		double num6 = dt * Bpm / 60.0 * speed * (1.0 - Stall);
		Beat += num6;
		if (Math.Floor(Beat) > Math.Floor(beat))
		{
			double num7 = Math.Floor(Beat) - Math.Floor(Math.Floor(Beat) / 4.0) * 4.0;
			double num8 = ((num7 == 0.0) ? 1.0 : ((num7 == 2.0) ? 0.72 : 0.48));
			double num9 = ((!f.Active) ? 0.8 : ((iois.Count >= 4) ? (0.5 + 0.5 * strength) : 0.55));
			Accent = Math.Max(Accent, num8 * Math.Min(1.0, 0.7 + 0.4 * num9));
			Pulse = Math.Max(Pulse, (!f.Active) ? 0.9 : ((iois.Count >= 4) ? (0.5 + 0.5 * strength) : 0.3));
		}
		Accent *= Math.Exp((0.0 - dt) * 4.2);
		Pulse *= Math.Exp((0.0 - dt) * 7.0);
		if (Stall > 0.0)
		{
			Accent *= 1.0 - Stall;
			Pulse *= 1.0 - Stall;
		}
		return num6;
	}
}
