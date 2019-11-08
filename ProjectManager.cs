using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gtk;
using iMobileDevice;
using iMobileDevice.iDevice;
using iMobileDevice.Lockdown;
using iMobileDevice.MobileImageMounter;
using iMobileDevice.Plist;
using Newtonsoft.Json.Linq;
using Stetic;
using Action = Gtk.Action;

namespace iCode
{
    public static class ProjectManager
    {
        public static Project Project;

        private static List<Widget> SensitiveWidgets = new List<Widget>();
        private static List<Action> SensitiveActions = new List<Action>();

        private static List<RowActivatedHandler> handlers = new List<RowActivatedHandler>();

        private static TreeIter projectNode;
        private static TreeIter resourcesNode;

        private static List<TreeIter> classNodes = new List<TreeIter>();
        private static List<TreeIter> resourceNodes = new List<TreeIter>();

        public static bool ProjectLoaded
        {
            get
            {
                return ProjectManager.Project != null;
            }
        }

        public static void AddSensitiveObject(GLib.Object obj)
        {
            if (obj is Widget)
            {
                var widget = obj as Widget;
                widget.Sensitive = ProjectLoaded;
                SensitiveWidgets.Add(widget);
            }
            else if (obj is Action)
            {
                var action = obj as Action;
                action.Sensitive = ProjectLoaded;
                SensitiveActions.Add(action);
            }
        }

        public static void LoadProject(string file)
        {
            TreeView treeView = Program.WinInstance.ProjectExplorer.TreeView;
            TreeStore treeStore = (TreeStore)Program.WinInstance.ProjectExplorer.TreeView.Model;
            ProjectManager.Project = new Project(file);

            treeStore.Clear();
            
            CodeWidget.RemoveTab("Welcome to iCode !");

            var e = new RowActivatedHandler((o, args) =>
            {
                TreeIter treeIter;
                treeStore.GetIter(out treeIter, args.Path);

                int type = 0;

                foreach (var @class in classNodes)
                {
                    if (Equals(treeIter, @class))
                    {
                        type = 1;
                        break;
                    }
                }

                if (type != 1)
                {
                    foreach (var @class in resourceNodes)
                    {
                        if (Equals(treeIter, @class))
                        {
                            type = 2;
                            break;
                        }
                    }
                }

                switch (type)
                {
                    case 1:
                        var code = CodeWidget.AddCodeTab(Path.Combine(Path.GetDirectoryName(file), (string)treeStore.GetValue(treeIter, 1)));
                        CodeWidget.codewidget.tabs.Page = CodeWidget.codewidget.tabs.PageNum(Extensions.tabs.First(x => x.Key == (string)treeStore.GetValue(treeIter, 1)).Value);
                        break;

                    case 2:
                        Process.Start("gio", "open '" + Path.Combine(Path.GetDirectoryName(file), "Resources", (string)treeStore.GetValue(treeIter, 1)) + "'");
                        break;
                }
            });

            foreach (var row in handlers)
            {
                treeView.RowActivated -= row;
            }
            handlers.Clear();

            treeView.RowActivated += e;
            handlers.Add(e);

            projectNode = treeStore.AppendValues(new object[]
            {
                IconLoader.LoadIcon(Program.WinInstance.ProjectExplorer, "gtk-directory", IconSize.Menu),
                ProjectManager.Project.Name
            });

            resourcesNode = treeStore.AppendValues(projectNode, new object[]
            {
                IconLoader.LoadIcon(Program.WinInstance.ProjectExplorer, "gtk-directory", IconSize.Menu),
                "Resources"
            });

            foreach (Class @class in ProjectManager.Project.Classes)
            {
                var node = treeStore.AppendValues(projectNode,
                    Extensions.GetIconFromFile(Path.Combine(Directory.GetParent(file).FullName, Path.GetFileName(@class.Filename))),
                    Path.GetFileName(@class.Filename)
                );

                classNodes.Add(node);
            }

            foreach (string path in Directory.GetFiles(Path.Combine(Path.GetDirectoryName(file), "Resources")))
            {
                var node = treeStore.AppendValues(resourcesNode,
                    Extensions.GetIconFromFile(Path.GetFullPath(path)),
                    Path.GetFileName(path)
                );

                resourceNodes.Add(node);
            }

            var filea = "";

            using (var f = File.Open(Path.Combine(Program.ConfigPath, "recentProjects"), FileMode.OpenOrCreate))
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

            foreach (var widget in SensitiveWidgets)
            {
                widget.Sensitive = ProjectLoaded;
            }

            foreach (var action in SensitiveActions)
            {
                action.Sensitive = ProjectLoaded;
            }

            File.WriteAllText(Path.Combine(Program.ConfigPath, "recentProjects"), filea);
        }

        public static Project CreateProject(string name, string id, string prefix, string zip, string path = null)
        {
            if (string.IsNullOrWhiteSpace(path))
                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "iCode Projects/", name);

            Console.WriteLine("Extracting template from: {0}", zip);

            var Frameworks = new List<string>();
            var Classes = new List<Class>();
            var Attributes = new JObject();

            if (Directory.Exists(path))
                Directory.Delete(path, true);

            Directory.CreateDirectory(path);

            Frameworks.Add("Foundation");
            Frameworks.Add("UIKit");

            ZipFile.ExtractToDirectory(zip, path);

            FormatFiles(path, name, prefix, id);

            foreach (var file in from f in Directory.GetFiles(path) where f.EndsWith(".m", StringComparison.CurrentCulture) || f.EndsWith(".h", StringComparison.CurrentCulture) || f.EndsWith(".swift", StringComparison.CurrentCulture) select f)
            {
                var a = new JObject();
                a.Add("flags", JArray.FromObject(new string[] { }));
                a.Add("filename", Path.GetFileName(file));
                Classes.Add(new Class(a));
            }

            Attributes.Add("name", name);
            Attributes.Add("package", id);
            Attributes.Add("frameworks", JArray.FromObject(Frameworks.ToArray()));

            var classStruct = new JArray();
            foreach (Class @class in Classes)
            {
                classStruct.Add(@class.Attributes);
            }

            Attributes.Add("classes", classStruct);

            File.WriteAllText(Path.Combine(path, "project.json"), Attributes.ToString());
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
            var cachedir = Path.Combine(Project.Path, ".icode");

            if (Directory.Exists(cachedir))
                Directory.Delete(cachedir, true);

            Directory.CreateDirectory(cachedir);
            string s = "";

            Program.WinInstance.ProgressBar.PulseStep = 1.0d / (double)(((double) Project.Classes.Count(c => c.Filename.EndsWith(".m", StringComparison.CurrentCulture))) + 2d);

            if (Project.Classes.Any((arg) => arg.Filename.EndsWith(".swift", StringComparison.CurrentCultureIgnoreCase)))
                Extensions.ShowMessage(MessageType.Error, "Cannot build.", "Swift is not supported for the moment.");

            foreach (var @class in from c in Project.Classes where !c.Filename.EndsWith(".h", StringComparison.CurrentCultureIgnoreCase) select c)
            {
                Directory.CreateDirectory(Path.Combine(cachedir, "build"));
                var proc = Extensions.GetProcess("clang", @"-target '" + Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/target/arm64-apple-darwin14") + "' -x objective-c -c -isysroot '" + Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/sdk") + "' -fmodules " + string.Join(" ", @class.CompilerFlags) + "-arch arm64 '" + Path.Combine(Project.Path, @class.Filename) + "' -o '" + Path.Combine(cachedir, "build", @class.Filename + ".output") + "'");
                if (!(Program.WinInstance.Output.Run(proc, (int)ActionCategory.MAKE, out _, out _) == 0))
                    return false;
                s += "'" + Path.Combine(cachedir, "build", @class.Filename + ".output") + "' ";
                Program.WinInstance.ProgressBar.Fraction += Program.WinInstance.ProgressBar.PulseStep;
            }

            Directory.CreateDirectory(Path.Combine(Project.Path, ".icode/Payload/" + Project.Name + ".app/"));
            var process = Extensions.GetProcess("clang", @"-target '" + Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/target/arm64-apple-darwin14") + "' -framework " + string.Join(" -framework ", Project.Frameworks)  + " -isysroot '" + Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/sdk") + "' -arch arm64 -o '" + Path.Combine(Project.Path, ".icode/Payload/" + Project.Name + ".app/" + Project.Name) + "' " + s);
            if (!(Program.WinInstance.Output.Run(process, (int)ActionCategory.LINK, out _, out _) == 0))
                return false;

            Program.WinInstance.ProgressBar.Fraction += Program.WinInstance.ProgressBar.PulseStep;

            foreach (var f in Directory.GetFiles(Path.Combine(Project.Path, "Resources"))) 
            {
                File.Copy(f, Path.Combine(Project.Path, ".icode/Payload/" + Project.Name + ".app/"));
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
            if (!File.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/developer/key.pem")))
            {
                Extensions.ShowMessage(MessageType.Error, "Cannot codesign application", "No certificate found in ./tools/developer/.\nRead README for information about how to place them,\n and run ./tools/helper/gen-certs to generate certificates.\n The syntax is:\ngen-certs apple-id app-only-password device-udid \nNote: the device udid i can be automatically retrieved\n if your device is connected to the computer and\n if you trusted the computer.");
                return false;
            }
            if (File.Exists(Path.Combine(Project.Path, "build/" + Project.Name + ".ipa")))
                File.Delete(Path.Combine(Project.Path, "build/" + Project.Name + ".ipa"));

            var process = Extensions.GetProcess(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/helper/sign-ipa"), string.Format("-m {4} -c {3} -k {2} -o {1} {0}",
                "'" + path + "'",
                "'" + Path.Combine(Project.Path, "build/" + Project.Name + ".ipa'"),
                "'" + Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/developer/key.pem'"),
                "'" + Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/developer/certificate.pem'"),
                "'" + Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/developer/provision-profile.mobileprovision'")
            ));

            int i = Program.WinInstance.Output.Run(process, (int)ActionCategory.SIDELOAD, out _, out _);
            Program.WinInstance.ProgressBar.Fraction += Program.WinInstance.ProgressBar.PulseStep;

            return i == 0;
        }

        public static void RunProject()
        {
            var win = DeviceSelectorWindow.Create();

            if (win.Run() == (int) ResponseType.Ok)
            {
                JObject jobj = PList.ParsePList(new PList(win.attributesPlist));

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
                            var file_sig = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/dmgs/" + jobj["BuildVersion"].ToString() + "/DeveloperDiskImage.dmg.signature");
                            Program.WinInstance.Output.Run(Extensions.GetProcess("ideviceinstaller", "-U '" + Path.Combine(Project.Path, "build/" + Project.Name + ".ipa") + "'"), (int)ActionCategory.SIDELOAD, out _, out _);
                            Thread.Sleep(500);
                            Program.WinInstance.Output.Run(Extensions.GetProcess("ideviceinstaller", "-i '" + Path.Combine(Project.Path, "build/" + Project.Name + ".ipa") + "'"), (int)ActionCategory.SIDELOAD, out _, out _);
                            Thread.Sleep(500);
                            Program.WinInstance.Output.Run(Extensions.GetProcess("ideviceimagemounter", "'" + file + "' '" + file_sig + "'"), (int)ActionCategory.LAUNCH, out _, out _);
                            Thread.Sleep(500);
                            Program.WinInstance.Output.Run(Extensions.GetProcess("idevicedebug", "run " + Project.BundleId), (int)ActionCategory.LAUNCH, out _, out _);
                        }
                        catch (Exception e)
                        {
                            _ = new ExceptionWindow(e, null);
                        }
                    });
                }
            }
        }

    }
}
