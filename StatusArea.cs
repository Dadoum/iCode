//
// StatusArea.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Diagnostics;
using Gtk;
using MonoDevelop.Components;
using Cairo;
using System.Collections.Generic;
using Xwt.Motion;
using System.Threading;
using System.Linq;

namespace MonoDevelop.Components.MainToolbar
{
	class StatusArea : EventBox, Xwt.Motion.IAnimatable
	{
		struct Message
		{
			public string Text;
			// public IconId Icon;
			public bool IsMarkup;

			public Message (string text, bool markup)
			{
				Text = text;
				// Icon = icon;
				IsMarkup = markup;
			}
		}

		public struct RenderArg
		{
			public Gdk.Rectangle Allocation { get; set; }
			public double        BuildAnimationProgress { get; set; }
			public double        BuildAnimationOpacity { get; set; }
			public Gdk.Rectangle ChildAllocation { get; set; }
			public Xwt.Drawing.Image CurrentPixbuf { get; set; }
			public string        CurrentText { get; set; }
			public bool          CurrentTextIsMarkup { get; set; }
			public double        ErrorAnimationProgress { get; set; }
			public double        HoverProgress { get; set; }
			public string        LastText { get; set; }
			public bool          LastTextIsMarkup { get; set; }
			public Xwt.Drawing.Image LastPixbuf { get; set; }
			public Gdk.Point     MousePosition { get; set; }
			public Pango.Context Pango { get; set; }
			public double        ProgressBarAlpha { get; set; }
			public float         ProgressBarFraction { get; set; }
			public bool          ShowProgressBar { get; set; }
			public double        TextAnimationProgress { get; set; }
		}

		StatusAreaTheme theme;
		RenderArg renderArg;

		HBox contentBox = new HBox (false, 8);

		StatusAreaSeparator statusIconSeparator;
		Gtk.Widget buildResultWidget;

		readonly HBox messageBox = new HBox ();
		internal readonly HBox statusIconBox = new HBox ();
		Alignment mainAlign;

		uint animPauseHandle;

		MouseTracker tracker;

		IDisposable currentIconAnimation;

		bool errorAnimPending;

		StatusBarContextHandler ctxHandler;
		bool progressBarVisible;

		string currentApplicationName = String.Empty;

		Queue<Message> messageQueue;

		public int MaxWidth { get; set; }

		void messageBoxToolTip (object o, QueryTooltipArgs e)
		{
			if (theme.IsEllipsized && (e.X < messageBox.Allocation.Width)) {
				var label = new Label ();
				if (renderArg.CurrentTextIsMarkup) {
					label.Markup = renderArg.CurrentText;
				} else {
					label.Text = renderArg.CurrentText;
				}

				label.Wrap = true;
				label.WidthRequest = messageBox.Allocation.Width;

				e.Tooltip.Custom = label;
				e.RetVal = true;
			} else {
				e.RetVal = false;
			}
		}

		public StatusArea ()
		{
			theme = new StatusAreaTheme ();
			renderArg = new RenderArg ();

			VisibleWindow = false;
			NoShowAll = true;

			statusIconBox.BorderWidth = 0;
			statusIconBox.Spacing = 3;

			Action<bool> animateProgressBar =
				showing => this.Animate ("ProgressBarFade",
				                         val => renderArg.ProgressBarAlpha = val,
				                         renderArg.ProgressBarAlpha,
				                         showing ? 1.0f : 0.0f,
				                         easing: Easing.CubicInOut);

			ProgressBegin += delegate {
				renderArg.ShowProgressBar = true;
//				StartBuildAnimation ();
				renderArg.ProgressBarFraction = 0;
				QueueDraw ();
				animateProgressBar (true);
			};

			ProgressEnd += delegate {
				renderArg.ShowProgressBar = false;
//				StopBuildAnimation ();
				QueueDraw ();
				animateProgressBar (false);
			};

			ProgressFraction += delegate(object sender, FractionEventArgs e) {
				renderArg.ProgressBarFraction = (float)e.Work;
				QueueDraw ();
			};

			contentBox.PackStart (messageBox, true, true, 0);
			contentBox.PackEnd (statusIconBox, false, false, 0);
			contentBox.PackEnd (statusIconSeparator = new StatusAreaSeparator (), false, false, 0);
			contentBox.PackEnd (buildResultWidget = CreateBuildResultsWidget (Orientation.Horizontal), false, false, 0);

			HasTooltip = true;
			QueryTooltip += messageBoxToolTip;

			mainAlign = new Alignment (0, 0.5f, 1, 0);
			mainAlign.LeftPadding = 12;
			mainAlign.RightPadding = 8;
			mainAlign.Add (contentBox);
			Add (mainAlign);

			mainAlign.ShowAll ();
			statusIconBox.Hide ();
			statusIconSeparator.Hide ();
			buildResultWidget.Hide ();
			Show ();

			statusIconBox.Shown += delegate {
				UpdateSeparators ();
			};

			statusIconBox.Hidden += delegate {
				UpdateSeparators ();
			};

			messageQueue = new Queue<Message> ();

			tracker = new MouseTracker(this);
			tracker.MouseMoved += (sender, e) => QueueDraw ();
			tracker.HoveredChanged += (sender, e) => {
				this.Animate ("Hovered",
				              x => renderArg.HoverProgress = x,
				              renderArg.HoverProgress,
				              tracker.Hovered ? 1.0f : 0.0f,
				              easing: Easing.SinInOut);
			};
		}

		protected override void OnDestroyed ()
		{
			if (theme != null)
				theme.Dispose ();
			base.OnDestroyed ();
		}

		void IAnimatable.BatchBegin () { }
		void IAnimatable.BatchCommit () { QueueDraw (); }

		void StartBuildAnimation ()
		{
			this.Animate ("Build",
			              val => renderArg.BuildAnimationProgress = val,
			              length: 5000,
			              repeat: () => true);

			this.Animate ("BuildOpacity",
			              start: renderArg.BuildAnimationOpacity,
			              end: 1.0f,
			              callback: x => renderArg.BuildAnimationOpacity = x);
		}

		void StopBuildAnimation ()
		{
			this.Animate ("BuildOpacity",
			              x => renderArg.BuildAnimationOpacity = x,
			              renderArg.BuildAnimationOpacity,
			              0.0f,
			              finished: (val, aborted) => { if (!aborted) this.AbortAnimation ("Build"); });
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			if (MaxWidth > 0 && allocation.Width > MaxWidth) {
				allocation = new Gdk.Rectangle (allocation.X + (allocation.Width - MaxWidth) / 2, allocation.Y, MaxWidth, allocation.Height);
			}
			base.OnSizeAllocated (allocation);
		}

		void TriggerErrorAnimation ()
		{
/* Hack for a compiler error - csc crashes on this:
 			this.Animate (name: "statusAreaError",
			              length: 700,
			              callback: val => renderArg.ErrorAnimationProgress = val);
*/
			this.Animate ("statusAreaError",
			              val => renderArg.ErrorAnimationProgress = val,
			              length: 900);
		}

		void UpdateSeparators ()
		{
			statusIconSeparator.Visible = statusIconBox.Visible && buildResultWidget.Visible;
		}

		public Widget CreateBuildResultsWidget (Orientation orientation)
		{
			EventBox ebox = new EventBox ();

			Gtk.Box box;
			if (orientation == Orientation.Horizontal)
				box = new HBox ();
			else
				box = new VBox ();
			box.Spacing = 3;

			var errorIcon = Stock.DialogError;
			var warningIcon = Stock.DialogWarning;

			//var errorImage = new Xwt.ImageView (errorIcon.To);
			//var warningImage = new Xwt.ImageView (warningIcon);

			//box.PackStart (errorImage.ToGtkWidget (), false, false, 0);
			Label errors = new Gtk.Label ();
			box.PackStart (errors, false, false, 0);

			//box.PackStart (warningImage.ToGtkWidget (), false, false, 0);
			Label warnings = new Gtk.Label ();
			box.PackStart (warnings, false, false, 0);
			box.NoShowAll = true;
			box.Show ();

			return ebox;
		}

		void ApplicationNameChanged (object sender, EventArgs e)
		{
			if (renderArg.CurrentText == currentApplicationName) {
				LoadText ("iCode", false);
				QueueDraw ();
			}
			currentApplicationName = "iCode";
		}

		protected override void OnRealized ()
		{
			base.OnRealized ();
			ModifyText (StateType.Normal, new Gdk.Color(255, 255, 255));
			ModifyFg (StateType.Normal, new Gdk.Color(255, 255, 255));
		}

		protected override void OnGetPreferredHeight(out int minimum_height, out int natural_height)
		{
            HeightRequest = 32;
            minimum_height = 32;
            natural_height = 32;
		}

        protected override bool OnDrawn(Context context)
        {

            renderArg.Allocation = Allocation;
            renderArg.ChildAllocation = messageBox.Allocation;
            renderArg.MousePosition = tracker.MousePosition;
            renderArg.Pango = PangoContext;

            theme.Render(context, renderArg, this);

            return base.OnDrawn(context);
        }


		#region StatusBar implementation

		List<StatusIcon> icons = new List<StatusIcon> ();

        /*public StatusBarIcon ShowStatusIcon (Xwt.Drawing.Image pixbuf)
		{
			Runtime.AssertMainThread ();
			StatusIcon icon = new StatusIcon (this, pixbuf);
			statusIconBox.PackEnd (icon.box);
			statusIconBox.ShowAll ();
			icons.Add (icon);
			return icon;
		}*/

        /*void HideStatusIcon (StatusIcon icon)
		{
			icons.Remove (icon);
			statusIconBox.Remove (icon.EventBox);
			if (statusIconBox.Children.Length == 0)
				statusIconBox.Hide ();
			icon.EventBox.Destroy ();
		}

		public StatusBarContext CreateContext ()
		{
			return ctxHandler.CreateContext ();
		}
        */
        public void ShowReady ()
		{
			ShowMessage ("");
		}

		public bool HasResizeGrip {
			get;
			set;
		}
		
		#endregion

		#region StatusBarContextBase implementation

		public void ShowMessage (string message)
		{
			ShowMessage (message, false);
		}

		public void ShowMessage (string message, bool isMarkup)
		{
            if (this.AnimationIsRunning("Text") || animPauseHandle > 0)
            {
                messageQueue.Clear();
                messageQueue.Enqueue(new Message(message, isMarkup));
            }
            else
            {
                ShowMessageInner(message, isMarkup);
            }
        }


		void ShowMessageInner (string message, bool isMarkup)
		{
			LoadText (message, isMarkup);
			/* Hack for a compiler error - csc crashes on this:
			this.Animate ("Text", easing: Easing.SinInOut,
			              callback: x => renderArg.TextAnimationProgress = x,
			              finished: x => { animPauseHandle = GLib.Timeout.Add (1000, () => {
					if (messageQueue.Count > 0) {
						Message m = messageQueue.Dequeue();
						ShowMessageInner (m.Icon, m.Text, m.IsMarkup);
					}
					animPauseHandle = 0;
					return false;
				});
			});
			*/
			this.Animate ("Text",
			              x => renderArg.TextAnimationProgress = x,
			              easing: Easing.SinInOut,
			              finished: (x, b) => { animPauseHandle = GLib.Timeout.Add (1000, () => {
					if (messageQueue.Count > 0) {
						Message m = messageQueue.Dequeue();
						ShowMessageInner (m.Text, m.IsMarkup);
					}
					animPauseHandle = 0;
					return false;
				});
			});


			if (renderArg.CurrentText == renderArg.LastText)
				this.AbortAnimation ("Text");

			QueueDraw ();
		}

		void LoadText (string message, bool isMarkup)
		{
			if (string.IsNullOrEmpty(message))
				message = "iCode";
			message = message ?? "";

			renderArg.LastText = renderArg.CurrentText;
			renderArg.CurrentText = message.Replace (System.Environment.NewLine, " ").Replace ("\n", " ").Trim ();

			renderArg.LastTextIsMarkup = renderArg.CurrentTextIsMarkup;
			renderArg.CurrentTextIsMarkup = isMarkup;
		}
		#endregion


		#region Progress Monitor implementation
		public static event EventHandler ProgressBegin, ProgressEnd, ProgressPulse;
		public static event EventHandler<FractionEventArgs> ProgressFraction;

		public sealed class FractionEventArgs : EventArgs
		{
			public double Work { get; private set; }

			public FractionEventArgs (double work)
			{
				this.Work = work;
			}
		}

		static void OnProgressBegin (EventArgs e)
		{
			var handler = ProgressBegin;
			if (handler != null)
				handler (null, e);
		}

		static void OnProgressEnd (EventArgs e)
		{
			var handler = ProgressEnd;
			if (handler != null)
				handler (null, e);
		}

		static void OnProgressPulse (EventArgs e)
		{
			var handler = ProgressPulse;
			if (handler != null)
				handler (null, e);
		}

		static void OnProgressFraction (FractionEventArgs e)
		{
			var handler = ProgressFraction;
			if (handler != null)
				handler (null, e);
		}

		public void BeginProgress (string name)
		{
			ShowMessage (name);
			if (!progressBarVisible) {
				progressBarVisible = true;
				OnProgressBegin (EventArgs.Empty);
			}
		}

		public void SetProgressFraction (double work)
		{
			OnProgressFraction (new FractionEventArgs (work));
		}

		public void EndProgress ()
		{
			if (!progressBarVisible)
				return;

			progressBarVisible = false;
			OnProgressEnd (EventArgs.Empty);
			AutoPulse = false;
		}

		public void Pulse ()
		{
			OnProgressPulse (EventArgs.Empty);
		}

		uint autoPulseTimeoutId;
		public bool AutoPulse {
			get { return autoPulseTimeoutId != 0; }
			set {
				if (value) {
					if (autoPulseTimeoutId == 0) {
						autoPulseTimeoutId = GLib.Timeout.Add (100, delegate {
							Pulse ();
							return true;
						});
					}
				} else {
					if (autoPulseTimeoutId != 0) {
						GLib.Source.Remove (autoPulseTimeoutId);
						autoPulseTimeoutId = 0;
					}
				}
			}
		}

		public void SetCancellationTokenSource (CancellationTokenSource source)
		{
		}
        #endregion
	}

	class StatusAreaSeparator: HBox
	{
        protected override bool OnDrawn(Context ctx)
        {
            var alloc = Allocation;
            //alloc.Inflate (0, -2);
            ctx.Rectangle(alloc.X, alloc.Y, 1, alloc.Height);

            // FIXME: VV: Remove gradient features
            using (Cairo.LinearGradient gr = new LinearGradient(alloc.X, alloc.Y, alloc.X, alloc.Y + alloc.Height))
            {
                gr.AddColorStop(0, new Cairo.Color(0, 0, 0, 0));
                gr.AddColorStop(0.5, new Cairo.Color(0, 0, 0, 0.2));
                gr.AddColorStop(1, new Cairo.Color(0, 0, 0, 0));
                ctx.SetSource(gr);
                ctx.Fill();
            }

            return true;
        }

		protected override void OnGetPreferredWidth(out int minimum_width, out int natural_width)
		{
			minimum_width = natural_width = 1;
		}
	}
}

