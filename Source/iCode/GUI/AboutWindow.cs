using System;
using System.Reflection;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;

namespace iCode.GUI
{
	public class AboutWindow : Window
	{
		Builder _builder;

#pragma warning disable 649
		[UI] private Gtk.Button _okButton;
		[UI] private Gtk.Label _label;
#pragma warning restore 649

		public static AboutWindow Create()
		{
			Builder builder = new Builder(null, "About", null);
			return new AboutWindow(builder, builder.GetObject("AboutWindow").Handle);
		}

		private AboutWindow(Builder builder, IntPtr handle) : base(handle)
		{
			this._builder = builder;
			builder.Autoconnect(this);

			_okButton.Clicked += (sender, e) =>
			{
				this.Dispose();
			};

			this.Title = ("About " + Names.ApplicationName);
			this._label.LabelProp = this._label.LabelProp.Replace("VERSIONNUMBER", Assembly.GetEntryAssembly().GetName().Version.ToString());
		}
	}
}