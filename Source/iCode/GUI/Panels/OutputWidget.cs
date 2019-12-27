using System;
using System.Diagnostics;
using UI = Gtk.Builder.ObjectAttribute;

namespace iCode.GUI.Panels
{
	public class OutputWidget : Gtk.ScrolledWindow
	{
		Gtk.Builder _builder;

#pragma warning disable 649
		[UI] private Gtk.TextView _output;
#pragma warning restore 649

		int _lastAction = -1;

		public static OutputWidget Create()
		{
			Gtk.Builder builder = new Gtk.Builder(null, "Output", null);
			return new OutputWidget(builder, builder.GetObject("OutputWindow").Handle);
		}

		private OutputWidget(Gtk.Builder builder, IntPtr handle) : base(handle)
		{
			this._builder = builder;
			builder.Autoconnect(this);

			_output.Buffer = new Gtk.TextBuffer(new Gtk.TextTagTable());
			_output.SizeAllocated += (o, args) =>
			{
				this.Vadjustment.Value = this.Vadjustment.Upper - this.Vadjustment.PageSize;
			};
		}

		public int Run(Process p, int action, out string error, out string outp)
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

			while (!p.StandardOutput.EndOfStream)
			{
				string line = p.StandardOutput.ReadLine();

				Gtk.Application.Invoke((sender, e) =>
				{
					_output.Buffer.Text += line + "\n";
				});
			}

			outp = p.StandardOutput.ReadToEnd();
			error = p.StandardError.ReadToEnd();

			p.WaitForExit();

			Gtk.Application.Invoke((sender, e) =>
			{
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