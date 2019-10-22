using System;
using Gdk;
using Gtk;

namespace iCode
{
    public partial class NewProjectDialog : Gtk.Dialog
    {
        public NewProjectDialog()
        {
            this.Build();
            TreeStore model = new TreeStore(new Type[]
            {
                typeof(Pixbuf),
                typeof(string)
            });
            this.iconview1.Model = model;
            model.AppendValues(new object[]
            {
                IconLoader.LoadIcon(Program.WinInstance.ProjectExplorer, "gtk-directory", IconSize.Menu),
                ""
            });
        }

        private void OnRowActivated(object o, ItemActivatedArgs args)
        {
            throw new NotImplementedException();
        }
    }
}
