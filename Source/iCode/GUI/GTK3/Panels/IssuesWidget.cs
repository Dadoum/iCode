using System;
using System.Collections.Generic;
using Gdk;
using Gtk;
using iCode.GUI.Backend.Interfaces.Panels;
using iCode.GUI.GTK3.GladeUI;
using iCode.GUI.GTK3.Tabs;
using iCode.Projects;
using iCode.Utils;
using NClang;
using UI = Gtk.Builder.ObjectAttribute;

namespace iCode.GUI.GTK3.Panels
{
	public class IssuesWidget : Gtk.ScrolledWindow, IGladeWidget, IIssuesWidget
	{
#pragma warning disable 649
		[UI] private TreeStore _errorStore;
		[UI] private TreeView _treeView;
#pragma warning restore 649

		private Pixbuf _errorIcon;
		private Pixbuf _fatalIcon;
		private Pixbuf _noteIcon;
		private Pixbuf _warningIcon;

		private Dictionary<TreeIter, ClangSourceLocation> _locations = new Dictionary<TreeIter, ClangSourceLocation>();
		private Dictionary<CodeTabWidget, ClangDiagnosticSet> _assignedDiagnos = new Dictionary<CodeTabWidget, ClangDiagnosticSet>();

		public string ResourceName => "Issues";

		public string WidgetName => "IssuesWidget";

		public void Initialize() 
		{
			_errorStore = new TreeStore(typeof(Pixbuf), typeof(string), typeof(string), typeof(string));
			
			_treeView.AppendColumn("", new CellRendererPixbuf(), "pixbuf", 0);
			_treeView.AppendColumn("Category", new CellRendererText(), "text", 1);
			_treeView.AppendColumn("Spelling", new CellRendererText(), "text", 2);
			_treeView.AppendColumn("Location", new CellRendererText(), "text", 3);
			
			this._treeView.HeadersVisible = true;
			_treeView.Model = _errorStore;
			_treeView.ShowAll();
			this.ShowAll();

			_errorIcon = IconLoader.LoadIcon(this, "gtk-dialog-error", IconSize.Menu);
			_warningIcon = IconLoader.LoadIcon(this, "gtk-dialog-warning", IconSize.Menu);
			_noteIcon = IconLoader.LoadIcon(this, "gtk-dialog-info", IconSize.Menu);
			_fatalIcon = IconLoader.LoadIcon(this, "gtk-dialog-error", IconSize.Menu);
			
			_treeView.RowActivated += (o, args) =>
			{
				_errorStore.GetIter(out TreeIter treeIter, args.Path);

				var filename = _locations[treeIter].FileLocation.File.FileName.Split('~')[1];
				Console.WriteLine(filename);
				CodeWidget.AddCodeTab(System.IO.Path.Combine(ProjectManager.Project.Path, filename));

				var tab = Extensions.Tabs[filename] as CodeTabWidget;
				var buffer = tab!.Buffer;
				
				buffer.PlaceCursor(buffer.GetIterAtOffset(_locations[treeIter].FileLocation.Offset));
			};
		}

		public void ProcessDiagnosticSet(ClangDiagnosticSet set, CodeTabWidget processor)
		{
			if (_assignedDiagnos.ContainsKey(processor))
			{
				try
				{
					foreach (var issue in _assignedDiagnos[processor].Items)
					{
						TreeIter treeIter = TreeIter.Zero;
						for (int i = 0; i != -1; i++)
						{
							try
							{
								_errorStore.GetIterFromString(out var iter,
									i.ToString());

								if (_locations[iter] == issue.Location)
								{
									treeIter = iter;
									break;
								}
							}
							catch
							{
								break;
							}
						}
						_errorStore.Remove(ref treeIter);
						_locations.Remove(treeIter);
					}
				}
				catch
				{
					// Not important
				}
			}

			_assignedDiagnos[processor] = set;
			
			foreach (var diagnostic in set.Items)
			{
				var issue = _errorStore.AppendValues(
					diagnostic.Severity == DiagnosticSeverity.Error ? _errorIcon :
					diagnostic.Severity == DiagnosticSeverity.Warning ? _warningIcon :
					diagnostic.Severity == DiagnosticSeverity.Fatal ? _fatalIcon : _noteIcon,
					diagnostic.CategoryText,
					diagnostic.Spelling,
					diagnostic.Location.ToString());

				_locations.Add(issue, diagnostic.Location);
				
				diagnostic.Dispose();
			}
		}
	}
}