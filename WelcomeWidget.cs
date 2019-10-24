using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Gtk;
using Newtonsoft.Json.Linq;
using Pango;

namespace iCode
{
    [ToolboxItem(true)]
    public partial class WelcomeWidget : Bin
    {
        public WelcomeWidget()
        {
            this.Build();
            FontDescription fontDescription = base.PangoContext.FontDescription;
            fontDescription.Size *= 4;
            this.label1.UseMarkup = true;
            this.label1.OverrideFont(fontDescription);
            FontDescription fontDescription2 = base.PangoContext.FontDescription;
            fontDescription2.Size = 30520;
            this.label2.OverrideFont(fontDescription2);

            button1.Clicked += ProjectButton_Activated;
            button2.Clicked += ProjectButton_Activated;
            button3.Clicked += ProjectButton_Activated;
            button4.Clicked += ProjectButton_Activated;
            button5.Clicked += Button5_Activated;
            button6.Clicked += Button6_Activated;

            if (File.Exists(System.IO.Path.Combine(Program.ConfigPath, "recentProjects")))
            {
                string text = File.ReadAllText(System.IO.Path.Combine(Program.ConfigPath, "recentProjects"));
                var paths = text.Split('\n');

                foreach (var path in from p in paths where File.Exists(System.IO.Path.Combine(p, "project.json")) select p)
                {
                    if (button4.Label == "Placeholder project")
                    {
                        button4.Label = JObject.Parse(File.ReadAllText(System.IO.Path.Combine(path, "project.json")))["name"] + "\n" + path;
                    }
                    else if (button3.Label == "Placeholder project")
                    {
                        button3.Label = JObject.Parse(File.ReadAllText(System.IO.Path.Combine(path, "project.json")))["name"] + "\n" + path;
                    }
                    else if (button2.Label == "Placeholder project")
                    {
                        button2.Label = JObject.Parse(File.ReadAllText(System.IO.Path.Combine(path, "project.json")))["name"] + "\n" + path;
                    }
                    else if (button1.Label == "Placeholder project")
                    {
                        button1.Label = JObject.Parse(File.ReadAllText(System.IO.Path.Combine(path, "project.json")))["name"] + "\n" + path;
                    }
                }
            }

            if (button1.Label == "Placeholder project")
            {
                button1.Destroy();
            }

            if (button2.Label == "Placeholder project")
            {
                button2.Destroy();
            }

            if (button3.Label == "Placeholder project")
            {
                button3.Destroy();
            }

            if (button4.Label == "Placeholder project")
            {
                button4.Destroy();
            }
        }
            
        void ProjectButton_Activated (object sender, EventArgs e)
        {
            ProjectManager.LoadProject(System.IO.Path.Combine((sender as Gtk.Button).Label.Split('\n')[1], "project.json"));
        }

        void Button5_Activated(object sender, EventArgs e)
        {
            var dialog = NewProjectWindow.Create();

            if (dialog.Run() == (int)ResponseType.Ok)
            {
                ProjectManager.CreateProject(dialog.ProjectName, dialog.Id, dialog.Prefix);
                ProjectManager.LoadProject(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "iCode Projects/", dialog.ProjectName, "project.json"));
            }
        }

        void Button6_Activated(object sender, EventArgs e)
        {
            try
            {
                FileChooserDialog fileChooserDialog = new FileChooserDialog("Select project file", null, FileChooserAction.Open, new object[0]);
                fileChooserDialog.AddButton(Stock.Cancel, ResponseType.Cancel);
                fileChooserDialog.AddButton(Stock.Open, ResponseType.Ok);
                fileChooserDialog.DefaultResponse = ResponseType.Ok;
                fileChooserDialog.SelectMultiple = false;
                FileFilter fileFilter = new FileFilter();
                fileFilter.AddMimeType("application/json");
                fileFilter.AddPattern("project.json");
                fileFilter.Name = "iCode project file (project.json)";
                fileChooserDialog.AddFilter(fileFilter);
                bool flag = fileChooserDialog.Run() == -5;
                if (flag)
                {
                    ProjectManager.LoadProject(fileChooserDialog.Filename);
                }
                fileChooserDialog.Destroy();
            }
            catch (Exception ex)
            {
                new ExceptionWindow(ex, this).ShowAll();
            }
        }

    }
}
