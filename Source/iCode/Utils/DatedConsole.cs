using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace iCode.Utils
{
	public class DatedConsole : TextWriter
    {
        private TextWriter console;

        public DatedConsole()
		{
			this.console = System.Console.Out;
		}

		~DatedConsole()
		{
			this.console.Dispose();
		}

		public override void WriteLine(string value)
		{
            try
            {
                this.console.WriteLine("[{0}] " + value, DateTime.Now.ToString("HH:mm:ss"));
            }
            catch
            {
                try
                {
                    this.console.WriteLine(value);
                }
                catch (Exception e)
                {
                    this.console.WriteLine("Something went wrong in a class, unable to log its output. Weird: {0}", e.ToString());
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
