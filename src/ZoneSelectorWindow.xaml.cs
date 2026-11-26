using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DesktopRightAlign.Models;
using DesktopRightAlign.Services;

namespace DesktopRightAlign
{
    public partial class ZoneSelectorWindow : Window
    {
        private readonly DesktopEnvironmentInfo _env;
        private readonly List<ExclusionZone> _existingZones;
        private readonly ExclusionZone _editingZone;
        private double _dpiScaleX = 1.0;
        private double _dpiScaleY = 1.0;

        private bool _isDragging;
        private Point _startDip;

        public ExclusionZone SelectedZone { get; private set; }

        public ZoneSelectorWindow(DesktopEnvironmentInfo env, List<ExclusionZone> existingZones, ExclusionZone editingZone)
        {
            InitializeComponent();
            _env = env;
            _existingZones = existingZones ?? new List<ExclusionZone>();
            _editingZone = editingZone;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            OverlayHintText.Text = _editingZone != null
                ? LocalizationService.F("OverlayEditHint", _editingZone.Name)
                : LocalizationService.T("OverlayHint");
            PresentationSource source = PresentationSource.FromVisual(this);
            if (source != null && source.CompositionTarget != null)
            {
                _dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                _dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
            }

            Left = _env.WorkAreaLeft / _dpiScaleX;
            Top = _env.WorkAreaTop / _dpiScaleY;
            Width = _env.WorkAreaWidth / _dpiScaleX;
            Height = _env.WorkAreaHeight / _dpiScaleY;

            RenderExistingZones();
            Focus();
        }

        private void RenderExistingZones()
        {
            for (int i = 0; i < _existingZones.Count; i++)
            {
                ExclusionZone zone = _existingZones[i];
                if (zone == null || !zone.Enabled || zone.Width <= 0 || zone.Height <= 0) continue;

                double x = (zone.Left - _env.WorkAreaLeft) / _dpiScaleX;
                double y = (zone.Top - _env.WorkAreaTop) / _dpiScaleY;
                double w = zone.Width / _dpiScaleX;
                double h = zone.Height / _dpiScaleY;

                bool isEditingTarget = _editingZone != null && string.Equals(zone.Id, _editingZone.Id, StringComparison.OrdinalIgnoreCase);
                Border b = new Border
                {
                    Width = w,
                    Height = h,
                    Background = isEditingTarget
                        ? new SolidColorBrush(Color.FromArgb(55, 0, 162, 255))
                        : new SolidColorBrush(Color.FromArgb(60, 232, 17, 35)),
                    BorderBrush = isEditingTarget
                        ? new SolidColorBrush(Color.FromArgb(230, 0, 180, 255))
                        : new SolidColorBrush(Color.FromArgb(200, 255, 90, 90)),
                    BorderThickness = new Thickness(isEditingTarget ? 2.0 : 1.5),
                    CornerRadius = new CornerRadius(2),
                    IsHitTestVisible = false,
                    Child = new TextBlock
                    {
                        Text = LocalizationService.F("OverlayExcludedTag", zone.Name),
                        Foreground = Brushes.White,
                        FontSize = 12,
                        Margin = new Thickness(8, 6, 0, 0)
                    }
                };
                Canvas.SetLeft(b, x);
                Canvas.SetTop(b, y);
                OverlayCanvas.Children.Insert(0, b);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }

        private void OverlayCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _startDip = e.GetPosition(OverlayCanvas);
            OverlayCanvas.CaptureMouse();

            Canvas.SetLeft(SelectionRect, _startDip.X);
            Canvas.SetTop(SelectionRect, _startDip.Y);
            SelectionRect.Width = 0;
            SelectionRect.Height = 0;
            SelectionRect.Visibility = Visibility.Visible;
        }

        private void OverlayCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;

            Point cur = e.GetPosition(OverlayCanvas);
            double x = Math.Min(_startDip.X, cur.X);
            double y = Math.Min(_startDip.Y, cur.Y);
            double w = Math.Abs(cur.X - _startDip.X);
            double h = Math.Abs(cur.Y - _startDip.Y);

            Canvas.SetLeft(SelectionRect, x);
            Canvas.SetTop(SelectionRect, y);
            SelectionRect.Width = w;
            SelectionRect.Height = h;

            int physX = _env.WorkAreaLeft + (int)Math.Round(x * _dpiScaleX);
            int physY = _env.WorkAreaTop + (int)Math.Round(y * _dpiScaleY);
            int physW = (int)Math.Round(w * _dpiScaleX);
            int physH = (int)Math.Round(h * _dpiScaleY);

            SelectionSizeText.Text = string.Format("{0} × {1} px  ({2}, {3})", physW, physH, physX, physY);
        }

        private void OverlayCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging) return;
            _isDragging = false;
            OverlayCanvas.ReleaseMouseCapture();

            Point cur = e.GetPosition(OverlayCanvas);
            double x = Math.Min(_startDip.X, cur.X);
            double y = Math.Min(_startDip.Y, cur.Y);
            double w = Math.Abs(cur.X - _startDip.X);
            double h = Math.Abs(cur.Y - _startDip.Y);

            int physW = (int)Math.Round(w * _dpiScaleX);
            int physH = (int)Math.Round(h * _dpiScaleY);

            if (physW < 24 || physH < 24)
            {
                SelectionRect.Visibility = Visibility.Collapsed;
                return;
            }

            SelectedZone = new ExclusionZone
            {
                Id = _editingZone != null ? _editingZone.Id : Guid.NewGuid().ToString("N"),
                Name = _editingZone != null ? _editingZone.Name : LocalizationService.F("ZoneNamePrefix", _existingZones.Count + 1),
                Left = _env.WorkAreaLeft + (int)Math.Round(x * _dpiScaleX),
                Top = _env.WorkAreaTop + (int)Math.Round(y * _dpiScaleY),
                Width = physW,
                Height = physH,
                Enabled = _editingZone != null ? _editingZone.Enabled : true
            };

            DialogResult = true;
            Close();
        }
    }
}
