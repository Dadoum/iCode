using System.Diagnostics;
using iCode.GUI.Backend.Interfaces.Panels;
using iCode.GUI.GTK3.GladeUI;
using UI = Gtk.Builder.ObjectAttribute;

namespace iCode.GUI.GTK3.Panels
{
	public class OutputWidget : Gtk.ScrolledWindow, IGladeWidget, IOutputWidget
	{
#pragma warning disable 649
		[UI] private Gtk.TextView _output;
#pragma warning restore 649

		int _lastAction = -1;

		public string ResourceName => "Output";

		public string WidgetName => "OutputWindow";

		public void Initialize()
		{
			_output.Buffer = new Gtk.TextBuffer(new Gtk.TextTagTable());
			_output.SizeAllocated += (o, args) =>
			{
				this.Vadjustment.Value = this.Vadjustment.Upper - this.Vadjustment.PageSize;
			};
		}

		public int Run(Process p, int action)
		{
			if (action == (int)ActionCategory.Make && action != _lastAction)
				Gtk.Application.Invoke((a, b) =>
				{
					_output.Buffer.Text = "";
				});

			switch ((ActionCategory) action)
			{
				case ActionCategory.Make:
					if (action != _lastAction)
						Gtk.Application.Invoke((a, b) =>
						{
							_output.Buffer.Text += "=========== MAKE ==========\n";
						});
					break;

				case ActionCategory.Link:
					if (action != _lastAction)
						Gtk.Application.Invoke((a, b) =>
						{
							_output.Buffer.Text += "=========== LINK ==========\n";
						});
					break;

				case ActionCategory.Enroll:
					if (action != _lastAction)
						Gtk.Application.Invoke((a, b) =>
						{
							_output.Buffer.Text += "========== ENROLL =========\n";
						});
					break;

				case ActionCategory.Sideload:
					if (action != _lastAction)
						Gtk.Application.Invoke((a, b) =>
						{
							_output.Buffer.Text += "========= SIDELOAD ========\n";
						});
					break;

				case ActionCategory.Launch:
					if (action != _lastAction)
						Gtk.Application.Invoke((a, b) =>
						{
							_output.Buffer.Text += "========== LAUNCH =========\n";
						});
					break;
			}

			_lastAction = action;
			Gtk.Application.Invoke((a, b) =>
			{
				_output.Buffer.Text += "-> '" + p.StartInfo.FileName + "' " + p.StartInfo.Arguments + "\n";
			});

			p.StartInfo.UseShellExecute = false;
			p.StartInfo.RedirectStandardError = p.StartInfo.RedirectStandardOutput = true;

			p.Start();
			p.WaitForExit();
			
			Gtk.Application.Invoke((a, b) =>
			{
				_output.Buffer.Text += p.StandardError.ReadToEnd();
				_output.Buffer.Text += p.StartInfo.FileName + " exited with the code " + p.ExitCode + "\n\n";
			});

			return p.ExitCode;
		}

	}

	public enum ActionCategory
	{
		Make=0,
		Link=1,
		Enroll=2,
		Sideload=3,
		Launch=4
	}
}