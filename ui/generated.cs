
// This file has been generated by the GUI designer. Do not modify.
namespace Stetic
{
	internal class Gui
	{
		private static bool initialized;

		internal static void Initialize(Gtk.Widget iconRenderer)
		{
			if ((Stetic.Gui.initialized == false))
			{
				Stetic.Gui.initialized = true;
			}
		}
	}

	internal class BinContainer
	{
		private Gtk.Widget child;

		private Gtk.UIManager uimanager;

		public static BinContainer Attach(Gtk.Bin bin)
		{
			BinContainer bc = new BinContainer();
			bin.SizeAllocated += new Gtk.SizeAllocatedHandler(bc.OnSizeAllocated);
			bin.Added += new Gtk.AddedHandler(bc.OnAdded);
			return bc;
		}

		private void OnSizeAllocated(object sender, Gtk.SizeAllocatedArgs args)
		{
			if ((this.child != null))
			{
				this.child.SetAllocation(args.Allocation);
			}
		}

		private void OnAdded(object sender, Gtk.AddedArgs args)
		{
			this.child = args.Widget;
		}

		public void SetUiManager(Gtk.UIManager uim)
		{
			this.uimanager = uim;
			this.child.Realized += new System.EventHandler(this.OnRealized);
		}

		private void OnRealized(object sender, System.EventArgs args)
		{
			if ((this.uimanager != null))
			{
				Gtk.Widget w;
				w = this.child.Toplevel;
				if (((w != null)
							&& typeof(Gtk.Window).IsInstanceOfType(w)))
				{
					((Gtk.Window)(w)).AddAccelGroup(this.uimanager.AccelGroup);
					this.uimanager = null;
				}
			}
		}
	}

	public class IconLoader
	{
		public static Gdk.Pixbuf LoadIcon(Gtk.Widget widget, string name, Gtk.IconSize size)
		{
			Gdk.Pixbuf res = widget.RenderIconPixbuf(name, size);
			if ((res != null))
			{
				return res;
			}
			else
			{
				int sz;
				int sy;
				global::Gtk.Icon.SizeLookup(size, out sz, out sy);
				try
				{
					return Gtk.IconTheme.Default.LoadIcon(name, sz, 0);
				}
				catch (System.Exception ex)
				{
					if ((name != "gtk-missing-image"))
					{
						return Stetic.IconLoader.LoadIcon(widget, "gtk-missing-image", size);
					}
					else
					{
                        throw ex;
					}
				}
			}
		}
	}

	internal class ActionGroups
	{
		public static Gtk.ActionGroup GetActionGroup(System.Type type)
		{
			return Stetic.ActionGroups.GetActionGroup(type.FullName);
		}

		public static Gtk.ActionGroup GetActionGroup(string name)
		{
			return null;
		}
	}
}