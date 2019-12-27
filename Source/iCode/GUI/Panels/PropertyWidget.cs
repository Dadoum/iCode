using System;
using System.ComponentModel;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;

namespace iCode.GUI.Panels
{
	public class PropertyWidget : Bin
	{
#pragma warning disable 649
		[UI]
		private global::Gtk.TreeView _treeview1;
#pragma warning restore 649

		public TreeView Tree => this._treeview1;

		public static PropertyWidget Create()
		{
			Builder builder = new Builder(null, "Property", null);
			return new PropertyWidget(builder, builder.GetObject("PropertyWidget").Handle);
		}

		PropertyWidget(Builder b, IntPtr handle) : base(handle)
		{
			b.Autoconnect(this);
			base.SetSizeRequest(100, 1);
		}
	}
}