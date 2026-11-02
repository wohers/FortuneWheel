using System.Windows.Media;

namespace FortuneWheel
{
    public class WheelSegment
    {
        public string Text { get; set; } = "";
        public Brush Color { get; set; } = Brushes.Gray;
        public double Weight { get; set; } = 1;
    }
}