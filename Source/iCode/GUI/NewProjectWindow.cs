using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Gdk;
using Gtk;
using iCode.Utils;
using UI = Gtk.Builder.ObjectAttribute;

namespace iCode.GUI
{
	public class NewProjectWindow : Dialog
	{
		Builder _builder;

#pragma warning disable 649
		[UI] private Gtk.IconView _iconView;

		[UI] private Gtk.Button _buttonOk;
		[UI] private Gtk.Button _buttonCancel;

		[UI] private Gtk.Entry _inputName;
		[UI] private Gtk.Entry _inputId;
		[UI] private Gtk.Entry _inputPrefix;
		[UI] private Gtk.Entry _inputPath;
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
			this._builder = builder;

			builder.Autoconnect(this);

			_buttonOk.Clicked += (sender, e) =>
			{
				ProjectName = _inputName.Text;

				Id = _inputId.Text;

				Prefix = _inputPrefix.Text;

				Path = _inputPath.Text;

				if (_iconView.SelectedItems.Length == 0)
					return;

				((ListStore)_iconView.Model).GetIter(out TreeIter temp, _iconView.SelectedItems.FirstOrDefault());

				Console.WriteLine(_iconView.PathIsSelected(((ListStore)_iconView.Model).GetPath(temp)).ToString());
				SelectedTemplatePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/templates/" + ((string) ((ListStore)_iconView.Model).GetValue(temp, 1)) + ".zip");
				Console.WriteLine(SelectedTemplatePath);
				Respond(ResponseType.Ok);
				this.Dispose();
			};

			_buttonCancel.Clicked += (sender, e) =>
			{
				Respond(ResponseType.Cancel);
				this.Dispose();
			};

			this.Title = "New project wizard";
			this._iconView.SelectionMode = SelectionMode.Single;

			_iconView.PixbufColumn = 0;
			_iconView.TextColumn = 1;
            
			var store = new ListStore(typeof(Pixbuf), typeof(string));

			_iconView.SelectionMode = SelectionMode.Single;

			foreach (var file in from f in Directory.GetFiles(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "tools/templates/")) where f.EndsWith(".zip", StringComparison.CurrentCultureIgnoreCase) select f)
			{
				store.AppendValues(IconLoader.LoadIcon(this, "gtk-file", IconSize.Dialog), System.IO.Path.GetFileNameWithoutExtension(file));
			}

			store.SetSortColumnId(2, SortType.Ascending);

			_iconView.Model = store;
			_iconView.ShowAll();
		}
	}
}