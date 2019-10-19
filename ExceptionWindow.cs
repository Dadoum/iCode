using System;
using System.Diagnostics;
using Pango;

namespace iCode
{
    public partial class ExceptionWindow : Gtk.Window
    {
        public ExceptionWindow(Exception ex, Gtk.Widget parent) :
                base(Gtk.WindowType.Toplevel)
        {
            this.Parent = parent;
            this.Build();
            this.Title = "Exception occured";
            ExceptionType.Text = ex.GetType().FullName;
            ExceptionTitle.Text = string.Format("<b> {0} </b>", ex.Message);
            FontDescription fontDescription = base.PangoContext.FontDescription;
            fontDescription.Size = (int) (fontDescription.Size * 1.5);
            this.ExceptionTitle.UseMarkup = true;
            this.ExceptionTitle.OverrideFont(fontDescription);
            ExceptionStacktrace.Text = ex.StackTrace;
            DeleteEvent += delegate {
                Gtk.Application.Quit();
            };
            Console.WriteLine("from {0}: {1}", new StackTrace().GetFrame(1).GetMethod().ReflectedType.Name, ex);
        }
    }
}
