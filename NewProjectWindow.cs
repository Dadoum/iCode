using System;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;

namespace iCode
{
    public class NewProjectWindow : Dialog
    {
        Builder builder;

        [UI] private Gtk.Button button_ok;
        [UI] private Gtk.Button button_cancel;

        [UI] private Gtk.Entry input_name;
        [UI] private Gtk.Entry input_id;
        [UI] private Gtk.Entry input_prefix;

        public string ProjectName;

        public string Id;

        public string Prefix;

        public static NewProjectWindow Create()
        {
            Builder builder = new Builder(null, "NewProject", null);
            return new NewProjectWindow(builder, builder.GetObject("dialog").Handle);
        }

        protected NewProjectWindow(Builder builder, IntPtr handle) : base(handle)
        {
            this.builder = builder;

            builder.Autoconnect(this);

            button_ok.Clicked += (sender, e) =>
            {
                ProjectName = input_name.Text;
                Id = input_id.Text;
                Prefix = input_prefix.Text;
                Respond(ResponseType.Ok);
                this.Destroy();
            };

            button_cancel.Clicked += (sender, e) =>
            {
                Respond(ResponseType.Cancel);
                this.Destroy();
            };
        }
    }
}
