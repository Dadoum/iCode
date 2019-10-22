using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Gdk;
using Gtk;
using iCode;
using Pango;

public static class Extensions
{
    public static Dictionary<string, Widget> tabs = new Dictionary<string, Widget>();

    public static void ModifyFont(this Widget widget, string Family, int Size, Pango.Style Sty = Pango.Style.Normal)
	{
		FontDescription font_desc = new FontDescription
		{
			Family = Family,
			Size = Convert.ToInt32((double)Size * Pango.Scale.PangoScale),
			Style = Sty
		};
		widget.OverrideFont(font_desc);
	}

	public static void Add(this Notebook notebook, Widget widget, string str, bool isVolatile)
	{
        try
        {
            widget.Name = str;
            Extensions.tabs.Add(str, widget);

            ScrolledWindow scrolledWindow = new ScrolledWindow();
            scrolledWindow.Add(widget);
            scrolledWindow.Name = str;
            if (widget is CodeTabWidget)
            {
                notebook.AppendPage(scrolledWindow, ((widget as CodeTabWidget).GetLabel()));
                (widget as CodeTabWidget).GetLabel().ShowAll();
                (widget as CodeTabWidget).GetLabel().CloseClicked += delegate (object obj, EventArgs eventArgs)
                {
                    notebook.RemovePage(notebook.PageNum(notebook.Children.First(x => x == scrolledWindow)));
                    Extensions.tabs.Remove(str);
                };
            }
            else
            {
                NotebookTabLabel notebookTabLabel = new NotebookTabLabel(str, widget);
                notebookTabLabel.CloseClicked += delegate (object obj, EventArgs eventArgs)
                {
                    notebook.RemovePage(notebook.PageNum(notebook.Children.First(x => x == scrolledWindow)));
                    Extensions.tabs.Remove(str);
                };
                notebook.AppendPage(scrolledWindow, notebookTabLabel);
                notebookTabLabel.ShowAll();
            }

            notebook.SetTabDetachable(scrolledWindow, isVolatile);
            notebook.SetTabReorderable(scrolledWindow, isVolatile);

            widget.ShowAll();
        }
        catch (ArgumentException)
        {
            notebook.Page = notebook.PageNum(tabs.First(x => x.Key == str).Value);
        }
        catch (Exception e)
        {
            new ExceptionWindow(e, notebook).ShowAll();
        }
    }

    public static byte[] ToByteArray(this Stream input)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            input.CopyTo(ms);
            return ms.ToArray();
        }
    }

    public static Stream ToStream(this byte[] input)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            ms.Write(input, 0, input.Length);
            return ms;
        }
    }

    public static Pixbuf GetIconFromFile(string path)
    {
        string[] mimetypes = LaunchProcess("gio", string.Format("info -a standard::icon '{0}'", path)).Split('\n')[2].Split(new string[] { ": " },  StringSplitOptions.None)[1].Split(new string[] { ", " }, StringSplitOptions.None);
        mimetypes[0] = mimetypes[0].TrimStart(' ');

        string theme = LaunchProcess("gsettings", "get org.gnome.desktop.interface icon-theme");
        theme = theme.TrimEnd('\n').Trim('\'');

        foreach (string s in mimetypes)
        {
            string file = string.Format("/usr/share/icons/{0}/mimetypes/16/{1}.svg", theme, s);
            if (File.Exists(file))
            {
                var pixbufld = new PixbufLoader();
                pixbufld.Write(Encoding.UTF8.GetBytes(File.ReadAllText(file)));
                pixbufld.Close();
                return pixbufld.Pixbuf;
            }
        }

        return IconLoader.LoadIcon(Program.WinInstance, "gtk-file", IconSize.Menu);
    }

    private static string LaunchProcess(string process, string arguments)
    {
        var proc = new Process();
        proc.StartInfo.Arguments = arguments;
        proc.StartInfo.FileName = process;
        proc.StartInfo.UseShellExecute = false;
        proc.StartInfo.RedirectStandardOutput = true;

        proc.Start();
        proc.WaitForExit();

        return proc.StandardOutput.ReadToEnd();
    }
}
