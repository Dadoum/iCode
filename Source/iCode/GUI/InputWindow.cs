using System;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;

namespace iCode.GUI
{
	public class InputWindow : Dialog
	{
		Builder _builder;

#pragma warning disable 649
		[UI] private Gtk.Button _okButton;
		[UI] private Gtk.Button _cancelButton;
		[UI] private Gtk.Entry _entry;
#pragma warning restore 649

		public string Text;

		public static InputWindow Create()
		{
			Builder builder = new Builder(null, "Input", null);
			return new InputWindow(builder, builder.GetObject("InputWindow").Handle);
		}

		private InputWindow(Builder builder, IntPtr handle) : base(handle)
		{
			this._builder = builder;
			builder.Autoconnect(this);

			_okButton.Clicked += (sender, e) =>
			{
				Text = _entry.Text;
				this.Dispose();
			};
			
			_cancelButton.Clicked += (sender, e) =>
			{
				Text = "";
				this.Dispose();
			};
		}
	}
}