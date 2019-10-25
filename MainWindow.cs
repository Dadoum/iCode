using System;
using Gdk;
using Gdl;
using Gtk;
using iCode;
using Stetic;

public partial class MainWindow : Gtk.Window
{
    #region Getters
    public Label StateLabel
    {
        get
        {
            return this.label1;
        }
    }

    public ProjectExplorerWidget ProjectExplorer
    {
        get
        {
            return this.projectExplorerView;
        }
    }

    public ProgressBar ProgressBar
    {
        get
        {
            return this.progressbar1;
        }
    }

    public PropertyWidget Properties
    {
        get
        {
            return this.propertyWidgetView;
        }
    }

    public OutputWidget Output
    {
        get
        {
            return this.outputWidget;
        }
    }
    #endregion

    public MainWindow() : base(Gtk.WindowType.Toplevel)
    {
        try 
        { 
            CodeWidget.Initialize();
            Build();
            SetSizeRequest(800, 600);
            Box box = new Box(Orientation.Vertical, 0);
            dock = new Dock();
            this.master = (DockMaster) this.dock.Master;
            this.layout = new DockLayout(this.dock);
            layout.Master = master;
            this.bar = new DockBar(this.dock);
            Box box2 = new Box(Orientation.Horizontal, 5);
            box.PackStart(box2, true, true, 0u);
            box2.PackStart(this.bar, false, false, 0u);
            box2.PackEnd(this.dock, true, true, 0u);

            DockItem dockItem = new DockItem("code1", "Code", Stock.Edit, DockItemBehavior.CantClose);
            dockItem.Grip.Hide();
            this.dock.Add(dockItem, DockPlacement.Center);
            this.dock.BorderWidth = 2u;
            CodeWidget.AddWelcomeTab("Welcome to iCode !");
            dockItem.Add(this.GetCodePane());
            dockItem.ShowAll();

            DockItem dockItem4 = new DockItem("outputConsole", "Output", Stock.Execute, 0);
            this.dock.Add(dockItem4, DockPlacement.Bottom);
            dockItem4.Add(outputWidget);
            dockItem4.ShowAll();

            DockItem dockItem2 = new DockItem("projectExplorer", "Project Explorer", Stock.Harddisk, 0);
            this.dock.Add(dockItem2, DockPlacement.Left);
            dockItem2.Add(this.CreateProjectExplorerPane());
            dockItem2.ShowAll();

            DockItem dockItem3 = new DockItem("properties", "Properties", Stock.Properties, 0);
            this.dock.Add(dockItem3, DockPlacement.Right);
            dockItem3.Add(this.CreatePropertiesPane());
            dockItem3.ShowAll();

            base.Remove(this.container);
            this.container.Add(box);
            Box.BoxChild boxChild = (Box.BoxChild)this.container[box];
            boxChild.Position = 2;
            boxChild.Expand = true;
            boxChild.Fill = true;
            base.Add(this.container);
            this.Icon = Pixbuf.LoadFromResource("iCode.resources.images.icon.png");
            this.BuildProjectAction.Activated += (sender, e) => 
            {
                ProjectManager.BuildProject();
            };
            this.AboutICodeAction.Activated += (sender, e) =>
            {
                AboutWindow.Create().ShowAll();
            };
           
            ProjectManager.AddSensitiveObject(BuildProjectAction);
            ProjectManager.AddSensitiveObject(button6);
            button6.Label = "▶";
            button6.StyleContext.AddClass("circular");

            this.button6.Clicked += (sender, e) => 
            {
                ProjectManager.RunProject();
            };
            // var b = layout.LoadFromFile(System.IO.Path.Combine(Program.ConfigPath, "layouts/saved.layout"));
            // Console.WriteLine("Fail or success ? It's " + b + " !");
        }
        catch (Exception e)
        {
            new ExceptionWindow(e, this).ShowAll();
        }
    }

    private Widget CreateProjectExplorerPane()
    {
        return this.projectExplorerView;
    }

    private Widget CreatePropertiesPane()
    {
        return this.propertyWidgetView;
    }

    private Widget GetCodePane()
    {
        return CodeWidget.codewidget;
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
            fileChooserDialog.Destroy();
        }
        catch (Exception ex)
        {
            new ExceptionWindow(ex, this).ShowAll();
        }
    }

    private readonly ProjectExplorerWidget projectExplorerView = new ProjectExplorerWidget();
    private readonly PropertyWidget propertyWidgetView = new PropertyWidget();
    private readonly OutputWidget outputWidget = OutputWidget.Create();

    private Dock dock;
    private DockMaster master;
    private DockLayout layout;
    private DockBar bar;

    public void CreateProject(object sender, EventArgs e)
    {
        var dialog = NewProjectWindow.Create();

        if (dialog.Run() == (int) ResponseType.Ok)
        {
            ProjectManager.CreateProject(dialog.ProjectName, dialog.Id, dialog.Prefix, dialog.Path);
            ProjectManager.LoadProject(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "iCode Projects/", dialog.ProjectName, "project.json"));
        }
    }

}
