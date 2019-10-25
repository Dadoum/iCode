using System;
using System.Collections.Generic;
using System.Linq;
using Gtk;
using System.IO;

namespace iCode
{
	public class CodeWidget : Bin
    {
        public static CodeWidget codewidget;
        public Notebook tabs;

        public static void Initialize()
		{
			CodeWidget.codewidget = new CodeWidget();
		}

		public static void AddWelcomeTab(string name)
		{
			ScrolledWindow scrolledWindow = new ScrolledWindow();
			scrolledWindow.Add(new WelcomeWidget());
			CodeWidget.codewidget.tabs.Add(scrolledWindow, name, false);
			scrolledWindow.ShowAll();
			CodeWidget.codewidget.tabs.ShowAll();
		}

		public static CodeTabWidget AddCodeTab(string file)
		{
            var c = new CodeTabWidget(file);
            CodeWidget.codewidget.tabs.Add(c, System.IO.Path.GetFileName(file), true);
			c.ShowAll();
			CodeWidget.codewidget.tabs.ShowAll();
            return c;
		}

		public static void RemoveTab(string name)
		{
            try
            {
                (CodeWidget.codewidget.tabs.GetTabLabel(
                    CodeWidget.codewidget.tabs.Children.First(
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
			this.tabs = new Notebook();
			this.tabs.ShowAll();
            ScrolledWindow window = new ScrolledWindow();
            window.Add(tabs);
			base.Add(window);
            window.ShowAll();
            tabs.PageReordered += Tabs_PageReordered;
        }

        public void Tabs_PageReordered(object o = null, PageReorderedArgs args = null)
        {
            var dict = new Dictionary<string, Widget>();
            foreach (var c in tabs.Children)
            {
                try
                {
                    dict.Add(Extensions.tabs.First(x => x.Value == c).Key, c);
                }
                catch
                {

                }
            }
            Extensions.tabs = dict;
        }

    }
}
