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
            double h = ActualHeight > 0 ? ActualHeight : 28;

            // Background Track
            var trackBrush = new SolidColorBrush(Color.FromRgb(241, 245, 249));
            var borderPen = new Pen(new SolidColorBrush(Color.FromRgb(203, 213, 225)), 1.5);
            dc.DrawRoundedRectangle(trackBrush, borderPen, new Rect(0, 0, w, h), 6, 6);

            // Fill Bar
            double pct = Math.Clamp(Value, 0, 100);
            double fillWidth = (pct / 100.0) * w;
            if (fillWidth > 0)
            {
                dc.DrawRoundedRectangle(BarColor, null, new Rect(0, 0, fillWidth, h), 6, 6);
            }

            // Label Text (Increased to 12pt bold)
            Brush textBrush = pct > 60 ? Brushes.White : new SolidColorBrush(Color.FromRgb(15, 23, 42));
            var formattedLabel = new FormattedText(
                Label,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI Bold"),
                12,
                textBrush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            dc.DrawText(formattedLabel, new Point((w - formattedLabel.Width) / 2, (h - formattedLabel.Height) / 2));
        }
    }
}
