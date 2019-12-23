using System;
using System.Reflection;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;

namespace iCode
{
    public class AboutWindow : Window
    {
        Builder builder;

#pragma warning disable 649
        [UI] private Gtk.Button ok_button;
        [UI] private Gtk.Label label;
#pragma warning restore 649

        public static AboutWindow Create()
        {
            Builder builder = new Builder(null, "About", null);
            return new AboutWindow(builder, builder.GetObject("AboutWindow").Handle);
        }

        private AboutWindow(Builder builder, IntPtr handle) : base(handle)
        {
            this.builder = builder;
            builder.Autoconnect(this);

            ok_button.Clicked += (sender, e) =>
            {
                this.Destroy();
            };

            this.Title = ("About " + Names.ApplicationName);
            this.label.LabelProp = this.label.LabelProp.Replace("VERSIONNUMBER", Assembly.GetEntryAssembly().GetName().Version.ToString());
        }
    }
}
