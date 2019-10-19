using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Gtk;
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
		NotebookTabLabel notebookTabLabel = new NotebookTabLabel(str);
		notebook.AppendPage(widget, notebookTabLabel);
		notebookTabLabel.CloseClicked += delegate(object obj, EventArgs eventArgs)
		{
			notebook.RemovePage(notebook.PageNum(widget));
		};
		notebook.SetTabDetachable(widget, isVolatile);
		notebook.SetTabReorderable(widget, isVolatile);
		Extensions.tabs.Add(str, widget);
		widget.ShowAll();
		notebookTabLabel.ShowAll();
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
}
