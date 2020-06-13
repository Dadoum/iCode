using System;
using System.Collections.Generic;
using System.Linq;
using Gtk;
using iCode.GUI.Backend.Interfaces;
using iCode.GUI.GTK3.GladeUI;
using Newtonsoft.Json.Linq;
using UI = Gtk.Builder.ObjectAttribute;

namespace iCode.GUI.GTK3
{
	public class SettingsWindow : Dialog, IGladeWidget, IDialog
	{
		private readonly Dictionary<string, string> _translations = new Dictionary<string, string>()
		{
			{"check_updates","Check for updates on startup"},
			{"auto_install","Install automatically updates"},
			{"tab_width","Tab width"},
		};

		private Dictionary<string, Widget> _settings = new Dictionary<string, Widget>();
		
#pragma warning disable 649
		[UI] private Box _mainBox;
		[UI] private Button _okButton;
		[UI] private Button _cancelButton;
#pragma warning restore 649

		public string ResourceName => "Settings";
		public string WidgetName => "SettingsWindow";

		public void Initialize()
		{
			this.Icon = Identity.ApplicationIcon;

			var notebook = new Notebook();
			notebook.Expand = true;
			notebook.TabPos = PositionType.Left;
			var settings = Program.Settings.GetSettings();
			var settingShow = settings.ToList();
			settingShow.Sort((token1, token2) =>
				string.Compare(token2["name"]!.ToString(), token1["name"]!.ToString(), StringComparison.CurrentCulture) ==
				0
					? string.Compare(token2["path"]!.ToString(), token1["path"]!.ToString(),
						StringComparison.CurrentCulture)
					: string.Compare(token2["name"]!.ToString(), token1["name"]!.ToString(),
						StringComparison.CurrentCulture));

			foreach (var setting in settingShow)
			{
				var path = setting["path"];
				var name = setting["name"];
				var value = setting["value"];

				var tab = path!.ToString().Split('/')[0];
				var category = path.ToString().Split('/')[1];

				try
				{
					// Try to get the children with the name
					_ = notebook.Children.First(c => c.Name == tab);
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
				switch (value!.Type)
				{
					case JTokenType.Boolean:
					{
						var checkbox = new CheckButton();
						if (_translations.ContainsKey(name!.ToString()))
							checkbox.Label = _translations[name.ToString()];
						else
							checkbox.Label = name.ToString();
						checkbox.Active = (bool) value;
						cat.Add(checkbox);
						_settings.Add(name.ToString(), checkbox);
					}
						break;
					case JTokenType.String:
					{
						var hbox = new Box(Orientation.Horizontal, 0);
						var label = new Label();
						var entry = new Entry();
						if (_translations.ContainsKey(name!.ToString()))
							label.Text = $" {_translations[name.ToString()]}: ";
						else
							label.Text = $" {name}: ";
						entry.Text = (string) value;
						hbox.Add(label);
						hbox.Add(entry);
						cat.Add(hbox);
						_settings.Add(name.ToString(), hbox);
					}
						break;
					case JTokenType.Integer:
					{
						var hbox = new Box(Orientation.Horizontal, 0);
						var label = new Label();
						var entry = new SpinButton(0, int.MaxValue, 1);
						entry.Value = (uint) value;
						if (_translations.ContainsKey(name!.ToString()))
							label.Text = $" {_translations[name.ToString()]}: ";
						else
							label.Text = $" {name}: ";
						entry.Text = (string) value;
						hbox.Add(label);
						hbox.Add(entry);
						cat.Add(hbox);
						_settings.Add(name.ToString(), hbox);
					}
						break;
				}
			}

			_mainBox.Add(notebook);

			_cancelButton.Clicked += (sender, e) => { this.Dispose(); };

			_okButton.Clicked += (sender, e) =>
			{
				foreach (var setting in _settings)
				{
					switch (setting.Value)
					{
						case CheckButton btn:
							Program.Settings.SetSetting(setting.Key, btn.Active);
							break;
						case Box box:
							switch (box.Children[1])
							{
								case SpinButton btn:
									Program.Settings.SetSetting(setting.Key, btn.ValueAsInt);
									break;
								case Entry entry:
									Program.Settings.SetSetting(setting.Key, entry.Text);
									break;
							}

							break;
					}
				}

				this.Dispose();
			};

			this.ShowAll();
		}
	}
}