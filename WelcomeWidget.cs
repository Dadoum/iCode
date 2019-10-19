using System;
using System.ComponentModel;
using Gtk;
using Mono.Unix;
using Pango;
using Stetic;

namespace iCode
{
	[ToolboxItem(true)]
	public partial class WelcomeWidget : Bin
	{
		public WelcomeWidget()
		{
			this.Build();
			FontDescription fontDescription = base.PangoContext.FontDescription;
			fontDescription.Size *= 4;
			this.label1.UseMarkup = true;
			this.label1.OverrideFont(fontDescription);
			FontDescription fontDescription2 = base.PangoContext.FontDescription;
			fontDescription2.Size = 30520;
			this.label2.OverrideFont(fontDescription2);
		}
	}
}
