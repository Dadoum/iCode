using System;
using System.Diagnostics;
using Gtk;
using Stetic;

public class NotebookTabLabel : EventBox
{
	public NotebookTabLabel(string title)
	{
		Button button = new Button();
        var img = new Image(Stock.Close, IconSize.SmallToolbar);
        img.SetPadding(0, 0);
        img.Margin = 0;
        img.SizeAllocate(new Gdk.Rectangle(0, 0, 0, 0));
        button.Image = img;
		button.TooltipText = "Close Tab";
		button.Relief = ReliefStyle.None;
		button.FocusOnClick = false;
		button.Clicked += this.OnCloseClicked;
		button.Show();
		Label label = new Label(title);
		label.UseMarkup = false;
		label.UseUnderline = false;
		label.Show();
		HBox hbox = new HBox(false, 0);
		hbox.Spacing = 0;
		hbox.Add(label);
		hbox.Add(button);
		hbox.Show();
		base.Add(hbox);
	}

	public event EventHandler<EventArgs> CloseClicked;

	public void OnCloseClicked(object sender, EventArgs e)
	{
		bool flag = this.CloseClicked != null;
		if (flag)
		{
			this.CloseClicked(sender, e);
		}
	}

	public void OnCloseClicked()
	{
		this.OnCloseClicked(this, EventArgs.Empty);
	}
}
