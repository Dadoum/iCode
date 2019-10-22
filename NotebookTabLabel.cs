using System;
using System.Diagnostics;
using System.Linq;
using Gtk;
using iCode;
using Stetic;

public class NotebookTabLabel : EventBox
{
    private Widget widget;
    public Label Label;

    public bool b = false;

    public NotebookTabLabel(string title, Widget widget)
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
        this.widget = widget;
	}

	public event EventHandler<EventArgs> CloseClicked;

	public void OnCloseClicked(object sender, EventArgs e)
	{
        CodeWidget.codewidget.Tabs_PageReordered();

        bool flag = this.CloseClicked != null && !b;
		if (flag)
		{
			this.CloseClicked(sender, e);
            b = true;
		}
        else
        {
            b = false;
        }
    }

	public void OnCloseClicked()
	{
		this.OnCloseClicked(this, EventArgs.Empty);
	}
}
