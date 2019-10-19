using System;
using System.ComponentModel;
using Gdk;
using Gtk;
using Stetic;

namespace iCode
{
	[ToolboxItem(true)]
	public partial class ProjectExplorerWidget : Bin
	{
		public ProjectExplorerWidget()
		{
			this.Build();
			base.SetSizeRequest(100, 1);
			this.treeview1.HeadersVisible = false;
			TreeStore model = new TreeStore(new Type[]
			{
				typeof(Pixbuf),
				typeof(string)
			});
			this.treeview1.Model = model;
			TreeViewColumn treeViewColumn = new TreeViewColumn();
			this.treeview1.AppendColumn("Icon", new CellRendererPixbuf(), new object[]
			{
				"pixbuf",
				0
			});
			this.treeview1.AppendColumn(treeViewColumn);
			CellRendererText cell = new CellRendererText();
			treeViewColumn.PackStart(cell, true);
			treeViewColumn.AddAttribute(cell, "icon", 0);
			treeViewColumn.AddAttribute(cell, "text", 1);
		}

		public TreeView TreeView
		{
			get
			{
				return this.treeview1;
			}
		}
	}
}
