using System;
using System.Diagnostics;
using UI = Gtk.Builder.ObjectAttribute;

namespace iCode
{
    public class OutputWidget : Gtk.ScrolledWindow
    {
        Gtk.Builder builder;

#pragma warning disable 649
        [UI] private Gtk.TextView output;
#pragma warning restore 649

        int lastAction = -1;

        public static OutputWidget Create()
        {
            Gtk.Builder builder = new Gtk.Builder(null, "Output", null);
            return new OutputWidget(builder, builder.GetObject("OutputWindow").Handle);
        }

        private OutputWidget(Gtk.Builder builder, IntPtr handle) : base(handle)
        {
            this.builder = builder;
            builder.Autoconnect(this);

            output.Buffer = new Gtk.TextBuffer(new Gtk.TextTagTable());
        }

        public bool Run(Process p, int action, out string error, out string outp)
        {
            if (action == (int)ActionCategory.MAKE && action != lastAction)
                output.Buffer.Text = "";

            switch ((ActionCategory) action)
            {
                case ActionCategory.MAKE:
                    if (action != lastAction)
                        output.Buffer.Text += "=========== MAKE ==========\n";
                    break;

                case ActionCategory.LINK:
                    if (action != lastAction)
                        output.Buffer.Text += "=========== LINK ==========\n";
                    break;

                case ActionCategory.ENROLL:
                    if (action != lastAction)
                        output.Buffer.Text += "========== ENROLL =========\n";
                    break;

                case ActionCategory.SIDELOAD:
                    if (action != lastAction)
                        output.Buffer.Text += "========= SIDELOAD ========\n";
                    break;

                case ActionCategory.LAUNCH:
                    if (action != lastAction)
                        output.Buffer.Text += "========== LAUNCH =========\n";
                    break;
            }

            lastAction = action;

            output.Buffer.Text += "-> '" + p.StartInfo.FileName + "' " + p.StartInfo.Arguments + "\n";

            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardError = p.StartInfo.RedirectStandardOutput = true;
            p.Start();
            p.WaitForExit();

            outp = p.StandardOutput.ReadToEnd();
            error = p.StandardError.ReadToEnd();

            output.Buffer.Text += outp + "\n";
            output.Buffer.Text += p.StartInfo.FileName + " exited with the code " + p.ExitCode + "\n\n";

            if (p.ExitCode == 0)
                return true;
            else
                return false;
        }
    }

    public enum ActionCategory
    {
        MAKE=0,
        LINK=1,
        ENROLL=2,
        SIDELOAD=3,
        LAUNCH=4
    }
}
