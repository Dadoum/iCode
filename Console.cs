using System;
using System.Diagnostics;

public static class Console
{
	public static void WriteLine(string s)
	{
		string name = new StackTrace().GetFrame(1).GetMethod().ReflectedType.Name;
		System.Console.WriteLine("[" + name + "]: " + s);
	}

	public static void WriteLine(string s, params object[] format)
	{
		string name = new StackTrace().GetFrame(1).GetMethod().ReflectedType.Name;
		System.Console.WriteLine("[" + name + "]: " + string.Format(s, format));
	}
}