using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using Gdk;
using Gdl;
using GLib;
using Gtk;
using iCode.GUI;
using iCode.Native.Appimage.Updater;
using iCode.Settings;
using iCode.Utils;
using iMobileDevice;
using iMobileDevice.MobileBackup2;
using Newtonsoft.Json.Linq;
using Action = Gtk.Action;
using Extensions = iCode.Utils.Extensions;
using Notification = iCode.Native.Notify.Notification;
using Process = GLib.Process;
using Task = System.Threading.Tasks.Task;

namespace iCode
{
	internal static class Program
	{
		public static int Main(string[] args)
		{
			// Make the console use date TODO: finish implementation
			System.Console.SetOut(new DatedConsole());
			Console.WriteLine("Initialized output.");
			try
			{
				// Check for AppImage Environment Variable, if empty, the App is not running from an AppImage 
				if (string.IsNullOrWhiteSpace(AppImagePath))
				{
					Console.WriteLine("iCode is running outside of an AppImage.");
					
					Notification notification = new Notification(Names.ApplicationName,
						$"iCode is running outside of an AppImage.\n" +
							 $"We do recommand running AppImage directly to benefit from updates.",
						5000);
					notification.SetIconFromPixbuf(
						Pixbuf.LoadFromResource("iCode.resources.images.icon.png"));
					notification.Show();
				}

				// Create directories if they do not exist
				Directory.CreateDirectory(ConfigPath);
				Directory.CreateDirectory(SDKPath);
				Directory.CreateDirectory(DeveloperPath);
				Directory.CreateDirectory(UserDefinedTemplatesPath);
				
				Gtk.Application.Init();
				
				// Prevent the 1000203000 warnings of GTK
				GLib.Log.SetDefaultHandler(new GLib.LogFunc((domain, level, message) =>
				{
					if (level != GLib.LogLevelFlags.Error && level != GLib.LogLevelFlags.FlagFatal)
						return;

					Console.WriteLine($"Gtk error: {message} ({domain})");
				}));

				Console.WriteLine("Initialized GTK and GDL.");

				if (!File.Exists(SettingsPath))
				{
					Settings = new SettingsManager(SettingsPath);
					Settings.InitializeSettings();
				}
				else
				{
					var settings = JObject.Parse(File.ReadAllText(SettingsPath));
					var updateConsent = settings["updateConsent"];
					
					// There is 3 settings format, 1, 1.99 and 2
					// 1 uses a bool to store update consent.
					// 1.99 does not contains format field.
					// So we need to convert both to format 2 ! -> And this breaks compatibility with format 1 !
					if (settings.ContainsKey("format"))
					{
						// This permits to avoid compatibility breaking changes;
						// when format is higher, iCode will not crash and will just ignore the file and warns the user
						if ((int) settings["format"] > SettingsManager.LatestFormatSupported)
						{
							MessageDialog md = new MessageDialog(null, DialogFlags.Modal, MessageType.Error,
								ButtonsType.YesNo, true,
								"iCode is unable to parse settings file. Settings file version is higher than the higher version supported by iCode. \n" +
								"iCode is not going to take your settings in consideration and will not keep trace of your settings after reboot.\n" +
								$"If you want to preserve settings of this version, you can move the settings file ({SettingsPath}) to another place to create a configuration of older iCode.\n" +
								"Proceed anyway ?");
							md.Title = "Unable to start iCode.";
							md.ShowAll();
							md.Present();
							var outp = md.Run();
							md.Dispose();
							if (outp == (int) ResponseType.Yes)
							{
								SettingsPath = Path.GetTempFileName();
							}
							else
							{
								return 1;
							}
						}
					}

					Settings = new SettingsManager(SettingsPath);
					Settings.InitializeSettings();
					
					// Check for permission to search updates and verify if running in AppImage
					if (!string.IsNullOrWhiteSpace(AppImagePath) &&
					    (bool) settings["updateConsent"]["checkUpdates"])
					{
						// Create a new thread
						Task.Factory.StartNew(() =>
						{
							Console.WriteLine("Checking for updates...");
							Updater updater = new Updater(AppImagePath, true);

							// Checking for changes
							updater.CheckForChanges(ref UpdateAvailable, 0);
							
							if (UpdateAvailable)
							{
								Console.WriteLine("Update available.");

								// Retrieve latest version informations
								var request =
									(HttpWebRequest) WebRequest.Create(
										"https://api.github.com/repos/Dadoum/iCode/releases/latest");
								request.Method = "GET";
								request.Headers = new WebHeaderCollection();
								request.UserAgent =
									$"{Names.ApplicationName}/{Assembly.GetEntryAssembly().GetName().Version}";
								var stream = request.GetResponse().GetResponseStream();
								var read = new StreamReader(stream);
								var str = read.ReadToEnd();
								read.Dispose();
								stream.Dispose();
								UpdateInfo = JObject.Parse(str);
								
								// Check if we can automatically install it
								if (!(bool) settings["updateConsent"]["autoInstall"])
								{
									// Show a notification with a button to ask user if they want to update
									Notification notification = new Notification(Names.ApplicationName,
										$"{Names.ApplicationName} is ready to update from v{Assembly.GetEntryAssembly().GetName().Version} to {UpdateInfo["tag_name"]}",
										15000);
									notification.SetIconFromPixbuf(
										Pixbuf.LoadFromResource("iCode.resources.images.icon.png"));
									notification.AddAction("click", "Update now",
										((ptr, action, data) => { Update(updater); }));
									notification.Show();
									System.Threading.Thread.Sleep(15000);
									GC.KeepAlive(notification);
								}
								else
								{
									// The update is being downloaded and installed automatically
									// because user consented it explicitly
									Update(updater);
								}

							}
						});
					}
				}
				
				// Load LibiMobileDevice native libraries TODO: Marshall LibiMobileDevice to avoid Quamotion fork
				NativeLibraries.Load(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
					"tools/libs/"));
				Console.WriteLine("Initialized libimobiledevice.");
				
				// Actually create window
				Program.WinInstance = MainWindow.Create();
				Program.WinInstance.ShowAll();
				Console.WriteLine("Initialized window.");
				
				// And finally run application
				Gtk.Application.Run();
				return 0;
			}
			catch (Exception e)
			{
				// Create the window that will show the fatal error
				ExceptionWindow.Create(e, null).ShowAll();
				return 1;
			}
		}

		public static void Update(Updater updater)
		{
			// Show notification informing to user that we are updating
			Notification notification = new Notification(Names.ApplicationName,
				$"{Names.ApplicationName} is updating from v{Assembly.GetEntryAssembly().GetName().Version} to {UpdateInfo["tag_name"]}",
				1000000000);
			notification.SetIconFromPixbuf(
				Pixbuf.LoadFromResource("iCode.resources.images.icon.png"));
			notification.Show();
			
			if (updater.Start())
			{
				Console.WriteLine("Downloading update...");

				while (!updater.IsDone)
				{
					double progress = 0d;
					if (updater.Progress(ref progress))
					{
						// Console.WriteLine($"Update {progress * 100} % completed");
						notification.SetHint("value", new Variant((int) (progress * 100)));
						notification.Show();
					}
					
					if ((int) progress == 1)
						break;
				}
				
				Console.WriteLine("Installing update...");
				
				while (!updater.IsDone) ;
				
				Console.WriteLine(!updater.HasError ? "Update done !" : "Update failed.");

				while (updater.NextStatusMessage(out string message) && !string.IsNullOrEmpty(message))
					Console.WriteLine(message);
				
				if (updater.HasError)
					updater.RestoreOriginalFile();
				
				notification.Close();

				Notification n = new Notification(Names.ApplicationName,
					updater.HasError ? $"{Names.ApplicationName} failed to update to {UpdateInfo["tag_name"]}, the original file has been restored." : $"{Names.ApplicationName} finished to update to {UpdateInfo["tag_name"]}",
					15000);
				
				n.SetIconFromPixbuf(
					Pixbuf.LoadFromResource("iCode.resources.images.icon.png"));
				
				if (!updater.HasError)
					n.AddAction("click", "Restart",
						(ptr, action, data) =>
						{
							Console.WriteLine(AppImagePath);
							string tempFile = Path.GetTempFileName();
							File.Move(AppImagePath, tempFile, true);
							File.Move(tempFile, AppImagePath, true);
							System.Diagnostics.Process.Start(AppImagePath);
							Environment.Exit(0);
						});
				
				n.Show();
			}
			else
			{
				Console.WriteLine();
				Console.WriteLine("Failed to update :/.");
			}
			
			GC.KeepAlive(notification);
		}

		public static readonly string ConfigPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "iCode/");
		public static readonly string SDKPath = System.IO.Path.Combine(Program.ConfigPath, "SDK/");
		public static readonly string DeveloperPath = System.IO.Path.Combine(Program.ConfigPath, "Developer/");
		public static readonly string UserDefinedTemplatesPath = System.IO.Path.Combine(Program.ConfigPath, "Templates/");
		public static readonly string AppImagePath = Environment.GetEnvironmentVariable("APPIMAGE");
		
		public static string SettingsPath = System.IO.Path.Combine(Program.ConfigPath, "Settings.json");
		public static SettingsManager Settings;

		public static bool UpdateAvailable = false;
		public static JObject UpdateInfo;
		
		public static MainWindow WinInstance;
	}
}