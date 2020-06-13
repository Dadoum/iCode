#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace iCode.Utils
{
	public class DatedConsole : TextWriter
	{
		private TextWriter _console;

		public DatedConsole(TextWriter console)
		{
			this._console = console;
		}

		~DatedConsole()
		{
			this._console.Dispose();
		}

		public override void Write(bool value)
        {
            Write();
            _console.Write(value);
        }

		public override void Write(char value)
        {
            Write();
            _console.Write(value);
        }

		public override void Write(char[]? buffer)
        {
            Write();
            _console.Write(buffer);
        }

		public override void Write(char[] buffer, int index, int count)
        {
            Write();
            _console.Write(buffer, index, count);
        }

		public override void Write(decimal value)
        {
            Write();
            _console.Write(value);
        }

		public override void Write(double value)
        {
            Write();
            _console.Write(value);
        }

		public override void Write(int value)
        {
            Write();
            _console.Write(value);
        }

		public override void Write(long value)
        {
            Write();
            _console.Write(value);
        }

		public override void Write(object? value)
        {
            Write();
            _console.Write(value);
        }

		public override void Write(ReadOnlySpan<char> buffer)
        {
            Write();
            _console.Write(buffer);
        }

		public override void Write(float value)
        {
            Write();
            _console.Write(value);
        }

		public override void Write(string? value)
        {
            Write();
            _console.Write(value);
        }

		public override void Write(string format, object? arg0)
        {
            Write();
            _console.Write(format, arg0);
        }

		public override void Write(string format, object? arg0, object? arg1)
        {
            Write();
            _console.Write(format, arg0, arg1);
        }

		public override void Write(string format, object? arg0, object? arg1, object? arg2)
        {
            Write();
            _console.Write(format, arg0, arg1, arg2);
        }

		public override void Write(string format, params object?[] arg)
        {
            Write();
            _console.Write(format, arg);
        }

		public override void Write(StringBuilder? value)
        {
            Write();
            _console.Write(value);
        }

		public override void Write(uint value)
        {
            Write();
            _console.Write(value);
        }

		public override void Write(ulong value)
        {
            Write();
            _console.Write(value);
        }

		public override void WriteLine(bool value)
        {
            Write();
            _console.WriteLine(value);
        }

		public override void WriteLine(char value)
        {
            Write();
            _console.WriteLine(value);
        }

		public override void WriteLine(char[]? buffer)
        {
            Write();
            _console.WriteLine(buffer);
        }

		public override void WriteLine(char[] buffer, int index, int count)
        {
            Write();
            _console.WriteLine(buffer, index, count);
        }

		public override void WriteLine(decimal value)
        {
            Write();
            _console.WriteLine(value);
        }

		public override void WriteLine(double value)
        {
            Write();
            _console.WriteLine(value);
        }

		public override void WriteLine(int value)
        {
            Write();
            _console.WriteLine(value);
        }

		public override void WriteLine(long value)
        {
            Write();
            _console.WriteLine(value);
        }

		public override void WriteLine(object? value)
        {
            Write();
            _console.WriteLine(value);
        }

		public override void WriteLine(ReadOnlySpan<char> buffer)
        {
            Write();
            _console.WriteLine(buffer);
        }

		public override void WriteLine(float value)
        {
            Write();
            _console.WriteLine(value);
        }

		public override void WriteLine(string? value)
        {
            Write();
            _console.WriteLine(value);
        }

		public override void WriteLine(string format, object? arg0)
        {
            Write();
            _console.WriteLine(format, arg0);
        }

		public override void WriteLine(string format, object? arg0, object? arg1)
        {
            Write();
            _console.WriteLine(format, arg0, arg1);
        }

		public override void WriteLine(string format, object? arg0, object? arg1, object? arg2)
        {
            Write();
            _console.WriteLine(format, arg0, arg1, arg2);
        }

		public override void WriteLine(string format, params object?[] arg)
        {
            Write();
            _console.WriteLine(format, arg);
        }

		public override void WriteLine(StringBuilder? value)
        {
            Write();
            _console.WriteLine(value);
        }

		public override void WriteLine(uint value)
        {
            Write();
            _console.WriteLine(value);
        }

		public override void WriteLine(ulong value)
        {
            Write();
            _console.WriteLine(value);
        }

		public override void WriteLine()
        {
            Write();
            _console.WriteLine();
        }

		public void Write()
		{
			try
			{
				var stacktrace = new StackTrace().GetFrames();
				//_console.WriteLine(stacktrace.Length);
				int i = 2;

				while (stacktrace[i]!.GetMethod()!.ReflectedType!.FullName!.StartsWith("System"))
					i++;

				string? name = stacktrace[i]!.GetMethod()!.ReflectedType!.FullName;

				string args = "(" + string.Join(", ",
					stacktrace[i]!.GetMethod()!.GetParameters().Select(m =>
						ProcessType(m.ParameterType.FullName) + " " + m.Name +
						(m.IsOptional ? " = " + m.DefaultValue : ""))) + ")";
				
				string ln = stacktrace[i]!.GetMethod()!.Name + args;
				string line = stacktrace[i]!.GetFileLineNumber() != 0
					? " (" + stacktrace[i]!.GetFileLineNumber() + ";" + stacktrace[i]!.GetFileColumnNumber() + ")"
					: "";
				this._console.Write("[{0}] [" + name + ":" + ln + line + "]: ", DateTime.Now.ToString("HH:mm:ss.ff"));
			}
			catch (Exception e)
			{
				this._console.WriteLine("Something went wrong in a class, unable to log its output. Weird: {0}",
					e);
			}
		}

		private string ProcessType(string? parameterTypeFullName)
		{
			var name = parameterTypeFullName;
			name = name!.Replace("System.SByte", "sbyte");
			name = name.Replace("System.Int16", "short");
			name = name.Replace("System.Int32", "int");
			name = name.Replace("System.Int64", "long");
			name = name.Replace("System.Boolean", "bool");
			name = name.Replace("System.Byte", "byte");
			name = name.Replace("System.Char", "char");
			name = name.Replace("System.Decimal", "decimal");
			name = name.Replace("System.Double", "double");
			name = name.Replace("System.Single", "float");
			name = name.Replace("System.UInt16", "ushort");
			name = name.Replace("System.UInt32", "uint");
			name = name.Replace("System.UInt64", "ulong");
			name = name.Replace("System.Object", "object");
			name = name.Replace("System.String", "string");
			
			return name;
		}

		public override Encoding Encoding => Encoding.UTF8;
	}
}