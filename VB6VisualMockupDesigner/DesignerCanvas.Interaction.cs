using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace VB6VisualMockupDesigner
{
    // PARTIAL CLASS: Manejo de Interacción con Controles
    public partial class DesignerCanvas
    {
        private bool _isDragging = false;
        private Point _clickOffset;
        private UIElement _selectedControl;
        private Border _selectionBorder;

        // Manejo de Redimensión de Controles
        private enum ResizeDirection { None, TopLeft, Top, TopRight, Right, BottomRight, Bottom, BottomLeft, Left }
        private ResizeDirection _currentResizeDir = ResizeDirection.None;
        private List<Rectangle> _resizeHandles = new List<Rectangle>();

        // EVENTOS DE CONTROL (Arrastre y Selección)
        private void Control_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var control = sender as UIElement;
            _selectedControl = control;
            _isDragging = true;
            _clickOffset = e.GetPosition(control);
            control.CaptureMouse();
            ShowSelectionIndicator(control);
            e.Handled = true;
            ControlSelected?.Invoke(this, control as FrameworkElement);
        }

        private void Control_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _selectedControl != null)
            {
                Point currentPos = e.GetPosition(DesignSurface);
                double newLeft = SnapToGrid(currentPos.X - _clickOffset.X);
                double newTop = SnapToGrid(currentPos.Y - _clickOffset.Y);

                // Límites
                var fe = _selectedControl as FrameworkElement;
                double maxLeft = DesignSurface.ActualWidth - fe.ActualWidth;
                double maxTop = DesignSurface.ActualHeight - fe.ActualHeight;

                if (newLeft < 0) newLeft = 0;
                if (newTop < 0) newTop = 0;
                if (maxLeft > 0 && newLeft > maxLeft) newLeft = maxLeft;
                if (maxTop > 0 && newTop > maxTop) newTop = maxTop;

                Canvas.SetLeft(_selectedControl, newLeft);
                Canvas.SetTop(_selectedControl, newTop);

                ShowSelectionIndicator(_selectedControl); // Actualizar handles
            }
        }

        private void Control_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                if (_selectedControl != null) _selectedControl.ReleaseMouseCapture();
            }
        }

        private void DesignSurface_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ShowSelectionIndicator(null);
            _selectedControl = null;
            ControlSelected?.Invoke(this, null);
        }

        // LÓGICA DE HANDLES (Los 8 cuadritos)
        private void ShowSelectionIndicator(UIElement control)
        {
            if (_selectionBorder != null) { DesignSurface.Children.Remove(_selectionBorder); _selectionBorder = null; }
            foreach (var r in _resizeHandles) DesignSurface.Children.Remove(r);
            _resizeHandles.Clear();

            if (control == null) return;

            // Borde visual
            var fe = control as FrameworkElement;
            _selectionBorder = new Border
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Width = fe.ActualWidth + 6,
                Height = fe.ActualHeight + 6,
                IsHitTestVisible = false
            };
            double l = Canvas.GetLeft(control), t = Canvas.GetTop(control);
            Canvas.SetLeft(_selectionBorder, l - 3); Canvas.SetTop(_selectionBorder, t - 3);
            DesignSurface.Children.Add(_selectionBorder);

            // Crear 8 Handles
            CreateHandle(control, ResizeDirection.TopLeft, Cursors.SizeNWSE);
            CreateHandle(control, ResizeDirection.Top, Cursors.SizeNS);
            CreateHandle(control, ResizeDirection.TopRight, Cursors.SizeNESW);
            CreateHandle(control, ResizeDirection.Right, Cursors.SizeWE);
            CreateHandle(control, ResizeDirection.BottomRight, Cursors.SizeNWSE);
            CreateHandle(control, ResizeDirection.Bottom, Cursors.SizeNS);
            CreateHandle(control, ResizeDirection.BottomLeft, Cursors.SizeNESW);
            CreateHandle(control, ResizeDirection.Left, Cursors.SizeWE);
        }

        private void CreateHandle(UIElement control, ResizeDirection dir, Cursor cursor)
        {
            var rect = new Rectangle { Width = 6, Height = 6, Fill = Brushes.Navy, Stroke = Brushes.White, StrokeThickness = 1, Cursor = cursor, Tag = dir };
            rect.MouseLeftButtonDown += Handle_MouseDown;
            rect.MouseLeftButtonUp += Handle_MouseUp;
            rect.MouseMove += Handle_MouseMove;

            double l = Canvas.GetLeft(control), t = Canvas.GetTop(control);
            double w = ((FrameworkElement)control).ActualWidth, h = ((FrameworkElement)control).ActualHeight;
            double x = 0, y = 0;

            switch (dir)
            {
                case ResizeDirection.TopLeft: x = l - 3; y = t - 3; break;
                case ResizeDirection.Top: x = l + w / 2 - 3; y = t - 3; break;
                case ResizeDirection.TopRight: x = l + w - 3; y = t - 3; break;
                case ResizeDirection.Right: x = l + w - 3; y = t + h / 2 - 3; break;
                case ResizeDirection.BottomRight: x = l + w - 3; y = t + h - 3; break;
                case ResizeDirection.Bottom: x = l + w / 2 - 3; y = t + h - 3; break;
                case ResizeDirection.BottomLeft: x = l - 3; y = t + h - 3; break;
                case ResizeDirection.Left: x = l - 3; y = t + h / 2 - 3; break;
            }
            Canvas.SetLeft(rect, x); Canvas.SetTop(rect, y);
            DesignSurface.Children.Add(rect);
            _resizeHandles.Add(rect);
        }

        // LÓGICA DE REDIMENSIÓN DE CONTROL
        private void Handle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var rect = sender as Rectangle;
            _currentResizeDir = (ResizeDirection)rect.Tag;
            _isDragging = false;
            rect.CaptureMouse();
            e.Handled = true;
        }

        private void Handle_MouseMove(object sender, MouseEventArgs e)
        {
            if (_currentResizeDir != ResizeDirection.None && _selectedControl != null)
            {
                Point pos = e.GetPosition(DesignSurface);
                double destX = SnapToGrid(pos.X);
                double destY = SnapToGrid(pos.Y);
                var item = _selectedControl as FrameworkElement;
                double oldL = Canvas.GetLeft(item), oldT = Canvas.GetTop(item);

                if (_currentResizeDir == ResizeDirection.Right || _currentResizeDir == ResizeDirection.BottomRight)
                {
                    double newW = destX - oldL;
                    if (newW >= 8) item.Width = newW;
                }
                if (_currentResizeDir == ResizeDirection.Bottom || _currentResizeDir == ResizeDirection.BottomRight)
                {
                    double newH = destY - oldT;
                    if (newH >= 8) item.Height = newH;
                }
                // (Agregar lógica para Left/Top si es necesario)

                ShowSelectionIndicator(_selectedControl);
            }
        }

        private void Handle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var rect = sender as Rectangle;
            rect?.ReleaseMouseCapture();
            _currentResizeDir = ResizeDirection.None;
        }

        private double SnapToGrid(double val) => Math.Round(val / 8.0) * 8.0;
        private void DesignSurface_Drop(object sender, DragEventArgs e)
        {
            // Implementar Drop si se requiere agregar desde Toolbox
            if (e.Data.GetDataPresent("ControlToolboxItem"))
            {
                string type = e.Data.GetData("ControlToolboxItem") as string;
                Point p = e.GetPosition(DesignSurface);
                AddControlToCanvas(RetroControlFactory.Create(type), p.X, p.Y);
            }
        }

        public void AddControlToCanvas(UIElement control, double x, double y)
        {
            if (control == null) return;
            // Conectar eventos al nuevo control
            control.PreviewMouseDown += Control_PreviewMouseDown;
            control.PreviewMouseMove += Control_PreviewMouseMove;
            control.PreviewMouseUp += Control_PreviewMouseUp;

            Canvas.SetLeft(control, SnapToGrid(x));
            Canvas.SetTop(control, SnapToGrid(y));
            DesignSurface.Children.Add(control);
        }

        // 2. Solución al error 'CreateRetroControl'
        // Sirve de puente para que el código antiguo siga funcionando usando la nueva Factory.
        public UIElement CreateRetroControl(string type)
        {
            return RetroControlFactory.Create(type);
        }


    }
}