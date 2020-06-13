using System;
using Gtk;
using iCode.GUI.GTK3.Panels;

namespace iCode.GUI.GTK3.Tabs
{
	public class NotebookTabLabel : EventBox
	{
		private Widget _widget;
		public Label Label;

		public bool B = false;

		public NotebookTabLabel(string title, Widget widget)
		{
			Button button = new Button();
			var img = new Image(Stock.Close, IconSize.SmallToolbar);
			// img.Padd(0, 0);
			img.Margin = 0;
			img.SizeAllocate(new Gdk.Rectangle(0, 0, 0, 0));
			button.Image = img;
			button.TooltipText = "Close Tab";
			button.Relief = ReliefStyle.None;
			button.FocusOnClick = false;
			button.Clicked += this.OnCloseClicked;
			button.Show();
			Label = new Label();
			Label.Text = title;
			Label.UseMarkup = false;
			Label.UseUnderline = false;
			Label.Show();
			HBox hbox = new HBox(false, 0);
			hbox.Spacing = 0;
			hbox.Add(Label);
			hbox.Add(button);
			hbox.Show();
			base.Add(hbox);
			this._widget = widget;
		}

		public event EventHandler<EventArgs> CloseClicked;

		public void OnCloseClicked(object sender, EventArgs e)
		{
			CodeWidget.Codewidget.Tabs_PageReordered();

			bool flag = this.CloseClicked != null && !B;
			if (flag)
			{
				this.CloseClicked(sender, e);
				B = true;
			}
			else
			{
				B = false;
			}
		}

		public void OnCloseClicked()
		{
			this.OnCloseClicked(this, EventArgs.Empty);
		}
	}
}