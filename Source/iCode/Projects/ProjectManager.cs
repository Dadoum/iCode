using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using iCode.GUI;
using iCode.GUI.Backend;
using iCode.GUI.Backend.Interfaces.Panels;
using iCode.GUI.GTK3;
using iCode.GUI.GTK3.GladeUI;
using iCode.GUI.GTK3.Panels;
using iCode.Utils;
using Newtonsoft.Json.Linq;
using Action = Gtk.Action;
using Extensions = iCode.Utils.Extensions;
using Process = System.Diagnostics.Process;
using Task = System.Threading.Tasks.Task;
using Thread = System.Threading.Thread;

namespace iCode.Projects
{
	public static class ProjectManager
	{
		public static Project Project;
		public static string[] Flags = 
		{ 
			"-target", Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/target/arm64-apple-darwin14"),
			"-x","objective-c",
			"-arch", "arm64",
			"--sysroot", Program.SdkPath,
			"-std=c11", 
			"-I", "/usr/lib/clang/9.0.1/include/",
			"-fmodules"
		};


		private static List<dynamic> _sensitiveWidgets = new List<dynamic>();

		public static bool ProjectLoaded
		{
			get
			{
				return ProjectManager.Project != null;
			}
		}

		public static void AddSensitiveWidget(dynamic obj)
		{
			obj.Sensitive = (ProjectLoaded);
			_sensitiveWidgets.Add(obj);
		}

		public static void LoadProject(string file)
		{
			ProjectManager.Project = new Project(file);

			Program.WinInstance.ProjectExplorerWidget.LoadProject(file);


			CodeWidget.RemoveTab("Welcome to iCode !");

			var filea = "";

			using (var f = File.Open(Path.Combine(Program.ConfigPath, "RecentProjects"), FileMode.OpenOrCreate))
			{
				var wr = new StreamWriter(f);
				var re = new StreamReader(f);
				var content = re.ReadToEnd();
				var lines = content.Split('\n').ToList();
				if (lines.Count == 4)
				{
					lines.Remove(lines.First());
				}

				var temp = new List<string>();
				foreach (var line in from l in lines where l == Path.GetDirectoryName(file) select l)
				{
					temp.Add(line);
				}

				foreach (var temp2 in temp)
				{
					lines.Remove(temp2);
				}

				lines.Add(Path.GetDirectoryName(file));

				filea = string.Join("\n", lines);
				wr.Dispose();
				re.Dispose();
			}

			foreach (var widget in _sensitiveWidgets)
			{
				widget.Sensitive = (ProjectLoaded);
			}

			File.WriteAllText(Path.Combine(Program.ConfigPath, "RecentProjects"), filea);
		}

		public static Project CreateProject(string name, string id, string prefix, string zip, string path = null)
		{
			if (string.IsNullOrWhiteSpace(path))
				path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "iCode Projects/", name);

			Console.WriteLine("Extracting template from: {0}", zip);

			var frameworks = new List<string>();
			var classes = new List<Class>();
			var attributes = new JObject();

			if (Directory.Exists(path))
				Directory.Delete(path, true);

			Directory.CreateDirectory(path);

			frameworks.Add("Foundation");
			frameworks.Add("UIKit");

			ZipFile.ExtractToDirectory(zip, path);

			FormatFiles(path, name, prefix, id);

			foreach (var file in from f in Directory.GetFiles(path) where f.EndsWith(".m", StringComparison.CurrentCulture) || f.EndsWith(".h", StringComparison.CurrentCulture) || f.EndsWith(".swift", StringComparison.CurrentCulture) select f)
			{
				var a = new JObject();
				a.Add("flags", JArray.FromObject(new string[] { }));
				a.Add("filename", Path.GetFileName(file));
				classes.Add(new Class(a));
			}

			attributes.Add("name", name);
			attributes.Add("package", id);
			attributes.Add("frameworks", JArray.FromObject(frameworks.ToArray()));

			var classStruct = new JArray();
			foreach (Class @class in classes)
			{
				classStruct.Add(@class.Attributes);
			}

			attributes.Add("classes", classStruct);

			File.WriteAllText(Path.Combine(path, "project.json"), attributes.ToString());
			return new Project(Path.Combine(path, "project.json"));
		}

		private static void FormatFiles(string path, string name, string prefix, string package)
		{
			foreach (var d in from d in Directory.GetDirectories(path) where !d.StartsWith(".", StringComparison.CurrentCulture) select d)
				FormatFiles(d, name, prefix, package);


			foreach (var f in Directory.GetFiles(path))
			{
				var file = f;
				if (f.EndsWith(".m", StringComparison.CurrentCulture) || f.EndsWith(".h", StringComparison.CurrentCulture))
				{
					File.Move(f, Path.Combine(path, prefix + Path.GetFileName(f)));
					file = Path.Combine(path, prefix + Path.GetFileName(f));
				}

				File.WriteAllText(file, File.ReadAllText(file).Replace("@@CLASSPREFIX@@", prefix).Replace("@@PROJECTNAME@@", name).Replace("@@PACKAGENAME@@", package));
			}
		}

		public static bool BuildProject()
		{
			Console.WriteLine("Building application...");
			if (!Directory.Exists(Program.SdkPath) || !Directory.EnumerateFileSystemEntries(Program.SdkPath).Any())
			{
				Console.WriteLine("Unable to build application, no SDK was provided.");
				UIHelper.ShowModal("Cannot build application", "SDK path is empty, add SDK to " + Program.SdkPath, UIHelper.ModalCategory.Error, UIHelper.ModalActions.Ok);
				return false;
			}
			var cachedir = Path.Combine(Project.Path, ".icode");

			if (Directory.Exists(cachedir))
				Directory.Delete(cachedir, true);

			Directory.CreateDirectory(cachedir);
			string s = "";

			var step = 100 / (double)(((double) Project.Classes.Count(c => c.Filename.EndsWith(".m", StringComparison.CurrentCulture))) + 2d);

			Program.WinInstance.SetProgressValue(0);
			
			if (Project.Classes.Any((arg) => arg.Filename.EndsWith(".swift", StringComparison.CurrentCultureIgnoreCase)))
				UIHelper.ShowModal("Cannot build.", "Swift is not supported for the moment.", UIHelper.ModalCategory.Error, UIHelper.ModalActions.Ok);
                
			foreach (var @class in from c in Project.Classes where !c.Filename.EndsWith(".h", StringComparison.CurrentCultureIgnoreCase) select c)
			{
				Program.WinInstance.SetStatusText("Building " + @class.Filename);
				Directory.CreateDirectory(Path.Combine(cachedir, "build"));
				var flags = string.Join(" ", Flags) + " -c " + string.Join(" ", @class.CompilerFlags) + " ";
				var proc = Extensions.GetProcess("clang", flags + "\"" + Path.Combine(Project.Path, @class.Filename) + "\" -o \"" + Path.Combine(cachedir, "build", @class.Filename + ".output") + "\"");
				if (Program.WinInstance.OutputWidget.Run(proc, (int)ActionCategory.Make) != 0)
					return false;
				s += "\"" + Path.Combine(cachedir, "build", @class.Filename + ".output") + "\" ";
				Program.WinInstance.AddProgressValue(step);
			}
			Program.WinInstance.SetStatusText( "Linking");
			Directory.CreateDirectory(Path.Combine(Project.Path, ".icode/Payload/" + Project.Name + ".app/"));
			var process = Extensions.GetProcess("clang", "-target \"" + Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/target/bin/arm64-apple-darwin14") + "\" -framework " + string.Join(" -framework ", Project.Frameworks)  + " -isysroot \"" + Program.SdkPath + "\" -arch arm64 -o \"" + Path.Combine(Project.Path, ".icode/Payload/" + Project.Name + ".app/" + Project.Name) + "\" " + s);

			if (Program.WinInstance.OutputWidget.Run(process, (int)ActionCategory.Link) != 0)
				return false;

			Program.WinInstance.AddProgressValue(step);
			Program.WinInstance.SetStatusText("Packing IPA");
			foreach (var f in Directory.GetFiles(Path.Combine(Project.Path, "Resources")))
			{
				File.Copy(f, Path.Combine(Project.Path, ".icode/Payload/" + Project.Name + ".app/", Path.GetFileName(f)));
			}

			Directory.Delete(Path.Combine(cachedir, "build"), true);

			if (!Directory.Exists(Path.Combine(Project.Path, "build")))
				Directory.CreateDirectory(Path.Combine(Project.Path, "build"));

			if (File.Exists(Path.Combine(Project.Path, "build/" + Project.Name + "-unsigned.ipa")))
				File.Delete(Path.Combine(Project.Path, "build/" + Project.Name + "-unsigned.ipa"));

			ZipFile.CreateFromDirectory(cachedir, Path.Combine(Project.Path, "build/" + Project.Name + "-unsigned.ipa"));
			return SignIpa(Path.Combine(Project.Path, "build/" + Project.Name + "-unsigned.ipa"));
		}

		public static bool SignIpa(string path)
		{
			if (!File.Exists(Path.Combine(Program.DeveloperPath, "key.pem")))
			{
				File.Copy(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/developer/readme"), Path.Combine(Program.DeveloperPath, "readme"), true);
				UIHelper.ShowModal("Cannot codesign application", "No certificate found in " +  Program.DeveloperPath + ".\nRead README for information about how to place them.", UIHelper.ModalCategory.Error, UIHelper.ModalActions.Ok);
				return false;
			}

			Program.WinInstance.SetStatusText("Signing IPA");
			if (File.Exists(Path.Combine(Project.Path, "build/" + Project.Name + ".ipa")))
				File.Delete(Path.Combine(Project.Path, "build/" + Project.Name + ".ipa"));

			var process = Extensions.GetProcess(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/helper/sign-ipa"), string.Format("-m {4} -c {3} -k {2} -o {1} {0}",
				"'" + path + "'",
				"'" + Path.Combine(Project.Path, "build/" + Project.Name + ".ipa'"),
				"'" + Path.Combine(Program.DeveloperPath, "key.pem'"),
				"'" + Path.Combine(Program.DeveloperPath, "certificate.pem'"),
				"'" + Path.Combine(Program.DeveloperPath, "provision-profile.mobileprovision'")
			));
			
			int i = Program.WinInstance.OutputWidget.Run(process, (int)ActionCategory.Sideload);
			Program.WinInstance.SetProgressValue(100);

			return i == 0;
		}

		public static void RunProject()
		{
			var win = GladeHelper.Create<DeviceSelectorWindow>();

			if (win.Run() == -5)
			{
				JObject jobj = PList.ParsePList(new PList(win.AttributesPlist));

				/* Can't get this code working.
				 * Will use temporarily the command ideviceimagemounter
				var iDevice = LibiMobileDevice.Instance.iDevice;
				var Lockdown = LibiMobileDevice.Instance.Lockdown;
				var ImageMounter = LibiMobileDevice.Instance.MobileImageMounter;
				var Plist = LibiMobileDevice.Instance.Plist;
    
				iDeviceHandle deviceHandle;
				iDevice.idevice_new(out deviceHandle, jobj["UniqueDeviceID"].ToString()).ThrowOnError();
    
				LockdownClientHandle lockdownHandle;
				Lockdown.lockdownd_client_new_with_handshake(deviceHandle, out lockdownHandle, "iCode").ThrowOnError();
    
				LockdownServiceDescriptorHandle lockdownServiceHandle;
				Lockdown.lockdownd_start_service(lockdownHandle, "com.apple.mobile.mobile_image_mounter", out lockdownServiceHandle).ThrowOnError();
    
				MobileImageMounterClientHandle mounterHandle;
				ImageMounter.mobile_image_mounter_new(deviceHandle, lockdownServiceHandle, out mounterHandle).ThrowOnError();
    
				string s = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/dmgs/" + jobj["BuildVersion"].ToString() + "/DeveloperDiskImage.dmg.signature");
    
				var b = File.Open(s, FileMode.Open, FileAccess.Read);
				var str = b.ReadFully();
				var b_length = new System.IO.FileInfo(s).Length;
				var file = File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/dmgs/" + jobj["BuildVersion"].ToString() + "/DeveloperDiskImage.dmg"));
				GCHandle pinnedArray = GCHandle.Alloc(file, GCHandleType.Pinned);
				IntPtr pointer = pinnedArray.AddrOfPinnedObject();
    
				ImageMounter.mobile_image_mounter_upload_image(mounterHandle, "Developer", (ushort) file.Length, str, (ushort) str.Length, (buffer, length, userData) => { return (int) length; }, pointer).ThrowOnError();
    
				PlistHandle plist;
				ImageMounter.mobile_image_mounter_mount_image(mounterHandle, "/private/var/mobile/Media/PublicStaging/staging.dimage", str, (ushort) str.Length, "Developer", out plist).ThrowOnError();
    
				uint a = 20;
				string xml;
				Plist.plist_to_xml(plist, out xml, ref a);
    
				JObject result = PList.ParsePList(new PList(xml));
				Console.WriteLine(result.ToString());
    
    
				b.Close();
				deviceHandle.Dispose();
				lockdownHandle.Dispose();
				lockdownServiceHandle.Dispose();
				mounterHandle.Dispose();
				pinnedArray.Free();
				plist.Dispose();*/
				if (BuildProject())
				{
					Task.Factory.StartNew(() =>
					{
						try
						{
							var file = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/dmgs/" + jobj["BuildVersion"].ToString() + "/DeveloperDiskImage.dmg");
							var fileSig = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/dmgs/" + jobj["BuildVersion"].ToString() + "/DeveloperDiskImage.dmg.signature");
							Program.WinInstance.OutputWidget.Run(Extensions.GetProcess("ideviceinstaller", "-U '" + Path.Combine(Project.Path, "build/" + Project.Name + ".ipa") + "'"), (int)ActionCategory.Sideload);
							Thread.Sleep(500);
							Program.WinInstance.OutputWidget.Run(Extensions.GetProcess("ideviceinstaller", "-i '" + Path.Combine(Project.Path, "build/" + Project.Name + ".ipa") + "'"), (int)ActionCategory.Sideload);
							Thread.Sleep(500);
							Program.WinInstance.OutputWidget.Run(Extensions.GetProcess("ideviceimagemounter", "'" + file + "' '" + fileSig + "'"), (int)ActionCategory.Launch);
							Thread.Sleep(500);
							Program.WinInstance.OutputWidget.Run(Extensions.GetProcess("idevicedebug", "run " + Project.BundleId), (int)ActionCategory.Launch);
						}
						catch (Exception e)
						{
							GladeHelper.Create<ExceptionWindow>().ShowException(e, Program.WinInstance);
						}
					});
				}
			}
		}

	}
}