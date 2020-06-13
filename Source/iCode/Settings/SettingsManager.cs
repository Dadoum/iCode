using System;
using System.IO;
using iCode.GUI;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iCode.GUI.Backend;
using iCode.GUI.Backend.Interfaces;
using Newtonsoft.Json;

namespace iCode.Settings
{
	public class SettingsManager
	{
		private JObject settings;
		private string settingsPath;
		private List<Setting> _defaultSettings = new List<Setting>();

		internal struct Setting
		{
			internal string Name;
			internal string Path;
			internal JToken Value;
		}

		public const int LatestFormatSupported = 3;

		public SettingsManager(string settingsPath)
		{
			settings = JObject.Parse(File.ReadAllText(settingsPath));
			this.settingsPath = settingsPath;
		}

		public void InitializeSettings()
		{
			Console.WriteLine("Loading settings...");
			
			_defaultSettings.Add(new Setting
			{
				Name = "check_updates",
				Path = "General/Updates",
				Value = false
			});
			
			_defaultSettings.Add(new Setting
			{
				Name = "auto_install",
				Path = "General/Updates",
				Value = false
			});
			
			_defaultSettings.Add(new Setting
			{
				Name = "tab_width",
				Path = "Editor/Editor",
				Value = 4
			});
			
			_defaultSettings.Add(new Setting
			{
				Name = "ui_backend",
				Path = "Appearance/Appearance",
				Value = 4
			});

			bool approvalCheck = false;
			bool approvalInstall = false;
			bool recreationNeeded = false;

			if (File.Exists(settingsPath) && !string.IsNullOrWhiteSpace(File.ReadAllText(settingsPath)))
			{
				if (!settings.ContainsKey("format") || (int) settings["format"] != LatestFormatSupported)
				{
					Console.WriteLine("Conversion required.");
					recreationNeeded = true;
					// Convertion from 1 to 3
					if (settings.ContainsKey("updateConsent") && settings["updateConsent"].Type == JTokenType.Boolean)
					{
						approvalCheck = (bool) settings["updateConsent"];
					}
					// Conversion from 2-like to 3
					else
					{
						approvalCheck = (bool) settings["updateConsent"]["checkUpdates"];
						approvalInstall = (bool) settings["updateConsent"]["autoInstall"];
					}
				}

				if (!recreationNeeded)
				{
					// Check for iCode's default settings keys
					var settings = GetSettings();
					
					if (!settings.Any(s => s["name"].ToString() == "check_updates"))
						AddSettingsEntry("check_updates", "General/Updates", approvalCheck);
					if (!settings.Any(s => s["name"].ToString() == "auto_install"))
						AddSettingsEntry("auto_install", "General/Updates", approvalInstall);
					File.WriteAllText(settingsPath, this.settings.ToString());
				}
			}
			else
			{
				var startup = UIHelper.CreateFromInterface<IStartupWindow>();
				if (startup.Run() != -5)
					Environment.Exit(1);

				approvalCheck = startup.Accepted;
				recreationNeeded = true;
			}

			if (recreationNeeded)
			{
				Console.WriteLine("Initializing settings file in format " + LatestFormatSupported);
				settings = new JObject { new JProperty("settings", new JArray()) };
				
				AddSettingsEntry("check_updates", "General/Updates", approvalCheck);
				AddSettingsEntry("auto_install", "General/Updates", approvalInstall);

				settings.Add(new JProperty("format", LatestFormatSupported));

				File.WriteAllText(settingsPath, settings.ToString());
				Console.WriteLine("Initialized settings file.");
			}

			foreach (var setting in _defaultSettings)
				if (!GetSettings().Any(s => s["name"].ToString() == setting.Name))
					AddSettingsEntry(setting.Name, setting.Path, setting.Value);

			Console.WriteLine("Settings loaded.");
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
			var set = GetSetting(name);
			((JArray) settings["settings"]).Remove(set);
			set["value"] = value;
			((JArray) settings["settings"]).Add(set);
			File.WriteAllText(settingsPath, settings.ToString());
		}
		
		public JToken GetSettingsEntry(string name)
		{
			return settings["settings"].First(setting => setting["name"].ToString() == name)["value"];
		}

		public JToken GetSetting(string name)
		{
			return settings["settings"].First(setting => setting["name"].ToString() == name);
		}
		
		public JArray GetSettings()
		{
			return (JArray) settings["settings"];
		}
	}
}