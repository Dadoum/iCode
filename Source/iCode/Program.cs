using System;
using System.IO;
using System.Reflection;
using Gdl;
using GLib;
using Gtk;
using iCode.GUI;
using iCode.Utils;
using iMobileDevice;

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
				Log.SetDefaultHandler(new LogFunc((domain, level, message) =>
				{
					if (level != LogLevelFlags.Error && level != LogLevelFlags.FlagFatal)
						return;
					
					Console.WriteLine($"Gtk error: {message} ({domain})");
				}));
				Directory.CreateDirectory(ConfigPath);
				Gtk.Application.Init();
				Console.WriteLine("Initialized GTK and GDL.");
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

		public static MainWindow WinInstance;
	}
}