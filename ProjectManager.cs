using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using Gtk;
using iMobileDevice;
using iMobileDevice.iDevice;
using iMobileDevice.Lockdown;
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

        public static Project CreateProject(string name, string id, string prefix, string path = null)
        {
            if (string.IsNullOrWhiteSpace(path))
                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "iCode Projects/", name);

            var Frameworks = new List<string>();
            var Classes = new List<Class>();
            var Attributes = new JObject();

            if (Directory.Exists(path))
                Directory.Delete(path, true);

            Directory.CreateDirectory(path);

            Frameworks.Add("Foundation");
            Frameworks.Add("UIKit");

            var mainStruct = new JObject();
            mainStruct.Add("flags", JArray.FromObject(new string[] { }));
            mainStruct.Add("filename", "main.m");
            Classes.Add(new Class(mainStruct));

            var viewStruct = new JObject();
            viewStruct.Add("flags", JArray.FromObject(new string[] { }));
            viewStruct.Add("filename", prefix + "RootViewController.m");
            Classes.Add(new Class(viewStruct));

            var delegateStruct = new JObject();
            delegateStruct.Add("flags", JArray.FromObject(new string[] { }));
            delegateStruct.Add("filename", prefix + "AppDelegate.m");
            Classes.Add(new Class(delegateStruct));

            var viewhStruct = new JObject();
            viewhStruct.Add("flags", JArray.FromObject(new string[] { }));
            viewhStruct.Add("filename", prefix + "RootViewController.h");
            Classes.Add(new Class(viewhStruct));

            var delegatehStruct = new JObject();
            delegatehStruct.Add("flags", JArray.FromObject(new string[] { }));
            delegatehStruct.Add("filename", prefix + "AppDelegate.h");
            Classes.Add(new Class(delegatehStruct));

            Attributes.Add("name", name);
            Attributes.Add("package", id);
            Attributes.Add("frameworks", JArray.FromObject(Frameworks.ToArray()));

            var classStruct = new JArray();
            foreach (Class @class in Classes)
            {
                classStruct.Add(@class.Attributes);
            }

            Attributes.Add("classes", classStruct);

            string temp = Path.GetTempFileName();
            var template = Assembly.GetExecutingAssembly().GetManifestResourceStream("objc-template").ToByteArray();
            File.WriteAllBytes(temp, template);

            ZipFile.ExtractToDirectory(temp, path);
            File.Move(Path.Combine(path, "AppDelegate.m"), Path.Combine(path, prefix + "AppDelegate.m"));
            File.WriteAllText(Path.Combine(path, prefix + "AppDelegate.m"), File.ReadAllText(Path.Combine(path, prefix + "AppDelegate.m")).Replace("@@CLASSPREFIX@@", prefix));

            File.Move(Path.Combine(path, "AppDelegate.h"), Path.Combine(path, prefix + "AppDelegate.h"));
            File.WriteAllText(Path.Combine(path, prefix + "AppDelegate.h"), File.ReadAllText(Path.Combine(path, prefix + "AppDelegate.h")).Replace("@@CLASSPREFIX@@", prefix));

            File.Move(Path.Combine(path, "RootViewController.m"), Path.Combine(path, prefix + "RootViewController.m"));
            File.WriteAllText(Path.Combine(path, prefix + "RootViewController.m"), File.ReadAllText(Path.Combine(path, prefix + "RootViewController.m")).Replace("@@CLASSPREFIX@@", prefix));

            File.Move(Path.Combine(path, "RootViewController.h"), Path.Combine(path, prefix + "RootViewController.h"));
            File.WriteAllText(Path.Combine(path, prefix + "RootViewController.h"), File.ReadAllText(Path.Combine(path, prefix + "RootViewController.h")).Replace("@@CLASSPREFIX@@", prefix));

            File.WriteAllText(Path.Combine(path, "main.m"), File.ReadAllText(Path.Combine(path, "main.m")).Replace("@@CLASSPREFIX@@", prefix));

            File.WriteAllText(Path.Combine(path, "Resources/Info.plist"), File.ReadAllText(Path.Combine(path, "Resources/Info.plist")).Replace("@@CLASSPREFIX@@", prefix).Replace("@@PROJECTNAME@@", name).Replace("@@PACKAGENAME@@", id));

            File.WriteAllText(Path.Combine(path, "project.json"), Attributes.ToString());
            return new Project(Path.Combine(path, "project.json"));
        }

        public static void BuildProject()
        {
            var cachedir = Path.Combine(Project.Path, ".icode");

            if (Directory.Exists(cachedir))
                Directory.Delete(cachedir, true);

            Directory.CreateDirectory(cachedir);
            string s = "";

            Program.WinInstance.ProgressBar.PulseStep = 1.0d / (double)(((double) Project.Classes.Count(c => c.Filename.EndsWith(".m", StringComparison.CurrentCulture))) + 2d);

            foreach (var @class in from c in Project.Classes where c.Filename.EndsWith(".m", StringComparison.CurrentCulture) select c)
            {
                Directory.CreateDirectory(Path.Combine(cachedir, "build"));
                var proc = Extensions.GetProcess("clang", @"-target '" + Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/target/arm64-apple-darwin14") + "' -x objective-c -c -isysroot '" + Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/sdk") + "' -fmodules " + string.Join(" ", @class.CompilerFlags) + "-arch arm64 '" + Path.Combine(Project.Path, @class.Filename) + "' -o '" + Path.Combine(cachedir, "build", @class.Filename + ".output") + "'");
                if (!Program.WinInstance.Output.Run(proc, (int)ActionCategory.MAKE, out _, out _))
                    return;
                s += "'" + Path.Combine(cachedir, "build", @class.Filename + ".output") + "' ";
                Program.WinInstance.ProgressBar.Fraction += Program.WinInstance.ProgressBar.PulseStep;
            }

            Directory.CreateDirectory(Path.Combine(Project.Path, ".icode/Payload/" + Project.Name + ".app/"));
            var process = Extensions.GetProcess("clang", @"-target '" + Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/target/arm64-apple-darwin14") + "' -framework " + string.Join(" -framework ", Project.Frameworks)  + " -isysroot '" + Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/sdk") + "' -arch arm64 -o '" + Path.Combine(Project.Path, ".icode/Payload/" + Project.Name + ".app/" + Project.Name) + "' " + s);
            if (!Program.WinInstance.Output.Run(process, (int)ActionCategory.LINK, out _, out _))
                return;

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
            SignIpa(Path.Combine(Project.Path, "build/" + Project.Name + "-unsigned.ipa"));
        }

        public static void SignIpa(string path)
        {
            if (!File.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/developer/key.pem")))
                return;

            if (File.Exists(Path.Combine(Project.Path, "build/" + Project.Name + ".ipa")))
                File.Delete(Path.Combine(Project.Path, "build/" + Project.Name + ".ipa"));

            var process = Extensions.GetProcess(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/helper/sign-ipa"), string.Format("-m {4} -c {3} -k {2} -o {1} {0}",
                "'" + path + "'",
                "'" + Path.Combine(Project.Path, "build/" + Project.Name + ".ipa'"),
                "'" + Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/developer/key.pem'"),
                "'" + Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/developer/certificate.pem'"),
                "'" + Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/developer/provision-profile.mobileprovision'")
            ));

            Program.WinInstance.Output.Run(process, (int)ActionCategory.SIDELOAD, out _, out _);
            Program.WinInstance.ProgressBar.Fraction += Program.WinInstance.ProgressBar.PulseStep;
        }

        public static void RunProject()
        {
            var win = DeviceSelectorWindow.Create();

            if (win.Run() == (int) ResponseType.Ok)
            {

            }
        }
    }
}
