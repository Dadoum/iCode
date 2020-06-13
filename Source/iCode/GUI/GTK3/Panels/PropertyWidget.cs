using Gtk;
using iCode.GUI.Backend.Interfaces.Panels;
using iCode.GUI.GTK3.GladeUI;
using UI = Gtk.Builder.ObjectAttribute;

namespace iCode.GUI.GTK3.Panels
{
	public class PropertyWidget : Bin, IPropertiesWidget, IGladeWidget
	{
#pragma warning disable 649
		[UI]
		private TreeView _treeview1;
#pragma warning restore 649

		public TreeView Tree => _treeview1;

		public string ResourceName => "Property";
		public string WidgetName => "PropertyWidget";

		public void Initialize()
		{
			SetSizeRequest(100, 1);
		}
	}
}