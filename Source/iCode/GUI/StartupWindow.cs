using System;
using Gdk;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;

namespace iCode.GUI
{
	public class StartupWindow : Dialog
	{
		Builder _builder;

#pragma warning disable 649
		[UI] private Gtk.Button _okButton;
		[UI] private Gtk.CheckButton _consentUpdateCheckbox;
#pragma warning restore 649

		public bool Accepted = false;

		public string Text;

		public static StartupWindow Create()
		{
			Builder builder = new Builder(null, "Startup", null);
			return new StartupWindow(builder, builder.GetObject("StartupWindow").Handle);
		}

		private StartupWindow(Builder builder, IntPtr handle) : base(handle)
		{
			this._builder = builder;
			builder.Autoconnect(this);
			this.Icon = Pixbuf.LoadFromResource("iCode.resources.images.icon.png");
			_okButton.Clicked += (sender, e) =>
			{
				Accepted = _consentUpdateCheckbox.Active;
				Respond(ResponseType.Ok);
				this.Dispose();
			};
		}
	}
}