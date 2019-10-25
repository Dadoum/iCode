//
// StatusAreaTheme.cs
//
// Author:
//       Jason Smith <jason@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc.
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
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Tasks;
using System.Collections.Generic;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Components;

using StockIcons = MonoDevelop.Ide.Gui.Stock;

namespace MonoDevelop.Components.MainToolbar
{
	internal class StatusAreaTheme : IDisposable
	{
		public bool IsEllipsized {
			get;
			private set;
		}

		SurfaceWrapper backgroundSurface, errorSurface;
		
		public void Dispose ()
		{
			if (backgroundSurface != null)
				backgroundSurface.Dispose ();
			if (errorSurface != null)
				errorSurface.Dispose ();
		}

		public void Render (Cairo.Context context, StatusArea.RenderArg arg, Gtk.Widget widget)
		{
			context.CachedDraw (surface: ref backgroundSurface, 
			                    region: arg.Allocation,
			                    draw: (c, o) => DrawBackground (c, new Gdk.Rectangle (0, 0, arg.Allocation.Width, arg.Allocation.Height)));

			if (arg.BuildAnimationOpacity > 0.001f)
				DrawBuildEffect (context, arg.Allocation, arg.BuildAnimationProgress, arg.BuildAnimationOpacity);

			if (arg.ErrorAnimationProgress > 0.001 && arg.ErrorAnimationProgress < .999) {
				DrawErrorAnimation (context, arg);
			}

			DrawBorder (context, arg.Allocation);

			if (arg.HoverProgress > 0.001f)
			{
				context.Clip ();
				int x1 = arg.Allocation.X + arg.MousePosition.X - 200;
				int x2 = x1 + 400;

				// FIXME: VV: Remove gradient features
				using (Cairo.LinearGradient gradient = new LinearGradient (x1, 0, x2, 0))
				{
					Cairo.Color targetColor = Styles.StatusBarFill1Color.ToCairoColor ();
					Cairo.Color transparentColor = targetColor;
					targetColor.A = .7;
					transparentColor.A = 0;

					targetColor.A = .7 * arg.HoverProgress;

					gradient.AddColorStop (0.0, transparentColor);
					gradient.AddColorStop (0.5, targetColor);
					gradient.AddColorStop (1.0, transparentColor);

					context.SetSource (gradient);

					context.Rectangle (x1, arg.Allocation.Y, x2 - x1, arg.Allocation.Height);
					context.Fill ();
				}
				context.ResetClip ();
			} else {
				context.NewPath ();
			}

			int progress_bar_x = arg.ChildAllocation.X;
			int progress_bar_width = arg.ChildAllocation.Width;

			if (arg.CurrentPixbuf != null) {
				int y = arg.Allocation.Y + (arg.Allocation.Height - (int)arg.CurrentPixbuf.Size.Height) / 2;
				context.DrawImage (widget, arg.CurrentPixbuf, arg.ChildAllocation.X, y);
				progress_bar_x += (int)arg.CurrentPixbuf.Width + Styles.ProgressBarOuterPadding;
				progress_bar_width -= (int)arg.CurrentPixbuf.Width + Styles.ProgressBarOuterPadding;
			}

			int center = arg.Allocation.Y + arg.Allocation.Height / 2;

			Gdk.Rectangle progressArea = new Gdk.Rectangle (progress_bar_x, center - Styles.ProgressBarHeight / 2, progress_bar_width, Styles.ProgressBarHeight);
			if (arg.ShowProgressBar || arg.ProgressBarAlpha > 0) {
				DrawProgressBar (context, arg.ProgressBarFraction, progressArea, arg);
				ClipProgressBar (context, progressArea);
			}

			int text_x = progress_bar_x + Styles.ProgressBarInnerPadding;
			int text_width = progress_bar_width - (Styles.ProgressBarInnerPadding * 2);

			double textTweenValue = arg.TextAnimationProgress;

			if (arg.LastText != null) {
				double opacity = Math.Max (0.0f, 1.0f - textTweenValue);
				DrawString (arg.LastText, arg.LastTextIsMarkup, context, text_x, 
				            center - (int)(textTweenValue * arg.Allocation.Height * 0.3), text_width, opacity, arg.Pango, arg);
			}

			if (arg.CurrentText != null) {
				DrawString (arg.CurrentText, arg.CurrentTextIsMarkup, context, text_x, 
				            center + (int)((1.0f - textTweenValue) * arg.Allocation.Height * 0.3), text_width, Math.Min (textTweenValue, 1.0), arg.Pango, arg);
			}

			if (arg.ShowProgressBar || arg.ProgressBarAlpha > 0)
				context.ResetClip ();
		}

		protected void LayoutRoundedRectangle (Cairo.Context context, Gdk.Rectangle region, int inflateX = 0, int inflateY = 0, float rounding = 3)
		{
			region.Inflate (inflateX, inflateY);
			CairoExtensions.RoundedRectangle (context, region.X + .5, region.Y + .5, region.Width - 1, region.Height - 1, rounding);
		}

		void DrawBuildEffect (Cairo.Context context, Gdk.Rectangle area, double progress, double opacity)
		{
			context.Save ();
			LayoutRoundedRectangle (context, area);
			context.Clip ();

			Gdk.Point center = new Gdk.Point (area.Left + 19, (area.Top + area.Bottom) / 2);
			context.Translate (center.X, center.Y);
			var circles = new [] {
				new { Radius = 200, Thickness = 12, Speed = 1, ArcLength = Math.PI * 1.50 },
				new { Radius = 195, Thickness = 15, Speed = 2, ArcLength = Math.PI * 0.50 },
				new { Radius = 160, Thickness = 17, Speed = 3, ArcLength = Math.PI * 0.75 },
				new { Radius = 200, Thickness = 15, Speed = 2, ArcLength = Math.PI * 0.25 },
				new { Radius = 240, Thickness = 12, Speed = 3, ArcLength = Math.PI * 1.50 },
				new { Radius = 160, Thickness = 17, Speed = 3, ArcLength = Math.PI * 0.75 },
				new { Radius = 200, Thickness = 15, Speed = 2, ArcLength = Math.PI * 0.25 },
				new { Radius = 215, Thickness = 20, Speed = 2, ArcLength = Math.PI * 1.25 }
			};

			double zmod = 1.0d;
			double zporg = progress;
			foreach (var arc in circles) {
				double zoom = 1.0d;
				zoom = (double) Math.Sin (zporg * Math.PI * 2 + zmod);
				zoom = ((zoom + 1) / 6.0d) + .05d;

				context.Rotate (Math.PI * 2 * progress * arc.Speed);
				context.MoveTo (arc.Radius * zoom, 0);
				context.Arc (0, 0, arc.Radius * zoom, 0, arc.ArcLength);
				context.LineWidth = arc.Thickness * zoom;
				context.SetSourceColor (CairoExtensions.ParseColor ("B1DDED", 0.35 * opacity));
				context.Stroke ();
				context.Rotate (Math.PI * 2 * -progress * arc.Speed);

				progress = -progress;

				context.Rotate (Math.PI * 2 * progress * arc.Speed);
				context.MoveTo (arc.Radius * zoom, 0);
				context.Arc (0, 0, arc.Radius * zoom, 0, arc.ArcLength);
				context.LineWidth = arc.Thickness * zoom;
				context.Stroke ();
				context.Rotate (Math.PI * 2 * -progress * arc.Speed);

				progress = -progress;

				zmod += (float)Math.PI / circles.Length;
			}

			context.LineWidth = 1;
			context.ResetClip ();
			context.Restore ();
		}

		protected virtual void DrawBorder (Cairo.Context context, Gdk.Rectangle region)
		{
			LayoutRoundedRectangle (context, region, -1, -1);
			context.LineWidth = 1;
			context.SetSourceColor (Styles.StatusBarInnerColor.ToCairoColor ());
			context.Stroke ();

			LayoutRoundedRectangle (context, region);
			context.LineWidth = 1;
			context.SetSourceColor (Styles.StatusBarBorderColor.ToCairoColor ());
			context.StrokePreserve ();
		}

		protected virtual void DrawBackground (Cairo.Context context, Gdk.Rectangle region)
		{	
			LayoutRoundedRectangle (context, region);
			context.ClipPreserve ();

			using (LinearGradient lg = new LinearGradient (region.X, region.Y, region.X, region.Y + region.Height)) {
				lg.AddColorStop (0, Styles.StatusBarFill1Color.ToCairoColor ());
				lg.AddColorStop (1, Styles.StatusBarFill4Color.ToCairoColor ());

				context.SetSource (lg);
				context.FillPreserve ();
			}

			context.Save ();
			double midX = region.X + region.Width / 2.0;
			double midY = region.Y + region.Height;
			context.Translate (midX, midY);

			using (RadialGradient rg = new RadialGradient (0, 0, 0, 0, 0, region.Height * 1.2)) {
				rg.AddColorStop (0, Styles.StatusBarFill1Color.ToCairoColor ());
				rg.AddColorStop (1, Styles.StatusBarFill1Color.WithAlpha (0).ToCairoColor ());

				context.Scale (region.Width / (double)region.Height, 1.0);
				context.SetSource (rg);
				context.Fill ();
			}
			context.Restore ();

			using (LinearGradient lg = new LinearGradient (0, region.Y, 0, region.Y + region.Height)) {
				lg.AddColorStop (0, Styles.StatusBarShadowColor1.ToCairoColor ());
				lg.AddColorStop (1, Styles.StatusBarShadowColor1.WithAlpha (Styles.StatusBarShadowColor1.Alpha * 0.2).ToCairoColor ());

				LayoutRoundedRectangle (context, region, 0, -1);
				context.LineWidth = 1;
				context.SetSource (lg);
				context.Stroke ();
			}

			using (LinearGradient lg = new LinearGradient (0, region.Y, 0, region.Y + region.Height)) {
				lg.AddColorStop (0, Styles.StatusBarShadowColor2.ToCairoColor ());
				lg.AddColorStop (1, Styles.StatusBarShadowColor2.WithAlpha (Styles.StatusBarShadowColor2.Alpha * 0.2).ToCairoColor ());

				LayoutRoundedRectangle (context, region, 0, -2);
				context.LineWidth = 1;
				context.SetSource (lg);
				context.Stroke ();
			}

			context.ResetClip ();
		}

		void DrawErrorAnimation (Cairo.Context context, StatusArea.RenderArg arg)
		{
			const int surfaceWidth = 2000;
			double opacity;
			int progress;

			if (arg.ErrorAnimationProgress < .5f) {
				progress = (int) (arg.ErrorAnimationProgress * arg.Allocation.Width * 2.4);
				opacity = 1.0d;
			} else {
				progress = (int) (arg.ErrorAnimationProgress * arg.Allocation.Width * 2.4);
				opacity = 1.0d - (arg.ErrorAnimationProgress - .5d) * 2;
			}

			LayoutRoundedRectangle (context, arg.Allocation);

			context.Clip ();
			context.CachedDraw (surface: ref errorSurface,
			                    position: new Gdk.Point (arg.Allocation.X - surfaceWidth + progress, arg.Allocation.Y),
			                    size: new Gdk.Size (surfaceWidth, arg.Allocation.Height),
			                    opacity: (float)opacity,
			                    draw: (c, o) => {
				// The smaller the pixel range of our gradient the less error there will be in it.
				using (var lg = new LinearGradient (surfaceWidth - 250, 0, surfaceWidth, 0)) {
					lg.AddColorStop (0.00, Styles.StatusBarErrorColor.WithAlpha (0.15 * o).ToCairoColor ());
					lg.AddColorStop (0.10, Styles.StatusBarErrorColor.WithAlpha (0.15 * o).ToCairoColor ());
					lg.AddColorStop (0.88, Styles.StatusBarErrorColor.WithAlpha (0.30 * o).ToCairoColor ());
					lg.AddColorStop (1.00, Styles.StatusBarErrorColor.WithAlpha (0.00 * o).ToCairoColor ());

					c.SetSource (lg);
					c.Paint ();
				}
			});
			context.ResetClip ();
		}

		void DrawProgressBar (Cairo.Context context, double progress, Gdk.Rectangle bounding, StatusArea.RenderArg arg)
		{
			LayoutRoundedRectangle (context, new Gdk.Rectangle (bounding.X, bounding.Y, (int) (bounding.Width * progress), bounding.Height), 0, 0, 1);
			context.Clip ();

			LayoutRoundedRectangle (context, bounding, 0, 0, 1);
			context.SetSourceColor (Styles.StatusBarProgressBackgroundColor.WithAlpha (Styles.StatusBarProgressBackgroundColor.Alpha * arg.ProgressBarAlpha).ToCairoColor ());
			context.FillPreserve ();

			context.ResetClip ();

			context.SetSourceColor (Styles.StatusBarProgressOutlineColor.WithAlpha (Styles.StatusBarProgressOutlineColor.Alpha * arg.ProgressBarAlpha).ToCairoColor ());
			context.LineWidth = 1;
			context.Stroke ();
		}

		void ClipProgressBar (Cairo.Context context, Gdk.Rectangle bounding)
		{
			LayoutRoundedRectangle (context, bounding);
			context.Clip ();
		}

		protected virtual Cairo.Color FontColor ()
		{
			return Styles.StatusBarTextColor.ToCairoColor ();
		}

		void DrawString (string text, bool isMarkup, Cairo.Context context, int x, int y, int width, double opacity, Pango.Context pango, StatusArea.RenderArg arg)
		{
			Pango.Layout pl = new Pango.Layout (pango);
			if (isMarkup)
				pl.SetMarkup (text);
			else
				pl.SetText (text);
			pl.FontDescription = Styles.StatusFont;
			pl.FontDescription.AbsoluteSize = Pango.Units.FromPixels (Styles.StatusFontPixelHeight);
			pl.Ellipsize = Pango.EllipsizeMode.End;
			pl.Width = Pango.Units.FromPixels(width);

			int w, h;
			pl.GetPixelSize (out w, out h);

			context.Save ();
			// use widget height instead of message box height as message box does not have a true height when no widgets are packed in it
			// also ensures animations work properly instead of getting clipped
			context.Rectangle (new Rectangle (x, arg.Allocation.Y, width, arg.Allocation.Height));
			context.Clip ();

			// Subtract off remainder instead of drop to prefer higher centering when centering an odd number of pixels
			context.MoveTo (x, y - h / 2 - (h % 2));
			context.SetSourceColor (CairoExtensions.WithAlpha (FontColor (), opacity));

			Pango.CairoHelper.ShowLayout (context, pl);

			IsEllipsized = pl.IsEllipsized;

			pl.Dispose ();
			context.Restore ();
		}
	}
}

