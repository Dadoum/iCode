using System;
using System.ComponentModel;
using Gtk;
using Stetic;

namespace iCode
{
	[ToolboxItem(true)]
	public partial class PropertyWidget : Bin
    {
        public TreeView tree
		{
			get
			{
				return this.treeview1;
			}
		}

		public PropertyWidget()
		{
			this.Build();
			base.SetSizeRequest(100, 1);
		}
	}
}
