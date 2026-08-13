using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;

namespace Wingman.Controls
{
    public class LinearGauge : FrameworkElement
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(LinearGauge),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(LinearGauge),
                new FrameworkPropertyMetadata("C: 0GB / 0GB", FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty BarColorProperty =
            DependencyProperty.Register(nameof(BarColor), typeof(Brush), typeof(LinearGauge),
                new FrameworkPropertyMetadata(Brushes.SkyBlue, FrameworkPropertyMetadataOptions.AffectsRender));

        private static readonly Typeface BoldTypeface = new Typeface("Segoe UI Bold");
        private static readonly Brush TrackBrush;
        private static readonly Pen BorderPen;
        private static readonly Brush DarkTextBrush;
        private static readonly Brush WarnBarBrush;
        private static readonly Brush CritBarBrush;

        static LinearGauge()
        {
            TrackBrush = new SolidColorBrush(Color.FromRgb(241, 245, 249));
            TrackBrush.Freeze();

            BorderPen = new Pen(new SolidColorBrush(Color.FromRgb(203, 213, 225)), 1.5);
            BorderPen.Freeze();

            DarkTextBrush = new SolidColorBrush(Color.FromRgb(15, 23, 42));
            DarkTextBrush.Freeze();

            WarnBarBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11));
            WarnBarBrush.Freeze();

            CritBarBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            CritBarBrush.Freeze();
        }

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public Brush BarColor
        {
            get => (Brush)GetValue(BarColorProperty);
            set => SetValue(BarColorProperty, value);
        }

        public LinearGauge()
        {
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double w = ActualWidth > 0 ? ActualWidth : 180;
            double h = ActualHeight > 0 ? ActualHeight : 24;

            // Background Track
            dc.DrawRoundedRectangle(TrackBrush, BorderPen, new Rect(0, 0, w, h), 6, 6);

            // Fill Bar
            double pct = Math.Clamp(Value, 0, 100);
            double fillWidth = (pct / 100.0) * w;
            if (fillWidth > 0)
            {
                Brush activeBarColor = pct >= 90 ? CritBarBrush : (pct >= 80 ? WarnBarBrush : BarColor);
                dc.DrawRoundedRectangle(activeBarColor, null, new Rect(0, 0, fillWidth, h), 6, 6);
            }

            // Label Text
            Brush textBrush = pct > 60 ? Brushes.White : DarkTextBrush;
            var formattedLabel = new FormattedText(
                Label,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                BoldTypeface,
                13,
                textBrush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            dc.DrawText(formattedLabel, new Point((w - formattedLabel.Width) / 2, (h - formattedLabel.Height) / 2));
        }
    }
}
