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
        // ==========================================
        // 1. VARIABLES DE ESTADO Y COLECCIONES
        // ==========================================

        private bool _isSelectingArea = false;
        private Point _areaStartPoint;

        private readonly HashSet<UIElement> _selectedControls = new HashSet<UIElement>();
        private Dictionary<UIElement, FrameworkElement> _selectionAdorners = new Dictionary<UIElement, FrameworkElement>();
        private Dictionary<UIElement, Point> _initialPositions = new Dictionary<UIElement, Point>();

        private bool _isDragging = false;
        private Point _dragStartPoint;

        // Variables de Redimensión
        private List<Rectangle> _resizeHandles = new List<Rectangle>();
        private Point _resizeClickStart;

        // Estado inicial para redimensión
        private double _initSelLeft, _initSelTop, _initSelWidth, _initSelHeight;

        private enum ResizeDirection { None, TopLeft, Top, TopRight, Right, BottomRight, Bottom, BottomLeft, Left }
        private ResizeDirection _currentResizeDir = ResizeDirection.None;

        private UIElement _primarySelection => _selectedControls.Count == 1 ? _selectedControls.First() : null;

        // ==========================================
        // 2. EVENTOS DEL MOUSE (SELECCIÓN Y ARRASTRE)
        // ==========================================

        private void Control_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var control = sender as UIElement;
            if (control == null) return;

            bool isCtrlPressed = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

            if (isCtrlPressed)
            {
                ToggleSelection(control);
            }
            else
            {
                if (!_selectedControls.Contains(control))
                {
                    ClearSelection();
                    AddToSelection(control);
                }
            }

            _isDragging = true;
            _dragStartPoint = e.GetPosition(DesignSurface);

            _initialPositions.Clear();
            foreach (var item in _selectedControls)
            {
                _initialPositions[item] = new Point(Canvas.GetLeft(item), Canvas.GetTop(item));
            }

            control.CaptureMouse();
            e.Handled = true;
            NotifySelectionChanged();
        }

        private void Control_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _selectedControls.Count > 0)
            {
                Point currentMousePos = e.GetPosition(DesignSurface);

                double rawDeltaX = currentMousePos.X - _dragStartPoint.X;
                double rawDeltaY = currentMousePos.Y - _dragStartPoint.Y;
                double snapDeltaX = SnapToGrid(rawDeltaX);
                double snapDeltaY = SnapToGrid(rawDeltaY);

                if (Math.Abs(snapDeltaX) < 1 && Math.Abs(snapDeltaY) < 1) return;

                foreach (var control in _selectedControls)
                {
                    if (_initialPositions.TryGetValue(control, out Point startPos))
                    {
                        double newLeft = Math.Max(0, startPos.X + snapDeltaX);
                        double newTop = Math.Max(0, startPos.Y + snapDeltaY);

                        Canvas.SetLeft(control, newLeft);
                        Canvas.SetTop(control, newTop);
                    }
                }

                UpdateSelectionVisuals();
            }
        }

        private void Control_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                var control = sender as UIElement;
                control?.ReleaseMouseCapture();
            }
        }

        private void DesignSurface_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Si hacemos clic directo en el fondo
            if (e.Source == DesignSurface)
            {
                ClearSelection();
                NotifySelectionChanged();

                _isSelectingArea = true;
                _areaStartPoint = e.GetPosition(DesignSurface);

                Canvas.SetLeft(SelectionRect, _areaStartPoint.X);
                Canvas.SetTop(SelectionRect, _areaStartPoint.Y);
                SelectionRect.Width = 0;
                SelectionRect.Height = 0;
                SelectionRect.Visibility = Visibility.Visible;

                DesignSurface.CaptureMouse();
                e.Handled = true;
            }
        }

        private void DesignSurface_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isSelectingArea)
            {
                Point currentPos = e.GetPosition(DesignSurface);

                double x = Math.Min(currentPos.X, _areaStartPoint.X);
                double y = Math.Min(currentPos.Y, _areaStartPoint.Y);
                double w = Math.Abs(currentPos.X - _areaStartPoint.X);
                double h = Math.Abs(currentPos.Y - _areaStartPoint.Y);

                Canvas.SetLeft(SelectionRect, x);
                Canvas.SetTop(SelectionRect, y);
                SelectionRect.Width = w;
                SelectionRect.Height = h;
            }
        }

        private void DesignSurface_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isSelectingArea)
            {
                _isSelectingArea = false;
                DesignSurface.ReleaseMouseCapture();
                SelectionRect.Visibility = Visibility.Collapsed;

                // --- CORRECCIÓN CRÍTICA AQUÍ ---
                double rectL = Canvas.GetLeft(SelectionRect);
                double rectT = Canvas.GetTop(SelectionRect);
                Rect selectionArea = new Rect(rectL, rectT, SelectionRect.Width, SelectionRect.Height);

                // 1. Usamos una lista temporal para guardar lo que encontramos
                // No podemos llamar a AddToSelection dentro del bucle de Children porque modificaría la colección
                var hits = new List<UIElement>();

                foreach (UIElement child in DesignSurface.Children)
                {
                    // Ignoramos el propio cuadro de selección y los adornos (bordes, handles)
                    if (child == SelectionRect || child is Border || (child is Rectangle && child != SelectionRect)) continue;

                    if (child is FrameworkElement fe)
                    {
                        double childL = Canvas.GetLeft(fe);
                        double childT = Canvas.GetTop(fe);
                        Rect childRect = new Rect(childL, childT, fe.ActualWidth, fe.ActualHeight);

                        if (selectionArea.IntersectsWith(childRect))
                        {
                            hits.Add(fe);
                        }
                    }
                }

                // 2. Ahora sí aplicamos la selección (fuera del bucle de Children)
                foreach (var hit in hits)
                {
                    AddToSelection(hit);
                }

                if (hits.Count > 0) NotifySelectionChanged();
            }
        }


        // ==========================================
        // 3. GESTIÓN DE LA SELECCIÓN
        // ==========================================

        private void AddToSelection(UIElement control)
        {
            if (_selectedControls.Add(control)) UpdateSelectionVisuals();
        }

        private void RemoveFromSelection(UIElement control)
        {
            if (_selectedControls.Remove(control)) UpdateSelectionVisuals();
        }

        private void ToggleSelection(UIElement control)
        {
            if (_selectedControls.Contains(control)) RemoveFromSelection(control);
            else AddToSelection(control);
        }

        private void ClearSelection()
        {
            _selectedControls.Clear();
            UpdateSelectionVisuals();
        }

        private void NotifySelectionChanged()
        {
            if (_selectedControls.Count == 1)
                ControlSelected?.Invoke(this, _selectedControls.First() as FrameworkElement);
            else
                ControlSelected?.Invoke(this, null);
        }

        // ==========================================
        // 4. VISUALES (BORDES Y HANDLES)
        // ==========================================

        private void UpdateSelectionVisuals()
        {
            // A) Actualizar Bordes Azules
            // Usamos .ToList() para evitar excepción al modificar el diccionario mientras iteramos claves
            var adornersToRemove = _selectionAdorners.Keys
                                    .Where(k => !_selectedControls.Contains(k))
                                    .ToList();

            foreach (var ctrl in adornersToRemove)
            {
                if (_selectionAdorners.ContainsKey(ctrl))
                {
                    DesignSurface.Children.Remove(_selectionAdorners[ctrl]);
                    _selectionAdorners.Remove(ctrl);
                }
            }

            foreach (var control in _selectedControls)
            {
                var item = control as FrameworkElement;
                if (item == null) continue;

                if (!_selectionAdorners.ContainsKey(control))
                {
                    var border = new Border
                    {
                        BorderBrush = Brushes.Blue,
                        BorderThickness = new Thickness(1),
                        IsHitTestVisible = false
                    };
                    DesignSurface.Children.Add(border);
                    _selectionAdorners[control] = border;
                }

                var visualBorder = _selectionAdorners[control];
                visualBorder.Width = item.ActualWidth + 4;
                visualBorder.Height = item.ActualHeight + 4;
                Canvas.SetLeft(visualBorder, Canvas.GetLeft(item) - 2);
                Canvas.SetTop(visualBorder, Canvas.GetTop(item) - 2);
            }

            // B) Actualizar Handles de Redimensión
            if (_selectedControls.Count == 1)
            {
                var item = _primarySelection as FrameworkElement;
                if (_resizeHandles.Count == 0) CreateResizeHandles();
                UpdateHandlePositions(item);
            }
            else
            {
                foreach (var h in _resizeHandles) DesignSurface.Children.Remove(h);
                _resizeHandles.Clear();
            }
        }

        private void CreateResizeHandles()
        {
            void AddHandle(ResizeDirection dir, Cursor cursor)
            {
                var rect = new Rectangle
                {
                    Width = 7,
                    Height = 7,
                    Fill = Brushes.White,
                    Stroke = Brushes.Black,
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

            AddHandle(ResizeDirection.TopLeft, Cursors.SizeNWSE);
            AddHandle(ResizeDirection.Top, Cursors.SizeNS);
            AddHandle(ResizeDirection.TopRight, Cursors.SizeNESW);
            AddHandle(ResizeDirection.Right, Cursors.SizeWE);
            AddHandle(ResizeDirection.BottomRight, Cursors.SizeNWSE);
            AddHandle(ResizeDirection.Bottom, Cursors.SizeNS);
            AddHandle(ResizeDirection.BottomLeft, Cursors.SizeNESW);
            AddHandle(ResizeDirection.Left, Cursors.SizeWE);
        }

        private void UpdateHandlePositions(FrameworkElement item)
        {
            if (item == null || _resizeHandles.Count < 8) return;

            double l = Canvas.GetLeft(item);
            double t = Canvas.GetTop(item);
            double w = item.ActualWidth;
            double h = item.ActualHeight;

            void MoveHandle(int index, double x, double y)
            {
                var rect = _resizeHandles[index];
                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, y);
            }

            MoveHandle(0, l - 4, t - 4);                // TopLeft
            MoveHandle(1, l + w / 2 - 4, t - 4);        // Top
            MoveHandle(2, l + w - 4, t - 4);            // TopRight
            MoveHandle(3, l + w - 4, t + h / 2 - 4);    // Right
            MoveHandle(4, l + w - 4, t + h - 4);        // BottomRight
            MoveHandle(5, l + w / 2 - 4, t + h - 4);    // Bottom
            MoveHandle(6, l - 4, t + h - 4);            // BottomLeft
            MoveHandle(7, l - 4, t + h / 2 - 4);        // Left
        }

        // ==========================================
        // 5. LÓGICA DE REDIMENSIÓN
        // ==========================================

        private void Handle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_primarySelection == null) return;

            SaveUndoSnapshot();

            var rect = sender as Rectangle;
            _currentResizeDir = (ResizeDirection)rect.Tag;

            var item = _primarySelection as FrameworkElement;
            _initSelLeft = Canvas.GetLeft(item);
            _initSelTop = Canvas.GetTop(item);
            _initSelWidth = item.ActualWidth;
            _initSelHeight = item.ActualHeight;

            if (double.IsNaN(_initSelLeft)) _initSelLeft = 0;
            if (double.IsNaN(_initSelTop)) _initSelTop = 0;

            _resizeClickStart = e.GetPosition(DesignSurface);

            rect.CaptureMouse();
            e.Handled = true;
        }

        private void Handle_MouseMove(object sender, MouseEventArgs e)
        {
            if (_currentResizeDir == ResizeDirection.None || _primarySelection == null) return;

            var item = _primarySelection as FrameworkElement;
            Point currentPos = e.GetPosition(DesignSurface);

            double deltaX = SnapToGrid(currentPos.X - _resizeClickStart.X);
            double deltaY = SnapToGrid(currentPos.Y - _resizeClickStart.Y);

            double newW = _initSelWidth;
            double newH = _initSelHeight;
            double newL = _initSelLeft;
            double newT = _initSelTop;

            switch (_currentResizeDir)
            {
                case ResizeDirection.Right: newW += deltaX; break;
                case ResizeDirection.Bottom: newH += deltaY; break;
                case ResizeDirection.BottomRight: newW += deltaX; newH += deltaY; break;

                case ResizeDirection.Left:
                    newW -= deltaX;
                    newL += deltaX;
                    break;

                case ResizeDirection.Top:
                    newH -= deltaY;
                    newT += deltaY;
                    break;

                case ResizeDirection.TopRight:
                    newW += deltaX;
                    newH -= deltaY;
                    newT += deltaY;
                    break;

                case ResizeDirection.BottomLeft:
                    newW -= deltaX;
                    newL += deltaX;
                    newH += deltaY;
                    break;

                case ResizeDirection.TopLeft:
                    newW -= deltaX;
                    newL += deltaX;
                    newH -= deltaY;
                    newT += deltaY;
                    break;
            }

            if (newW >= 8)
            {
                item.Width = newW;
                Canvas.SetLeft(item, newL);
            }
            if (newH >= 8)
            {
                item.Height = newH;
                Canvas.SetTop(item, newT);
            }

            UpdateSelectionVisuals();
        }

        private void Handle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var rect = sender as Rectangle;
            rect?.ReleaseMouseCapture();
            _currentResizeDir = ResizeDirection.None;
        }

        // ==========================================
        // 6. UTILIDADES
        // ==========================================

        private double SnapToGrid(double val) => Math.Round(val / 8.0) * 8.0;

        private void DesignSurface_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent("ControlToolboxItem") ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void DesignSurface_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("ControlToolboxItem"))
            {
                SaveUndoSnapshot();
                string controlType = e.Data.GetData("ControlToolboxItem") as string;
                Point dropPos = e.GetPosition(DesignSurface);

                UIElement newControl = RetroControlFactory.Create(controlType);
                if (newControl != null)
                {
                    double x = SnapToGrid(dropPos.X);
                    double y = SnapToGrid(dropPos.Y);

                    Canvas.SetLeft(newControl, x);
                    Canvas.SetTop(newControl, y);
                    AddControlToCanvas(newControl, x, y);

                    ClearSelection();
                    AddToSelection(newControl);
                    NotifySelectionChanged();
                }
                e.Handled = true;
            }
        }

        public void AddControlToCanvas(UIElement control, double x, double y)
        {
            if (control == null) return;
            control.PreviewMouseDown += Control_PreviewMouseDown;
            control.PreviewMouseMove += Control_PreviewMouseMove;
            control.PreviewMouseUp += Control_PreviewMouseUp;

            if (!DesignSurface.Children.Contains(control))
                DesignSurface.Children.Add(control);
        }

        public UIElement CreateRetroControl(string type) => RetroControlFactory.Create(type);

        public void DeleteSelectedControl()
        {
            if (_selectedControls.Count == 0) return;

            SaveUndoSnapshot();

            // IMPORTANTE: .ToList() para evitar excepción al modificar colección durante iteración
            var toDelete = _selectedControls.ToList();

            foreach (var ctrl in toDelete)
            {
                DesignSurface.Children.Remove(ctrl);

                if (_selectionAdorners.ContainsKey(ctrl))
                {
                    DesignSurface.Children.Remove(_selectionAdorners[ctrl]);
                    _selectionAdorners.Remove(ctrl);
                }
            }

            ClearSelection();
            NotifySelectionChanged();
        }


        // ==========================================
        // 7. HERRAMIENTAS DE ALINEACIÓN Y DISTRIBUCIÓN
        // ==========================================

        public void AlignSelected(string operation)
        {
            if (_selectedControls.Count < 2) return;

            SaveUndoSnapshot();

            // Necesitamos una referencia. 
            // Opción A: El primer seleccionado (Primary).
            // Opción B: El control que esté más al extremo (ej: más a la izquierda).
            // Usaremos la lógica de VB6/VS: El último seleccionado suele ser el "Primary", 
            // pero aquí usaremos el control "Dominante" según la operación (ej: el que está más a la izquierda para AlignLeft).

            switch (operation)
            {
                case "Left":
                    // Alinea todos al borde izquierdo del control que esté más a la izquierda
                    double minLeft = _selectedControls.Min(c => Canvas.GetLeft(c));
                    foreach (var ctrl in _selectedControls) Canvas.SetLeft(ctrl, minLeft);
                    break;

                case "Center":
                    // Alinea los centros verticales
                    // Usamos el promedio o el del control principal. Usaremos el del último seleccionado como ancla.
                    var anchorC = _selectedControls.Last() as FrameworkElement;
                    double center = Canvas.GetLeft(anchorC) + (anchorC.ActualWidth / 2);

                    foreach (var ctrl in _selectedControls)
                    {
                        if (ctrl is FrameworkElement fe)
                            Canvas.SetLeft(ctrl, center - (fe.ActualWidth / 2));
                    }
                    break;

                case "Right":
                    // Alinea al borde derecho del control que esté más a la derecha
                    double maxRight = _selectedControls.Max(c => Canvas.GetLeft(c) + (c as FrameworkElement).ActualWidth);
                    foreach (var ctrl in _selectedControls)
                    {
                        if (ctrl is FrameworkElement fe)
                            Canvas.SetLeft(ctrl, maxRight - fe.ActualWidth);
                    }
                    break;

                case "Top":
                    double minTop = _selectedControls.Min(c => Canvas.GetTop(c));
                    foreach (var ctrl in _selectedControls) Canvas.SetTop(ctrl, minTop);
                    break;

                case "Middle":
                    // Alinea centros horizontales
                    var anchorM = _selectedControls.Last() as FrameworkElement;
                    double mid = Canvas.GetTop(anchorM) + (anchorM.ActualHeight / 2);
                    foreach (var ctrl in _selectedControls)
                    {
                        if (ctrl is FrameworkElement fe)
                            Canvas.SetTop(ctrl, mid - (fe.ActualHeight / 2));
                    }
                    break;

                case "Bottom":
                    double maxBottom = _selectedControls.Max(c => Canvas.GetTop(c) + (c as FrameworkElement).ActualHeight);
                    foreach (var ctrl in _selectedControls)
                    {
                        if (ctrl is FrameworkElement fe)
                            Canvas.SetTop(ctrl, maxBottom - fe.ActualHeight);
                    }
                    break;

                case "SameWidth":
                    // Ancho del último seleccionado (Primary)
                    var anchorW = _selectedControls.Last() as FrameworkElement;
                    foreach (var ctrl in _selectedControls)
                    {
                        if (ctrl is FrameworkElement fe) fe.Width = anchorW.ActualWidth;
                    }
                    break;

                case "SameHeight":
                    // Alto del último seleccionado
                    var anchorH = _selectedControls.Last() as FrameworkElement;
                    foreach (var ctrl in _selectedControls)
                    {
                        if (ctrl is FrameworkElement fe) fe.Height = anchorH.ActualHeight;
                    }
                    break;
            }

            // Actualizar los bordes azules a las nuevas posiciones
            UpdateSelectionVisuals();
        }
    }
}