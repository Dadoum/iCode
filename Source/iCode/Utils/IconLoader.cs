using System;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using Gdk;
using Gtk;

namespace iCode.Utils
{
	internal class IconLoader
	{
		public static Pixbuf LoadIcon(Widget widget, string name, IconSize size)
		{
			
			Pixbuf pixbuf = Gtk.IconTheme.Default.LoadIcon(name, (int) size, IconLookupFlags.UseBuiltin);
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