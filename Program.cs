using System;
using System.IO;
using Gdl;
using Gtk;

namespace iCode
{
	internal class Program
	{
		public static void Main(string[] args)
        {
            System.Console.SetOut(new DatedConsole());
            Console.WriteLine("Initialized output.");

            try
            {
                Directory.CreateDirectory(ConfigPath);
                Gtk.Application.Init();
                Console.WriteLine("Initialized GTK and GDL.");
                Program.WinInstance = new MainWindow();
                Program.WinInstance.ShowAll();
                Console.WriteLine("Initialized window.");
                Gtk.Application.Run();
            }
            catch (Exception e)
            {
                new ExceptionWindow(e, null).ShowAll();
            }
        }

        public static readonly string ConfigPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "iCode/");

        public static MainWindow WinInstance;
	}
}
