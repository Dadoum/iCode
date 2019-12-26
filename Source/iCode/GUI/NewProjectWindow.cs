using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Gdk;
using Gtk;
using iCode.Utils;
using Console = iCode.Utils.Console;
using UI = Gtk.Builder.ObjectAttribute;

namespace iCode.GUI
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

        public string SelectedTemplatePath;

        public static NewProjectWindow Create()
        {
            Builder builder = new Builder(null, "NewProject", null);
            return new NewProjectWindow(builder, builder.GetObject("NewProjectWindow").Handle);
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

                if (iconView.SelectedItems.Length == 0)
                    return;

                ((ListStore)iconView.Model).GetIter(out TreeIter temp, iconView.SelectedItems.FirstOrDefault());

                Console.WriteLine(iconView.PathIsSelected(((ListStore)iconView.Model).GetPath(temp)).ToString());
                SelectedTemplatePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/templates/" + ((string) ((ListStore)iconView.Model).GetValue(temp, 1)) + ".zip");
                Console.WriteLine(SelectedTemplatePath);
                Respond(ResponseType.Ok);
                this.Dispose();
            };

            button_cancel.Clicked += (sender, e) =>
            {
                Respond(ResponseType.Cancel);
                this.Dispose();
            };

            this.Title = "New project wizard";
            this.iconView.SelectionMode = SelectionMode.Single;

            iconView.PixbufColumn = 0;
            iconView.TextColumn = 1;
            
            var store = new ListStore(typeof(Pixbuf), typeof(string));

            iconView.SelectionMode = SelectionMode.Single;

            foreach (var file in from f in Directory.GetFiles(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/templates/")) where f.EndsWith(".zip", StringComparison.CurrentCultureIgnoreCase) select f)
            {
                store.AppendValues(IconLoader.LoadIcon(this, "gtk-file", IconSize.Dialog), System.IO.Path.GetFileNameWithoutExtension(file));
            }

            store.SetSortColumnId(2, SortType.Ascending);

            iconView.Model = store;
            iconView.ShowAll();
        }
    }
}
