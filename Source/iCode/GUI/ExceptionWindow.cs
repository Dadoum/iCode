using System;
using System.Diagnostics;
using Gtk;
using Pango;
using UI = Gtk.Builder.ObjectAttribute;

namespace iCode.GUI
{
	public class ExceptionWindow : Gtk.Window
	{
#pragma warning disable 649
		[UI]
		private global::Gtk.Label _exceptionType;
		[UI]
		private global::Gtk.Label _exceptionTitle;
		[UI]
		private global::Gtk.Label _exceptionStacktrace;
		[UI]
		private global::Gtk.Button _quitButton;
#pragma warning restore 649

		public static ExceptionWindow Create(Exception ex, Gtk.Widget parent)
		{
			Builder builder = new Builder(null, "ExceptionWindow", null);
			return new ExceptionWindow(ex, parent, builder, builder.GetObject("ExceptionWindow").Handle);
		}

		ExceptionWindow(Exception ex, Gtk.Widget parent, Builder builder, IntPtr handle) :
			base(handle)
		{
			builder.Autoconnect(this);			
			this.Icon = Gdk.Pixbuf.LoadFromResource("iCode.resources.images.icon.png");

			this.Parent = parent;
			this.Title = "Exception occured";
			_exceptionType.Text = ex.GetType().FullName;
			_exceptionTitle.Text = string.Format("<b> {0} </b>", ex.Message);
			FontDescription fontDescription = base.PangoContext.FontDescription;
			fontDescription.Size = (int) (fontDescription.Size * 1.5);
			this._exceptionTitle.UseMarkup = true;
			var attrList = new Pango.AttrList();
			Pango.Attribute attr = new Pango.AttrFontDesc(fontDescription);          
			attrList.Insert(attr);
			_exceptionTitle.Attributes = attrList;
			_exceptionStacktrace.Text = ex.StackTrace;
			DeleteEvent += delegate {
				Gtk.Application.Quit();
			};
			_quitButton.Activated += (sender, e) => {
				Gtk.Application.Quit();
			};
			Console.WriteLine("from {0}: {1}", new StackTrace().GetFrame(1).GetMethod().ReflectedType.Name, ex);
		}
	}
}