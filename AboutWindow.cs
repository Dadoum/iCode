using System;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;

namespace iCode
{
    public class AboutWindow : Window
    {
        Builder builder;

#pragma warning disable 649
        [UI] private Gtk.Button ok_button;
        [UI] private Gtk.TextView text;
#pragma warning restore 649

        public static AboutWindow Create()
        {
            Builder builder = new Builder(null, "NewProject", null);
            return new AboutWindow(builder, builder.GetObject("about").Handle);
        }

        private AboutWindow(Builder builder, IntPtr handle) : base(handle)
        {
            this.builder = builder;
            builder.Autoconnect(this);

            ok_button.Activated += (sender, e) =>
            {
                this.Destroy();
            };

            this.Title = ("About iCode");
            text.Buffer.Text = @"iCode uses:
 - theos template, 
 - kabiroberai's iOS toolchain for linux, 
 - Apple's iOS SDK, patched with theos tool,
 - precompiled zsign binary, by zhlynn
 - Newtonsoft.Json
 - Gtk, gtk# and mono
iPhone, iPad, iPod, iDevice, Xcode and iOS belongs to Apple.

iCode codename Harmonica, beta 1.";
        }
    }
}
