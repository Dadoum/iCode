using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Gdk;
using Gtk;
using iCode;
using iCode.GUI;
using iCode.GUI.Backend.Interfaces;
using iCode.GUI.GTK3;
using iCode.GUI.GTK3.GladeUI;
using iCode.GUI.GTK3.Tabs;
using NClang;
using Pango;
using MainWindow = iCode.GUI.GTK3.MainWindow;

namespace iCode.Utils
{
	public static class Extensions
	{
		public static Dictionary<string, Widget> Tabs = new Dictionary<string, Widget>();

		public static void ModifyFont(this Gtk.Label label, string family, int size, Pango.Style sty = Pango.Style.Normal)
		{
			FontDescription fontDesc = new FontDescription
			{
				Family = family,
				Size = Convert.ToInt32((double)size * Pango.Scale.PangoScale),
				Style = sty
			};
			label.Attributes.Insert(new Pango.AttrFontDesc(fontDesc));
		}

		public static string GetLast(this string source, int tailLength)
		{
			if (tailLength >= source.Length)
				return source;
			return source.Substring(source.Length - tailLength);
		}

		public static int To256Integer(this double f) =>
			(f >= 1.0 ? 255 : (f <= 0.0 ? 0 : (int)Math.Floor(f * 256.0)));

		public static void Add(this Notebook notebook, Widget widget, string str, bool isVolatile)
		{
			try
			{
				widget.Name = str;
				Extensions.Tabs.Add(str, widget);

				ScrolledWindow scrolledWindow = new ScrolledWindow();
				scrolledWindow.Add(widget);
				scrolledWindow.Name = str;
				if (widget is CodeTabWidget tabWidget)
				{
					notebook.AppendPage(scrolledWindow, (tabWidget.GetLabel()));

					tabWidget.GetLabel().ShowAll();
					tabWidget.GetLabel().CloseClicked += delegate
					{
						notebook.RemovePage(notebook.PageNum(notebook.Children.First(x => x == scrolledWindow)));
						Tabs.Remove(str);
					};
				}
				else
				{
					NotebookTabLabel notebookTabLabel = new NotebookTabLabel(str, widget);

					notebookTabLabel.CloseClicked += delegate
					{
						notebook.RemovePage(notebook.PageNum(notebook.Children.First(x => x == scrolledWindow)));
						Tabs.Remove(str);
					};
					notebook.AppendPage(scrolledWindow, notebookTabLabel);
					notebookTabLabel.ShowAll();
				}

				notebook.SetTabDetachable(scrolledWindow, isVolatile);
				notebook.SetTabReorderable(scrolledWindow, isVolatile);

				widget.ShowAll();

			}
			catch (ArgumentException)
			{
				notebook.Page = notebook.PageNum(Tabs.First(x => x.Key == str).Value);
			}
			catch (Exception e)
			{
				GladeHelper.Create<ExceptionWindow>().ShowException(e, Program.WinInstance);
			}
		}

		public static byte[] ToByteArray(this Stream input)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				input.CopyTo(ms);
				return ms.ToArray();
			}
		}

		public static Stream ToStream(this byte[] input)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				ms.Write(input, 0, input.Length);
				return ms;
			}
		}

		public static Pixbuf GetIconFromFile(string path)
		{
			try
			{
				string[] mimetypes =
					LaunchProcess("gio", $"info -a standard::icon \"{path}\"")
						.Split(new string[] {": "}, StringSplitOptions.None)[3]
						.Split(new string[] {", "}, StringSplitOptions.None);
				mimetypes[0] = mimetypes[0].TrimStart(' ');

				string theme = LaunchProcess("gsettings", "get org.gnome.desktop.interface icon-theme");
				theme = theme.TrimEnd('\n').Trim('\'');

				foreach (string s in mimetypes)
				{
					string file = string.Format("/usr/share/icons/{0}/mimetypes/16/{1}.svg", theme, s);
					if (File.Exists(file))
					{
						var pixbufld = new PixbufLoader();
						pixbufld.Write(Encoding.UTF8.GetBytes(File.ReadAllText(file)));
						pixbufld.Close();
						return pixbufld.Pixbuf;
					}

					file = string.Format("/usr/share/icons/{0}/16x16/mimetypes/{1}.svg", theme, s);
					if (File.Exists(file))
					{
						var pixbufld = new PixbufLoader();
						pixbufld.Write(Encoding.UTF8.GetBytes(File.ReadAllText(file)));
						pixbufld.Close();
						return pixbufld.Pixbuf;
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine($"Unable to retrieve the icon of {path}: {e}");
			}

			return IconLoader.LoadIcon((Widget) Program.WinInstance, "gtk-file", IconSize.Menu);
		}

		public static Gdk.Color RgbaFromHex(string s)
		{
			if (s.StartsWith ("#"))
				s = s.Substring (1);
			if (s.Length == 3)
				s = "" + s[0]+s[0]+s[1]+s[1]+s[2]+s[2];
			ushort r = Convert.ToUInt16(ushort.Parse (s.Substring (0,2), System.Globalization.NumberStyles.HexNumber) * 255);
			ushort g = Convert.ToUInt16(ushort.Parse (s.Substring (2,2), System.Globalization.NumberStyles.HexNumber) * 255);
			ushort b = Convert.ToUInt16(ushort.Parse (s.Substring (4,2), System.Globalization.NumberStyles.HexNumber) * 255);
			return new Gdk.Color()
			{
				Red = r,
				Green = g,
				Blue = b
			};
		}
		
		public static MessageDialog ShowMessage(MessageType type, string title, string message, Gtk.Window parent = null)
		{
			MessageDialog md = null;
			Gtk.Application.Invoke((o, a) =>
			{
				md = new MessageDialog(parent, DialogFlags.Modal, type, ButtonsType.Ok, true, message);
				md.Title = title;
				md.Run();
				md.Dispose();
			});
			
			return md;
		}

		public static Process GetProcess(string process, string arguments)
		{
			var proc = new Process();
			proc.StartInfo.Arguments = arguments;
			proc.StartInfo.FileName = process;
			proc.StartInfo.UseShellExecute = false;
			proc.StartInfo.RedirectStandardOutput = true;
			proc.StartInfo.RedirectStandardError = true;
			proc.StartInfo.EnvironmentVariables.Add("LD_LIBRARY_PATH", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));	
			
			// proc.Start();
			// Console.WriteLine(process + " " + arguments);
			return proc;
		}

		// From SO-4680128 
		public static IList<string> SplitWithDelims(this string s, params char[] delimiters)
		{
			var parts = new List<string>();
			if (!string.IsNullOrEmpty(s))
			{
				int iFirst = 0;
				do
				{
					int iLast = s.IndexOfAny(delimiters, iFirst);
					if (iLast >= 0)
					{
						if (iLast > iFirst)
							parts.Add(s.Substring(iFirst, iLast - iFirst)); //part before the delimiter
						parts.Add(new string(s[iLast], 1));//the delimiter
						iFirst = iLast + 1;
						continue;
					}

					//No delimiters were found, but at least one character remains. Add the rest and stop.
					parts.Add(s.Substring(iFirst, s.Length - iFirst));
					break;

				} while (iFirst < s.Length);
			}

			return parts;
		}
		
		public static string LaunchProcess(string process, string arguments) => LaunchProcess(process, arguments, out _);

		public static string LaunchProcess(string process, string arguments, out int? ret, bool wait = true)
		{
			var proc = new Process();
			proc.StartInfo.Arguments = arguments;
			proc.StartInfo.FileName = process;
			proc.StartInfo.UseShellExecute = false;
			proc.StartInfo.RedirectStandardOutput = true;
			proc.StartInfo.RedirectStandardError = true;
			var outputBuilder = new StringBuilder();
			proc.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e)
			{
				outputBuilder.Append(e.Data);
			};
            
			proc.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e)
			{
				outputBuilder.Append(e.Data);
			};
			proc.Start();
			proc.BeginOutputReadLine();
			if (wait)
			{
				proc.WaitForExit();
				proc.CancelOutputRead();
				ret = proc.ExitCode;
			}
			else 
				ret = null;
			
			var str = outputBuilder.ToString();
			// Console.WriteLine($"process:{process} args:{arguments} stdout:\n{str}");
			return str;
		}

		public static void RemoveEvents(TreeView b)
		{
			FieldInfo f1 = typeof(TreeView).GetField("EventRowActivated",
				BindingFlags.Static | BindingFlags.NonPublic);
			object obj = f1.GetValue(b);
			PropertyInfo pi = b.GetType().GetProperty("Events",
				BindingFlags.NonPublic | BindingFlags.Instance);
			EventHandlerList list = (EventHandlerList)pi.GetValue(b, null);
			list.RemoveHandler(obj, list[obj]);
		}

		public static byte[] ReadFully(this Stream input)
		{
			byte[] buffer = new byte[16 * 1024];
			using (MemoryStream ms = new MemoryStream())
			{
				int read;
				while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
				{
					ms.Write(buffer, 0, read);
				}
				return ms.ToArray();
			}
		}
	}
}