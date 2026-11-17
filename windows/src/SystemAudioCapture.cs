using System;
using System.Runtime.InteropServices;
using System.Threading;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace DesktopDance;

public sealed class SystemAudioCapture
{
	private readonly BeatAnalyzer analyzer;

	private readonly object gate = new object();

	private WasapiLoopbackCapture? capture;

	private MMDeviceEnumerator? enumerator;

	private Timer? watch;

	private string deviceId = "";

	private string deviceName = "";

	private int channels = 2;

	private int bits = 32;

	private double sampleRate = 48000.0;

	private float[] mono = new float[8192];

	private double lastSound = -100.0;

	private volatile bool wanted;

	private volatile bool dead;

	private bool warnedFormat;

	public bool Running
	{
		get
		{
			if (wanted)
			{
				return capture != null;
			}
			return false;
		}
	}

	public string DeviceName => deviceName;

	public bool Hearing => Clock.Now - Volatile.Read(in lastSound) < 4.0;

	public SystemAudioCapture(BeatAnalyzer analyzer)
	{
		this.analyzer = analyzer;
	}

	public Msg? Start()
	{
		lock (gate)
		{
			wanted = true;
			Msg? result = Open();
			if (watch == null)
			{
				watch = new Timer(Watch, null, 3000, 3000);
			}
			return result;
		}
	}

	public void Stop()
	{
		lock (gate)
		{
			wanted = false;
			if (watch != null)
			{
				watch.Dispose();
				watch = null;
			}
			Close();
		}
	}

	private Msg? Open()
	{
		Close();
		try
		{
			if (enumerator == null)
			{
				enumerator = new MMDeviceEnumerator();
			}
			MMDevice defaultAudioEndpoint = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
			WasapiLoopbackCapture wasapiLoopbackCapture = new WasapiLoopbackCapture(defaultAudioEndpoint);
			WaveFormat waveFormat = wasapiLoopbackCapture.WaveFormat;
			channels = Math.Max(1, waveFormat.Channels);
			bits = waveFormat.BitsPerSample;
			sampleRate = waveFormat.SampleRate;
			deviceId = defaultAudioEndpoint.ID ?? "";
			try
			{
				deviceName = defaultAudioEndpoint.FriendlyName ?? "";
			}
			catch
			{
				deviceName = "";
			}
			if (bits != 32 && bits != 16)
			{
				wasapiLoopbackCapture.Dispose();
				return new Msg("这个输出设备的声音格式不认识（" + waveFormat?.ToString() + "）", "this output device uses an audio format the app does not understand (" + waveFormat?.ToString() + ")");
			}
			wasapiLoopbackCapture.DataAvailable += OnData;
			wasapiLoopbackCapture.RecordingStopped += OnStopped;
			dead = false;
			wasapiLoopbackCapture.StartRecording();
			capture = wasapiLoopbackCapture;
			Log.Write("系统声音已接通：" + deviceName + "，格式 " + waveFormat);
			return null;
		}
		catch (Exception ex)
		{
			Log.Write("系统声音接不上：" + ex.GetType().Name + " " + ex.Message);
			return new Msg(ex.Message, ex.Message);
		}
	}

	private void Close()
	{
		WasapiLoopbackCapture wasapiLoopbackCapture = capture;
		capture = null;
		if (wasapiLoopbackCapture == null)
		{
			return;
		}
		try
		{
			wasapiLoopbackCapture.DataAvailable -= OnData;
			wasapiLoopbackCapture.RecordingStopped -= OnStopped;
		}
		catch
		{
		}
		try
		{
			wasapiLoopbackCapture.StopRecording();
		}
		catch
		{
		}
		try
		{
			wasapiLoopbackCapture.Dispose();
		}
		catch
		{
		}
	}

	private void Watch(object? state)
	{
		if (!wanted || !Monitor.TryEnter(gate))
		{
			return;
		}
		try
		{
			if (!wanted)
			{
				return;
			}
			bool flag = dead || capture == null;
			if (!flag)
			{
				try
				{
					if (enumerator == null)
					{
						enumerator = new MMDeviceEnumerator();
					}
					string text = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID ?? "";
					if (text.Length > 0 && text != deviceId)
					{
						flag = true;
						Log.Write("默认输出设备换了，重新接系统声音");
					}
				}
				catch
				{
				}
			}
			if (flag)
			{
				Open();
			}
		}
		catch (Exception ex)
		{
			Log.Write("系统声音看门狗出错：" + ex.Message);
		}
		finally
		{
			Monitor.Exit(gate);
		}
	}

	private void OnStopped(object? sender, StoppedEventArgs e)
	{
		if (e.Exception != null)
		{
			Log.Write("系统声音中断：" + e.Exception.Message);
		}
		dead = true;
	}

	private void OnData(object? sender, WaveInEventArgs e)
	{
		try
		{
			int num = bits / 8 * channels;
			if (num <= 0)
			{
				return;
			}
			int num2 = e.BytesRecorded / num;
			if (num2 <= 0)
			{
				return;
			}
			if (mono.Length < num2)
			{
				mono = new float[num2 * 2];
			}
			float num3 = 0f;
			float num4 = 1f / (float)channels;
			if (bits == 32)
			{
				ReadOnlySpan<float> readOnlySpan = MemoryMarshal.Cast<byte, float>(new ReadOnlySpan<byte>(e.Buffer, 0, num2 * num));
				for (int i = 0; i < num2; i++)
				{
					float num5 = 0f;
					int num6 = i * channels;
					for (int j = 0; j < channels; j++)
					{
						num5 += readOnlySpan[num6 + j];
					}
					num5 *= num4;
					if (float.IsNaN(num5) || float.IsInfinity(num5))
					{
						num5 = 0f;
					}
					mono[i] = num5;
					float num7 = Math.Abs(num5);
					if (num7 > num3)
					{
						num3 = num7;
					}
				}
			}
			else
			{
				ReadOnlySpan<short> readOnlySpan2 = MemoryMarshal.Cast<byte, short>(new ReadOnlySpan<byte>(e.Buffer, 0, num2 * num));
				for (int k = 0; k < num2; k++)
				{
					float num8 = 0f;
					int num9 = k * channels;
					for (int l = 0; l < channels; l++)
					{
						num8 += (float)readOnlySpan2[num9 + l] / 32768f;
					}
					num8 *= num4;
					mono[k] = num8;
					float num10 = Math.Abs(num8);
					if (num10 > num3)
					{
						num3 = num10;
					}
				}
			}
			double now = Clock.Now;
			if (num3 > 0.0005f)
			{
				Volatile.Write(ref lastSound, now);
			}
			analyzer.Process(mono, num2, sampleRate, now - (double)num2 / sampleRate);
		}
		catch (Exception ex)
		{
			if (!warnedFormat)
			{
				warnedFormat = true;
				Log.Write("处理系统声音时出错：" + ex.Message);
			}
		}
	}
}
