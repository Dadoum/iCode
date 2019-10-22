using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using Gtk;
using Newtonsoft.Json.Linq;
using Stetic;

namespace iCode
{
	public static class ProjectManager
    {
        public static Project Project;

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

		public static void LoadProject(string file)
		{
			TreeView treeView = Program.WinInstance.ProjectExplorer.TreeView;
			TreeStore treeStore = (TreeStore) treeView.Model;
			ProjectManager.Project = new Project(File.ReadAllText(file));

            CodeWidget.RemoveTab("Welcome to iCode !");

            treeView.RowActivated += (o, args) => 
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
                        var code = CodeWidget.AddCodeTab(Path.Combine(Path.GetDirectoryName(file), (string) treeStore.GetValue(treeIter, 1)));
                        break;

                    case 2:
                        Process.Start("gio", "open '" + Path.Combine(Path.GetDirectoryName(file), "Resources", (string) treeStore.GetValue(treeIter, 1)) + "'");
                        break;
                }
            };
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
		}

        public static Project CreateProject(string name, string id, string prefix)
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "iCode Projects/", name);
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
            return new Project(Attributes.ToString());
        }
    }
}
