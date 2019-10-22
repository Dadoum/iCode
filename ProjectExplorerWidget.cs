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
            CellRendererText ct = new CellRendererText();
            CellRendererPixbuf cb = new CellRendererPixbuf();
            TreeViewColumn column = new TreeViewColumn();
            column.PackStart(cb, false);
            column.PackStart(ct, false);
            column.AddAttribute(cb, "pixbuf", 0);
            column.AddAttribute(ct, "text", 1);
            column.AddAttribute(ct, "editable", 2);
            treeview1.AppendColumn(column);
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
