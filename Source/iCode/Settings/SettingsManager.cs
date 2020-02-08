using System;
using System.IO;
using Gtk;
using iCode.GUI;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace iCode.Settings
{
	public class SettingsManager
	{
		private JObject settings;
		private string settingsPath;

		public const int LatestFormatSupported = 3;

		public SettingsManager(string settingsPath)
		{
			settings = JObject.Parse(File.ReadAllText(settingsPath));
			this.settingsPath = settingsPath;
		}

		public void InitializeSettings()
		{
			Console.WriteLine("Loading settings...");

			bool approvalCheck = false;
			bool approvalInstall = false;
			bool recreationNeeded = false;

			if (File.Exists(settingsPath) && !string.IsNullOrWhiteSpace(File.ReadAllText(settingsPath)))
			{
				if (settings.ContainsKey("format") && (int) settings["format"] == LatestFormatSupported)
					return;

				Console.WriteLine("Conversion required.");
				// Convertion from 1 to 3
				if (settings.ContainsKey("updateConsent") && settings["updateConsent"].Type == JTokenType.Boolean)
				{
					recreationNeeded = true;
					approvalCheck = (bool) settings["updateConsent"];
				}
				// Conversion from 2-like to 3
				else
				{
					recreationNeeded = true;
					approvalCheck = (bool) settings["updateConsent"]["checkUpdates"];
					approvalInstall = (bool) settings["updateConsent"]["autoInstall"];
				}
			}
			else
			{
				var startup = StartupWindow.Create();
				if ((ResponseType) startup.Run() != ResponseType.Ok)
					Environment.Exit(1);

				approvalCheck = startup.Accepted;
				recreationNeeded = true;
			}

			if (recreationNeeded)
			{
				Console.WriteLine("Initializing settings file in format " + LatestFormatSupported);
				settings = new JObject();

				settings.Add(new JProperty("settings", new JArray()));

				AddSettingsEntry("check_updates", "General/Updates", approvalCheck);
				AddSettingsEntry("auto_install", "General/Updates", approvalInstall);

				settings.Add(new JProperty("format", LatestFormatSupported));

				File.WriteAllText(settingsPath, settings.ToString());
				Console.WriteLine("Initialized settings file.");
			}
		}

		public void AddSettingsEntry(string name, string path, JToken value)
		{
			var dic = new Dictionary<string, JToken>()
			{
				{"name", name},
				{"path", path},
				{"value", value}
			};
			var setting = JToken.FromObject(dic);

			(settings["settings"] as JArray).Add(setting);
		}

		public void SetSetting(string name, JToken value)
		{
			var set = GetSettingsEntry(name);
			((JArray) settings["settings"]).Remove(set);
			set["value"] = value;
			((JArray) settings["settings"]).Add(set);
			File.WriteAllText(settingsPath, settings.ToString());
		}
		
		public JToken GetSettingsEntry(string name)
		{
			return settings["settings"].First(setting => setting["name"].ToString() == name);
		}

		public JArray GetSettings()
		{
			return (JArray) settings["settings"];
		}
	}
}