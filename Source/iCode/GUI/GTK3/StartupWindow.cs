using Gtk;
using iCode.GUI.Backend.Interfaces;
using iCode.GUI.GTK3.GladeUI;
using UI = Gtk.Builder.ObjectAttribute;

namespace iCode.GUI.GTK3
{
	public class StartupWindow : Dialog, IGladeWidget, IStartupWindow
	{
#pragma warning disable 649
		[UI] private Gtk.Button _okButton;
		[UI] private Gtk.CheckButton _consentUpdateCheckbox;
#pragma warning restore 649

		public bool Accepted { get; private set; }
		
		public string ResourceName => "Startup";
		public string WidgetName => "StartupWindow";

		public void Initialize()
		{
			this.Icon = Identity.ApplicationIcon;
			_okButton.Clicked += (sender, e) =>
			{
				Accepted = _consentUpdateCheckbox.Active;
				Respond(ResponseType.Ok);
				this.Dispose();
			};
		}
	}
}