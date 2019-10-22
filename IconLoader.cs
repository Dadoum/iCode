using System;
using Gdk;
using Gtk;

namespace iCode
{
	internal class IconLoader
	{
		public static Pixbuf LoadIcon(Widget widget, string name, IconSize size)
		{
			Pixbuf pixbuf = widget.RenderIconPixbuf(name, size);
			bool flag = pixbuf != null;
			Pixbuf result;
			if (flag)
			{
				result = pixbuf;
			}
			else
			{
				int size2;
				int num;
				Icon.SizeLookup(size, out size2, out num);
                result = IconTheme.Default.LoadIcon(name, size2, (IconLookupFlags)0);
			}
			return result;
		}
	}
}
