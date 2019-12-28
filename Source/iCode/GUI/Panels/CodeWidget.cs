using System;
using System.Collections.Generic;
using System.Linq;
using Gtk;
using System.IO;
using iCode.GUI.Tabs;
using iCode.Utils;
using iCode.Projects;

namespace iCode.GUI.Panels
{
	public class CodeWidget : Bin
	{
		public static CodeWidget Codewidget;
		public Notebook Tabs;

		public static void Initialize()
		{
			CodeWidget.Codewidget = new CodeWidget();
		}

		public static void AddWelcomeTab(string name)
		{
			ScrolledWindow scrolledWindow = new ScrolledWindow();
			scrolledWindow.Add(WelcomeWidget.Create());
			CodeWidget.Codewidget.Tabs.Add(scrolledWindow, name, false);
			scrolledWindow.ShowAll();
			CodeWidget.Codewidget.Tabs.ShowAll();
		}

		public static CodeTabWidget AddCodeTab(string file)
		{
			// Console.WriteLine(file);
			// Console.WriteLine(ProjectManager.Project.Classes.First().Filename);
			var c = new CodeTabWidget(ProjectManager.Project.Classes.First(x => System.IO.Path.Combine(ProjectManager.Project.Path, x.Filename) == file));
			CodeWidget.Codewidget.Tabs.Add(c, System.IO.Path.GetFileName(file), true);
			c.ShowAll();
			CodeWidget.Codewidget.Tabs.ShowAll();
			return c;
		}

		public static void RemoveTab(string name)
		{
			try
			{
				(CodeWidget.Codewidget.Tabs.GetTabLabel(
					CodeWidget.Codewidget.Tabs.Children.First(
						x => (x as ScrolledWindow).Name == name)
				) as NotebookTabLabel).OnCloseClicked();
			}
			catch (Exception e)
			{
				Console.WriteLine("SEVERE -> {0}", e);
			}
		}

		private CodeWidget()
		{
			base.SetSizeRequest(300, 1);
			this.Tabs = new Notebook();
			this.Tabs.ShowAll();
			ScrolledWindow window = new ScrolledWindow();
			window.Add(Tabs);
			base.Add(window);
			window.ShowAll();
			Tabs.PageReordered += Tabs_PageReordered;
		}

		public void Tabs_PageReordered(object o = null, PageReorderedArgs args = null)
		{
			var dict = new Dictionary<string, Widget>();
			foreach (var c in Tabs.Children)
			{
				try
				{
					dict.Add(Extensions.Tabs.First(x => x.Value == c).Key, c);
				}
				catch
				{

				}
			}
			Extensions.Tabs = dict;
		}

	}
}