using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;

namespace Wingman.Controls
{
    public class CircularGauge : FrameworkElement
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(CircularGauge),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(CircularGauge),
                new FrameworkPropertyMetadata("CPU LOAD", FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty GaugeColorProperty =
            DependencyProperty.Register(nameof(GaugeColor), typeof(Brush), typeof(CircularGauge),
                new FrameworkPropertyMetadata(Brushes.SkyBlue, FrameworkPropertyMetadataOptions.AffectsRender));

        private static readonly Typeface BoldTypeface = new Typeface("Segoe UI Bold");
        private static readonly Brush TrackBrush;
        private static readonly Pen TrackPen;

        static CircularGauge()
        {
            TrackBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240));
            TrackBrush.Freeze();

            TrackPen = new Pen(TrackBrush, 13)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            TrackPen.Freeze();
        }

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public Brush GaugeColor
        {
            get => (Brush)GetValue(GaugeColorProperty);
            set => SetValue(GaugeColorProperty, value);
        }

        public CircularGauge()
        {
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double w = ActualWidth > 0 ? ActualWidth : 140;
            double h = ActualHeight > 0 ? ActualHeight : 140;
            Point center = new Point(w / 2, h / 2);
            double radius = Math.Min(w, h) / 2 - 14;

            if (radius <= 0) return;

            double startAngle = 150;
            double totalSweep = 240;

            // Draw Track
            DrawArc(dc, TrackPen, center, radius, startAngle, totalSweep);

            // Draw Value Arc
            double clampedVal = Math.Clamp(Value, 0, 100);
            double valueSweep = (clampedVal / 100.0) * totalSweep;

            if (valueSweep > 0)
            {
                var valuePen = new Pen(GaugeColor, 13)
                {
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round
                };
                DrawArc(dc, valuePen, center, radius, startAngle, valueSweep);
            }

            // Draw Percentage Text
            string pctText = $"{Math.Round(clampedVal)}%";
            Brush primaryBrush = (Brush)Application.Current.FindResource("FgPrimaryBrush") ?? Brushes.DarkSlateGray;
            var formattedPct = new FormattedText(
                pctText,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                BoldTypeface,
                30,
                primaryBrush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            dc.DrawText(formattedPct, new Point(center.X - formattedPct.Width / 2, center.Y - formattedPct.Height / 2 - 12));

            // Draw Title Text
            Brush mutedBrush = (Brush)Application.Current.FindResource("FgMutedBrush") ?? Brushes.Gray;
            var formattedTitle = new FormattedText(
                Title,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                BoldTypeface,
                13,
                mutedBrush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            dc.DrawText(formattedTitle, new Point(center.X - formattedTitle.Width / 2, center.Y + 20));
        }

        private void DrawArc(DrawingContext dc, Pen pen, Point center, double radius, double startAngleDeg, double sweepAngleDeg)
        {
            double startAngleRad = startAngleDeg * Math.PI / 180.0;
            double endAngleRad = (startAngleDeg + sweepAngleDeg) * Math.PI / 180.0;

            Point startPoint = new Point(
                center.X + radius * Math.Cos(startAngleRad),
                center.Y + radius * Math.Sin(startAngleRad));

            Point endPoint = new Point(
                center.X + radius * Math.Cos(endAngleRad),
                center.Y + radius * Math.Sin(endAngleRad));

            bool isLargeArc = sweepAngleDeg > 180;

            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure
            {
                StartPoint = startPoint,
                IsClosed = false
            };

            pathFigure.Segments.Add(new ArcSegment(
                endPoint,
                new Size(radius, radius),
                0,
                isLargeArc,
                SweepDirection.Clockwise,
                true));

            pathGeometry.Figures.Add(pathFigure);
            dc.DrawGeometry(null, pen, pathGeometry);
        }
    }
}
