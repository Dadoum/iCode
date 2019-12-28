using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace iCode.Utils
{
	public class DatedConsole : TextWriter
	{
		private TextWriter _console;

		public DatedConsole()
		{
			this._console = System.Console.Out;
		}

		~DatedConsole()
		{
			this._console.Dispose();
		}

		public override void WriteLine(string value)
		{
			try
			{
				var stacktrace = new StackTrace().GetFrames();
				//_console.WriteLine(stacktrace.Length);
				string name = stacktrace[3].GetMethod().ReflectedType.FullName; // The number 3 is the index of the method that called the function and skips all funcs of WriteLine 
				string ln = stacktrace[3].GetMethod().Name + "()";
				string line = stacktrace[3].GetFileLineNumber() != 0 ? " (" + stacktrace[3].GetFileLineNumber() + ";" + stacktrace[3].GetFileColumnNumber() +  ")" : "";
				this._console.WriteLine("[{0}] [" + name + ":" + ln + line + "]: " + value, DateTime.Now.ToString("HH:mm:ss"));
			}
			catch
			{
				try
				{
					this._console.WriteLine(value);
				}
				catch (Exception e)
				{
					this._console.WriteLine("Something went wrong in a class, unable to log its output. Weird: {0}", e.ToString());
				}
			}
		}

		public override Encoding Encoding
		{
			get
			{
				return Encoding.UTF8;
			}
		}
	}
}