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
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MonoDevelop.Components.MainToolbar
{
    internal class StatusAreaTheme : IDisposable
    {
        public bool IsEllipsized
        {
            get;
            private set;
        }

        SurfaceWrapper backgroundSurface, errorSurface;

        public void Dispose()
        {
            if (backgroundSurface != null)
                backgroundSurface.Dispose();
            if (errorSurface != null)
                errorSurface.Dispose();
        }

        public void Render(Cairo.Context context, StatusArea.RenderArg arg, Gtk.Widget widget)
        {
            if (arg.BuildAnimationOpacity > 0.001f)
                DrawBuildEffect(context, arg.Allocation, arg.BuildAnimationProgress, arg.BuildAnimationOpacity);

            if (arg.ErrorAnimationProgress > 0.001 && arg.ErrorAnimationProgress < .999)
            {
                DrawErrorAnimation(context, arg);
            }

            DrawBorder(context, arg.Allocation);

            if (arg.HoverProgress > 0.001f)
            {
                context.Clip();
                int x1 = arg.Allocation.X + arg.MousePosition.X - 200;
                int x2 = x1 + 400;

                // FIXME: VV: Remove gradient features
                using (Cairo.LinearGradient gradient = new LinearGradient(x1, 0, x2, 0))
                {
                    Cairo.Color targetColor = new Color();
                    targetColor.R = 230;
                    targetColor.G = 230;
                    targetColor.B = 230;

                    Cairo.Color transparentColor = targetColor;
                    targetColor.A = .7;
                    transparentColor.A = 0;

                    targetColor.A = .7 * arg.HoverProgress;

                    gradient.AddColorStop(0.0, transparentColor);
                    gradient.AddColorStop(0.5, targetColor);
                    gradient.AddColorStop(1.0, transparentColor);

                    context.SetSource(gradient);

                    context.Rectangle(x1, arg.Allocation.Y, x2 - x1, arg.Allocation.Height);
                    context.Fill();
                }
                context.ResetClip();
            }
            else
            {
                context.NewPath();
            }

            int progress_bar_x = arg.ChildAllocation.X;
            int progress_bar_width = arg.ChildAllocation.Width;

            if (arg.CurrentPixbuf != null)
            {
                int y = arg.Allocation.Y + (arg.Allocation.Height - (int)arg.CurrentPixbuf.Size.Height) / 2;
                progress_bar_x += (int)arg.CurrentPixbuf.Width + 1;
                progress_bar_width -= (int)arg.CurrentPixbuf.Width + 1;
            }

            int center = arg.Allocation.Y + arg.Allocation.Height / 2;

            Gdk.Rectangle progressArea = new Gdk.Rectangle(progress_bar_x, center - 2 / 2, progress_bar_width, 2);
            if (arg.ShowProgressBar || arg.ProgressBarAlpha > 0)
            {
                DrawProgressBar(context, arg.ProgressBarFraction, progressArea, arg);
                ClipProgressBar(context, progressArea);
            }

            int text_x = progress_bar_x + 1;
            int text_width = progress_bar_width - (1 * 2);

            double textTweenValue = arg.TextAnimationProgress;

            if (arg.LastText != null)
            {
                double opacity = Math.Max(0.0f, 1.0f - textTweenValue);
                DrawString(arg.LastText, arg.LastTextIsMarkup, context, text_x,
                            center - (int)(textTweenValue * arg.Allocation.Height * 0.3), text_width, opacity, arg.Pango, arg);
            }

            if (arg.CurrentText != null)
            {
                DrawString(arg.CurrentText, arg.CurrentTextIsMarkup, context, text_x,
                            center + (int)((1.0f - textTweenValue) * arg.Allocation.Height * 0.3), text_width, Math.Min(textTweenValue, 1.0), arg.Pango, arg);
            }

            if (arg.ShowProgressBar || arg.ProgressBarAlpha > 0)
                context.ResetClip();
        }

        protected void LayoutRoundedRectangle(Cairo.Context context, Gdk.Rectangle region, int inflateX = 0, int inflateY = 0, float rounding = 3)
        {
            region.Inflate(inflateX, inflateY);
            CairoExtensions.RoundedRectangle(context, region.X + .5, region.Y + .5, region.Width - 1, region.Height - 1, rounding);
        }

        void DrawBuildEffect(Cairo.Context context, Gdk.Rectangle area, double progress, double opacity)
        {
            context.Save();
            LayoutRoundedRectangle(context, area);
            context.Clip();

            Gdk.Point center = new Gdk.Point(area.Left + 19, (area.Top + area.Bottom) / 2);
            context.Translate(center.X, center.Y);
            var circles = new[] {
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
            foreach (var arc in circles)
            {
                double zoom = 1.0d;
                zoom = (double)Math.Sin(zporg * Math.PI * 2 + zmod);
                zoom = ((zoom + 1) / 6.0d) + .05d;

                context.Rotate(Math.PI * 2 * progress * arc.Speed);
                context.MoveTo(arc.Radius * zoom, 0);
                context.Arc(0, 0, arc.Radius * zoom, 0, arc.ArcLength);
                context.LineWidth = arc.Thickness * zoom;
                context.SetSourceColor(CairoExtensions.ParseColor("B1DDED", 0.35 * opacity));
                context.Stroke();
                context.Rotate(Math.PI * 2 * -progress * arc.Speed);

                progress = -progress;

                context.Rotate(Math.PI * 2 * progress * arc.Speed);
                context.MoveTo(arc.Radius * zoom, 0);
                context.Arc(0, 0, arc.Radius * zoom, 0, arc.ArcLength);
                context.LineWidth = arc.Thickness * zoom;
                context.Stroke();
                context.Rotate(Math.PI * 2 * -progress * arc.Speed);

                progress = -progress;

                zmod += (float)Math.PI / circles.Length;
            }

            context.LineWidth = 1;
            context.ResetClip();
            context.Restore();
        }

        protected virtual void DrawBorder(Cairo.Context context, Gdk.Rectangle region)
        {
            LayoutRoundedRectangle(context, region, -1, -1);
            context.LineWidth = 1;
            context.SetSourceColor(Styles.StatusBarInnerColor.ToCairoColor());
            context.Stroke();

            LayoutRoundedRectangle(context, region);
            context.LineWidth = 1;
            context.SetSourceColor(Styles.StatusBarBorderColor.ToCairoColor());
            context.StrokePreserve();
        }

        protected virtual void DrawBackground(Cairo.Context context, Gdk.Rectangle region)
        {
            LayoutRoundedRectangle(context, region);
            context.ClipPreserve();

            using (LinearGradient lg = new LinearGradient(region.X, region.Y, region.X, region.Y + region.Height))
            {
                lg.AddColorStop(0, Styles.StatusBarFill1Color.ToCairoColor());
                lg.AddColorStop(1, Styles.StatusBarFill4Color.ToCairoColor());

                context.SetSource(lg);
                context.FillPreserve();
            }

            context.Save();
            double midX = region.X + region.Width / 2.0;
            double midY = region.Y + region.Height;
            context.Translate(midX, midY);

            using (RadialGradient rg = new RadialGradient(0, 0, 0, 0, 0, region.Height * 1.2))
            {
                rg.AddColorStop(0, Styles.StatusBarFill1Color.ToCairoColor());
                rg.AddColorStop(1, Styles.StatusBarFill1Color.WithAlpha(0).ToCairoColor());

                context.Scale(region.Width / (double)region.Height, 1.0);
                context.SetSource(rg);
                context.Fill();
            }
            context.Restore();

            using (LinearGradient lg = new LinearGradient(0, region.Y, 0, region.Y + region.Height))
            {
                lg.AddColorStop(0, Styles.StatusBarShadowColor1.ToCairoColor());
                lg.AddColorStop(1, Styles.StatusBarShadowColor1.WithAlpha(Styles.StatusBarShadowColor1.Alpha * 0.2).ToCairoColor());

                LayoutRoundedRectangle(context, region, 0, -1);
                context.LineWidth = 1;
                context.SetSource(lg);
                context.Stroke();
            }

            using (LinearGradient lg = new LinearGradient(0, region.Y, 0, region.Y + region.Height))
            {
                lg.AddColorStop(0, Styles.StatusBarShadowColor2.ToCairoColor());
                lg.AddColorStop(1, Styles.StatusBarShadowColor2.WithAlpha(Styles.StatusBarShadowColor2.Alpha * 0.2).ToCairoColor());

                LayoutRoundedRectangle(context, region, 0, -2);
                context.LineWidth = 1;
                context.SetSource(lg);
                context.Stroke();
            }

            context.ResetClip();
        }

        void DrawErrorAnimation(Cairo.Context context, StatusArea.RenderArg arg)
        {
            const int surfaceWidth = 2000;
            double opacity;
            int progress;

            if (arg.ErrorAnimationProgress < .5f)
            {
                progress = (int)(arg.ErrorAnimationProgress * arg.Allocation.Width * 2.4);
                opacity = 1.0d;
            }
            else
            {
                progress = (int)(arg.ErrorAnimationProgress * arg.Allocation.Width * 2.4);
                opacity = 1.0d - (arg.ErrorAnimationProgress - .5d) * 2;
            }

            LayoutRoundedRectangle(context, arg.Allocation);

            context.Clip();
            context.CachedDraw(surface: ref errorSurface,
                                position: new Gdk.Point(arg.Allocation.X - surfaceWidth + progress, arg.Allocation.Y),
                                size: new Gdk.Size(surfaceWidth, arg.Allocation.Height),
                                opacity: (float)opacity,
                                draw: (c, o) =>
                                {
                                    // The smaller the pixel range of our gradient the less error there will be in it.
                                    using (var lg = new LinearGradient(surfaceWidth - 250, 0, surfaceWidth, 0))
                                    {
                                        lg.AddColorStop(0.00, Styles.StatusBarErrorColor.WithAlpha(0.15 * o).ToCairoColor());
                                        lg.AddColorStop(0.10, Styles.StatusBarErrorColor.WithAlpha(0.15 * o).ToCairoColor());
                                        lg.AddColorStop(0.88, Styles.StatusBarErrorColor.WithAlpha(0.30 * o).ToCairoColor());
                                        lg.AddColorStop(1.00, Styles.StatusBarErrorColor.WithAlpha(0.00 * o).ToCairoColor());

                                        c.SetSource(lg);
                                        c.Paint();
                                    }
                                });
            context.ResetClip();
        }

        void DrawProgressBar(Cairo.Context context, double progress, Gdk.Rectangle bounding, StatusArea.RenderArg arg)
        {
            LayoutRoundedRectangle(context, new Gdk.Rectangle(bounding.X, bounding.Y, (int)(bounding.Width * progress), bounding.Height), 0, 0, 1);
            context.Clip();

            LayoutRoundedRectangle(context, bounding, 0, 0, 1);
            context.SetSourceColor(Styles.StatusBarProgressBackgroundColor.WithAlpha(Styles.StatusBarProgressBackgroundColor.Alpha * arg.ProgressBarAlpha).ToCairoColor());
            context.FillPreserve();

            context.ResetClip();

            context.SetSourceColor(Styles.StatusBarProgressOutlineColor.WithAlpha(Styles.StatusBarProgressOutlineColor.Alpha * arg.ProgressBarAlpha).ToCairoColor());
            context.LineWidth = 1;
            context.Stroke();
        }

        void ClipProgressBar(Cairo.Context context, Gdk.Rectangle bounding)
        {
            LayoutRoundedRectangle(context, bounding);
            context.Clip();
        }

        protected virtual Cairo.Color FontColor()
        {
            return Styles.StatusBarTextColor.ToCairoColor();
        }

        void DrawString(string text, bool isMarkup, Cairo.Context context, int x, int y, int width, double opacity, Pango.Context pango, StatusArea.RenderArg arg)
        {
            Pango.Layout pl = new Pango.Layout(pango);
            if (isMarkup)
                pl.SetMarkup(text);
            else
                pl.SetText(text);
            pl.FontDescription.AbsoluteSize = Pango.Units.FromPixels(12);
            pl.Ellipsize = Pango.EllipsizeMode.End;
            pl.Width = Pango.Units.FromPixels(width);

            int w, h;
            pl.GetPixelSize(out w, out h);

            context.Save();
            // use widget height instead of message box height as message box does not have a true height when no widgets are packed in it
            // also ensures animations work properly instead of getting clipped
            context.Rectangle(new Rectangle(x, arg.Allocation.Y, width, arg.Allocation.Height));
            context.Clip();

            // Subtract off remainder instead of drop to prefer higher centering when centering an odd number of pixels
            context.MoveTo(x, y - h / 2 - (h % 2));
            context.SetSourceColor(CairoExtensions.WithAlpha(FontColor(), opacity));

            Pango.CairoHelper.ShowLayout(context, pl);

            IsEllipsized = pl.IsEllipsized;

            pl.Dispose();
            context.Restore();
        }
    }

    public class SurfaceWrapper : IDisposable
    {
        public Cairo.Surface Surface { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public object Data { get; set; }
        public bool IsDisposed { get; private set; }

        public SurfaceWrapper(Cairo.Context similar, int width, int height)
        {
            Surface = new ImageSurface(Cairo.Format.ARGB32, width, height);

            Width = width;
            Height = height;
        }

        public SurfaceWrapper(Cairo.Context similar, Gdk.Pixbuf source)
        {
            Cairo.Surface surface;
            surface = new ImageSurface(Format.ARGB32, source.Width, source.Height);


            using (Context context = new Context(surface))
            {
                Gdk.CairoHelper.SetSourcePixbuf(context, source, 0, 0);
                context.Paint();
            }

            Surface = surface;
            Width = source.Width;
            Height = source.Height;
        }

        public void Dispose()
        {
            IsDisposed = true;
            if (Surface != null)
            {
                ((IDisposable)Surface).Dispose();
            }
        }
    }
    public static class CairoExtensions
    {
        internal const string LIBCAIRO = "libcairo-2.dll";
        public static Cairo.Rectangle ToCairoRect(this Gdk.Rectangle rect)
        {
            return new Cairo.Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
        }

        public static Surface CreateSurfaceForPixbuf(Context cr, Pixbuf pixbuf)
        {
            Surface surface;
            using (var t = cr.GetTarget())
            {
                surface = t.CreateSimilar(t.Content, pixbuf.Width, pixbuf.Height);
            }
            using (Context surface_cr = new Context(surface))
            {
                CairoHelper.SetSourcePixbuf(surface_cr, pixbuf, 0, 0);
                surface_cr.Paint();
                surface_cr.Dispose();
            }
            return surface;
        }

        public static Cairo.Color AlphaBlend(Cairo.Color ca, Cairo.Color cb, double alpha)
        {
            return new Cairo.Color(
                (1.0 - alpha) * ca.R + alpha * cb.R,
                (1.0 - alpha) * ca.G + alpha * cb.G,
                (1.0 - alpha) * ca.B + alpha * cb.B);
        }
        public static Gdk.Color CairoColorToGdkColor(Cairo.Color color)
        {
            return new Gdk.Color((byte)(color.R * 255), (byte)(color.G * 255), (byte)(color.B * 255));
        }

        public static Cairo.Color GdkColorToCairoColor(Gdk.Color color)
        {
            return GdkColorToCairoColor(color, 1.0);
        }

        public static Cairo.Color GdkColorToCairoColor(Gdk.Color color, double alpha)
        {
            return new Cairo.Color(
                (double)(color.Red >> 8) / 255.0,
                (double)(color.Green >> 8) / 255.0,
                (double)(color.Blue >> 8) / 255.0,
                alpha);
        }

        public static Cairo.Color RgbToColor(uint rgbColor)
        {
            return RgbaToColor((rgbColor << 8) | 0x000000ff);
        }

        public static Cairo.Color RgbaToColor(uint rgbaColor)
        {
            return new Cairo.Color(
                (byte)(rgbaColor >> 24) / 255.0,
                (byte)(rgbaColor >> 16) / 255.0,
                (byte)(rgbaColor >> 8) / 255.0,
                (byte)(rgbaColor & 0x000000ff) / 255.0);
        }

        public static Cairo.Color InterpolateColors(Cairo.Color start, Cairo.Color end, float amount)
        {
            return new Cairo.Color(start.R + (end.R - start.R) * amount,
                                    start.G + (end.G - start.G) * amount,
                                    start.B + (end.B - start.B) * amount,
                                    start.A + (end.A - start.A) * amount);
        }

        public static bool ColorIsDark(Cairo.Color color)
        {
            double h, s, b;
            HsbFromColor(color, out h, out s, out b);
            return b < 0.5;
        }

        public static void HsbFromColor(Cairo.Color color, out double hue,
            out double saturation, out double brightness)
        {
            double min, max, delta;
            double red = color.R;
            double green = color.G;
            double blue = color.B;

            hue = 0;
            saturation = 0;
            brightness = 0;

            if (red > green)
            {
                max = Math.Max(red, blue);
                min = Math.Min(green, blue);
            }
            else
            {
                max = Math.Max(green, blue);
                min = Math.Min(red, blue);
            }

            brightness = (max + min) / 2;

            if (Math.Abs(max - min) < 0.0001)
            {
                hue = 0;
                saturation = 0;
            }
            else
            {
                saturation = brightness <= 0.5
                    ? (max - min) / (max + min)
                    : (max - min) / (2 - max - min);

                delta = max - min;

                if (red == max)
                {
                    hue = (green - blue) / delta;
                }
                else if (green == max)
                {
                    hue = 2 + (blue - red) / delta;
                }
                else if (blue == max)
                {
                    hue = 4 + (red - green) / delta;
                }

                hue *= 60;
                if (hue < 0)
                {
                    hue += 360;
                }
            }
        }

        private static double Modula(double number, double divisor)
        {
            return ((int)number % divisor) + (number - (int)number);
        }

        public static Cairo.Color ColorFromHsb(double hue, double saturation, double brightness)
        {
            int i;
            double[] hue_shift = { 0, 0, 0 };
            double[] color_shift = { 0, 0, 0 };
            double m1, m2, m3;

            m2 = brightness <= 0.5
                ? brightness * (1 + saturation)
                : brightness + saturation - brightness * saturation;

            m1 = 2 * brightness - m2;

            hue_shift[0] = hue + 120;
            hue_shift[1] = hue;
            hue_shift[2] = hue - 120;

            color_shift[0] = color_shift[1] = color_shift[2] = brightness;

            i = saturation == 0 ? 3 : 0;

            for (; i < 3; i++)
            {
                m3 = hue_shift[i];

                if (m3 > 360)
                {
                    m3 = Modula(m3, 360);
                }
                else if (m3 < 0)
                {
                    m3 = 360 - Modula(Math.Abs(m3), 360);
                }

                if (m3 < 60)
                {
                    color_shift[i] = m1 + (m2 - m1) * m3 / 60;
                }
                else if (m3 < 180)
                {
                    color_shift[i] = m2;
                }
                else if (m3 < 240)
                {
                    color_shift[i] = m1 + (m2 - m1) * (240 - m3) / 60;
                }
                else
                {
                    color_shift[i] = m1;
                }
            }

            return new Cairo.Color(color_shift[0], color_shift[1], color_shift[2]);
        }

        public static Cairo.Color ColorShade(Cairo.Color @base, double ratio)
        {
            double h, s, b;

            HsbFromColor(@base, out h, out s, out b);

            b = Math.Max(Math.Min(b * ratio, 1), 0);
            s = Math.Max(Math.Min(s * ratio, 1), 0);

            Cairo.Color color = ColorFromHsb(h, s, b);
            color.A = @base.A;
            return color;
        }

        public static Cairo.Color ColorAdjustBrightness(Cairo.Color @base, double br)
        {
            double h, s, b;
            HsbFromColor(@base, out h, out s, out b);
            b = Math.Max(Math.Min(br, 1), 0);
            return ColorFromHsb(h, s, b);
        }

        public static string ColorGetHex(Cairo.Color color, bool withAlpha = false)
        {
            if (withAlpha)
            {
                return String.Format("#{0:x2}{1:x2}{2:x2}{3:x2}", (byte)(color.R * 255), (byte)(color.G * 255),
                    (byte)(color.B * 255), (byte)(color.A * 255));
            }
            else
            {
                return String.Format("#{0:x2}{1:x2}{2:x2}", (byte)(color.R * 255), (byte)(color.G * 255),
                    (byte)(color.B * 255));
            }
        }

        public static void RoundedRectangle(this Cairo.Context cr, double x, double y, double w, double h, double r)
        {
            RoundedRectangle(cr, x, y, w, h, r, CairoCorners.All, false);
        }

        public static void RoundedRectangle(this Cairo.Context cr, double x, double y, double w, double h,
            double r, CairoCorners corners)
        {
            RoundedRectangle(cr, x, y, w, h, r, corners, false);
        }

        public static void RoundedRectangle(this Cairo.Context cr, double x, double y, double w, double h,
            double r, CairoCorners corners, bool topBottomFallsThrough)
        {
            if (topBottomFallsThrough && corners == CairoCorners.None)
            {
                cr.MoveTo(x, y - r);
                cr.LineTo(x, y + h + r);
                cr.MoveTo(x + w, y - r);
                cr.LineTo(x + w, y + h + r);
                return;
            }
            else if (r < 0.0001 || corners == CairoCorners.None)
            {
                cr.Rectangle(x, y, w, h);
                return;
            }

            if ((corners & (CairoCorners.TopLeft | CairoCorners.TopRight)) == 0 && topBottomFallsThrough)
            {
                y -= r;
                h += r;
                cr.MoveTo(x + w, y);
            }
            else
            {
                if ((corners & CairoCorners.TopLeft) != 0)
                {
                    cr.MoveTo(x + r, y);
                }
                else
                {
                    cr.MoveTo(x, y);
                }

                if ((corners & CairoCorners.TopRight) != 0)
                {
                    cr.Arc(x + w - r, y + r, r, Math.PI * 1.5, Math.PI * 2);
                }
                else
                {
                    cr.LineTo(x + w, y);
                }
            }

            if ((corners & (CairoCorners.BottomLeft | CairoCorners.BottomRight)) == 0 && topBottomFallsThrough)
            {
                h += r;
                cr.LineTo(x + w, y + h);
                cr.MoveTo(x, y + h);
                cr.LineTo(x, y + r);
                cr.Arc(x + r, y + r, r, Math.PI, Math.PI * 1.5);
            }
            else
            {
                if ((corners & CairoCorners.BottomRight) != 0)
                {
                    cr.Arc(x + w - r, y + h - r, r, 0, Math.PI * 0.5);
                }
                else
                {
                    cr.LineTo(x + w, y + h);
                }

                if ((corners & CairoCorners.BottomLeft) != 0)
                {
                    cr.Arc(x + r, y + h - r, r, Math.PI * 0.5, Math.PI);
                }
                else
                {
                    cr.LineTo(x, y + h);
                }

                if ((corners & CairoCorners.TopLeft) != 0)
                {
                    cr.Arc(x + r, y + r, r, Math.PI, Math.PI * 1.5);
                }
                else
                {
                    cr.LineTo(x, y);
                }
            }
        }

        static void ShadowGradient(Cairo.Gradient lg, double strength)
        {
            lg.AddColorStop(0, new Cairo.Color(0, 0, 0, strength));
            lg.AddColorStop(1.0 / 6.0, new Cairo.Color(0, 0, 0, .85 * strength));
            lg.AddColorStop(2.0 / 6.0, new Cairo.Color(0, 0, 0, .54 * strength));
            lg.AddColorStop(3.0 / 6.0, new Cairo.Color(0, 0, 0, .24 * strength));
            lg.AddColorStop(4.0 / 6.0, new Cairo.Color(0, 0, 0, .07 * strength));
            lg.AddColorStop(5.0 / 6.0, new Cairo.Color(0, 0, 0, .01 * strength));
            lg.AddColorStop(1, new Cairo.Color(0, 0, 0, 0));
        }

        // VERY SLOW, only use on cached renders
        public static void RenderOuterShadow(this Cairo.Context self, Gdk.Rectangle area, int size, int rounding, double strength)
        {
            area.Inflate(-1, -1);
            size++;

            int doubleRounding = rounding * 2;
            // left side
            self.Rectangle(area.X - size, area.Y + rounding, size, area.Height - doubleRounding - 1);
            using (var lg = new LinearGradient(area.X, 0, area.X - size, 0))
            {
                ShadowGradient(lg, strength);
                self.SetSource(lg);
                self.Fill();
            }

            // right side
            self.Rectangle(area.Right, area.Y + rounding, size, area.Height - doubleRounding - 1);
            using (var lg = new LinearGradient(area.Right, 0, area.Right + size, 0))
            {
                ShadowGradient(lg, strength);
                self.SetSource(lg);
                self.Fill();
            }

            // top side
            self.Rectangle(area.X + rounding, area.Y - size, area.Width - doubleRounding - 1, size);
            using (var lg = new LinearGradient(0, area.Y, 0, area.Y - size))
            {
                ShadowGradient(lg, strength);
                self.SetSource(lg);
                self.Fill();
            }

            // bottom side
            self.Rectangle(area.X + rounding, area.Bottom, area.Width - doubleRounding - 1, size);
            using (var lg = new LinearGradient(0, area.Bottom, 0, area.Bottom + size))
            {
                ShadowGradient(lg, strength);
                self.SetSource(lg);
                self.Fill();
            }

            // top left corner
            self.Rectangle(area.X - size, area.Y - size, size + rounding, size + rounding);
            using (var rg = new RadialGradient(area.X + rounding, area.Y + rounding, rounding, area.X + rounding, area.Y + rounding, size + rounding))
            {
                ShadowGradient(rg, strength);
                self.SetSource(rg);
                self.Fill();
            }

            // top right corner
            self.Rectangle(area.Right - rounding, area.Y - size, size + rounding, size + rounding);
            using (var rg = new RadialGradient(area.Right - rounding, area.Y + rounding, rounding, area.Right - rounding, area.Y + rounding, size + rounding))
            {
                ShadowGradient(rg, strength);
                self.SetSource(rg);
                self.Fill();
            }

            // bottom left corner
            self.Rectangle(area.X - size, area.Bottom - rounding, size + rounding, size + rounding);
            using (var rg = new RadialGradient(area.X + rounding, area.Bottom - rounding, rounding, area.X + rounding, area.Bottom - rounding, size + rounding))
            {
                ShadowGradient(rg, strength);
                self.SetSource(rg);
                self.Fill();
            }

            // bottom right corner
            self.Rectangle(area.Right - rounding, area.Bottom - rounding, size + rounding, size + rounding);
            using (var rg = new RadialGradient(area.Right - rounding, area.Bottom - rounding, rounding, area.Right - rounding, area.Bottom - rounding, size + rounding))
            {
                ShadowGradient(rg, strength);
                self.SetSource(rg);
                self.Fill();
            }
        }

        [DllImport(LIBCAIRO, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr cairo_pattern_set_extend(IntPtr pattern, CairoExtend extend);

        [DllImport(LIBCAIRO, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr cairo_get_source(IntPtr cr);

        enum CairoExtend
        {
            CAIRO_EXTEND_NONE,
            CAIRO_EXTEND_REPEAT,
            CAIRO_EXTEND_REFLECT,
            CAIRO_EXTEND_PAD
        }

        public static void RenderTiled(this Cairo.Context self, Gtk.Widget target, Xwt.Drawing.Image source, Gdk.Rectangle area, Gdk.Rectangle clip, double opacity = 1)
        {
            var ctx = Xwt.Toolkit.CurrentEngine.WrapContext(target, self);
            ctx.Save();
            ctx.Rectangle(clip.X, clip.Y, clip.Width, clip.Height);
            ctx.Clip();
            ctx.Pattern = new Xwt.Drawing.ImagePattern(source);
            ctx.Rectangle(area.X, area.Y, area.Width, area.Height);
            ctx.Fill();
            ctx.Restore();
        }

        public static void DisposeContext(Cairo.Context cr)
        {
            cr.Dispose();
        }

        private struct CairoInteropCall
        {
            public string Name;
            public MethodInfo ManagedMethod;
            public bool CallNative;

            public CairoInteropCall(string name)
            {
                Name = name;
                ManagedMethod = null;
                CallNative = false;
            }
        }

        private static bool CallCairoMethod(Cairo.Context cr, ref CairoInteropCall call)
        {
            if (call.ManagedMethod == null && !call.CallNative)
            {
                MemberInfo[] members = typeof(Cairo.Context).GetMember(call.Name, MemberTypes.Method,
                    BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public);

                if (members != null && members.Length > 0 && members[0] is MethodInfo)
                {
                    call.ManagedMethod = (MethodInfo)members[0];
                }
                else
                {
                    call.CallNative = true;
                }
            }

            if (call.ManagedMethod != null)
            {
                call.ManagedMethod.Invoke(cr, null);
                return true;
            }

            return false;
        }

        private static bool native_push_pop_exists = true;

        [DllImport(LIBCAIRO, CallingConvention = CallingConvention.Cdecl)]
        private static extern void cairo_push_group(IntPtr ptr);
        private static CairoInteropCall cairo_push_group_call = new CairoInteropCall("PushGroup");

        public static void PushGroup(Cairo.Context cr)
        {
            if (!native_push_pop_exists)
            {
                return;
            }

            try
            {
                if (!CallCairoMethod(cr, ref cairo_push_group_call))
                {
                    cairo_push_group(cr.Handle);
                }
            }
            catch
            {
                native_push_pop_exists = false;
            }
        }

        [DllImport(LIBCAIRO, CallingConvention = CallingConvention.Cdecl)]
        private static extern void cairo_pop_group_to_source(IntPtr ptr);
        private static CairoInteropCall cairo_pop_group_to_source_call = new CairoInteropCall("PopGroupToSource");

        public static void PopGroupToSource(Cairo.Context cr)
        {
            if (!native_push_pop_exists)
            {
                return;
            }

            try
            {
                if (!CallCairoMethod(cr, ref cairo_pop_group_to_source_call))
                {
                    cairo_pop_group_to_source(cr.Handle);
                }
            }
            catch (EntryPointNotFoundException)
            {
                native_push_pop_exists = false;
            }
        }

        public static Cairo.Color ParseColor(string s, double alpha = 1)
        {
            if (s.StartsWith("#"))
                s = s.Substring(1);
            if (s.Length == 3)
                s = "" + s[0] + s[0] + s[1] + s[1] + s[2] + s[2];
            double r = ((double)int.Parse(s.Substring(0, 2), System.Globalization.NumberStyles.HexNumber)) / 255;
            double g = ((double)int.Parse(s.Substring(2, 2), System.Globalization.NumberStyles.HexNumber)) / 255;
            double b = ((double)int.Parse(s.Substring(4, 2), System.Globalization.NumberStyles.HexNumber)) / 255;
            return new Cairo.Color(r, g, b, alpha);
        }

        public static ImageSurface LoadImage(Assembly assembly, string resource)
        {
            byte[] buffer;
            using (var stream = assembly.GetManifestResourceStream(resource))
            {
                buffer = new byte[stream.Length];
                stream.Read(buffer, 0, (int)stream.Length);
            }
            /* This should work, but doesn't:
                        using (var px = new Gdk.Pixbuf (buffer)) 
                            return new ImageSurface (px.Pixels, Format.Argb32, px.Width, px.Height, px.Rowstride);*/

            // Workaround: loading from file name.
            var tmp = System.IO.Path.GetTempFileName();
            System.IO.File.WriteAllBytes(tmp, buffer);
            var img = new ImageSurface(tmp);
            try
            {
                System.IO.File.Delete(tmp);
            }
            catch (Exception e)
            {
                // Only want to dispose when the Delete failed
                img.Dispose();
                throw;
            }
            return img;
        }

        public static Cairo.Color WithAlpha(Cairo.Color c, double alpha)
        {
            return new Cairo.Color(c.R, c.G, c.B, alpha);
        }

        public static Cairo.Color MultiplyAlpha(this Cairo.Color self, double alpha)
        {
            return new Cairo.Color(self.R, self.G, self.B, self.A * alpha);
        }

        public static void CachedDraw(this Cairo.Context self, ref SurfaceWrapper surface, Gdk.Point position, Gdk.Size size,
                                       object parameters = null, float opacity = 1.0f, Action<Cairo.Context, float> draw = null, double? forceScale = null)
        {
            self.CachedDraw(ref surface, new Gdk.Rectangle(position, size), parameters, opacity, draw, forceScale);
        }

        public static void CachedDraw(this Cairo.Context self, ref SurfaceWrapper surface, Gdk.Rectangle region,
                                       object parameters = null, float opacity = 1.0f, Action<Cairo.Context, float> draw = null, double? forceScale = null)
        {
            double displayScale = forceScale.HasValue ? forceScale.Value : QuartzSurface.GetRetinaScale(self);
            int targetWidth = (int)(region.Width * displayScale);
            int targetHeight = (int)(region.Height * displayScale);

            bool redraw = false;
            if (surface == null || surface.Width != targetWidth || surface.Height != targetHeight)
            {
                if (surface != null)
                    surface.Dispose();
                surface = new SurfaceWrapper(self, targetWidth, targetHeight);
                redraw = true;
            }
            else if ((surface.Data == null && parameters != null) || (surface.Data != null && !surface.Data.Equals(parameters)))
            {
                redraw = true;
            }


            if (redraw)
            {
                surface.Data = parameters;
                using (var context = new Cairo.Context(surface.Surface))
                {
                    context.Operator = Operator.Clear;
                    context.Paint();
                    context.Operator = Operator.Over;
                    context.Save();
                    context.Scale(displayScale, displayScale);
                    draw(context
, 1.0f);
                    context.Restore();
                }
            }

            self.Save();
            self.Translate(region.X, region.Y);
            self.Scale(1 / displayScale, 1 / displayScale);
            self.SetSourceSurface(surface.Surface, 0, 0);
            self.PaintWithAlpha(opacity);
            self.Restore();
        }
    }
    [Flags]
    public enum CairoCorners
    {
        None = 0,
        TopLeft = 1,
        TopRight = 2,
        BottomLeft = 4,
        BottomRight = 8,
        All = 15
    }
}

