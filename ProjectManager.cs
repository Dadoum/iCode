using System;
using System.Collections.Generic;
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
			TreeIter parent = treeStore.AppendValues(new object[]
			{
				null,
				ProjectManager.Project.Name
			});
			TreeIter parent2 = treeStore.AppendValues(parent, new object[]
			{
				IconLoader.LoadIcon(Program.WinInstance.ProjectExplorer, "gtk-open", IconSize.Menu),
				"Resources"
			});
			foreach (Class @class in ProjectManager.Project.Classes)
			{
				treeStore.AppendValues(parent, new object[]
				{
					null,
					Path.GetFileName(@class.Filename)
				});
			}
			foreach (string path in Directory.GetFiles(Path.Combine(Path.GetDirectoryName(file), "Resources")))
			{
				treeStore.AppendValues(parent2, new object[]
				{
					null,
					Path.GetFileName(path)
				});
			}
		}

        public static Project CreateProject(string name, string id, string prefix)
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "iCode Projects/", name);
            var Frameworks = new List<string>();
            var Classes = new List<Class>();
            var Attributes = new JObject();

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
            var template = Assembly.GetExecutingAssembly().GetManifestResourceStream("iCode.resources.archives.objective-c-template.tar").ToByteArray();
            File.WriteAllBytes(temp, template);

            ZipFile.ExtractToDirectory(temp, path);
            File.Move(Path.Combine(path, "AppDelegate.m"), Path.Combine(path, prefix + "AppDelegate.m"));
            File.WriteAllText(prefix + "AppDelegate.m", File.ReadAllText(prefix + "AppDelegate.m").Replace("@@CLASSPREFIX@@", prefix));

            File.Move(Path.Combine(path, "AppDelegate.h"), Path.Combine(path, prefix + "AppDelegate.h"));
            File.WriteAllText(prefix + "AppDelegate.h", File.ReadAllText(prefix + "AppDelegate.h").Replace("@@CLASSPREFIX@@", prefix));

            File.Move(Path.Combine(path, "RootViewController.m"), Path.Combine(path, prefix + "RootViewController.m"));
            File.WriteAllText(prefix + "RootViewController.m", File.ReadAllText(prefix + "RootViewController.m").Replace("@@CLASSPREFIX@@", prefix));

            File.Move(Path.Combine(path, "RootViewController.h"), Path.Combine(path, prefix + "RootViewController.h"));
            File.WriteAllText(prefix + "RootViewController.h", File.ReadAllText(prefix + "RootViewController.h").Replace("@@CLASSPREFIX@@", prefix));

            File.WriteAllText(prefix + "main.m", File.ReadAllText(prefix + "main.m").Replace("@@CLASSPREFIX@@", prefix));

            File.WriteAllText(Path.Combine(path, "project.json"), Attributes.ToString());
            return new Project(Attributes.ToString());
        }
    }
}
