using System;
using System.Threading.Tasks;
using Gdk;
using Gdl;
using Gtk;
using iCode.GUI.Panels;
using iCode.Projects;
using Console = iCode.Utils.Console;
using UI = Gtk.Builder.ObjectAttribute;

namespace iCode.GUI
{
	public class MainWindow : Gtk.Window
	{
	#region Getters
		public Label StateLabel
		{
			get
			{
				return _label1;
			}
		}

		public ProjectExplorerWidget ProjectExplorer
		{
			get
			{
				return this._projectExplorerView;
			}
		}

		public ProgressBar ProgressBar
		{
			get
			{
				return this._progressbar1;
			}
		}

		public PropertyWidget Properties
		{
			get
			{
				return this._propertyWidgetView;
			}
		}

		public OutputWidget Output
		{
			get
			{
				return this._outputWidget;
			}
		}
	#endregion

		Builder _builder;

#pragma warning disable 649
		[UI]
		private global::Gtk.MenuItem _openProjectAction;
		[UI]
		private global::Gtk.MenuItem _createProjectAction;
		[UI]
		private global::Gtk.MenuItem _aboutICodeAction;
		[UI]
		private global::Gtk.MenuItem _buildProjectAction;
		[UI]
		private global::Gtk.Button _button6;
		[UI]
		private global::Gtk.Button _button7;
		[UI]
		private global::Gtk.ProgressBar _progressbar1;
		[UI]
		private global::Gtk.EventBox _statusBox;
		[UI]
		private global::Gtk.Label _label1;
#pragma warning restore 649

		public static MainWindow Create()
		{
			Builder builder = new Builder(null, "MainWindow", null);
			return new MainWindow(builder, builder.GetObject("MainWindow").Handle);
		}

		public MainWindow(Builder builder, IntPtr handle) : base(handle)
		{
			try
			{
				this._builder = builder;
				builder.Autoconnect(this);
				this.Title = Names.ApplicationName;
				_progressbar1.Text = Names.ApplicationName;
				CodeWidget.Initialize();
				// SetSizeRequest(800, 600);
				Box box = new Box(Orientation.Vertical, 0);
				_dock = new Dock();
				this._master = (DockMaster)this._dock.Master;
				_master.SwitcherStyle = SwitcherStyle.Tabs;
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
				CodeWidget.AddWelcomeTab(string.Format("Welcome to {0} !", Names.ApplicationName));
				dockItem.Add(this.GetCodePane());
				dockItem.ShowAll();

				DockItem dockItem4 = new DockItem("outputConsole", "Output", Stock.Execute, 0);
				this._dock.AddItem(dockItem4, DockPlacement.Bottom);
				dockItem4.Add(_outputWidget);
				dockItem4.ShowAll();

				DockItem dockItem2 = new DockItem("projectExplorer", "Project Explorer", Stock.Harddisk, 0);
				this._dock.AddItem(dockItem2, DockPlacement.Left);
				dockItem2.Add(this.CreateProjectExplorerPane());
				dockItem2.ShowAll();

				DockItem dockItem3 = new DockItem("properties", "Properties", Stock.Properties, 0);
				this._dock.AddItem(dockItem3, DockPlacement.Right);
				dockItem3.Add(this.CreatePropertiesPane());
				dockItem3.ShowAll();
                
				this.DeleteEvent += OnDeleteEvent;
				this.Add(box);
				this.Icon = Pixbuf.LoadFromResource("iCode.resources.images.icon.png");
				this._buildProjectAction.Activated += (sender, e) =>
				{
					Task.Factory.StartNew(() =>
					{
						if (ProjectManager.BuildProject())
							StateLabel.Text = "Build succeeded.";
						else
							StateLabel.Text = "Build failed.";
					});
				};
				this._aboutICodeAction.Activated += (sender, e) =>
				{
					AboutWindow.Create().ShowAll();
				};

				ProjectManager.AddSensitiveWidget(_buildProjectAction);
				ProjectManager.AddSensitiveWidget(_button6);
				ProjectManager.AddSensitiveWidget(_button7);

				_label1.Text = Names.ApplicationName;

				this._button6.Clicked += (sender, e) =>
				{
					ProjectManager.RunProject();
				};
				Gtk.CssProvider nopad = new CssProvider();
				nopad.LoadFromData(@"
                widget
                { 
                    border-radius: 4px;
                    background: @borders;
                }
                progress, trough 
                {
                    border-bottom-right-radius: 4px;
                    border-bottom-left-radius: 4px;
                    min-height: 4px;
                }
                ");
				_statusBox.StyleContext.AddProvider(nopad, 1);
				_progressbar1.StyleContext.AddProvider(nopad, 1);
				_createProjectAction.Activated += CreateProject;
				_openProjectAction.Activated += LoadProjectActivated;
				// var b = layout.LoadFromFile(System.IO.Path.Combine(Program.ConfigPath, "layouts/saved.layout"));
				// Console.WriteLine("Fail or success ? It's " + b + " !");
			}
			catch (Exception e)
			{
				ExceptionWindow.Create(e, this).ShowAll();
			}
		}

		delegate void GetTheme();
        
		private Widget CreateProjectExplorerPane()
		{
			return this._projectExplorerView;
		}

		private Widget CreatePropertiesPane()
		{
			return this._propertyWidgetView;
		}

		private Widget GetCodePane()
		{
			return CodeWidget.Codewidget;
		}

		protected void OnDeleteEvent(object sender, DeleteEventArgs a)
		{
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
				ExceptionWindow.Create(ex, this).ShowAll();
			}
		}

		private readonly ProjectExplorerWidget _projectExplorerView = ProjectExplorerWidget.Create();
		private readonly PropertyWidget _propertyWidgetView = PropertyWidget.Create();
		private readonly OutputWidget _outputWidget = OutputWidget.Create();

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

	}
}