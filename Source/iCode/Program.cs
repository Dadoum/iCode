using System;
using System.IO;
using System.Reflection;
using Gdl;
using GLib;
using Gtk;
using iCode.GUI;
using iCode.Utils;
using iMobileDevice;
using Newtonsoft.Json.Linq;

namespace iCode
{
	internal static class Program
	{
		public static void Main(string[] args)
		{
			System.Console.SetOut(new DatedConsole());
			Console.WriteLine("Initialized output.");
			try
			{
				Directory.CreateDirectory(ConfigPath);
				Directory.CreateDirectory(SDKPath);
				Directory.CreateDirectory(DeveloperPath);
				Directory.CreateDirectory(UserDefinedTemplatesPath);
				Directory.CreateDirectory(ConfigPath);
				Gtk.Application.Init();

				Log.SetDefaultHandler(new LogFunc((domain, level, message) =>
				{
					if (level != LogLevelFlags.Error && level != LogLevelFlags.FlagFatal)
						return;

					Console.WriteLine($"Gtk error: {message} ({domain})");
				}));

				Console.WriteLine("Initialized GTK and GDL.");
				
				if (!File.Exists(SettingsPath))
				{
					var startup = StartupWindow.Create();
					if ((ResponseType) startup.Run() != ResponseType.Ok)
						return;

					var jobj = new JObject();
					jobj.Add("updateConsent", startup.Accepted);
					File.WriteAllText(SettingsPath, jobj.ToString());
					Console.WriteLine("Initialized settings file.");
				}
				
				NativeLibraries.Load(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/libs/"));
				Console.WriteLine("Initialized libimobiledevice.");
				Program.WinInstance = MainWindow.Create();
				Program.WinInstance.ShowAll();
				Console.WriteLine("Initialized window.");
				Gtk.Application.Run();
			}
			catch (Exception e)
			{
				ExceptionWindow.Create(e, null).ShowAll();
			}
		}

		public static readonly string ConfigPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "iCode/");
		public static readonly string SDKPath = System.IO.Path.Combine(Program.ConfigPath, "SDK/");
		public static readonly string DeveloperPath = System.IO.Path.Combine(Program.ConfigPath, "Developer/");
		public static readonly string UserDefinedTemplatesPath = System.IO.Path.Combine(Program.ConfigPath, "Templates/");
		public static readonly string SettingsPath = System.IO.Path.Combine(Program.ConfigPath, "Settings.json");

		public static MainWindow WinInstance;
	}
}