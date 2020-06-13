using System;
using System.Diagnostics;
using Gtk;
using iCode.GUI.Backend;
using iCode.GUI.Backend.Interfaces;
using iCode.GUI.GTK3.GladeUI;
using Pango;
using UI = Gtk.Builder.ObjectAttribute;

namespace iCode.GUI.GTK3
{
	public class ExceptionWindow : Gtk.Window, IGladeWidget, IExceptionWindow
	{
#pragma warning disable 649
		[UI]
		private Label _exceptionType;
		[UI]
		private Label _exceptionTitle;
		[UI]
		private Label _exceptionStacktrace;
		[UI]
		private Button _quitButton;
#pragma warning restore 649

		public string ResourceName => "ExceptionWindow";
		public string WidgetName => "ExceptionWindow";

		public ExceptionWindow() : base(WindowType.Toplevel)
		{
			
		}
		
		public void Initialize()
		{
			Icon = Identity.ApplicationIcon;

			Title = "Exception occured";
			FontDescription fontDescription = PangoContext.FontDescription;
			fontDescription.Size = (int) (fontDescription.Size * 1.5);
			_exceptionTitle.UseMarkup = true;
			var attrList = new AttrList();
			Pango.Attribute attr = new AttrFontDesc(fontDescription);          
			attrList.Insert(attr);
			_exceptionTitle.Attributes = attrList;
			DeleteEvent += delegate {
				Application.Quit();
			};
			_quitButton.Clicked += (sender, e) => {
				Application.Quit();
			};
		}
		
		public void ShowException(Exception ex, IWidget parent)
		{
			Parent = parent as Widget ?? throw new UIHelper.MixedBackendsException("");
			_exceptionType.Text = ex.GetType().FullName;
			_exceptionTitle.Text = $"<b> {ex.Message} </b>";
			_exceptionStacktrace.Text = ex.StackTrace;
			Console.WriteLine("from {0}: {1}", new StackTrace().GetFrame(1)?.GetMethod()?.ReflectedType?.Name, ex);
			ShowAll();
		}
	}
}