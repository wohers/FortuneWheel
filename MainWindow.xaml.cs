using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace FortuneWheel
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<WheelSegment> _segments;

        // Красивая палитра для автоцветов
        private readonly Color[] _palette = new[]
        {
            Color.FromRgb(231, 76, 60),  // Красный
            Color.FromRgb(241, 196, 15), // Желтый
            Color.FromRgb(46, 204, 113), // Зеленый
            Color.FromRgb(52, 152, 219), // Синий
            Color.FromRgb(155, 89, 182), // Фиолетовый
            Color.FromRgb(230, 126, 34), // Оранжевый
            Color.FromRgb(26, 188, 156), // Бирюзовый
            Color.FromRgb(192, 57, 43)   // Темно-красный
        };

        private int _colorIndex = 0;

        public MainWindow()
        {
            InitializeComponent();

            _segments = new ObservableCollection<WheelSegment>();

            AddNewSegment("Приз 1");
            AddNewSegment("Приз 2");
            AddNewSegment("Приз 3");
            AddNewSegment("Приз 4");

            SegmentsList.ItemsSource = _segments;

            Wheel.SetSegments(new List<WheelSegment>(_segments));
            Wheel.SpinFinished += Wheel_SpinFinished;
        }

        private void SpinButton_Click(object sender, RoutedEventArgs e)
        {
            if (_segments.Count < 2)
            {
                MessageBox.Show("Добавьте хотя бы 2 сектора!");
                return;
            }

            SpinButton.IsEnabled = false;
            ResultText.Text = "Выпало: ...";
            Wheel.Spin(5);
        }

        private void Wheel_SpinFinished(object? sender, int index)
        {
            ResultText.Text = $"Выпало: {_segments[index].Text}";
            SpinButton.IsEnabled = true;
        }

        private void AddSegment_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewTextTb.Text)) return;

            AddNewSegment(NewTextTb.Text);
            NewTextTb.Text = "Новый сектор";
            UpdateWheel();
        }

        private void AddNewSegment(string text)
        {
            var color = _palette[_colorIndex % _palette.Length];
            _colorIndex++;

            _segments.Add(new WheelSegment
            {
                Text = text,
                Color = new SolidColorBrush(color)
            });
        }

        private void RemoveSegment_Click(object sender, RoutedEventArgs e)
        {
            if (SegmentsList.SelectedItem is WheelSegment selected)
            {
                _segments.Remove(selected);
                UpdateWheel();
            }
        }

        private void UpdateWheel()
        {
            Wheel.SetSegments(new List<WheelSegment>(_segments));
        }
    }
}