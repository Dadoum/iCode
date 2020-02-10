using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Gtk;
using Newtonsoft.Json.Linq;
using UI = Gtk.Builder.ObjectAttribute;

namespace iCode.GUI
{
	public class SettingsWindow : Dialog
	{
		Builder _builder;

		private readonly Dictionary<string, string> _translations = new Dictionary<string, string>()
		{
			{"check_updates","Check for updates on startup"},
			{"auto_install","Install automatically updates"}
		};

		private Dictionary<string, Widget> _settings = new Dictionary<string, Widget>();
		
#pragma warning disable 649
		[UI] private Gtk.Box _mainBox;
		[UI] private Gtk.Button _okButton;
		[UI] private Gtk.Button _cancelButton;
#pragma warning restore 649

		public static SettingsWindow Create()
		{
			Builder builder = new Builder(null, "Settings", null);
			return new SettingsWindow(builder, builder.GetObject("SettingsWindow").Handle);
		}

		private SettingsWindow(Builder builder, IntPtr handle) : base(handle)
		{
			this._builder = builder;
			builder.Autoconnect(this);
			this.Icon = Identity.ApplicationIcon;

			var notebook = new Notebook();
			notebook.Expand = true;
			notebook.TabPos = PositionType.Left;
			var settings = Program.Settings.GetSettings();

			foreach (var setting in settings)
			{
				var path = setting["path"];
				var name = setting["name"];
				var value = setting["value"];
				
				var tab = path.ToString().Split('/')[0];
				var category = path.ToString().Split('/')[1];

				try
				{
					// Try to get the children with the name
					notebook.Children.First(c => c.Name == tab);
				}
				catch
				{ 
					// Add it if it is not existing
					var nbox = new Box(Orientation.Vertical, 1);
					nbox.Name = tab;
					notebook.AppendPage(nbox, new Label
					{
						Text = tab
					});
				}
				
				var box = (Box) notebook.Children.First(c => c.Name == tab);
				Box cat;
				
				try
				{
					// Try to get the children with the name
					cat = (Box) box.Children.First(c => c.Name == category);
				}
				catch
				{ 
					// Add it if it is not existing
					var nbox = new Box(Orientation.Vertical, 1);
					nbox.Name = category;
					box.Add(new Label
					{
						Markup = $"<b>{category}</b>",
						UseMarkup = true
					});
					box.Add(nbox);
					cat = nbox;
				}

				// Depending to the value, we use different widgets
				switch (value.Type)
				{
					case JTokenType.Boolean:
						var checkbox = new Gtk.CheckButton();
						if (_translations.ContainsKey(name.ToString()))
							checkbox.Label = _translations[name.ToString()];
						else
							checkbox.Label = name.ToString();
						checkbox.Active = (bool) value;
						cat.Add(checkbox);
						_settings.Add(name.ToString(), checkbox);
						break;
				}
			}
			
			_mainBox.Add(notebook);

			_cancelButton.Clicked += (sender, e) =>
			{
				this.Dispose();
			};
			
			_okButton.Clicked += (sender, e) =>
			{
				foreach (var setting in _settings)
				{
					switch (setting.Value)
					{
						case CheckButton btn:
							Program.Settings.SetSetting(setting.Key, btn.Active);
							break;
					}
				}
				this.Dispose();
			};
			
			this.ShowAll();
		}
	}
}