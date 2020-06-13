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
		public const string ApplicationDescription = "An Objective-C iOS IDE for Linux";

		public static Gdk.Pixbuf ApplicationIcon
		{
			get
			{
				var pixbuf = Pixbuf.LoadFromResource("iCode.resources.images.icon.svg");
				return pixbuf;
			}
		}
	}
}