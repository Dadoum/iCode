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
        private global::Gtk.Label ExceptionType;
        [UI]
        private global::Gtk.Label ExceptionTitle;
        [UI]
        private global::Gtk.Label ExceptionStacktrace;
        [UI]
        private global::Gtk.Button quit_button;
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
            this.Parent = parent;
            this.Title = "Exception occured";
            ExceptionType.Text = ex.GetType().FullName;
            ExceptionTitle.Text = string.Format("<b> {0} </b>", ex.Message);
            FontDescription fontDescription = base.PangoContext.FontDescription;
            fontDescription.Size = (int) (fontDescription.Size * 1.5);
            this.ExceptionTitle.UseMarkup = true;
            var attrList = new Pango.AttrList();
            Pango.Attribute attr = new Pango.AttrFontDesc(fontDescription);          
            attrList.Insert(attr);
            ExceptionTitle.Attributes = attrList;
            ExceptionStacktrace.Text = ex.StackTrace;
            DeleteEvent += delegate {
                Gtk.Application.Quit();
            };
            quit_button.Activated += (sender, e) => {
                this.Dispose();
            };
            Console.WriteLine("from {0}: {1}", new StackTrace().GetFrame(1).GetMethod().ReflectedType.Name, ex);
        }
    }
}
