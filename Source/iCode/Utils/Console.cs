using System;
using System.Diagnostics;

namespace iCode.Utils
{
	public static class Console
	{
		public static void WriteLine(string s)
		{
			string name = new StackTrace().GetFrame(1).GetMethod().ReflectedType.Name;
			string ln = new StackTrace().GetFrame(1).GetMethod().Name + "()";
			System.Console.WriteLine("[" + name + ":" + ln + "]: " + s);
		}

		public static void WriteLine(object o)
		{
			string name = new StackTrace().GetFrame(1).GetMethod().ReflectedType.Name;
			string ln = new StackTrace().GetFrame(1).GetMethod().Name + "()";
			System.Console.WriteLine("[" + name + ":" + ln + "]: " + o);
		}

		public static void WriteLine(string s, params object[] format)
		{
			string name = new StackTrace().GetFrame(1).GetMethod().ReflectedType.Name;
			string ln = new StackTrace().GetFrame(1).GetMethod().Name + "()";
			System.Console.WriteLine("[" + name + ":" + ln + "]: " + string.Format(s, format));
		}
	}
}