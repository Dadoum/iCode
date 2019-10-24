using System;
using Gdk;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;

namespace iCode
{
    public class NewProjectWindow : Dialog
    {
        Builder builder;

#pragma warning disable 649
        [UI] private Gtk.IconView iconView;

        [UI] private Gtk.Button button_ok;
        [UI] private Gtk.Button button_cancel;

        [UI] private Gtk.Entry input_name;
        [UI] private Gtk.Entry input_id;
        [UI] private Gtk.Entry input_prefix;
        [UI] private Gtk.Entry input_path;
#pragma warning restore 649

        public string ProjectName;

        public string Id;

        public string Prefix;

        public new string Path;

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
                Path = input_path.Text;
                Respond(ResponseType.Ok);
                this.Destroy();
            };

            button_cancel.Clicked += (sender, e) =>
            {
                Respond(ResponseType.Cancel);
                this.Destroy();
            };

            this.Title = "New project wizard";
            this.iconView.SelectionMode = SelectionMode.Single;

            iconView.PixbufColumn = 0;
            iconView.TextColumn = 1;
            
            var store = new ListStore(typeof(Pixbuf), typeof(string));
            store.AppendValues(Stetic.IconLoader.LoadIcon(this, "gtk-file", IconSize.Dialog), "Objective-C Project");
            store.SetSortColumnId(2, SortType.Ascending);

            iconView.Model = store;
            iconView.ShowAll();
        }
    }
}
