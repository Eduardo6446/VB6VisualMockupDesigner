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

        private bool _isTabOrderMode = false;
        private List<Border> _tabOrderIndicators = new List<Border>();
        private int _nextTabIndex = 0;


        // ==========================================
        // 2. EVENTOS DEL MOUSE (SELECCIÓN Y ARRASTRE)
        // ==========================================

        private void Control_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {

            if (_isTabOrderMode)
            {
                if (sender is Control clickedControl)
                {
                    // Asignar el nuevo índice
                    clickedControl.TabIndex = _nextTabIndex;
                    _nextTabIndex++;

                    // Refrescar visualmente TODOS los números para ver el cambio
                    // (Poco eficiente pero seguro para actualizar duplicados)
                    ShowTabIndices();
                }
                e.Handled = true; // Evitar selección/arrastre
                return;
            }


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
            var adornersToRemove = _selectionAdorners.Keys.Where(k => !_selectedControls.Contains(k)).ToList();
            foreach (var ctrl in adornersToRemove)
            {
                if (_selectionAdorners.ContainsKey(ctrl)) { DesignSurface.Children.Remove(_selectionAdorners[ctrl]); _selectionAdorners.Remove(ctrl); }
            }

            foreach (var control in _selectedControls)
            {
                var item = control as FrameworkElement;
                if (item == null) continue;

                if (!_selectionAdorners.ContainsKey(control))
                {
                    var border = new Border { BorderBrush = Brushes.Blue, BorderThickness = new Thickness(1), IsHitTestVisible = false };
                    DesignSurface.Children.Add(border);
                    _selectionAdorners[control] = border;
                }

                var visualBorder = _selectionAdorners[control];

                // USAMOS LOS HELPERS:
                double l = GetSafeLeft(item);
                double t = GetSafeTop(item);

                visualBorder.Width = item.ActualWidth + 4;
                visualBorder.Height = item.ActualHeight + 4;
                Canvas.SetLeft(visualBorder, l - 2);
                Canvas.SetTop(visualBorder, t - 2);
            }

            // B) Actualizar Handles (Cuadraditos blancos)
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

            // --- CORRECCIÓN CRÍTICA 2: Handles Blancos ---
            double l = Canvas.GetLeft(item);
            double t = Canvas.GetTop(item);

            // Si es NaN, usamos 0 para que la matemática funcione
            if (double.IsNaN(l)) l = 0;
            if (double.IsNaN(t)) t = 0;

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

            // SANITIZACIÓN: Si por error matemático llega un NaN, lo convertimos a 0
            if (double.IsNaN(x)) x = 0;
            if (double.IsNaN(y)) y = 0;

            // Suscribir eventos
            control.PreviewMouseDown += Control_PreviewMouseDown;
            control.PreviewMouseMove += Control_PreviewMouseMove;
            control.PreviewMouseUp += Control_PreviewMouseUp;

            // Asegurar posición inicial válida
            Canvas.SetLeft(control, SnapToGrid(x));
            Canvas.SetTop(control, SnapToGrid(y));

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

        /// <summary>
        /// Obtiene la posición Left real. Si es NaN (Auto), devuelve 0.
        /// </summary>
        private double GetSafeLeft(UIElement element)
        {
            double l = Canvas.GetLeft(element);
            return double.IsNaN(l) ? 0.0 : l;
        }

        /// <summary>
        /// Obtiene la posición Top real. Si es NaN (Auto), devuelve 0.
        /// </summary>
        private double GetSafeTop(UIElement element)
        {
            double t = Canvas.GetTop(element);
            return double.IsNaN(t) ? 0.0 : t;
        }

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


        // ==========================================
        // 8. ORDENAMIENTO (Z-ORDER)
        // ==========================================

        public void BringToFront()
        {
            if (_selectedControls.Count == 0) return;

            SaveUndoSnapshot();

            // Ordenamos por su índice actual para mantener el orden relativo entre ellos
            var sortedSelection = _selectedControls
                                  .OrderBy(c => DesignSurface.Children.IndexOf(c))
                                  .ToList();

            foreach (var ctrl in sortedSelection)
            {
                // El truco más simple en WPF/Canvas:
                // Quitarlo y volverlo a agregar lo pone al final de la lista visual (arriba de todo)
                DesignSurface.Children.Remove(ctrl);
                DesignSurface.Children.Add(ctrl);
            }

            // Aseguramos que el recuadro de selección (Rubberband) siga estando ENCIMA de todo
            if (SelectionRect != null && DesignSurface.Children.Contains(SelectionRect))
            {
                DesignSurface.Children.Remove(SelectionRect);
                DesignSurface.Children.Add(SelectionRect);
            }

            UpdateSelectionVisuals(); // Re-dibujar los bordes azules encima
        }

        public void SendToBack()
        {
            if (_selectedControls.Count == 0) return;

            SaveUndoSnapshot();

            // Para enviar al fondo, iteramos al revés para no invertir el orden relativo
            var sortedSelection = _selectedControls
                                  .OrderByDescending(c => DesignSurface.Children.IndexOf(c))
                                  .ToList();

            foreach (var ctrl in sortedSelection)
            {
                // Insertar en el índice 0 lo pone al fondo de todo
                DesignSurface.Children.Remove(ctrl);
                DesignSurface.Children.Insert(0, ctrl);
            }

            UpdateSelectionVisuals();
        }

        // ==========================================
        // 9. Panel bidireccional de propiedades
        // ==========================================

        // Método público para forzar el redibujado de los bordes azules
        // (Llamado desde MainWindow cuando el Panel de Propiedades cambia algo)
        public void RefreshSelectionVisuals()
        {
            UpdateSelectionVisuals();
        }


        // ==========================================
        // 10. EDITOR DE TAB ORDER (TAB INDEX)
        // ==========================================


        public void ToggleTabOrderMode()
        {
            _isTabOrderMode = !_isTabOrderMode;

            if (_isTabOrderMode)
            {
                // Entrar al modo
                ClearSelection();

                // --- CORRECCIÓN ---
                // Reiniciamos el contador AQUÍ, solo cuando activas la herramienta.
                _nextTabIndex = 0;

                ShowTabIndices();
            }
            else
            {
                // Salir del modo
                HideTabIndices();
            }
        }

        private void ShowTabIndices()
        {
            HideTabIndices();

            var newIndicators = new List<Border>();

            foreach (UIElement child in DesignSurface.Children)
            {
                // Filtramos solo Controles (Botones, TextBoxes, etc.) ignorando adornos
                if (child is Control control && !(child is Border))
                {
                    var border = new Border
                    {
                        Background = System.Windows.Media.Brushes.Blue,
                        BorderBrush = System.Windows.Media.Brushes.White,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(2),
                        Width = 20,
                        Height = 20,
                        IsHitTestVisible = false
                    };

                    // --- LÓGICA DEL # ---
                    // Si el índice es el valor máximo (int.MaxValue) o muy grande, mostramos #
                    // Esto indica que el control aún no tiene un orden definido por el usuario.
                    string indexText = (control.TabIndex >= int.MaxValue - 100) ? "#" : control.TabIndex.ToString();

                    var text = new TextBlock
                    {
                        Text = indexText,
                        Foreground = System.Windows.Media.Brushes.White,
                        FontSize = 11,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    border.Child = text;

                    double l = GetSafeLeft(control);
                    double t = GetSafeTop(control);

                    Canvas.SetLeft(border, l);
                    Canvas.SetTop(border, t);
                    Canvas.SetZIndex(border, 99999);

                    newIndicators.Add(border);
                    _tabOrderIndicators.Add(border);
                }
            }

            // Agregamos todos los indicadores de golpe al final
            foreach (var indicator in newIndicators)
            {
                DesignSurface.Children.Add(indicator);
            }

            
        }

        private void HideTabIndices()
        {
            foreach (var indicator in _tabOrderIndicators)
            {
                DesignSurface.Children.Remove(indicator);
            }
            _tabOrderIndicators.Clear();
        }



    }
}