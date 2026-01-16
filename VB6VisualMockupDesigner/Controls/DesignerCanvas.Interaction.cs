using System; // Para Math, Exception
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media; // <--- FALTABA: Para Brushes, ScaleTransform, VisualBrush
using System.Windows.Media.Imaging; // <--- FALTABA: Para RenderTargetBitmap
using System.Windows.Shapes; // <--- FALTABA: Para Rectangle
using VB6VisualMockupDesigner.Controls; // <--- VITAL: Para encontrar el DesignerCanvas
using VB6VisualMockupDesigner.Services; // Para ThemeManager
using VB6VisualMockupDesigner.Models;   // Por si acaso usas modelos ahí
using System.IO; // <--- Agrega esto para arreglar FileStream y FileMode
using System;

namespace VB6VisualMockupDesigner.Controls
{

    public static class VB6Data
    {
        public static readonly DependencyProperty IndexProperty =
            DependencyProperty.RegisterAttached("Index", typeof(int?), typeof(VB6Data), new PropertyMetadata(null));

        public static void SetIndex(DependencyObject element, int? value) => element.SetValue(IndexProperty, value);

        public static int? GetIndex(DependencyObject element) => (int?)element.GetValue(IndexProperty);


        public static readonly DependencyProperty IsLockedProperty =
            DependencyProperty.RegisterAttached("IsLocked", typeof(bool), typeof(VB6Data), new PropertyMetadata(false));

        public static void SetIsLocked(DependencyObject element, bool value) => element.SetValue(IsLockedProperty, value);
        public static bool GetIsLocked(DependencyObject element) => (bool)element.GetValue(IsLockedProperty);
    }


    // PARTIAL CLASS: Manejo de Interacción con Controles
    public partial class DesignerCanvas
    {
        // ==========================================
        // 1. VARIABLES DE ESTADO Y COLECCIONES
        // ==========================================

        private bool _isSelectingArea = false;
        private Point _areaStartPoint;

        private readonly HashSet<UIElement> _selectedControls = new HashSet<UIElement>();
        private Dictionary<UIElement, Border> _selectionAdorners = new Dictionary<UIElement, Border>();
        private Dictionary<UIElement, Point> _initialPositions = new Dictionary<UIElement, Point>();

        //private List<string> _clipboardControls = new List<string>();

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

        public List<FrameworkElement> GetSelectedControls()
        {
            // 'OfType' filtra y convierte a FrameworkElement, 'ToList' crea la lista requerida
            return _selectedControls.OfType<FrameworkElement>().ToList();
        }

        // ==========================================
        // NUEVOS MÉTODOS DE BLOQUEO
        // ==========================================

        public void LockSelected()
        {
            if (_selectedControls.Count == 0) return;
            SaveUndoSnapshot();
            foreach (var ctrl in _selectedControls)
            {
                VB6Data.SetIsLocked(ctrl, true);
            }
            UpdateSelectionVisuals(); // Refrescar para quitar los handles
        }

        public void UnlockSelected()
        {
            if (_selectedControls.Count == 0) return;
            SaveUndoSnapshot();
            foreach (var ctrl in _selectedControls)
            {
                VB6Data.SetIsLocked(ctrl, false);
            }
            UpdateSelectionVisuals(); // Refrescar para mostrar los handles
        }

        // ==========================================
        // HELPER DE ESTADO DE BLOQUEO
        // ==========================================

        public bool IsSelectionLocked()
        {
            if (_selectedControls.Count == 0) return false;

            // Si AL MENOS UNO está bloqueado, consideramos la selección como "mixta/bloqueada" visualmente
            // o podemos ser estrictos: solo devuelve true si TODOS están bloqueados.
            // Convención estándar: Si todos están bloqueados -> True. Si hay mezcla -> False.

            foreach (var ctrl in _selectedControls)
            {
                if (!VB6Data.GetIsLocked(ctrl)) return false; // Encontró uno desbloqueado
            }
            return true; // Todos están bloqueados
        }



        // ==========================================
        // 2. EVENTOS DEL MOUSE (SELECCIÓN Y ARRASTRE)
        // ==========================================

        private void Control_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // 1. Convertir sender a FrameworkElement
            var control = sender as FrameworkElement;
            if (control == null) return;

            control.Focus();

            // ================================================================
            // SOLUCIÓN DEFINITIVA: FILTRO INTELIGENTE DE PADRES/HIJOS
            // ================================================================
            if (control is GroupBox)
            {
                // El objeto exacto que recibió el clic (puede ser un borde, un texto, o el canvas interno)
                DependencyObject clickedObject = e.OriginalSource as DependencyObject;

                // Subimos desde lo que tocamos hasta llegar al Frame
                while (clickedObject != null && clickedObject != control)
                {
                    // Si en el camino encontramos un control interactivo (Botón, TextBox, CheckBox...)
                    // SIGNIFICA QUE EL CLIC ERA PARA EL HIJO, NO PARA EL FRAME.
                    if (clickedObject is Control || clickedObject is TextBlock)
                    {
                        // Pero ojo: El ContentPresenter o el Canvas interno NO cuentan como controles interactivos
                        if (!(clickedObject is ContentPresenter) && !(clickedObject is Panel))
                        {
                            return; // Dejamos pasar el evento para que lo maneje el hijo
                        }
                    }
                    clickedObject = VisualTreeHelper.GetParent(clickedObject);
                }
                // Si llegamos aquí, es que tocamos el fondo del Frame o su borde, así que LO SELECCIONAMOS.
            }
            // ================================================================


            // 2. DETECCIÓN DE DOBLE CLIC
            if (e.ClickCount == 2)
            {
                StartQuickEdit(control);
                e.Handled = true;
                return;
            }

            // 3. LÓGICA DE TAB ORDER (Si aplica)
            if (_isTabOrderMode)
            {
                if (sender is Control c) { c.TabIndex = _nextTabIndex++; ShowTabIndices(); }
                e.Handled = true;
                return;
            }

            // 4. SELECCIÓN MULTIPLE (Ctrl)
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

            // 5. SI ESTÁ BLOQUEADO, NO ARRASTRAMOS
            if (VB6Data.GetIsLocked(control))
            {
                e.Handled = true;
                NotifySelectionChanged();
                return;
            }

            // 6. INICIAR ARRASTRE (DRAGGING)
            // Esto es lo que fallaba antes: al no llegar aquí, no se activaba el flag _isDragging
            _isDragging = true;
            _dragStartPoint = e.GetPosition(DesignSurface);
            _hasSavedUndoForDrag = false;

            _initialPositions.Clear();
            foreach (var item in _selectedControls)
            {
                _initialPositions[item] = new Point(Canvas.GetLeft(item), Canvas.GetTop(item));
            }

            // Capturamos el mouse para que el arrastre sea fluido aunque salgamos del control rápido
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

                // --- INICIO DEL CAMBIO ---
                // Si nos estamos moviendo realmente y NO hemos guardado el estado previo todavía:
                if (!_hasSavedUndoForDrag)
                {
                    SaveUndoSnapshot();      // 1. Guardamos CÓMO ESTABAN antes de mover
                    _hasSavedUndoForDrag = true; // 2. Marcamos para no guardar 100 veces mientras arrastra
                }
                // --- FIN DEL CAMBIO ---

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
                this.Focus();
            }
        }

        private void Control_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            var control = sender as FrameworkElement;

            if (_isDragging)
            {
                control?.ReleaseMouseCapture();
                _isDragging = false;                
            }

            // Guardar Snapshot para Undo (esto ya lo tenías, asegúrate de mantenerlo)
            if (!_hasSavedUndoForDrag) SaveUndoSnapshot();

            // === AGREGAR ESTO: INTENTAR CAMBIAR DE PADRE ===
            // Solo si estamos moviendo un solo control (para evitar caos en selección múltiple por ahora)
            if (_selectedControls.Count == 1)
            {
                HandleReparenting(control);

                // Re-seleccionar para actualizar visuales (los bordes azules pueden perderse al cambiar de padre)
                this.Dispatcher.Invoke(() => {
                    UpdateSelectionVisuals();
                }, System.Windows.Threading.DispatcherPriority.Render);
            }
            // ===============================================

            NotifySelectionChanged();

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
                    var border = new Border
                    {
                        BorderBrush = Brushes.Blue,
                        BorderThickness = new Thickness(1),
                        IsHitTestVisible = false,
                        SnapsToDevicePixels = true,
                        UseLayoutRounding = true
                    };
                    DesignSurface.Children.Add(border);
                    // Aseguramos que el borde azul esté SIEMPRE encima de todo (Z-Index alto)
                    Panel.SetZIndex(border, int.MaxValue - 10);
                    _selectionAdorners[control] = border;
                }

                var visualBorder = _selectionAdorners[control];

                // Feedback visual de bloqueo
                bool isLocked = VB6Data.GetIsLocked(control);
                visualBorder.BorderBrush = isLocked ? Brushes.Gray : Brushes.Blue;

                // === CAMBIO CRÍTICO: COORDENADAS GLOBALES ===
                // En lugar de GetSafeLeft (que da 10 si está dentro de un frame),
                // usamos TranslatePoint para obtener la posición real en pantalla (ej: 160).
                Point globalPos = item.TranslatePoint(new Point(0, 0), DesignSurface);

                double globalLeft = globalPos.X;
                double globalTop = globalPos.Y;
                // ============================================

                visualBorder.Width = item.ActualWidth + 4;
                visualBorder.Height = item.ActualHeight + 4;

                // Usamos las coordenadas globales calculadas
                Canvas.SetLeft(visualBorder, globalLeft - 2);
                Canvas.SetTop(visualBorder, globalTop - 2);
            }

            // B) Actualizar Handles (Cuadraditos blancos)
            bool primaryLocked = _primarySelection != null && VB6Data.GetIsLocked(_primarySelection);

            if (_selectedControls.Count == 1 && !primaryLocked)
            {
                var item = _primarySelection as FrameworkElement;
                if (_resizeHandles.Count == 0) CreateResizeHandles();
                foreach (var h in _resizeHandles) h.Visibility = Visibility.Visible;

                // NOTA: También debemos actualizar la lógica interna de UpdateHandlePositions
                UpdateHandlePositions(item);
            }
            else
            {
                foreach (var h in _resizeHandles) h.Visibility = Visibility.Collapsed;
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
            if (item == null) return;

            // === USAR LA MISMA LÓGICA GLOBAL ===
            Point globalPos = item.TranslatePoint(new Point(0, 0), DesignSurface);
            double l = globalPos.X;
            double t = globalPos.Y;
            double w = item.ActualWidth;
            double h = item.ActualHeight;
            // ===================================

            // El resto es pura matemática de posicionamiento (TopLeft, TopRight, etc.)
            // Asumiendo que tus handles están en una lista _resizeHandles en orden:
            // 0:TL, 1:TM, 2:TR, 3:RM, 4:BR, 5:BM, 6:BL, 7:LM

            double offset = 6; // Mitad del tamaño del handle (suponiendo 12x12 o similar)

            // Top-Left
            MoveHandleTo(_resizeHandles[0], l - offset, t - offset);
            // Top-Middle
            MoveHandleTo(_resizeHandles[1], l + w / 2 - offset, t - offset);
            // Top-Right
            MoveHandleTo(_resizeHandles[2], l + w - offset, t - offset);

            // Right-Middle
            MoveHandleTo(_resizeHandles[3], l + w - offset, t + h / 2 - offset);

            // Bottom-Right
            MoveHandleTo(_resizeHandles[4], l + w - offset, t + h - offset);
            // Bottom-Middle
            MoveHandleTo(_resizeHandles[5], l + w / 2 - offset, t + h - offset);
            // Bottom-Left
            MoveHandleTo(_resizeHandles[6], l - offset, t + h - offset);

            // Left-Middle
            MoveHandleTo(_resizeHandles[7], l - offset, t + h / 2 - offset);
        }

        private void MoveHandleTo(UIElement handle, double x, double y)
        {
            Canvas.SetLeft(handle, x);
            Canvas.SetTop(handle, y);
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

        private double SnapToGrid(double val)
        {
            if (!_isSnappingEnabled) return val; // Si no hay imán, movimiento libre (1px)

            return Math.Round(val / _gridSize) * _gridSize;
        }

        private void DesignSurface_DragOver(object sender, DragEventArgs e)
        {
            // Validar si el dato es válido
            if (e.Data.GetDataPresent("ControlToolboxItem"))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void DesignSurface_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("ControlToolboxItem"))
            {
                SaveUndoSnapshot();

                string controlType = e.Data.GetData("ControlToolboxItem") as string;
                Point dropPos = e.GetPosition(DesignSurface);

                // Llamamos al nuevo método centralizado
                CreateControlAt(controlType, dropPos);
            }
            e.Handled = true;
        }


        public void CreateControlAt(string type, Point position)
        {
            UIElement newControl = RetroControlFactory.Create(type);

            if (newControl != null)
            {
                // Asignar tamaño por defecto si viene sin medidas
                if (newControl is FrameworkElement fe)
                {
                    if (double.IsNaN(fe.Width) || fe.Width == 0) fe.Width = 100;
                    if (double.IsNaN(fe.Height) || fe.Height == 0) fe.Height = 35;
                }

                // AddControlToCanvas ya se encarga de:
                // 1. Snap to Grid
                // 2. Children.Add
                // 3. Conectar eventos (MouseDown, etc)
                AddControlToCanvas(newControl, position.X, position.Y);

                // Seleccionar automáticamente el nuevo control
                ClearSelection();
                AddToSelection(newControl);
                NotifySelectionChanged();

                GenerateNewVersion();
            }
        }

        public void AddControlToCanvas(UIElement control, double x, double y)
        {
            if (control == null) return;

            if (double.IsNaN(x)) x = 0;
            if (double.IsNaN(y)) y = 0;

            // Suscribir eventos
            control.PreviewMouseDown += Control_PreviewMouseDown;
            control.PreviewMouseMove += Control_PreviewMouseMove;
            control.PreviewMouseUp += Control_PreviewMouseUp;

            if (control is FrameworkElement fe)
            {
                // Asumiendo que tienes el recurso ControlContextMenu definido en XAML
                fe.ContextMenu = (ContextMenu)this.Resources["ControlContextMenu"];
            }

            // Aplicar Grid Snapping y Posición
            Canvas.SetLeft(control, SnapToGrid(x));
            Canvas.SetTop(control, SnapToGrid(y));

            // Evitar duplicados
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


        public void SaveAsImage(string filePath)
        {
            // 1. Guardar la selección actual
            var currentSelection = _selectedControls.ToList();
            double scale = 3.0; // <--- 1.0 es calidad normal, 2.0 es retina, 3.0 es alta resolución
            double dpi = 96d;


            // 2. Limpiar selección visualmente
            ClearSelection();

            // Forzamos que WPF recalcule el layout visual YA, para asegurar que no hay bordes azules
            this.UpdateLayout();

            // 3. Elemento a capturar
            FrameworkElement elementToCapture = this.WindowResizerGrid;

            // Validación de seguridad: Si el ancho/alto es 0, no hay nada que guardar
            if (elementToCapture.ActualWidth == 0 || elementToCapture.ActualHeight == 0)
            {
                RestoreSelection(currentSelection);
                MessageBox.Show("El formulario tiene un tamaño inválido.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }



            // 4. TRUCO PARA EVITAR LA IMAGEN NEGRA: USAR DRAWINGVISUAL
            // En lugar de renderizar el grid directamente, creamos un "Lienzo Virtual"
            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext context = drawingVisual.RenderOpen())
            {
                context.PushTransform(new ScaleTransform(scale, scale));

                VisualBrush brush = new VisualBrush(elementToCapture);

                // El pincel sigue tomando el tamaño original del control
                context.DrawRectangle(brush, null, new Rect(0, 0, elementToCapture.ActualWidth, elementToCapture.ActualHeight));

                context.Pop(); // Cerramos la transformación
            }

            // 5. Renderizamos el Lienzo Virtual (no el grid directo)
            RenderTargetBitmap bmp = new RenderTargetBitmap(
               (int)(elementToCapture.ActualWidth * scale),  // <--- Ancho x3
                (int)(elementToCapture.ActualHeight * scale), // <--- Alto x3
                dpi,
                dpi,
                PixelFormats.Pbgra32);

            bmp.Render(drawingVisual); // Renderizamos el visual que acabamos de dibujar

            // 6. Guardar en disco
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));

            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(fs);
            }

            // 7. Restaurar la selección
            RestoreSelection(currentSelection);
        }

        // Helper pequeño para no repetir código de restauración
        private void RestoreSelection(List<UIElement> selection)
        {
            foreach (var ctrl in selection)
            {
                AddToSelection(ctrl);
            }
            NotifySelectionChanged();
        }


        // ==========================================
        // 11. ZOOM
        // ==========================================

        public void SetZoom(double zoomPercentage, Point? relativeMousePos = null)
        {
            if (CanvasScale == null || MainScrollViewer == null) return;

            double oldFactor = CanvasScale.ScaleX;
            double newFactor = zoomPercentage / 100.0;

            // 1. Determinar el punto de anclaje (Pivote)
            // Si nos dan la posición del mouse, la usamos.
            // Si no (ej: desde el slider), usamos el centro exacto de la pantalla visible.
            Point targetPoint;
            if (relativeMousePos.HasValue)
            {
                targetPoint = relativeMousePos.Value;
            }
            else
            {
                targetPoint = new Point(MainScrollViewer.ViewportWidth / 2, MainScrollViewer.ViewportHeight / 2);
            }

            // 2. Calcular qué punto del CONTENIDO REAL está bajo ese pivote
            // (Offset actual + Posición en pantalla) / Factor Viejo = Posición Real sin escala
            double absoluteX = (MainScrollViewer.HorizontalOffset + targetPoint.X) / oldFactor;
            double absoluteY = (MainScrollViewer.VerticalOffset + targetPoint.Y) / oldFactor;

            // 3. Aplicar el nuevo Zoom
            CanvasScale.ScaleX = newFactor;
            CanvasScale.ScaleY = newFactor;

            // 4. IMPORTANTE: Forzar actualización del layout
            // El ScrollViewer necesita recalcular el tamaño del contenido (Extent) ANTES de que movamos el scroll.
            this.UpdateLayout();

            // 5. Calcular y aplicar el nuevo Scroll
            // (Posición Real * Nuevo Factor) - Posición en pantalla = Nuevo Offset
            double newH = (absoluteX * newFactor) - targetPoint.X;
            double newV = (absoluteY * newFactor) - targetPoint.Y;

            MainScrollViewer.ScrollToHorizontalOffset(newH);
            MainScrollViewer.ScrollToVerticalOffset(newV);
        }

        // ==========================================
        // 12. GRID & SNAPPING
        // ==========================================

        private bool _isSnappingEnabled = true;
        private bool _isGridVisible = false;
        private double _gridSize = 8.0;

        public void ToggleSnapping()
        {
            _isSnappingEnabled = !_isSnappingEnabled;
        }

        public void ToggleGrid()
        {
            _isGridVisible = !_isGridVisible;

            // Accedemos al Grid principal definido en el XAML
            if (DesignSurface != null)
            {
                // Si es visible, restauramos el Brush de recursos. Si no, transparente.
                DesignSurface.Background = _isGridVisible
                    ? (Brush)FindResource("GridPatternBrush")
                    : Brushes.Transparent;
            }
        }

        public void SetGridSize(double size)
        {
            _gridSize = size;

            // Actualizar visualmente el tamaño de los puntitos
            if (FindResource("GridPatternBrush") is DrawingBrush brush)
            {
                brush.Viewport = new Rect(0, 0, size, size);
            }
        }


        // ==========================================
        // 13. DISTRIBUCIÓN (Espaciado Equitativo)
        // ==========================================

        public void DistributeSelected(string axis)
        {
            // Necesitamos al menos 3 controles para que distribuir tenga sentido
            // (Con 2 controles, distribuir no hace nada porque los extremos no se mueven)
            if (_selectedControls.Count < 3) return;

            SaveUndoSnapshot();

            // Convertir a lista para poder ordenar
            var sortedList = _selectedControls.OfType<FrameworkElement>().ToList();

            if (axis == "Horizontal")
            {
                // 1. Ordenar por posición visual actual (de Izquierda a Derecha)
                sortedList.Sort((a, b) => Canvas.GetLeft(a).CompareTo(Canvas.GetLeft(b)));

                // 2. Identificar extremos (que NO se moverán)
                var first = sortedList.First();
                var last = sortedList.Last();

                // 3. Calcular espacio total disponible para huecos
                // Distancia desde el final del primero hasta el inicio del último
                double startEdge = Canvas.GetLeft(first) + first.ActualWidth;
                double endEdge = Canvas.GetLeft(last);
                double totalSpan = endEdge - startEdge;

                // 4. Restar el ancho de los controles intermedios
                double middleItemsWidth = 0;
                for (int i = 1; i < sortedList.Count - 1; i++)
                {
                    middleItemsWidth += sortedList[i].ActualWidth;
                }

                // 5. Calcular el tamaño del hueco (Gap)
                double availableSpace = totalSpan - middleItemsWidth;
                double gap = availableSpace / (sortedList.Count - 1);

                // 6. Aplicar nuevas posiciones
                double currentLeft = startEdge + gap;
                for (int i = 1; i < sortedList.Count - 1; i++)
                {
                    Canvas.SetLeft(sortedList[i], Math.Floor(currentLeft)); // Floor para evitar medios pixeles
                    currentLeft += sortedList[i].ActualWidth + gap;
                }
            }
            else if (axis == "Vertical")
            {
                // 1. Ordenar de Arriba a Abajo
                sortedList.Sort((a, b) => Canvas.GetTop(a).CompareTo(Canvas.GetTop(b)));

                var first = sortedList.First();
                var last = sortedList.Last();

                double startEdge = Canvas.GetTop(first) + first.ActualHeight;
                double endEdge = Canvas.GetTop(last);
                double totalSpan = endEdge - startEdge;

                double middleItemsHeight = 0;
                for (int i = 1; i < sortedList.Count - 1; i++)
                {
                    middleItemsHeight += sortedList[i].ActualHeight;
                }

                double availableSpace = totalSpan - middleItemsHeight;
                double gap = availableSpace / (sortedList.Count - 1);

                double currentTop = startEdge + gap;
                for (int i = 1; i < sortedList.Count - 1; i++)
                {
                    Canvas.SetTop(sortedList[i], Math.Floor(currentTop));
                    currentTop += sortedList[i].ActualHeight + gap;
                }
            }

            UpdateSelectionVisuals();
        }


        // ==========================================
        // LÓGICA DEL MENÚ CONTEXTUAL
        // ==========================================

        private void MnuCut_Click(object sender, RoutedEventArgs e) => CutSelected();
        private void MnuCopy_Click(object sender, RoutedEventArgs e) => CopySelected();
        private void MnuPaste_Click(object sender, RoutedEventArgs e) => Paste();
        private void MnuDelete_Click(object sender, RoutedEventArgs e) => DeleteSelectedControl();
        private void MnuBringToFront_Click(object sender, RoutedEventArgs e) => BringToFront();
        private void MnuSendToBack_Click(object sender, RoutedEventArgs e) => SendToBack();

        private void MnuLock_Click(object sender, RoutedEventArgs e)
        {
            // Verificamos el estado actual para alternar
            if (IsSelectionLocked())
                UnlockSelected();
            else
                LockSelected();
        }

        // Evento inteligente: Se ejecuta justo antes de mostrar el menú
        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            var menu = sender as ContextMenu;
            if (menu == null) return;

            // 1. Gestionar opción de Bloqueo/Desbloqueo
            // Buscamos el item por nombre (definido en XAML como x:Name="MnuLockItem")
            // Nota: En WPF a veces es difícil acceder por nombre dentro de templates, 
            // así que lo buscamos en la colección de items.

            foreach (var item in menu.Items)
            {
                if (item is MenuItem menuItem && menuItem.Name == "MnuLockItem")
                {
                    if (IsSelectionLocked())
                    {
                        menuItem.Header = "Desbloquear Controles";
                        // Icono opcional: Candado abierto
                        if (menuItem.Icon is TextBlock icon) icon.Text = "\xE785";
                    }
                    else
                    {
                        menuItem.Header = "Bloquear Controles";
                        // Icono opcional: Candado cerrado
                        if (menuItem.Icon is TextBlock icon) icon.Text = "\xE72E";
                    }
                    break;
                }
            }

            // 2. Deshabilitar opciones si no hay selección
            bool hasSelection = _selectedControls.Count > 0;

            // Recorremos para habilitar/deshabilitar Cortar, Copiar, Borrar
            foreach (var item in menu.Items)
            {
                if (item is MenuItem mi)
                {
                    if ((string)mi.Header == "Cortar" ||
                        (string)mi.Header == "Copiar" ||
                        (string)mi.Header == "Eliminar" ||
                        (string)mi.Header == "Traer al Frente" ||
                        (string)mi.Header == "Enviar al Fondo")
                    {
                        mi.IsEnabled = hasSelection;
                    }
                }
            }
        }


        // ==========================================
        // 14. EDICIÓN RÁPIDA (QUICK EDIT)
        // ==========================================

        private FrameworkElement _controlBeingEdited;

        // 1. Detectar el Doble Clic
        private void StartQuickEdit(FrameworkElement control)
        {
            if (control == null) return;

            // Solo permitimos editar si no está bloqueado
            if (VB6Data.GetIsLocked(control)) return;

            // Determinamos qué propiedad vamos a editar
            string currentText = "";

            if (control is ContentControl cc) currentText = cc.Content?.ToString();
            else if (control is TextBox tb) currentText = tb.Text;
            else if (control is TextBlock txt) currentText = txt.Text;
            else return; // No es editable

            // Guardamos referencia
            _controlBeingEdited = control;

            // Configurar y Mostrar la Caja Flotante
            QuickEditBox.Text = currentText;
            QuickEditBox.Width = control.ActualWidth;
            QuickEditBox.Height = Math.Max(control.ActualHeight, 24);

            double l = Canvas.GetLeft(control);
            double t = Canvas.GetTop(control);
            Canvas.SetLeft(QuickEditBox, l);
            Canvas.SetTop(QuickEditBox, t);

            QuickEditBox.Visibility = Visibility.Visible;
            QuickEditBox.Focus();
            QuickEditBox.SelectAll();

            // Nota: Ya no necesitamos e.Handled aquí porque lo manejamos en el MouseDown
        }

        // 2. Confirmar con Enter (o Shift+Enter para nueva línea)
        private void QuickEditBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // CASO 1: Presionó ENTER
            // 1. CASO ENTER
            if (e.Key == Key.Enter)
            {
                // Si presionan Shift + Enter, dejamos pasar el evento para que haga salto de línea
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                {
                    return;
                }

                // Si es solo Enter -> GUARDAR
                e.Handled = true; // ¡IMPORTANTE! Esto mata el evento antes de que el TextBox cree una nueva línea
                CommitQuickEdit();
            }
            // 2. CASO ESCAPE
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                CancelQuickEdit();
            }
        }

        // 3. Confirmar al perder el foco (clic afuera)
        private void QuickEditBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Solo guardamos si sigue visible (para evitar doble commit)
            if (QuickEditBox.Visibility == Visibility.Visible)
            {
                CommitQuickEdit();
            }
        }

        // 4. Lógica de Guardado
        private void CommitQuickEdit()
        {
            if (_controlBeingEdited == null) return;

            string newText = QuickEditBox.Text;
            string oldText = "";

            // Obtener valor anterior para ver si cambió
            if (_controlBeingEdited is ContentControl cc) oldText = cc.Content?.ToString();
            else if (_controlBeingEdited is TextBox tb) oldText = tb.Text;
            else if (_controlBeingEdited is TextBlock txt) oldText = txt.Text;

            if (newText != oldText)
            {
                // ¡GUARDAR SNAPSHOT PARA UNDO!
                SaveUndoSnapshot();

                // Aplicar cambio
                if (_controlBeingEdited is ContentControl cc2) cc2.Content = newText;
                else if (_controlBeingEdited is TextBox tb2) tb2.Text = newText;
                else if (_controlBeingEdited is TextBlock txt2) txt2.Text = newText;

                // Avisar que cambió la selección (para actualizar el Panel de Propiedades si está abierto)
                NotifySelectionChanged();
            }

            // Ocultar y limpiar
            QuickEditBox.Visibility = Visibility.Collapsed;
            _controlBeingEdited = null;
        }

        private void CancelQuickEdit()
        {
            QuickEditBox.Visibility = Visibility.Collapsed;
            _controlBeingEdited = null;
        }

        // ==========================================
        // LÓGICA DE CONTENEDORES (PADRE-HIJO)
        // ==========================================

        private FrameworkElement GetContainerAtPoint(Point point, UIElement excludeControl)
        {
            FrameworkElement foundContainer = null;

            // Usamos HitTest con un Callback para poder "perforar" capas
            VisualTreeHelper.HitTest(
                DesignSurface,

                // 1. FILTRO: ¿Qué objetos ignoramos inmediatamente?
                (dependencyObject) =>
                {
                    // Ignorar el control que estamos arrastrando
                    if (dependencyObject == excludeControl) return HitTestFilterBehavior.ContinueSkipSelfAndChildren;

                    // Ignorar el Rectángulo de Selección y la Caja de Edición (Culpables habituales)
                    if (dependencyObject is Rectangle && (dependencyObject as FrameworkElement).Name == "SelectionRect")
                        return HitTestFilterBehavior.ContinueSkipSelf;

                    if (dependencyObject is TextBox && (dependencyObject as FrameworkElement).Name == "QuickEditBox")
                        return HitTestFilterBehavior.ContinueSkipSelf;

                    return HitTestFilterBehavior.Continue;
                },

                // 2. RESULTADO: ¿Qué hacemos cuando tocamos algo?
                (result) =>
                {
                    DependencyObject hit = result.VisualHit;

                    // Subimos por el árbol desde lo que tocamos
                    while (hit != null && hit != DesignSurface)
                    {
                        // ¿Es un GroupBox (Frame)?
                        if (hit is GroupBox)
                        {
                            foundContainer = hit as FrameworkElement;
                            return HitTestResultBehavior.Stop; // ¡ENCONTRADO! Detener búsqueda.
                        }

                        // ¿Es un Border que parece PictureBox? (Para el futuro)
                        if (hit is Border && (hit as FrameworkElement).Name.StartsWith("Picture"))
                        {
                            foundContainer = hit as FrameworkElement;
                            return HitTestResultBehavior.Stop;
                        }

                        hit = VisualTreeHelper.GetParent(hit);
                    }

                    // Si no era un contenedor, sigue buscando más abajo (Perforar)
                    return HitTestResultBehavior.Continue;
                },

                // Parámetros del punto
                new PointHitTestParameters(point)
            );

            return foundContainer;
        }

        private void HandleReparenting(FrameworkElement control)
        {

            Point mousePos = Mouse.GetPosition(DesignSurface);

            // (Ya no es estrictamente necesario apagar IsHitTestVisible con el nuevo método, 
            // pero es buena práctica mantenerlo por seguridad)
            bool wasHitVisible = control.IsHitTestVisible;
            control.IsHitTestVisible = false;

            // LLAMADA AL NUEVO RADAR
            FrameworkElement newParentContainer = GetContainerAtPoint(mousePos, control);

            control.IsHitTestVisible = wasHitVisible;

            // Identificar padre actual
            Panel oldParentPanel = VisualTreeHelper.GetParent(control) as Panel;

            // === CASO A: ENTRAR A UN FRAME ===
            if (newParentContainer != null && oldParentPanel != null)
            {
                // Verificar que no sea el mismo padre (evitar parpadeo)
                // Ojo: newParentContainer es el GroupBox, oldParentPanel es el Canvas interno
                // Hay que comparar con cuidado.

                Panel targetPanel = null;
                if (newParentContainer is GroupBox gb) targetPanel = gb.Content as Panel;

                // Si encontramos un destino válido y NO estamos ya ahí
                if (targetPanel != null && oldParentPanel != targetPanel)
                {
                    // Calculamos posición GLOBAL actual del control
                    Point globalPos = control.TranslatePoint(new Point(0, 0), DesignSurface);

                    // Calculamos posición RELATIVA al nuevo padre
                    Point relativePos = DesignSurface.TranslatePoint(globalPos, targetPanel);

                    // Mover
                    oldParentPanel.Children.Remove(control);
                    targetPanel.Children.Add(control);

                    control.Margin = new Thickness(0);
                    Canvas.SetLeft(control, relativePos.X);
                    Canvas.SetTop(control, relativePos.Y);
                }
            }
            // === CASO B: SALIR AL CANVAS PRINCIPAL ===
            else if (newParentContainer == null && oldParentPanel != DesignSurface)
            {
                // Posición GLOBAL
                Point globalPos = control.TranslatePoint(new Point(0, 0), DesignSurface);

                oldParentPanel.Children.Remove(control);
                DesignSurface.Children.Add(control);

                control.Margin = new Thickness(0);
                Canvas.SetLeft(control, globalPos.X);
                Canvas.SetTop(control, globalPos.Y);
            }
        }

        



    }
}