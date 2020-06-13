using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Gdl;
using GLib;
using Gtk;
using iCode.GUI.Backend.Interfaces;
using iCode.GUI.Backend.Interfaces.Panels;
using iCode.GUI.GTK3.GladeUI;
using iCode.GUI.GTK3.Panels;
using iCode.Projects;
using Menu = Gtk.Menu;
using MenuItem = Gtk.MenuItem;
using Task = System.Threading.Tasks.Task;
using UI = Gtk.Builder.ObjectAttribute;

namespace iCode.GUI.GTK3
{
	public class MainWindow : Window, IGladeWidget, IMainWindow
	{
#pragma warning disable 649
		[UI]
		private MenuItem _openProjectAction;
		[UI]
		private MenuItem _createProjectAction;
		[UI]
		private MenuItem _aboutICodeAction;
		[UI]
		private MenuItem _buildProjectAction;
		[UI]
		private Box _box;
		[UI]
		private MenuItem _layoutAction;
		[UI]
		private MenuItem _settingsAction;
		[UI]
		private MenuItem _checkUpdates;
		[UI]
		private Button _button6;
		[UI]
		private Button _button7;
		[UI]
		private LevelBar _progressbar1;
		[UI]
		private Label _label1;
#pragma warning restore 649

		public string ResourceName => "MainWindow";
		public string WidgetName => "MainWindow";

		public MainWindow() : base(WindowType.Toplevel)
		{
			
		}

		public void Initialize() 
		{
			try
			{
				this.WindowPosition = WindowPosition.Center;
				this.Title = Identity.ApplicationName;
				Box box = new Box(Orientation.Vertical, 0);
				_dock = new Dock();
				this._master = (DockMaster) this._dock.Master;
				_master.SwitcherStyle = SwitcherStyle.Tabs;
				_master.TabReorderable = true;
				this._layout = new DockLayout(this._dock);
				_layout.Master = _master;
				this._bar = new DockBar(this._dock);
				Box box2 = new Box(Orientation.Horizontal, 5);
				box.PackStart(box2, true, true, 0u);
				box2.PackStart(this._bar, false, false, 0u);
				box2.PackEnd(this._dock, true, true, 0u);
				DockItem dockItem = new DockItem("code1", "Code", Stock.Edit, DockItemBehavior.CantClose);
				dockItem.Grip.Hide();
				this._dock.AddItem(dockItem, DockPlacement.Center);
				this._dock.BorderWidth = 2u;
				Panels.CodeWidget.AddWelcomeTab(string.Format("Welcome to {0} !", Identity.ApplicationName));
				dockItem.Add((Widget) CodeWidget);
				dockItem.ShowAll();
				
				DockItem dockItem4 = new DockItem("outputConsole", "Output", Stock.Execute, 0);
				this._dock.AddItem(dockItem4, DockPlacement.Bottom);
				dockItem4.Add(_outputWidget);
				dockItem4.ShowAll();
				
				DockItem dockItem5 = new DockItem("issues", "Issues", Stock.DialogWarning, 0);
				this._dock.AddItem(dockItem5, DockPlacement.Bottom);
				dockItem5.Add(_issuesWidget);
				_issuesWidget.ShowAll();
				dockItem5.ShowAll();

				DockItem dockItem2 = new DockItem("projectExplorer", "Project Explorer", Stock.Harddisk, 0);
				this._dock.AddItem(dockItem2, DockPlacement.Left);
				dockItem2.Add(_projectExplorerWidget);
				dockItem2.ShowAll();

				DockItem dockItem3 = new DockItem("properties", "Properties", Stock.Properties, 0);
				this._dock.AddItem(dockItem3, DockPlacement.Right);
				dockItem3.Add(_propertiesWidget);
				dockItem3.ShowAll();

				this.DeleteEvent += OnDeleteEvent;

				_box.PackStart(box, true, true, 0);
				_box.ChildSetProperty(box, "position", new Value(2));
				
				this.Icon = Identity.ApplicationIcon;
				this._buildProjectAction.Activated += (sender, e) =>
				{
					Task.Factory.StartNew(() =>
					{
						if (ProjectManager.BuildProject())
						{
							Gtk.Application.Invoke((o, a) => { _label1.Text = "Build succeeded."; });
						}
						else
						{
							Gtk.Application.Invoke((o, a) => { _label1.Text = "Build failed."; });
						}
					});
				};
				this._aboutICodeAction.Activated += (sender, e) => { GladeHelper.Create<AboutWindow>().ShowAll(); };
				
				ProjectManager.AddSensitiveWidget(_buildProjectAction);
				ProjectManager.AddSensitiveWidget(_button6);
				ProjectManager.AddSensitiveWidget(_button7);

				_label1.Text = Identity.ApplicationName;

				this._button6.Clicked += (sender, e) => { ProjectManager.RunProject(); };
				CssProvider nopad = new CssProvider();
				nopad.LoadFromData(@"
                widget
                { 
                    border-radius: 4px;
                    background: @borders;
                }
                level, trough 
                {
                    border-bottom-right-radius: 4px;
                    border-bottom-left-radius: 4px;
					border-top-right-radius: 0px;
                    border-top-left-radius: 0px;
                    min-height: 4px;
                }
                ");
				var layoutFile = System.IO.Path.Combine(Program.ConfigPath, "Layouts.xml");
				
				if (!File.Exists(layoutFile))
				{
					_layout.SaveLayout("default_layout");
					_layout.SaveToFile(layoutFile);
				}
				
				_layout.LoadFromFile(layoutFile);

				XDocument xdoc = XDocument.Load(layoutFile);
				var layoutList = xdoc.Elements().First().Elements().ToList();
				Dictionary<MenuItem, string> names = new Dictionary<MenuItem, string>();
				Menu menu = new Menu();
				
				var saveItem = new MenuItem();
				saveItem.Label = "Save actual layout...";
				saveItem.Activated += (o, a) =>
				{
					InputWindow input = GladeHelper.CreateFromInterface<InputWindow>();
					input.Title = "Select name for the layout";
					input.Run();
					if (string.IsNullOrWhiteSpace(input.Text))
						return;
					Console.WriteLine($"Saving layout \"{input.Text}\"");
					_layout.SaveLayout(input.Text);
					var menuItem = new MenuItem();
					menuItem.Label = input.Text;
					menuItem.Activated += (ou, au) => { _layout.LoadLayout(input.Text); };
					menu.Append(menuItem);
					_layout.SaveToFile(layoutFile);
				};
				menu.Append(saveItem);
				
				foreach (var a in layoutList)
				{
					var menuItem = new MenuItem();
					var name = a.Attributes().First(x => x.Name == "name").Value;
					names.Add(menuItem, name);
					menuItem.Label = name;
					menuItem.Activated += (o, e) =>
					{
						Console.WriteLine($"Loading layout {names[menuItem]}");
						_layout.LoadLayout(names[menuItem]);
					};
					menu.Append(menuItem);

					if (name == "__default_")
					{
						this.ShowAll();
						_layout.LoadLayout(name);
					}
				}

				_layoutAction.Submenu = menu;
				
				_progressbar1.StyleContext.AddProvider(nopad, 1);
				_createProjectAction.Activated += CreateProject;
				_openProjectAction.Activated += LoadProjectActivated;

				_settingsAction.Activated += (o, a) => { GladeHelper.Create<SettingsWindow>().Run(); };
				_checkUpdates.Activated += (o, a) =>
				{
					Program.CheckUpdates();
				};
			}
			catch (Exception e)
			{
				GladeHelper.Create<ExceptionWindow>().ShowException(e, this);
			}
		}

		protected void OnDeleteEvent(object sender, DeleteEventArgs a)
		{
			var layoutFile = System.IO.Path.Combine(Program.ConfigPath, "Layouts.xml");
			_layout.SaveToFile(layoutFile);
			
			if (Program.UpdateInstalled)
				System.Diagnostics.Process.Start("killall", "appimagelauncherfs");
			
			Gtk.Application.Quit();
			a.RetVal = true;
		}

		public void LoadProjectActivated(object sender, EventArgs e)
		{
			try
			{
				FileChooserDialog fileChooserDialog = new FileChooserDialog("Select project file", this, FileChooserAction.Open, new object[0]);
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
				GladeHelper.Create<ExceptionWindow>().ShowException(ex, this);
			}
		}

		private readonly ProjectExplorerWidget _projectExplorerWidget = GladeHelper.Create<ProjectExplorerWidget>();
		private readonly PropertyWidget _propertiesWidget = GladeHelper.Create<PropertyWidget>();
		private readonly OutputWidget _outputWidget = GladeHelper.Create<OutputWidget>();
		private readonly IssuesWidget _issuesWidget = GladeHelper.Create<IssuesWidget>();

		private Dock _dock;
		private DockMaster _master;
		private DockLayout _layout;
		private DockBar _bar;

		public void CreateProject(object sender, EventArgs e)
		{
			var dialog = NewProjectWindow.Create();

			if (dialog.Run() == (int)ResponseType.Ok)
			{
				ProjectManager.CreateProject(dialog.ProjectName, dialog.Id, dialog.Prefix, dialog.SelectedTemplatePath, dialog.Path);
				ProjectManager.LoadProject(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "iCode Projects/", dialog.ProjectName, "project.json"));
			}
		}

		public IOutputWidget OutputWidget => this._outputWidget;
		public IIssuesWidget IssuesWidget => this._issuesWidget;
		public ICodeWidget CodeWidget => Panels.CodeWidget.Codewidget;
		public IPropertiesWidget PropertiesWidget => this._propertiesWidget;
		public IProjectExplorerWidget ProjectExplorerWidget => this._projectExplorerWidget;

		public void SetProgressMaxValue(double value)
		{
			this._progressbar1.MaxValue = value;
		}

		public void SetProgressValue(double value)
		{
			this._progressbar1.Value = value;
		}
		
		public void AddProgressValue(double value)
		{
			this._progressbar1.Value += value;
		}

		public void SetStatusText(string text)
		{
			this._label1.Text = text;
		}
	}
}