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


        // Variables para guardar el estado inicial del control al empezar a redimensionar
        private double _initSelLeft;
        private double _initSelTop;
        private double _initSelWidth;
        private double _initSelHeight;


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
            SaveUndoSnapshot();

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
            // CASO 1: Deseleccionar (Borrar todo)
            if (control == null)
            {
                if (_selectionBorder != null) DesignSurface.Children.Remove(_selectionBorder);
                _selectionBorder = null;
                foreach (var r in _resizeHandles) DesignSurface.Children.Remove(r);
                _resizeHandles.Clear();
                return;
            }

            // Datos del control actual
            var item = control as FrameworkElement;
            double l = Canvas.GetLeft(item);
            double t = Canvas.GetTop(item);
            double w = item.Width;  // Usamos Width/Height explícitos
            double h = item.Height;

            // CASO 2: Crear visuales (Solo si no existen)
            if (_selectionBorder == null)
            {
                // Crear Borde
                _selectionBorder = new Border
                {
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    IsHitTestVisible = false
                };
                DesignSurface.Children.Add(_selectionBorder);

                // Crear los 8 Handles (Solo se crean una vez)
                CreateHandle(ResizeDirection.TopLeft, Cursors.SizeNWSE);
                CreateHandle(ResizeDirection.Top, Cursors.SizeNS);
                CreateHandle(ResizeDirection.TopRight, Cursors.SizeNESW);
                CreateHandle(ResizeDirection.Right, Cursors.SizeWE);
                CreateHandle(ResizeDirection.BottomRight, Cursors.SizeNWSE);
                CreateHandle(ResizeDirection.Bottom, Cursors.SizeNS);
                CreateHandle(ResizeDirection.BottomLeft, Cursors.SizeNESW);
                CreateHandle(ResizeDirection.Left, Cursors.SizeWE);
            }

            // CASO 3: ACTUALIZAR POSICIONES (Se ejecuta siempre al mover/redimensionar)

            // Actualizar tamaño y posición del borde
            _selectionBorder.Width = w + 6;
            _selectionBorder.Height = h + 6;
            Canvas.SetLeft(_selectionBorder, l - 3);
            Canvas.SetTop(_selectionBorder, t - 3);

            // Actualizar posición de cada handle existente
            foreach (var rect in _resizeHandles)
            {
                ResizeDirection dir = (ResizeDirection)rect.Tag;
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
                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, y);
            }
        }

        private void CreateHandle(ResizeDirection dir, Cursor cursor)
        {
            var rect = new Rectangle
            {
                Width = 6,
                Height = 6,
                Fill = Brushes.Navy,
                Stroke = Brushes.White,
                StrokeThickness = 1,
                Cursor = cursor,
                Tag = dir
            };

            rect.MouseLeftButtonDown += Handle_MouseDown;
            rect.MouseLeftButtonUp += Handle_MouseUp;
            rect.MouseMove += Handle_MouseMove;

            DesignSurface.Children.Add(rect);
            _resizeHandles.Add(rect);
        }

        // LÓGICA DE REDIMENSIÓN DE CONTROL
        private void Handle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SaveUndoSnapshot();

            var rect = sender as Rectangle;
            _currentResizeDir = (ResizeDirection)rect.Tag;
            _isDragging = false;

            // Capturamos el punto de inicio del mouse
            _resizeClickStart = e.GetPosition(DesignSurface);

            // --- CORRECCIÓN: Guardar estado inicial del control ---
            if (_selectedControl is FrameworkElement item)
            {
                _initSelLeft = Canvas.GetLeft(item);
                _initSelTop = Canvas.GetTop(item);
                _initSelWidth = item.ActualWidth;   // Aquí sí es seguro leer ActualWidth
                _initSelHeight = item.ActualHeight;
            }

            rect.CaptureMouse();
            e.Handled = true;
        }

        private void Handle_MouseMove(object sender, MouseEventArgs e)
        {

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                _currentResizeDir = ResizeDirection.None;
                var r = sender as Rectangle;
                if (r != null) r.ReleaseMouseCapture();
                return;
            }


            if (_currentResizeDir != ResizeDirection.None && _selectedControl != null)
            {
                var item = _selectedControl as FrameworkElement;

                // 1. Calcular cuánto se ha movido el mouse desde el clic inicial (Delta)
                Point currentPos = e.GetPosition(DesignSurface);

                // Aplicamos SnapToGrid a la posición actual para que el movimiento sea "a saltos"
                double snappedCurrentX = SnapToGrid(currentPos.X);
                double snappedCurrentY = SnapToGrid(currentPos.Y);

                // El punto de inicio también debería considerarse "snapped" para que el delta sea exacto
                double startX = SnapToGrid(_resizeClickStart.X);
                double startY = SnapToGrid(_resizeClickStart.Y);

                double deltaX = snappedCurrentX - startX;
                double deltaY = snappedCurrentY - startY;

                // 2. Calcular nuevos valores basándonos en los INICIALES + DELTA
                double newLeft = _initSelLeft;
                double newTop = _initSelTop;
                double newWidth = _initSelWidth;
                double newHeight = _initSelHeight;

                switch (_currentResizeDir)
                {
                    case ResizeDirection.Right:
                        newWidth = _initSelWidth + deltaX;
                        break;

                    case ResizeDirection.Bottom:
                        newHeight = _initSelHeight + deltaY;
                        break;

                    case ResizeDirection.BottomRight:
                        newWidth = _initSelWidth + deltaX;
                        newHeight = _initSelHeight + deltaY;
                        break;

                    case ResizeDirection.Left:
                        // Al estirar a la izquierda: El ancho crece (restando delta) y la posición X se mueve
                        // Nota: deltaX será negativo si voy a la izquierda
                        newWidth = _initSelWidth - deltaX;
                        newLeft = _initSelLeft + deltaX;
                        break;

                    case ResizeDirection.Top:
                        newHeight = _initSelHeight - deltaY;
                        newTop = _initSelTop + deltaY;
                        break;

                    case ResizeDirection.TopRight:
                        newWidth = _initSelWidth + deltaX;
                        newHeight = _initSelHeight - deltaY;
                        newTop = _initSelTop + deltaY;
                        break;

                    case ResizeDirection.BottomLeft:
                        newWidth = _initSelWidth - deltaX;
                        newLeft = _initSelLeft + deltaX;
                        newHeight = _initSelHeight + deltaY;
                        break;

                    case ResizeDirection.TopLeft:
                        newWidth = _initSelWidth - deltaX;
                        newLeft = _initSelLeft + deltaX;
                        newHeight = _initSelHeight - deltaY;
                        newTop = _initSelTop + deltaY;
                        break;
                }

                // 3. Aplicar y Validar (Mínimo 8x8 pixeles)
                if (newWidth >= 8)
                {
                    item.Width = newWidth;
                    Canvas.SetLeft(item, newLeft);
                }

                if (newHeight >= 8)
                {
                    item.Height = newHeight;
                    Canvas.SetTop(item, newTop);
                }

                // 4. Actualizar los puntos visuales
                ShowSelectionIndicator(item);
            }
        }

        private void Handle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var rect = sender as Rectangle;
            rect?.ReleaseMouseCapture();
            _currentResizeDir = ResizeDirection.None;
        }

        private double SnapToGrid(double val) => Math.Round(val / 8.0) * 8.0;


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


        private void DesignSurface_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("ControlToolboxItem"))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }


        private void DesignSurface_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("ControlToolboxItem"))
            {

                SaveUndoSnapshot();

                string controlType = e.Data.GetData("ControlToolboxItem") as string;
                Point dropPosition = e.GetPosition(DesignSurface);

                // Crear el control usando nuestra Factory
                // Usamos el método puente CreateRetroControl o directamente la Factory
                UIElement newControl = RetroControlFactory.Create(controlType);

                if (newControl != null)
                {
                    // Posicionar donde cayó el mouse (con SnapToGrid)
                    double x = SnapToGrid(dropPosition.X);
                    double y = SnapToGrid(dropPosition.Y);

                    // Ajuste fino: Centrar el control en el mouse (opcional)
                    // Si quieres que el mouse quede en la esquina superior izquierda del control, déjalo así.
                    // Si quieres centrarlo:
                    // if (newControl is FrameworkElement fe) { x -= fe.Width / 2; y -= fe.Height / 2; }

                    Canvas.SetLeft(newControl, x);
                    Canvas.SetTop(newControl, y);

                    // Agregar al Canvas
                    AddControlToCanvas(newControl, x, y);

                    // Seleccionarlo automáticamente
                    // (Simulamos un click para activar los handles)
                    _selectedControl = newControl;

                    // 2. Dibujar los 8 puntos azules/blancos
                    ShowSelectionIndicator(newControl);

                    // 3. Avisar a la ventana principal (para que cargue el Panel de Propiedades)
                    ControlSelected?.Invoke(this, newControl as FrameworkElement);
                }

                e.Handled = true;
            }
        }

        // EN DesignerCanvas.Interaction.cs

        public void DeleteSelectedControl()
        {
            if (_selectedControl != null)
            {
                SaveUndoSnapshot();
                // 1. Quitar del Canvas visual
                DesignSurface.Children.Remove(_selectedControl);

                // 2. Limpiar la selección visual (puntos azules)
                ShowSelectionIndicator(null);

                // 3. Notificar a la ventana principal (para limpiar el panel de propiedades)
                ControlSelected?.Invoke(this, null);

                // 4. Olvidar la referencia
                _selectedControl = null;
            }
        }




    }
}