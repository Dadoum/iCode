using System;
using System.IO;
using System.Reflection;
using Gtk;
using Newtonsoft.Json.Linq;
using UI = Gtk.Builder.ObjectAttribute;

namespace iCode.GUI
{
	public class SettingsWindow : Dialog
	{
		Builder _builder;

#pragma warning disable 649
		[UI] private Gtk.Button _okButton;
		[UI] private Gtk.Button _cancelButton;
		[UI] private Gtk.CheckButton _updateBootCheck;
		[UI] private Gtk.CheckButton _autoUpdateCheck;
#pragma warning restore 649

		public static SettingsWindow Create()
		{
			Builder builder = new Builder(null, "Settings", null);
			return new SettingsWindow(builder, builder.GetObject("SettingsWindow").Handle);
		}

		private SettingsWindow(Builder builder, IntPtr handle) : base(handle)
		{
			this._builder = builder;
			builder.Autoconnect(this);
			this.Icon = Gdk.Pixbuf.LoadFromResource("iCode.resources.images.icon.png");

			var settings = JObject.Parse(File.ReadAllText(Program.SettingsPath));
			this._updateBootCheck.Active = (bool) settings["updateConsent"]["checkUpdates"];
			this._autoUpdateCheck.Active = (bool) settings["updateConsent"]["autoInstall"];
			
			_cancelButton.Clicked += (sender, e) =>
			{
				this.Dispose();
			};
			
			_okButton.Clicked += (sender, e) =>
			{
				settings["updateConsent"]["checkUpdates"] = this._updateBootCheck.Active;
				settings["updateConsent"]["autoInstall"] = this._autoUpdateCheck.Active;
				File.WriteAllText(Program.SettingsPath, settings.ToString());
				this.Dispose();
			};
		}
	}
}