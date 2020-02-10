using System;
using System.IO;
using System.Reflection;
using System.Text;
using Gdk;

namespace iCode
{
	public static class Identity
	{
		public const string ApplicationName = "iCode";

		public static Gdk.Pixbuf ApplicationIcon
		{
			get
			{
				/*
				PixbufLoader loader = new PixbufLoader();
				using (var res = Assembly.GetExecutingAssembly().GetManifestResourceStream("iCode.resources.images.icon.svg"))
				using (var reader = new StreamReader(res))
				{
					var svg = reader.ReadToEnd();
					loader.Write(Encoding.UTF8.GetBytes(svg));
				}
				return loader.Pixbuf;
				*/
				var pixbuf = Pixbuf.LoadFromResource("iCode.resources.images.icon.png");
				// pixbuf.
				return pixbuf;
			}
		}
	}
}