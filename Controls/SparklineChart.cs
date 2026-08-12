using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Wingman.Controls
{
    public class SparklineChart : FrameworkElement
    {
        public static readonly DependencyProperty HistoryProperty =
            DependencyProperty.Register(nameof(History), typeof(IList<double>), typeof(SparklineChart),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register(nameof(Status), typeof(string), typeof(SparklineChart),
                new FrameworkPropertyMetadata("ok", FrameworkPropertyMetadataOptions.AffectsRender));

        public IList<double>? History
        {
            get => (IList<double>?)GetValue(HistoryProperty);
            set => SetValue(HistoryProperty, value);
        }

        public string Status
        {
            get => (string)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double w = ActualWidth > 0 ? ActualWidth : 180;
            double h = ActualHeight > 0 ? ActualHeight : 30;

            // Background
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(248, 250, 252)), new Pen(new SolidColorBrush(Color.FromRgb(241, 245, 249)), 1), new Rect(0, 0, w, h));

            if (History == null || History.Count < 2) return;

            // Determine status color
            Color strokeColor = Status switch
            {
                "warn" => Color.FromRgb(245, 158, 11),  // Amber
                "crit" => Color.FromRgb(239, 68, 68),   // Red
                _ => Color.FromRgb(16, 185, 129)       // Emerald
            };

            var linePen = new Pen(new SolidColorBrush(strokeColor), 2)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };

            double maxVal = 200.0;
            double stepX = w / (History.Count - 1);

            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure();

            for (int i = 0; i < History.Count; i++)
            {
                double val = History[i];
                double x = i * stepX;
                double normalized = Math.Clamp(val, 0, maxVal) / maxVal;
                double y = h - (normalized * (h - 4)) - 2;

                Point pt = new Point(x, y);
                if (i == 0)
                {
                    pathFigure.StartPoint = pt;
                }
                else
                {
                    pathFigure.Segments.Add(new LineSegment(pt, true));
                }
            }

            pathGeometry.Figures.Add(pathFigure);
            dc.DrawGeometry(null, linePen, pathGeometry);
        }
    }
}
