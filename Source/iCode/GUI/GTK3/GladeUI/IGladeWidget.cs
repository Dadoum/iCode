using iCode.GUI.Backend.Interfaces;

namespace iCode.GUI.GTK3.GladeUI
{
	public interface IGladeWidget : IWidget
	{
		public string ResourceName { get; }
		public string WidgetName { get; }
	}
}