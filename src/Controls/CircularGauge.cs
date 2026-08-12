using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

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

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double w = ActualWidth > 0 ? ActualWidth : 160;
            double h = ActualHeight > 0 ? ActualHeight : 160;
            Point center = new Point(w / 2, h / 2);
            double radius = Math.Min(w, h) / 2 - 12;

            if (radius <= 0) return;

            double strokeWidth = 10;
            var trackPen = new Pen(new SolidColorBrush(Color.FromRgb(226, 232, 240)), strokeWidth)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };

            // Gauge track (-210 degrees to 30 degrees = 240 deg arc)
            double startAngle = 150;
            double totalSweep = 240;

            DrawArc(dc, trackPen, center, radius, startAngle, totalSweep);

            // Active gauge fill
            double clampedVal = Math.Clamp(Value, 0, 100);
            double valueSweep = (clampedVal / 100.0) * totalSweep;

            if (valueSweep > 0)
            {
                var valuePen = new Pen(GaugeColor, strokeWidth)
                {
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round
                };
                DrawArc(dc, valuePen, center, radius, startAngle, valueSweep);
            }

            // Percentage Text
            string pctText = $"{Math.Round(clampedVal)}%";
            var formattedPct = new FormattedText(
                pctText,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI Bold"),
                22,
                (Brush)Application.Current.FindResource("FgPrimaryBrush") ?? Brushes.DarkSlateGray,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            dc.DrawText(formattedPct, new Point(center.X - formattedPct.Width / 2, center.Y - formattedPct.Height / 2 - 8));

            // Title Text
            var formattedTitle = new FormattedText(
                Title,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI Semibold"),
                11,
                (Brush)Application.Current.FindResource("FgMutedBrush") ?? Brushes.Gray,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            dc.DrawText(formattedTitle, new Point(center.X - formattedTitle.Width / 2, center.Y + 14));
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
