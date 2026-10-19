// Disk Cleanup Helper - Copyright (C) 2026 Philip Droubi - https://github.com/Philip-Droubi
// Free software under the GNU GPL v3 with additional terms (section 7): see LICENSE and NOTICE.
// A small line chart. Hovering shows the reading nearest to the mouse (by left-right position, so it stays usable
// with thousands of points): a guide line, a dot on each line and a bubble with the values and their time.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DiskCleanupHelper.Localization;
using DiskCleanupHelper.UI;

namespace DiskCleanupHelper.UI.Controls
{
    public class MiniChart : FrameworkElement
    {
        public List<double[]> Series = new List<double[]>();
        public List<Brush> Colors = new List<Brush>();
        public double MaxY = 100;
        public double MinY = 0;
        public bool AutoMax;           // grow the top of the chart to fit the data
        public int Capacity;           // > 0: live chart that fills from the right
        public string LeftLabel = "", RightLabel = "", Unit = "";
        public DateTime[] Times;       // when each point was taken (same order as the series), shown on hover
        public string TimeFormat = "g";
        public string TipUnit;         // unit in the hover bubble (null = Unit)
        public string TipFormat = "0.#";

        static Brush Grid { get { return Theme.Get("#E2E8F0"); } }
        static Brush LabelBrush { get { return Theme.Get("#94A3B8"); } }
        // The language's font (Chinese, Japanese and Korean bring their own); rebuilt when the language changes.
        static string faceFont;
        static Typeface face, bold;
        static Typeface Face { get { UpdateFaces(); return face; } }
        static Typeface Bold { get { UpdateFaces(); return bold; } }

        static void UpdateFaces()
        {
            string font = string.IsNullOrEmpty(L.Font) ? "Segoe UI" : L.Font;
            if (face != null && font == faceFont) return;
            faceFont = font;
            face = new Typeface(font);
            bold = new Typeface(face.FontFamily, FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
        }

        // the lines are built once per size and data, so moving the mouse only redraws the hover marks
        Geometry[] lines;
        Size linesSize;
        int version, linesVersion = -1;
        double left = 38, right = 6, upper = 6, bottom = 16, pw, ph, top, range;
        int hover = -1;
        double mouseX = -1;   // kept so a live chart that moves on keeps showing the point under the mouse

        // time runs left to right in every language, so the chart is never mirrored in right-to-left windows
        public MiniChart() { FlowDirection = FlowDirection.LeftToRight; }

        public void Refresh() { version++; InvalidateVisual(); }

        FormattedText Label(string text) { return Txt(text, Face, 10.5, LabelBrush); }

        FormattedText Txt(string text, Typeface face, double size, Brush brush)
        {
            return new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, face, size, brush,
                                     VisualTreeHelper.GetDpi(this).PixelsPerDip);
        }

        int Count { get { return Series.Count > 0 ? Series[0].Length : 0; } }
        int Slots { get { return Capacity > 0 ? Capacity : Count; } }

        double X(int i)
        {
            int n = Count, slots = Slots;
            int slot = Capacity > 0 ? i + (Capacity - n) : i;
            return slots <= 1 ? left + pw / 2 : left + pw * slot / (slots - 1.0);
        }

        double Y(double v)
        {
            v = Math.Max(MinY, Math.Min(top, v));
            return upper + ph * (1 - (v - MinY) / range);
        }

        protected override void OnRender(DrawingContext dc)
        {
            double w = ActualWidth, h = ActualHeight;
            if (w < 40 || h < 30) return;
            top = MaxY;
            if (AutoMax)
            {
                double m = 0;
                foreach (double[] s in Series) foreach (double v in s) if (v > m) m = v;
                top = Math.Max(MaxY, NiceCeiling(m * 1.15));
            }
            range = Math.Max(1e-9, top - MinY);
            pw = w - left - right; ph = h - upper - bottom;
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, w, h));   // the whole area answers the mouse
            Pen gridPen = new Pen(Grid, 1);
            for (int i = 0; i <= 4; i++)
            {
                double y = upper + ph * i / 4.0;
                dc.DrawLine(gridPen, new Point(left, y), new Point(left + pw, y));
                double val = top - range * i / 4.0;
                FormattedText ft = Label(val.ToString(val >= 10 || val == 0 ? "0" : "0.##") + Unit);
                dc.DrawText(ft, new Point(left - 4 - ft.Width, y - ft.Height / 2));
            }
            if (lines == null || linesVersion != version || linesSize != new Size(w, h)) BuildLines(w, h);
            for (int si = 0; si < Series.Count; si++)
            {
                double[] s = Series[si];
                if (s.Length == 0) continue;
                Brush color = SeriesBrush(si);
                if (s.Length == 1) { dc.DrawEllipse(color, null, new Point(X(0), Y(s[0])), 3.5, 3.5); continue; }
                Pen pen = new Pen(color, 2);
                pen.LineJoin = PenLineJoin.Round;
                dc.DrawGeometry(null, pen, lines[si]);
                if (Capacity == 0 && s.Length <= 40) for (int i = 0; i < s.Length; i++) dc.DrawEllipse(color, null, new Point(X(i), Y(s[i])), 2.5, 2.5);
            }
            if (!string.IsNullOrEmpty(LeftLabel)) dc.DrawText(Label(LeftLabel), new Point(left, h - 14));
            if (!string.IsNullOrEmpty(RightLabel))
            {
                FormattedText ft = Label(RightLabel);
                dc.DrawText(ft, new Point(left + pw - ft.Width, h - 14));
            }
            if (mouseX >= 0) hover = IndexAt(mouseX);
            if (hover >= 0 && hover < Count) DrawHover(dc, w, h);
        }

        Brush SeriesBrush(int si) { return si < Colors.Count ? Theme.Current(Colors[si]) : Brushes.SteelBlue; }

        void BuildLines(double w, double h)
        {
            lines = new Geometry[Series.Count];
            for (int si = 0; si < Series.Count; si++)
            {
                double[] s = Series[si];
                StreamGeometry g = new StreamGeometry();
                if (s.Length > 1)
                {
                    using (StreamGeometryContext ctx = g.Open())
                    {
                        ctx.BeginFigure(new Point(X(0), Y(s[0])), false, false);
                        for (int i = 1; i < s.Length; i++) ctx.LineTo(new Point(X(i), Y(s[i])), true, true);
                    }
                }
                g.Freeze();
                lines[si] = g;
            }
            linesSize = new Size(w, h);
            linesVersion = version;
        }

        // ---------------------------------------------------------------- hover
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            mouseX = e.GetPosition(this).X;
            int i = IndexAt(mouseX);
            if (i != hover) { hover = i; InvalidateVisual(); }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            mouseX = -1;
            if (hover != -1) { hover = -1; InvalidateVisual(); }
        }

        // testing: draw the hover marks as if the mouse were at this share of the width (0..1)
        public void HoverAt(double share) { mouseX = left + Math.Max(0, Math.Min(1, share)) * Math.Max(0, ActualWidth - left - right); InvalidateVisual(); }

        // The point nearest to x, or -1 when the mouse is off the drawn part (a live chart that is not full yet).
        int IndexAt(double x)
        {
            int n = Count, slots = Slots;
            if (n == 0 || pw <= 0) return -1;
            if (slots <= 1) return 0;
            double step = pw / (slots - 1.0);
            int slot = (int)Math.Round((x - left) / step);
            int offset = Capacity > 0 ? Capacity - n : 0;
            if (slot < offset - 1) return -1;
            return Math.Max(0, Math.Min(n - 1, slot - offset));
        }

        // A dashed guide line, a ringed dot on each line, and a bubble beside it with the values and the time.
        void DrawHover(DrawingContext dc, double w, double h)
        {
            double x = X(hover);
            Pen guide = new Pen(Theme.Get("#94A3B8"), 1);
            guide.DashStyle = new DashStyle(new double[] { 3, 3 }, 0);
            dc.DrawLine(guide, new Point(x, upper), new Point(x, upper + ph));
            Brush ring = Theme.Get("#FFFFFF");
            double sumY = 0;
            int dots = 0;
            List<KeyValuePair<Brush, string>> rows = new List<KeyValuePair<Brush, string>>();
            for (int si = 0; si < Series.Count; si++)
            {
                double[] s = Series[si];
                if (hover >= s.Length) continue;
                Brush color = SeriesBrush(si);
                Point p = new Point(x, Y(s[hover]));
                dc.DrawEllipse(ring, null, p, 6, 6);
                dc.DrawEllipse(color, null, p, 4, 4);
                sumY += p.Y; dots++;
                rows.Add(new KeyValuePair<Brush, string>(color, s[hover].ToString(TipFormat, CultureInfo.CurrentCulture) + (TipUnit ?? Unit)));
            }
            if (dots == 0) return;

            // the bubble: dark in the light theme, light in the dark theme, so it always stands out
            Brush back = Theme.Get("#0F172A"), fore = Theme.Get("#FFFFFF"), dim = Theme.Get("#CBD5E1");
            FormattedText when = Times != null && hover < Times.Length ? Txt(Times[hover].ToString(TimeFormat, CultureInfo.CurrentCulture), Face, 11, dim) : null;
            List<FormattedText> values = new List<FormattedText>();
            double textW = when != null ? when.Width : 0, rowH = 17;
            foreach (KeyValuePair<Brush, string> r in rows)
            {
                FormattedText ft = Txt(r.Value, Bold, 12.5, fore);
                values.Add(ft);
                textW = Math.Max(textW, ft.Width + (rows.Count > 1 ? 14 : 0));
            }
            double padX = 9, padY = 6;
            double bw = textW + padX * 2;
            double bh = padY * 2 + (when != null ? 15 : 0) + rows.Count * rowH;
            double bx = x + 12;
            if (bx + bw > w) bx = x - 12 - bw;                 // no room on the right: put it on the left
            bx = Math.Max(0, Math.Min(w - bw, bx));
            double by = sumY / dots - bh / 2;
            by = Math.Max(0, Math.Min(h - bh, by));
            dc.DrawRoundedRectangle(back, null, new Rect(bx, by, bw, bh), 6, 6);
            double ty = by + padY;
            if (when != null) { dc.DrawText(when, new Point(bx + padX, ty)); ty += 15; }
            for (int k = 0; k < values.Count; k++)
            {
                double tx = bx + padX;
                if (rows.Count > 1)
                {
                    dc.DrawEllipse(rows[k].Key, null, new Point(tx + 4, ty + rowH / 2 - 0.5), 4, 4);   // which line it belongs to
                    tx += 14;
                }
                dc.DrawText(values[k], new Point(tx, ty + (rowH - values[k].Height) / 2));
                ty += rowH;
            }
        }

        static double NiceCeiling(double v)
        {
            if (v <= 0) return 1;
            double p = Math.Pow(10, Math.Floor(Math.Log10(v)));
            foreach (double m in new[] { 1, 1.2, 1.5, 2, 2.5, 3, 4, 5, 6, 8, 10 }) if (m * p >= v) return m * p;
            return 10 * p;
        }
    }
}
