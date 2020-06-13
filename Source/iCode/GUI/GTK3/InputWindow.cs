using Gtk;
using iCode.GUI.Backend.Interfaces;
using iCode.GUI.GTK3.GladeUI;
using UI = Gtk.Builder.ObjectAttribute;

namespace iCode.GUI.GTK3
{
	public class InputWindow : Dialog, IGladeWidget, IDialog
	{
#pragma warning disable 649
		[UI] private Button _okButton;
		[UI] private Button _cancelButton;
		[UI] private Entry _entry;
#pragma warning restore 649

		public string Text;
		
		public string ResourceName => "Input";
		public string WidgetName => "InputWindow";
		
		/*public static InputWindow Create()
		{
			Builder builder = new Builder(null, "Input", null);
			return new InputWindow(builder, builder.GetObject("InputWindow").Handle);
		}*/

		public void Initialize()
		{		
			this.Icon = Identity.ApplicationIcon;
			
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