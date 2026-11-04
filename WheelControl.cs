using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Reflection;
using System.Windows.Threading;

namespace FortuneWheel
{
    public class WheelControl : FrameworkElement
    {
        private readonly Canvas _canvas;
        private readonly RotateTransform _rotateTransform = new(0);
        private List<WheelSegment> _segments = new();
        private double _currentAngle;
        private double _wheelSize = 450;
        private MediaPlayer _mediaPlayer = new MediaPlayer();
        private DispatcherTimer _spinSoundTimer = new DispatcherTimer();
        private double _soundDuration = 6.0;

        public event EventHandler<int>? SpinFinished;

        public WheelControl()
        {
            _canvas = new Canvas
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = _rotateTransform
            };

            AddVisualChild(_canvas);
            AddLogicalChild(_canvas);


            try
            {
                var uri = new Uri("D:\\projects\\FortuneWheel\\wheel-of-fortune.wav");
                _mediaPlayer.Open(uri);
            }
            catch
            {

            }

            _spinSoundTimer.Interval = TimeSpan.FromMilliseconds(150);
            _spinSoundTimer.Tick += SpinSoundTimer_Tick;
        }

        protected override int VisualChildrenCount => 1;
        protected override Visual GetVisualChild(int index) => _canvas;

        protected override Size MeasureOverride(Size availableSize)
        {
            double w = double.IsInfinity(availableSize.Width) ? 450 : availableSize.Width;
            double h = double.IsInfinity(availableSize.Height) ? 450 : availableSize.Height;
            double size = Math.Min(w, h);
            if (size <= 0) size = 450;

            _canvas.Measure(new Size(size, size));
            return new Size(size, size);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double size = Math.Min(finalSize.Width, finalSize.Height);
            if (double.IsInfinity(size) || size <= 0) size = 450;

            double x = (finalSize.Width - size) / 2;
            double y = (finalSize.Height - size) / 2;
            _canvas.Arrange(new Rect(x, y, size, size));

            if (Math.Abs(size - _wheelSize) > 0.5)
            {
                _wheelSize = size;
                Redraw();
            }

            return finalSize;
        }

        public void SetSegments(List<WheelSegment> segments)
        {
            _segments = segments ?? new List<WheelSegment>();
            Redraw();
        }

        private void Redraw()
        {
            _canvas.Children.Clear();
            if (_segments.Count == 0) return;

            double size = _wheelSize;
            double radius = size / 2;
            double centerX = radius;
            double centerY = radius;

            var totalWeight = _segments.Sum(s => s.Weight);
            if (totalWeight <= 0) return;

            double startAngle = 0;

            foreach (var seg in _segments)
            {
                double sweep = 360.0 * seg.Weight / totalWeight;

                _canvas.Children.Add(new Path
                {
                    Data = CreateSectorGeometry(centerX, centerY, radius, startAngle, sweep),
                    Fill = seg.Color,
                    Stroke = Brushes.White,
                    StrokeThickness = 2
                });

                double midAngle = startAngle + sweep / 2;
                double rad = midAngle * Math.PI / 180;
                double textRadius = radius * 0.65;
                double tx = centerX + textRadius * Math.Cos(rad);
                double ty = centerY + textRadius * Math.Sin(rad);

                var tb = new TextBlock
                {
                    Text = seg.Text,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    RenderTransform = new RotateTransform(midAngle + 90)
                };
                tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(tb, tx - tb.DesiredSize.Width / 2);
                Canvas.SetTop(tb, ty - tb.DesiredSize.Height / 2);
                _canvas.Children.Add(tb);

                startAngle += sweep;
            }

            var hub = new Ellipse
            {
                Width = size * 0.12,
                Height = size * 0.12,
                Fill = Brushes.White,
                Stroke = Brushes.DarkGray,
                StrokeThickness = 2
            };
            Canvas.SetLeft(hub, centerX - hub.Width / 2);
            Canvas.SetTop(hub, centerY - hub.Height / 2);
            _canvas.Children.Add(hub);
        }

        private static Geometry CreateSectorGeometry(double cx, double cy, double r, double startAngle, double sweep)
        {
            double a1 = startAngle * Math.PI / 180;
            double a2 = (startAngle + sweep) * Math.PI / 180;

            var p1 = new Point(cx + r * Math.Cos(a1), cy + r * Math.Sin(a1));
            var p2 = new Point(cx + r * Math.Cos(a2), cy + r * Math.Sin(a2));

            var figure = new PathFigure
            {
                StartPoint = new Point(cx, cy),
                IsClosed = true
            };
            figure.Segments.Add(new LineSegment(p1, true));
            figure.Segments.Add(new ArcSegment
            {
                Point = p2,
                Size = new Size(r, r),
                IsLargeArc = sweep > 180,
                SweepDirection = SweepDirection.Clockwise
            });
            figure.Segments.Add(new LineSegment(new Point(cx, cy), true));

            var geo = new PathGeometry();
            geo.Figures.Add(figure);
            geo.Freeze();
            return geo;
        }

        public void Spin(int durationSeconds = 5)
        {
            if (_segments.Count == 0) return;

            var rnd = new Random();
            double extraTurns = 5 + rnd.Next(3);
            double randomOffset = rnd.NextDouble() * 360;

            double fromAngle = _rotateTransform.Angle;
            double targetAngle = fromAngle + extraTurns * 360 + randomOffset;

            _soundDuration = durationSeconds;
            _spinSoundTimer.Interval = TimeSpan.FromMilliseconds(60);
            _spinSoundTimer.Start();

            var anim = new DoubleAnimation
            {
                From = fromAngle,
                To = targetAngle,
                Duration = TimeSpan.FromSeconds(durationSeconds),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            anim.Completed += (s, e) =>
            {
                _spinSoundTimer.Stop();
                _mediaPlayer.Stop();

                _rotateTransform.BeginAnimation(RotateTransform.AngleProperty, null);
                _currentAngle = targetAngle % 360;
                _rotateTransform.Angle = _currentAngle;

                int index = GetIndexAtPointer();
                SpinFinished?.Invoke(this, index);
            };

            _rotateTransform.BeginAnimation(RotateTransform.AngleProperty, anim);
        }

        private int GetIndexAtPointer()
        {
            if (_segments.Count == 0) return -1;

            const double pointerAngle = 270;
            double angle = (pointerAngle - _currentAngle) % 360;
            if (angle < 0) angle += 360;

            var totalWeight = _segments.Sum(s => s.Weight);
            if (totalWeight <= 0) return 0;

            double startAngle = 0;
            for (int i = 0; i < _segments.Count; i++)
            {
                double sweep = 360.0 * _segments[i].Weight / totalWeight;
                if (angle >= startAngle && angle < startAngle + sweep)
                    return i;
                startAngle += sweep;
            }
            return _segments.Count - 1;
        }

        private void SpinSoundTimer_Tick(object? sender, EventArgs e)
        {
            _mediaPlayer.Position = TimeSpan.Zero;
            _mediaPlayer.Play();

            double currentAngle = _rotateTransform.Angle;

            if (_spinSoundTimer.Interval < TimeSpan.FromMilliseconds(300))
            {
                _spinSoundTimer.Interval += TimeSpan.FromMilliseconds(10);
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
        }
    }
}