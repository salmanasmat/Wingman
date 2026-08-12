using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;

namespace Wingman.Controls
{
    public class SparklineChart : FrameworkElement
    {
        public static readonly DependencyProperty HistoryProperty =
            DependencyProperty.Register(nameof(History), typeof(List<double>), typeof(SparklineChart),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register(nameof(Status), typeof(string), typeof(SparklineChart),
                new FrameworkPropertyMetadata("ok", FrameworkPropertyMetadataOptions.AffectsRender));

        private static readonly Brush OkBrush;
        private static readonly Brush WarnBrush;
        private static readonly Brush CritBrush;
        private static readonly Brush MutedBrush;

        private static readonly Pen OkPen;
        private static readonly Pen WarnPen;
        private static readonly Pen CritPen;
        private static readonly Pen MutedPen;

        static SparklineChart()
        {
            OkBrush = new SolidColorBrush(Color.FromRgb(5, 150, 105)); OkBrush.Freeze();
            WarnBrush = new SolidColorBrush(Color.FromRgb(217, 119, 6)); WarnBrush.Freeze();
            CritBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38)); CritBrush.Freeze();
            MutedBrush = new SolidColorBrush(Color.FromRgb(148, 163, 184)); MutedBrush.Freeze();

            OkPen = new Pen(OkBrush, 2); OkPen.Freeze();
            WarnPen = new Pen(WarnBrush, 2); WarnPen.Freeze();
            CritPen = new Pen(CritBrush, 2); CritPen.Freeze();
            MutedPen = new Pen(MutedBrush, 1.5); MutedPen.Freeze();
        }

        public List<double> History
        {
            get => (List<double>)GetValue(HistoryProperty);
            set => SetValue(HistoryProperty, value);
        }

        public string Status
        {
            get => (string)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        public SparklineChart()
        {
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double w = ActualWidth > 0 ? ActualWidth : 120;
            double h = ActualHeight > 0 ? ActualHeight : 24;

            if (History == null || History.Count < 2)
            {
                dc.DrawLine(MutedPen, new Point(0, h / 2), new Point(w, h / 2));
                return;
            }

            Pen linePen = Status switch
            {
                "ok" => OkPen,
                "warn" => WarnPen,
                "crit" => CritPen,
                _ => MutedPen
            };

            double maxVal = 200;
            foreach (var val in History)
            {
                if (val > maxVal) maxVal = val;
            }

            double stepX = w / (History.Count - 1);
            var geometry = new StreamGeometry();

            using (var ctx = geometry.Open())
            {
                double firstY = h - ((Math.Min(History[0], maxVal) / maxVal) * (h - 4)) - 2;
                ctx.BeginFigure(new Point(0, firstY), false, false);

                for (int i = 1; i < History.Count; i++)
                {
                    double x = i * stepX;
                    double y = h - ((Math.Min(History[i], maxVal) / maxVal) * (h - 4)) - 2;
                    ctx.LineTo(new Point(x, y), true, false);
                }
            }

            geometry.Freeze();
            dc.DrawGeometry(null, linePen, geometry);
        }
    }
}
