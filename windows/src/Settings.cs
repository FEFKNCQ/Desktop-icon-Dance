using System;
using System.IO;
using System.Text.Json;

namespace DesktopDance;

public sealed class Settings
{
	public string BallName = "";

	public bool FollowSystemAudio = true;

	public bool HidePanelFromCapture;

	public string Language = "";

	public bool NoSave;

	public static Settings Load()
	{
		Settings settings = new Settings();
		try
		{
			if (!File.Exists(AppPaths.SettingsFile))
			{
				return settings;
			}
			using JsonDocument jsonDocument = JsonDocument.Parse(File.ReadAllText(AppPaths.SettingsFile));
			JsonElement rootElement = jsonDocument.RootElement;
			if (rootElement.ValueKind != JsonValueKind.Object)
			{
				return settings;
			}
			if (rootElement.TryGetProperty("ballName", out var value) && value.ValueKind == JsonValueKind.String)
			{
				settings.BallName = value.GetString() ?? "";
			}
			if (rootElement.TryGetProperty("followSystemAudio", out var value2) && (value2.ValueKind == JsonValueKind.True || value2.ValueKind == JsonValueKind.False))
			{
				settings.FollowSystemAudio = value2.GetBoolean();
			}
			if (rootElement.TryGetProperty("hidePanelFromCapture", out var value3) && (value3.ValueKind == JsonValueKind.True || value3.ValueKind == JsonValueKind.False))
			{
				settings.HidePanelFromCapture = value3.GetBoolean();
			}
			if (rootElement.TryGetProperty("language", out var value4) && value4.ValueKind == JsonValueKind.String)
			{
				Lang? lang = L.Parse(value4.GetString());
				if (lang.HasValue)
				{
					Lang valueOrDefault = lang.GetValueOrDefault();
					settings.Language = L.Code(valueOrDefault);
				}
			}
		}
		catch (Exception ex)
		{
			Log.Write("读设置失败：" + ex.Message);
		}
		return settings;
	}

	public void Save()
	{
		if (NoSave)
		{
			return;
		}
		try
		{
			using MemoryStream memoryStream = new MemoryStream();
			using (Utf8JsonWriter utf8JsonWriter = new Utf8JsonWriter(memoryStream, new JsonWriterOptions
			{
				Indented = true
			}))
			{
				utf8JsonWriter.WriteStartObject();
				utf8JsonWriter.WriteString("ballName", BallName);
				utf8JsonWriter.WriteBoolean("followSystemAudio", FollowSystemAudio);
				utf8JsonWriter.WriteBoolean("hidePanelFromCapture", HidePanelFromCapture);
				if (Language.Length > 0)
				{
					utf8JsonWriter.WriteString("language", Language);
				}
				utf8JsonWriter.WriteEndObject();
			}
			File.WriteAllBytes(AppPaths.SettingsFile, memoryStream.ToArray());
		}
		catch (Exception ex)
		{
			Log.Write("写设置失败：" + ex.Message);
		}
	}
}
