using System;
using System.ComponentModel;
using Gdk;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;

namespace iCode.GUI.Panels
{
	public class ProjectExplorerWidget : Bin
	{

#pragma warning disable 649
		[UI]
		private global::Gtk.TreeView _treeview1;
#pragma warning restore 649

		public static ProjectExplorerWidget Create()
		{
			Builder builder = new Builder(null, "ProjectExplorer", null);
			return new ProjectExplorerWidget(builder, builder.GetObject("ProjectExplorerWidget").Handle);
		}


		ProjectExplorerWidget(Builder builder, IntPtr handle) :base(handle)
		{
			builder.Autoconnect(this);
			base.SetSizeRequest(100, 1);
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
			column.AddAttribute(ct, "editable", 2);
			_treeview1.AppendColumn(column);
		}

		public TreeView TreeView
		{
			get
			{
				return this._treeview1;
			}
			set 
			{
				_treeview1 = value;
			}
		}
	}
}