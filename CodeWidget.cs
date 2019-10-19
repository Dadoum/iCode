using System;
using Gtk;

namespace iCode
{
	public class CodeWidget : Bin
    {
        public static CodeWidget codewidget;
        private Notebook tabs;

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

		public static void AddCodeTab(string name)
		{
			ScrolledWindow scrolledWindow = new ScrolledWindow();
			scrolledWindow.Add(new CodeTabWidget());
			CodeWidget.codewidget.tabs.Add(scrolledWindow, name, true);
			scrolledWindow.ShowAll();
			CodeWidget.codewidget.tabs.ShowAll();
		}

		public static void RemoveTab(string name)
		{
			CodeWidget.codewidget.tabs.Remove(Extensions.tabs[name]);
			Extensions.tabs.Remove(name);
		}

		private CodeWidget()
		{
			base.SetSizeRequest(300, 1);
			this.tabs = new Notebook();
			this.tabs.ShowAll();
			base.Add(this.tabs);
		}
	}
}
