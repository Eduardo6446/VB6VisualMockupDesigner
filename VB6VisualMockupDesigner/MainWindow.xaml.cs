using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using VB6VisualMockupDesigner.Models;
using VB6VisualMockupDesigner.Services;
using VB6VisualMockupDesigner.Helpers;

namespace VB6VisualMockupDesigner
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>

    using VbHelpers = VB6VisualMockupDesigner.Helpers.VbHelpers;

    public partial class MainWindow : Window
    {
        // --- SERVICIOS ---
        private ControlFactory _controlFactory;
        private Vb6FileManager _fileManager;

        // --- ESTADO Y SELECCIÓN ---
        private List<UIElement> _selectedElements = new List<UIElement>();
        private List<ControlState> _stateBeforeDrag;
        private List<ClipboardData> _internalClipboard = new List<ClipboardData>();

        // --- UNDO / REDO ---
        private Stack<List<ControlState>> _undoStack = new Stack<List<ControlState>>();
        private Stack<List<ControlState>> _redoStack = new Stack<List<ControlState>>();

        // --- INTERACCIÓN ---
        private bool _isDragging = false;
        private Point _startClickPoint;
        private Dictionary<UIElement, Point> _initialPositions = new Dictionary<UIElement, Point>();
        private bool _isSelecting = false;
        private Point _selectionStartPoint;
        private AdornerLayer _adornerLayer;

        // --- MODOS ---
        private bool _isSimulationMode = false;
        private bool _isTabIndexMode = false;
        private int _nextTabIndex = 0;

        public MainWindow()
        {
            InitializeComponent();

            // Inicializar Servicios
            InitializeServices();

            // Cargar Icono
            try { this.Icon = new BitmapImage(new Uri("pack://application:,,,/Resource/icon.ico")); }
            catch { /* Ignorar si no existe, usa el default */ }

            // Inicializar Adorners al cargar
            this.Loaded += (s, e) => _adornerLayer = AdornerLayer.GetAdornerLayer(DesignCanvas);

            // IMPORTANTE: Asegurar que el Canvas maneje los eventos primero
            DesignCanvas.PreviewMouseLeftButtonDown += DesignCanvas_PreviewMouseLeftButtonDown;
        }

        private void InitializeServices()
        {
            _controlFactory = new ControlFactory();
            _fileManager = new Vb6FileManager(_controlFactory);
        }

        #region Eventos de Teclado (Globales)

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.F5)
            {
                if (_isTabIndexMode) return;
                BtnSimulate.IsChecked = !BtnSimulate.IsChecked;
                ToggleSimulationMode(BtnSimulate.IsChecked == true);
                return;
            }

            if (_isSimulationMode || _isTabIndexMode) return;

            // Edición
            if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control) Undo();
            else if (e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control) Redo();
            else if (e.Key == Key.Delete || e.Key == Key.Back) { RecordUndo(); DeleteSelectedControls(); }
            else if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control) CopyControls();
            else if (e.Key == Key.X && Keyboard.Modifiers == ModifierKeys.Control) { RecordUndo(); CutControls(); }
            else if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control) { RecordUndo(); PasteControls(); }

            // Archivo (Atajos)
            else if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control) NewDesign_Click(null, null);
            else if (e.Key == Key.O && Keyboard.Modifiers == ModifierKeys.Control) ImportFrm_Click(null, null);
        }

        #endregion

        #region Modos (Simulación y TabIndex)

        private void BtnSimulate_Click(object sender, RoutedEventArgs e)
        {
            if (_isTabIndexMode) { BtnTabIndex.IsChecked = false; ToggleTabIndexMode(false); }
            ToggleSimulationMode(BtnSimulate.IsChecked == true);
        }

        private void ToggleSimulationMode(bool enable)
        {
            _isSimulationMode = enable;
            if (_isSimulationMode)
            {
                ClearSelection();
                DesignCanvas.Background = Brushes.White;
                var stopIcon = new Canvas { Width = 16, Height = 16 };
                var rect = new Rectangle { Width = 10, Height = 10, Fill = Brushes.Red, Stroke = Brushes.DarkRed, StrokeThickness = 1 };
                Canvas.SetLeft(rect, 3); Canvas.SetTop(rect, 3);
                stopIcon.Children.Add(rect);
                BtnSimulate.Content = stopIcon;
                foreach (UIElement child in DesignCanvas.Children) { if (child is Control c && child != SelectionBox) { c.Focusable = true; c.IsHitTestVisible = true; } }
            }
            else
            {
                DesignCanvas.Background = (VisualBrush)this.Resources["VB6GridBrush"];
                var playIcon = new Canvas { Width = 16, Height = 16 };
                var poly = new Polygon { Points = new PointCollection { new Point(4, 2), new Point(14, 8), new Point(4, 14) }, Fill = Brushes.Green, Stroke = Brushes.DarkGreen, StrokeThickness = 1 };
                playIcon.Children.Add(poly);
                BtnSimulate.Content = playIcon;
                foreach (UIElement child in DesignCanvas.Children) { if (child is Control c) c.Focusable = false; }
            }
        }

        private void BtnTabIndex_Click(object sender, RoutedEventArgs e)
        {
            if (_isSimulationMode) { BtnSimulate.IsChecked = false; ToggleSimulationMode(false); }
            ToggleTabIndexMode(BtnTabIndex.IsChecked == true);
        }

        private void ToggleTabIndexMode(bool enable)
        {
            _isTabIndexMode = enable;
            if (_isTabIndexMode)
            {
                ClearSelection();
                _nextTabIndex = 0;
                RefreshTabIndexAdorners();
                DesignCanvas.Cursor = Cursors.Pen;
            }
            else
            {
                ClearAllAdorners();
                DesignCanvas.Cursor = Cursors.Arrow;
            }
        }

        private void RefreshTabIndexAdorners()
        {
            ClearAllAdorners();
            foreach (UIElement child in DesignCanvas.Children)
            {
                if (child is Control c && IsDesignerControl(c) && !(c is Label))
                {
                    _adornerLayer.Add(new TabIndexAdorner(c, c.TabIndex));
                }
            }
        }

        private void ClearAllAdorners()
        {
            if (_adornerLayer == null) return;
            Adorner[] toRemove = _adornerLayer.GetAdorners(DesignCanvas);
            if (toRemove != null) foreach (var ad in toRemove) _adornerLayer.Remove(ad);
            foreach (UIElement child in DesignCanvas.Children)
            {
                var ads = _adornerLayer.GetAdorners(child);
                if (ads != null) foreach (var ad in ads) _adornerLayer.Remove(ad);
            }
        }

        #endregion

        #region Interactividad (Mouse)

        private void DesignCanvas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isSimulationMode) return;

            // TabIndex
            if (_isTabIndexMode)
            {
                DependencyObject clickedObj = e.OriginalSource as DependencyObject;
                FrameworkElement clickedCtrl = null;
                while (clickedObj != null && clickedObj != DesignCanvas)
                {
                    if (clickedObj is Control c && IsDesignerControl(c))
                    {
                        clickedCtrl = c;
                        break;
                    }
                    clickedObj = VisualTreeHelper.GetParent(clickedObj);
                }

                if (clickedCtrl != null && clickedCtrl is Control control && !(control is Label))
                {
                    RecordUndo();
                    control.TabIndex = _nextTabIndex++;
                    RefreshTabIndexAdorners();
                }
                e.Handled = true;
                return;
            }

            // Selección Normal
            DependencyObject clickedObject = e.OriginalSource as DependencyObject;
            if (clickedObject is ContentElement ce) clickedObject = ContentOperations.GetParent(ce) ?? (ce as FrameworkContentElement)?.Parent;

            FrameworkElement clickedElement = null;
            while (clickedObject != null && clickedObject != DesignCanvas)
            {
                if (clickedObject is FrameworkElement fe && IsDesignerControl(fe)) { clickedElement = fe; break; }
                if (clickedObject is Visual || clickedObject is System.Windows.Media.Media3D.Visual3D) clickedObject = VisualTreeHelper.GetParent(clickedObject); else clickedObject = null;
            }

            if (clickedElement != null && clickedElement != SelectionBox)
            {
                _stateBeforeDrag = GetCurrentState();
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    if (_selectedElements.Contains(clickedElement)) { RemoveFromSelection(clickedElement); e.Handled = true; return; }
                    else AddToSelection(clickedElement);
                }
                else if (!_selectedElements.Contains(clickedElement))
                {
                    ClearSelection();
                    AddToSelection(clickedElement);
                }

                _isDragging = true;
                var parent = VisualTreeHelper.GetParent(clickedElement) as IInputElement;
                _startClickPoint = e.GetPosition(parent);
                _initialPositions.Clear();
                foreach (var el in _selectedElements) { var p = VisualTreeHelper.GetParent(el) as Panel; if (p != null) _initialPositions[el] = new Point(Canvas.GetLeft(el), Canvas.GetTop(el)); }
                DesignCanvas.CaptureMouse();
                e.Handled = true;
            }
            else { DesignCanvas_MouseDown(sender, e); }
        }

        private bool IsDesignerControl(FrameworkElement fe)
        {
            return !string.IsNullOrEmpty(fe.Name) && fe.Name != "SelectionBox" && fe.Name != "DesignCanvas";
        }

        private void DesignCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_isSimulationMode || _isTabIndexMode) return;
            ClearSelection();
            _isSelecting = true;
            _selectionStartPoint = e.GetPosition(DesignCanvas);
            Canvas.SetLeft(SelectionBox, _selectionStartPoint.X);
            Canvas.SetTop(SelectionBox, _selectionStartPoint.Y);
            SelectionBox.Width = 0; SelectionBox.Height = 0;
            SelectionBox.Visibility = Visibility.Visible;
            DesignCanvas.CaptureMouse();
        }

        private void DesignCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isSimulationMode || _isTabIndexMode) return;
            if (_isDragging)
            {
                if (_selectedElements.Count > 0)
                {
                    var refEl = _selectedElements[0];
                    var parent = VisualTreeHelper.GetParent(refEl) as IInputElement;
                    if (parent != null)
                    {
                        Point currentMousePos = e.GetPosition(parent);
                        double deltaX = currentMousePos.X - _startClickPoint.X;
                        double deltaY = currentMousePos.Y - _startClickPoint.Y;
                        foreach (var el in _selectedElements)
                        {
                            if (_initialPositions.ContainsKey(el))
                            {
                                Point init = _initialPositions[el];
                                double nl = Math.Round((init.X + deltaX) / 8) * 8;
                                double nt = Math.Round((init.Y + deltaY) / 8) * 8;
                                if (nl < 0) nl = 0; if (nt < 0) nt = 0;
                                Canvas.SetLeft(el, nl); Canvas.SetTop(el, nt);
                            }
                        }
                        UpdatePropertyPanel();
                    }
                }
            }
            else if (_isSelecting)
            {
                Point cur = e.GetPosition(DesignCanvas);
                double x = Math.Min(cur.X, _selectionStartPoint.X);
                double y = Math.Min(cur.Y, _selectionStartPoint.Y);
                Canvas.SetLeft(SelectionBox, x); Canvas.SetTop(SelectionBox, y);
                SelectionBox.Width = Math.Abs(cur.X - _selectionStartPoint.X);
                SelectionBox.Height = Math.Abs(cur.Y - _selectionStartPoint.Y);
            }
        }

        private void DesignCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isSimulationMode || _isTabIndexMode) return;
            if (_isDragging)
            {
                _isDragging = false;
                foreach (var el in _selectedElements) el.ReleaseMouseCapture();
                if (_stateBeforeDrag != null && !AreStatesEqual(_stateBeforeDrag, GetCurrentState())) { _undoStack.Push(_stateBeforeDrag); _redoStack.Clear(); }
            }
            if (_isSelecting)
            {
                _isSelecting = false; SelectionBox.Visibility = Visibility.Collapsed; DesignCanvas.ReleaseMouseCapture();
                SelectControlsInRect(new Rect(Canvas.GetLeft(SelectionBox), Canvas.GetTop(SelectionBox), SelectionBox.Width, SelectionBox.Height));
            }
        }

        private bool AreStatesEqual(List<ControlState> s1, List<ControlState> s2)
        {
            if (s1.Count != s2.Count) return false;
            for (int i = 0; i < s1.Count; i++) if (s1[i].Left != s2[i].Left || s1[i].Top != s2[i].Top || s1[i].ZIndex != s2[i].ZIndex) return false;
            return true;
        }

        private void Control_MouseMove(object sender, MouseEventArgs e) { if (_isDragging) DesignCanvas_MouseMove(sender, e); }
        private void Control_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) { if (_isDragging) DesignCanvas_MouseUp(sender, e); }

        #endregion

        #region Menú Archivo

        private void NewDesign_Click(object sender, RoutedEventArgs e)
        {
            if (DesignCanvas.Children.Count > 1) // Más de 1 por el SelectionBox
            {
                if (MessageBox.Show("¿Deseas comenzar un nuevo diseño? Se perderán los cambios no guardados.",
                                    "Nuevo Diseño", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                {
                    return;
                }
            }

            // Limpieza completa
            RecordUndo();
            ClearSelection();

            // Mantener solo el SelectionBox
            var selectionBox = DesignCanvas.Children.OfType<Rectangle>().FirstOrDefault(r => r.Name == "SelectionBox");
            DesignCanvas.Children.Clear();
            if (selectionBox != null) DesignCanvas.Children.Add(selectionBox);

            // Reiniciar contadores y pilas
            InitializeServices();
            _undoStack.Clear();
            _redoStack.Clear();
            UpdatePropertyPanel();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        #endregion

        #region Menú Herramientas / Archivo (Importar/Exportar)

        private void ImportFrm_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "VB6 Form (*.frm)|*.frm" };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    RecordUndo();
                    _fileManager.ParseVb6Frm(ofd.FileName, DesignCanvas, RegisterControl);
                    // Asegurar que el SelectionBox siga existiendo
                    if (!DesignCanvas.Children.Contains(SelectionBox)) DesignCanvas.Children.Add(SelectionBox);
                }
                catch (Exception ex) { MessageBox.Show("Error al importar: " + ex.Message); }
            }
        }

        private void ExportCode_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog { Filter = "VB6 Form (*.frm)|*.frm", FileName = "Form1_New.frm" };
            if (sfd.ShowDialog() == true)
            {
                try
                {
                    string code = _fileManager.GenerateVb6Code(DesignCanvas, SelectionBox);
                    File.WriteAllText(sfd.FileName, code);
                    MessageBox.Show("Código generado correctamente.");
                }
                catch (Exception ex) { MessageBox.Show("Error al guardar: " + ex.Message); }
            }
        }

        private void ExportImage_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog { Filter = "PNG (*.png)|*.png", FileName = "Mockup.png" };
            if (sfd.ShowDialog() == true)
            {
                SelectionBox.Visibility = Visibility.Collapsed;

                Size size = new Size(DesignCanvas.ActualWidth, DesignCanvas.ActualHeight);
                DesignCanvas.Measure(size); DesignCanvas.Arrange(new Rect(size));
                RenderTargetBitmap rtb = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96d, 96d, PixelFormats.Pbgra32);
                rtb.Render(DesignCanvas);

                using (FileStream fs = new FileStream(sfd.FileName, FileMode.Create))
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(rtb));
                    encoder.Save(fs);
                }
                MessageBox.Show("Imagen guardada.");
            }
        }

        #endregion

        #region Barra de Herramientas (Alineación y Orden Z)

        private FrameworkElement GetAnchorElement()
        {
            if (_selectedElements.Count == 0) return null;
            return _selectedElements.Last() as FrameworkElement;
        }

        // --- ORDEN Z (CAPAS) ---
        private void BringToFront_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedElements.Count == 0) return;
            RecordUndo();

            int maxZ = 0;
            foreach (UIElement child in DesignCanvas.Children)
            {
                if (child == SelectionBox) continue;
                int z = Panel.GetZIndex(child);
                if (z > maxZ) maxZ = z;
            }

            foreach (UIElement el in _selectedElements)
            {
                maxZ++;
                Panel.SetZIndex(el, maxZ);
            }
        }

        private void SendToBack_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedElements.Count == 0) return;
            RecordUndo();

            int minZ = 0;
            foreach (UIElement child in DesignCanvas.Children)
            {
                if (child == SelectionBox) continue;
                int z = Panel.GetZIndex(child);
                if (z < minZ) minZ = z;
            }

            foreach (UIElement el in _selectedElements)
            {
                minZ--;
                Panel.SetZIndex(el, minZ);
            }
        }

        // --- ALINEACIÓN ---
        private void AlignLeft_Click(object sender, RoutedEventArgs e)
        {
            var anchor = GetAnchorElement();
            if (anchor == null || _selectedElements.Count < 2) return;
            RecordUndo();
            double target = Canvas.GetLeft(anchor);
            foreach (FrameworkElement fe in _selectedElements) Canvas.SetLeft(fe, target);
            UpdatePropertyPanel();
        }

        private void AlignRight_Click(object sender, RoutedEventArgs e)
        {
            var anchor = GetAnchorElement();
            if (anchor == null || _selectedElements.Count < 2) return;
            RecordUndo();
            double target = Canvas.GetLeft(anchor) + anchor.Width;
            foreach (FrameworkElement fe in _selectedElements) Canvas.SetLeft(fe, target - fe.Width);
            UpdatePropertyPanel();
        }

        private void AlignCenter_Click(object sender, RoutedEventArgs e)
        {
            var anchor = GetAnchorElement();
            if (anchor == null || _selectedElements.Count < 2) return;
            RecordUndo();
            double center = Canvas.GetLeft(anchor) + (anchor.Width / 2);
            foreach (FrameworkElement fe in _selectedElements) Canvas.SetLeft(fe, center - (fe.Width / 2));
            UpdatePropertyPanel();
        }

        private void AlignTop_Click(object sender, RoutedEventArgs e)
        {
            var anchor = GetAnchorElement();
            if (anchor == null || _selectedElements.Count < 2) return;
            RecordUndo();
            double target = Canvas.GetTop(anchor);
            foreach (FrameworkElement fe in _selectedElements) Canvas.SetTop(fe, target);
            UpdatePropertyPanel();
        }

        private void AlignBottom_Click(object sender, RoutedEventArgs e)
        {
            var anchor = GetAnchorElement();
            if (anchor == null || _selectedElements.Count < 2) return;
            RecordUndo();
            double target = Canvas.GetTop(anchor) + anchor.Height;
            foreach (FrameworkElement fe in _selectedElements) Canvas.SetTop(fe, target - fe.Height);
            UpdatePropertyPanel();
        }

        private void AlignMiddle_Click(object sender, RoutedEventArgs e)
        {
            var anchor = GetAnchorElement();
            if (anchor == null || _selectedElements.Count < 2) return;
            RecordUndo();
            double middle = Canvas.GetTop(anchor) + (anchor.Height / 2);
            foreach (FrameworkElement fe in _selectedElements) Canvas.SetTop(fe, middle - (fe.Height / 2));
            UpdatePropertyPanel();
        }

        private void SameWidth_Click(object sender, RoutedEventArgs e)
        {
            var anchor = GetAnchorElement();
            if (anchor == null || _selectedElements.Count < 2) return;
            RecordUndo();
            foreach (FrameworkElement fe in _selectedElements) fe.Width = anchor.Width;
            UpdatePropertyPanel();
        }

        private void SameHeight_Click(object sender, RoutedEventArgs e)
        {
            var anchor = GetAnchorElement();
            if (anchor == null || _selectedElements.Count < 2) return;
            RecordUndo();
            foreach (FrameworkElement fe in _selectedElements) fe.Height = anchor.Height;
            UpdatePropertyPanel();
        }

        #endregion

        #region Toolbox (Creación Manual)

        private void AddButton_Click(object sender, RoutedEventArgs e) => CreateControlManual("CommandButton");
        private void AddLabel_Click(object sender, RoutedEventArgs e) => CreateControlManual("Label");
        private void AddTextBox_Click(object sender, RoutedEventArgs e) => CreateControlManual("TextBox");
        private void AddFrame_Click(object sender, RoutedEventArgs e) => CreateControlManual("Frame");
        private void AddCheckBox_Click(object sender, RoutedEventArgs e) => CreateControlManual("CheckBox");
        private void AddOptionButton_Click(object sender, RoutedEventArgs e) => CreateControlManual("OptionButton");
        private void AddComboBox_Click(object sender, RoutedEventArgs e) => CreateControlManual("ComboBox");
        private void AddListBox_Click(object sender, RoutedEventArgs e) => CreateControlManual("ListBox");
        private void AddPictureBox_Click(object sender, RoutedEventArgs e) => CreateControlManual("PictureBox");
        private void AddTimer_Click(object sender, RoutedEventArgs e) => CreateControlManual("Timer");
        private void AddHScrollBar_Click(object sender, RoutedEventArgs e) => CreateControlManual("HScrollBar");
        private void AddVScrollBar_Click(object sender, RoutedEventArgs e) => CreateControlManual("VScrollBar");
        private void AddDriveListBox_Click(object sender, RoutedEventArgs e) => CreateControlManual("DriveListBox");
        private void AddDirListBox_Click(object sender, RoutedEventArgs e) => CreateControlManual("DirListBox");
        private void AddFileListBox_Click(object sender, RoutedEventArgs e) => CreateControlManual("FileListBox");
        private void AddShape_Click(object sender, RoutedEventArgs e) => CreateControlManual("Shape");
        private void AddLine_Click(object sender, RoutedEventArgs e) => CreateControlManual("Line");
        private void AddImage_Click(object sender, RoutedEventArgs e) => CreateControlManual("Image");
        private void AddMenu_Click(object sender, RoutedEventArgs e) => CreateControlManual("Menu");
        private void AddStatusBar_Click(object sender, RoutedEventArgs e) => CreateControlManual("StatusBar");

        private void CreateControlManual(string vbType)
        {
            RecordUndo();
            FrameworkElement ctrl = _controlFactory.CreateElementInstance(vbType);
            if (ctrl == null) return;

            string name = _controlFactory.GetNextNameForType(vbType);
            ctrl.Name = name;

            // Posicionamiento por defecto
            if (vbType == "Menu")
            {
                ctrl.Width = 400; ctrl.Height = 25;
                Canvas.SetLeft(ctrl, 0); Canvas.SetTop(ctrl, 0);
            }
            else if (vbType == "StatusBar")
            {
                ctrl.Width = 400; ctrl.Height = 25;
                Canvas.SetLeft(ctrl, 0); Canvas.SetTop(ctrl, 300);
            }
            else
            {
                if (double.IsNaN(ctrl.Width)) ctrl.Width = 100;
                if (double.IsNaN(ctrl.Height)) ctrl.Height = 30;

                if (vbType == "Frame") { ctrl.Width = 150; ctrl.Height = 100; }
                if (vbType == "ListBox") { ctrl.Width = 100; ctrl.Height = 80; }
                if (!(vbType == "Timer" || vbType == "Shape" || vbType == "Image" || vbType.Contains("Scroll")))
                {
                    if (ctrl.Height == 30) ctrl.Height = (vbType == "TextBox" || vbType == "ComboBox") ? 24 : 35;
                }

                Canvas.SetLeft(ctrl, 80);
                Canvas.SetTop(ctrl, 80);
            }

            VbHelpers.SetControlText(ctrl, (vbType == "Menu") ? "File,Edit" : (vbType == "StatusBar" ? "Ready" : name));
            RegisterControl(ctrl);
            ClearSelection();
            AddToSelection(ctrl);
        }

        private void RegisterControl(FrameworkElement ctrl)
        {
            // Focusable false para que no atrapen teclado en modo edición
            ctrl.Focusable = false;

            if (ctrl is Control c && !(ctrl is Label))
            {
                c.FontFamily = new FontFamily("MS Sans Serif");
                c.FontSize = 11;
            }

            if (!DesignCanvas.Children.Contains(ctrl)) DesignCanvas.Children.Add(ctrl);
        }

        #endregion

        #region Undo / Redo & Clipboard

        private void RecordUndo() { _undoStack.Push(GetCurrentState()); _redoStack.Clear(); }

        private void Undo()
        {
            if (_undoStack.Count > 0)
            {
                _redoStack.Push(GetCurrentState());
                RestoreState(_undoStack.Pop());
            }
        }

        private void Redo()
        {
            if (_redoStack.Count > 0)
            {
                _undoStack.Push(GetCurrentState());
                RestoreState(_redoStack.Pop());
            }
        }

        private List<ControlState> GetCurrentState()
        {
            var state = new List<ControlState>();
            foreach (UIElement child in DesignCanvas.Children)
            {
                if (child is FrameworkElement fe && child != SelectionBox)
                {
                    int ti = (fe is Control c) ? c.TabIndex : 0;
                    state.Add(new ControlState
                    {
                        Name = fe.Name,
                        VbType = VbHelpers.MapWpfToVbType(fe),
                        Left = Canvas.GetLeft(fe),
                        Top = Canvas.GetTop(fe),
                        Width = fe.Width,
                        Height = fe.Height,
                        Text = VbHelpers.GetControlText(fe),
                        ZIndex = Panel.GetZIndex(fe),
                        TabIndex = ti
                    });
                }
            }
            return state;
        }

        private void RestoreState(List<ControlState> state)
        {
            ClearSelection();
            DesignCanvas.Children.Clear();
            DesignCanvas.Children.Add(SelectionBox);

            foreach (var item in state)
            {
                FrameworkElement ctrl = _controlFactory.CreateElementInstance(item.VbType);
                if (ctrl == null) continue;

                ctrl.Name = item.Name;
                ctrl.Width = item.Width;
                ctrl.Height = item.Height;
                VbHelpers.SetControlText(ctrl, item.Text);
                if (ctrl is Control c) c.TabIndex = item.TabIndex;

                RegisterControl(ctrl);
                Canvas.SetLeft(ctrl, item.Left);
                Canvas.SetTop(ctrl, item.Top);
                Panel.SetZIndex(ctrl, item.ZIndex);
            }
        }

        private void CopyControls()
        {
            if (_selectedElements.Count == 0) return;
            _internalClipboard.Clear();
            double minLeft = double.MaxValue; double minTop = double.MaxValue;
            foreach (FrameworkElement fe in _selectedElements)
            {
                minLeft = Math.Min(minLeft, Canvas.GetLeft(fe));
                minTop = Math.Min(minTop, Canvas.GetTop(fe));
            }

            foreach (FrameworkElement fe in _selectedElements)
            {
                _internalClipboard.Add(new ClipboardData
                {
                    VbType = VbHelpers.MapWpfToVbType(fe),
                    Width = fe.Width,
                    Height = fe.Height,
                    Left = Canvas.GetLeft(fe) - minLeft,
                    Top = Canvas.GetTop(fe) - minTop,
                    Text = VbHelpers.GetControlText(fe),
                    ZIndex = Panel.GetZIndex(fe)
                });
            }
        }

        private void CutControls()
        {
            CopyControls();
            DeleteSelectedControls();
        }

        private void PasteControls()
        {
            if (_internalClipboard.Count == 0) return;
            ClearSelection();
            double pasteOffsetX = 20; double pasteOffsetY = 20;

            foreach (var data in _internalClipboard)
            {
                FrameworkElement newCtrl = _controlFactory.CreateElementInstance(data.VbType);
                if (newCtrl == null) continue;

                newCtrl.Name = _controlFactory.GetNextNameForType(data.VbType);
                newCtrl.Width = data.Width; newCtrl.Height = data.Height;
                VbHelpers.SetControlText(newCtrl, data.Text);

                RegisterControl(newCtrl);
                Canvas.SetLeft(newCtrl, 50 + data.Left + pasteOffsetX);
                Canvas.SetTop(newCtrl, 50 + data.Top + pasteOffsetY);
                Panel.SetZIndex(newCtrl, data.ZIndex + 1); // Poner ligeramente encima
                AddToSelection(newCtrl);
            }
        }

        private void DeleteSelectedControls()
        {
            var toDelete = new List<UIElement>(_selectedElements);
            ClearSelection();
            foreach (var el in toDelete) DesignCanvas.Children.Remove(el);
        }

        #endregion

        #region Gestión de Selección y Propiedades

        private void ClearSelection()
        {
            if (_adornerLayer == null) return;
            foreach (var el in _selectedElements)
            {
                var ads = _adornerLayer.GetAdorners(el);
                if (ads != null) foreach (var ad in ads) _adornerLayer.Remove(ad);
            }
            _selectedElements.Clear();
            UpdatePropertyPanel();
        }

        private void AddToSelection(UIElement element)
        {
            if (!_selectedElements.Contains(element))
            {
                _selectedElements.Add(element);
                if (_adornerLayer != null) _adornerLayer.Add(new ResizeAdorner(element));
            }
            UpdatePropertyPanel();
        }

        private void RemoveFromSelection(UIElement element)
        {
            if (_selectedElements.Contains(element))
            {
                _selectedElements.Remove(element);
                if (_adornerLayer != null)
                {
                    var ads = _adornerLayer.GetAdorners(element);
                    if (ads != null) foreach (var ad in ads) _adornerLayer.Remove(ad);
                }
            }
            UpdatePropertyPanel();
        }

        private void SelectControlsInRect(Rect rect)
        {
            foreach (UIElement child in DesignCanvas.Children)
            {
                if (child == SelectionBox || !(child is FrameworkElement fe)) continue;
                if (rect.IntersectsWith(new Rect(Canvas.GetLeft(fe), Canvas.GetTop(fe), fe.Width, fe.Height)))
                    AddToSelection(fe);
            }
        }

        private void UpdatePropertyPanel()
        {
            if (_selectedElements.Count == 0)
            {
                PropHeaderTitle.Text = "Properties - Form1";
                PropName.Text = "Form1"; PropCaption.Text = "Form1";
                PropLeft.Text = "0"; PropTop.Text = "0";
            }
            else if (_selectedElements.Count == 1)
            {
                var el = _selectedElements[0] as FrameworkElement;
                if (el != null)
                {
                    PropHeaderTitle.Text = "Propiedades - " + el.Name;
                    PropName.Text = el.Name;

                    double leftPx = Canvas.GetLeft(el); double topPx = Canvas.GetTop(el);
                    PropLeft.Text = (Math.Round(leftPx) * VbHelpers.PixelsToTwips).ToString();
                    PropTop.Text = (Math.Round(topPx) * VbHelpers.PixelsToTwips).ToString();

                    PropCaption.Text = VbHelpers.GetControlText(el);
                    LabelPropCaption.Text = (el is TextBox || el is ComboBox) ? "Text" : "Caption";
                }
            }
            else
            {
                PropHeaderTitle.Text = "Varios elementos seleccionados";
                PropName.Text = ""; PropCaption.Text = ""; PropLeft.Text = ""; PropTop.Text = "";
            }
        }

        private void PropCaption_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedElements.Count == 1)
                VbHelpers.SetControlText(_selectedElements[0] as FrameworkElement, PropCaption.Text);
        }

        private void PropName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedElements.Count == 1 && _selectedElements[0] is FrameworkElement fe)
            {
                fe.Name = PropName.Text;
                PropHeaderTitle.Text = "Propiedades - " + PropName.Text;
            }
        }

        #endregion
    }

}