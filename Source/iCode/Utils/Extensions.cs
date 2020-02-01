using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Gdk;
using Gtk;
using iCode;
using iCode.GUI;
using iCode.GUI.Tabs;
using NClang;
using Pango;

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
				ExceptionWindow.Create(e, notebook).ShowAll();
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

			return IconLoader.LoadIcon(Program.WinInstance, "gtk-file", IconSize.Menu);
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
            
			
			// proc.Start();
			// Console.WriteLine(process + " " + arguments);
			return proc;
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