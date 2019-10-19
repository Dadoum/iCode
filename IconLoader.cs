using System;
using Gdk;
using Gtk;

namespace iCode
{
	// Token: 0x02000010 RID: 16
	internal class IconLoader
	{
		// Token: 0x06000030 RID: 48 RVA: 0x00003F24 File Offset: 0x00002124
		public static Pixbuf LoadIcon(Widget widget, string name, IconSize size)
		{
			Pixbuf pixbuf = widget.RenderIcon(name, size, null);
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
				try
				{
					result = IconTheme.Default.LoadIcon(name, size2, (IconLookupFlags)0);
				}
				catch (Exception ex)
				{
					throw ex;
				}
			}
			return result;
		}
	}
}
