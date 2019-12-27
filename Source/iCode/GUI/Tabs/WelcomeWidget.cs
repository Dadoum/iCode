using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Gtk;
using iCode.Projects;
using Newtonsoft.Json.Linq;
using Pango;
using UI = Gtk.Builder.ObjectAttribute;

namespace iCode.GUI.Tabs
{
	public class WelcomeWidget : Bin
	{
#pragma warning disable 649
		[UI]
		private global::Gtk.Label _label1;
		[UI]
		private global::Gtk.Label _label2;
		[UI]
		private global::Gtk.Button _button1;
		[UI]
		private global::Gtk.Button _button2;
		[UI]
		private global::Gtk.Button _button3;
		[UI]
		private global::Gtk.Button _button4;
		[UI]
		private global::Gtk.Button _button5;
		[UI]
		private global::Gtk.Button _button6;
#pragma warning restore 649


		public static WelcomeWidget Create()
		{
			Builder builder = new Builder(null, "Welcome", null);
			return new WelcomeWidget(builder, builder.GetObject("WelcomeWidget").Handle);
		}

		public WelcomeWidget(Builder builder, IntPtr handle) : base(handle)
		{
			builder.Autoconnect(this);
			FontDescription fontDescription2 = base.PangoContext.FontDescription;
			int defsize = fontDescription2.Size;
			fontDescription2.Size = 30520;
			var attr1 = new AttrList();
			attr1.Insert(new AttrFontDesc(fontDescription2));
			_label2.Attributes = attr1;
            
			FontDescription fontDescription = base.PangoContext.FontDescription;
			fontDescription.Size = defsize * 4;
			fontDescription.Weight = Weight.Bold;
			this._label1.UseMarkup = true;
			var attr = new AttrList();
			attr.Insert(new AttrFontDesc(fontDescription));
			_label1.Attributes = attr;
            
			_button1.Clicked += ProjectButton_Activated;
			_button2.Clicked += ProjectButton_Activated;
			_button3.Clicked += ProjectButton_Activated;
			_button4.Clicked += ProjectButton_Activated;
			_button5.Clicked += Button5_Activated;
			_button6.Clicked += Button6_Activated;

			if (File.Exists(System.IO.Path.Combine(Program.ConfigPath, "recentProjects")))
			{
				string text = File.ReadAllText(System.IO.Path.Combine(Program.ConfigPath, "recentProjects"));
				var paths = text.Split('\n');

				foreach (var path in from p in paths where File.Exists(System.IO.Path.Combine(p, "project.json")) select p)
				{
					if (_button4.Label == "Placeholder project")
					{
						_button4.Label = JObject.Parse(File.ReadAllText(System.IO.Path.Combine(path, "project.json")))["name"] + "\n" + path;
					}
					else if (_button3.Label == "Placeholder project")
					{
						_button3.Label = JObject.Parse(File.ReadAllText(System.IO.Path.Combine(path, "project.json")))["name"] + "\n" + path;
					}
					else if (_button2.Label == "Placeholder project")
					{
						_button2.Label = JObject.Parse(File.ReadAllText(System.IO.Path.Combine(path, "project.json")))["name"] + "\n" + path;
					}
					else if (_button1.Label == "Placeholder project")
					{
						_button1.Label = JObject.Parse(File.ReadAllText(System.IO.Path.Combine(path, "project.json")))["name"] + "\n" + path;
					}
				}
			}

			if (_button1.Label == "Placeholder project")
			{
				_button1.Dispose();
			}

			if (_button2.Label == "Placeholder project")
			{
				_button2.Dispose();
			}

			if (_button3.Label == "Placeholder project")
			{
				_button3.Dispose();
			}

			if (_button4.Label == "Placeholder project")
			{
				_button4.Dispose();
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
				ProjectManager.CreateProject(dialog.ProjectName, dialog.Id, dialog.Prefix, dialog.SelectedTemplatePath);
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
				fileChooserDialog.Dispose();
			}
			catch (Exception ex)
			{
				ExceptionWindow.Create(ex, this).ShowAll();
			}
		}

	}
}