using System.Reflection;
using Gtk;
using iCode.GUI.GTK3.GladeUI;
using UI = Gtk.Builder.ObjectAttribute;

namespace iCode.GUI.GTK3
{
	
	public class AboutWindow : Dialog, IGladeWidget
	{
		public string ResourceName => "About";
		public string WidgetName => "AboutWindow";

#pragma warning disable 649
		[UI] private Gtk.Button _okButton;
		[UI] private Gtk.Label _label;
#pragma warning restore 649

		public void Initialize()
		{
			this.Icon = Identity.ApplicationIcon;

			this.WindowPosition = WindowPosition.CenterOnParent;
			_okButton.Clicked += (sender, e) =>
			{
				this.Dispose();
			};

			this.Title = ("About " + Identity.ApplicationName);
			this._label.LabelProp = this._label.LabelProp.Replace("VERSIONNUMBER", Assembly.GetEntryAssembly().GetName().Version.ToString());
		}
	}
}