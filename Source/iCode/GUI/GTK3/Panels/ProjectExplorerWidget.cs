using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gdk;
using Gtk;
using iCode.GUI.Backend.Interfaces.Panels;
using iCode.GUI.GTK3.GladeUI;
using iCode.Projects;
using iCode.Utils;
using UI = Gtk.Builder.ObjectAttribute;

namespace iCode.GUI.GTK3.Panels
{
	public class ProjectExplorerWidget : Bin, IProjectExplorerWidget, IGladeWidget
	{

#pragma warning disable 649
		[UI]
		private TreeView _treeview1;
#pragma warning restore 649

		public string ResourceName => "ProjectExplorer";
		public string WidgetName => "ProjectExplorerWidget";
		
		private static List<RowActivatedHandler> _handlers = new List<RowActivatedHandler>();

		private static TreeIter _projectNode;
		private static TreeIter _resourcesNode;

		private static List<TreeIter> _classNodes = new List<TreeIter>();
		private static List<TreeIter> _resourceNodes = new List<TreeIter>();

		public void Initialize()
		{
			SetSizeRequest(100, 1);
			this._treeview1.HeadersVisible = false;
			TreeStore model = new TreeStore(new Type[]
			{
				typeof(Pixbuf),
				typeof(string)
			});
			this._treeview1.Model = model;
			CellRendererText ct = new CellRendererText();
			CellRendererPixbuf cb = new CellRendererPixbuf();
			TreeViewColumn column = new TreeViewColumn();
			column.PackStart(cb, false);
			column.PackStart(ct, false);
			column.AddAttribute(cb, "pixbuf", 0);
			column.AddAttribute(ct, "text", 1);
			_treeview1.AppendColumn(column);
			
		}

		public void LoadProject(string file)
		{
			RowActivatedHandler e;
				// It is GTK 3 classic frontend, do like before
				TreeStore treeStore = (TreeStore) _treeview1.Model;
				treeStore.Clear();

				_projectNode = treeStore.AppendValues(new object[]
				{
					Utils.IconLoader.LoadIcon(this, "gtk-directory", IconSize.Menu),
					ProjectManager.Project.Name
				});

				_resourcesNode = treeStore.AppendValues(_projectNode, new object[]
				{
					Utils.IconLoader.LoadIcon(this, "gtk-directory", IconSize.Menu),
					"Resources"
				});

				foreach (Class @class in ProjectManager.Project.Classes)
				{
					var node = treeStore.AppendValues(_projectNode,
						Extensions.GetIconFromFile(System.IO.Path.Combine(ProjectManager.Project.Path, @class.Filename)),
						System.IO.Path.GetFileName(@class.Filename)
					);
					_classNodes.Add(node);
				}

				foreach (string path in Directory.GetFiles(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(file), "Resources")))
				{
					var node = treeStore.AppendValues(_resourcesNode,
						Extensions.GetIconFromFile(System.IO.Path.GetFullPath(path)),
						System.IO.Path.GetFileName(path)
					);

					_resourceNodes.Add(node);
				}

				e = new RowActivatedHandler((o, args) =>
				{
					TreeIter treeIter;
					treeStore.GetIter(out treeIter, args.Path);

					int type = 0;

					foreach (var @class in _classNodes)
					{
						if (Equals(treeIter, @class))
						{
							type = 1;
							break;
						}
					}

					if (type != 1)
					{
						foreach (var @class in _resourceNodes)
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
							var code = CodeWidget.AddCodeTab(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(file),
								(string) treeStore.GetValue(treeIter, 1)));
							CodeWidget.Codewidget.Tabs.Page =
								CodeWidget.Codewidget.Tabs.PageNum(Extensions
																  .Tabs
																  .First(x =>
																	   x.Key == (string) treeStore.GetValue(treeIter,
																		   1)).Value);
							break;

						case 2:
							Extensions.LaunchProcess("gio",
								"open \"" + System.IO.Path.Combine(System.IO.Path.GetDirectoryName(file), "Resources",
									(string) treeStore.GetValue(treeIter, 1)) + "\"", out _, false);
							break;
					}
				});

				foreach (var row in _handlers)
				{
					_treeview1.RowActivated -= row;
				}

				_handlers.Clear();

				_treeview1.RowActivated += e;
				_handlers.Add(e);
		}

		public TreeView TreeView => this._treeview1;
	}
}