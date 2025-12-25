using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents; 
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Linq; 

namespace VB6VisualMockupDesigner
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        // --- ESTADO Y SELECCIÓN ---
        private List<UIElement> _selectedElements = new List<UIElement>();
        private bool _isDragging = false;
        private Point _startClickPoint;
        private Dictionary<UIElement, Point> _initialPositions = new Dictionary<UIElement, Point>();
        private bool _isSelecting = false;
        private Point _selectionStartPoint;
        private const int PixelsToTwips = 15;
        private AdornerLayer _adornerLayer;

        // --- SISTEMA DE UNDO / REDO ---
        private Stack<List<ControlState>> _undoStack = new Stack<List<ControlState>>();
        private Stack<List<ControlState>> _redoStack = new Stack<List<ControlState>>();
        private List<ControlState> _stateBeforeDrag; // Para guardar estado antes de mover

        private class ControlState
        {
            public string Name { get; set; }
            public string VbType { get; set; }
            public double Left { get; set; }
            public double Top { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public string Text { get; set; }
        }

        // --- PORTAPAPELES INTERNO ---
        private List<ClipboardData> _internalClipboard = new List<ClipboardData>();

        private class ClipboardData
        {
            public string VbType { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public double Left { get; set; }
            public double Top { get; set; }
            public string Text { get; set; }
        }

        // --- CONTADORES ---
        private int _btnCount = 1; private int _lblCount = 1;
        private int _txtCount = 1; private int _chkCount = 1;
        private int _optCount = 1; private int _frmCount = 1;
        private int _lstCount = 1; private int _cmbCount = 1;
        private int _picCount = 1; private int _tmrCount = 1;
        private int _shpCount = 1; private int _linCount = 1;
        private int _scrCount = 1; private int _drvCount = 1;
        private int _dirCount = 1; private int _filCount = 1;
        private int _imgCount = 1;

        public MainWindow()
        {
            InitializeComponent();

            try { this.Icon = new BitmapImage(new Uri("pack://application:,,,/Resource/icon.ico")); }
            catch { this.Icon = CreateAppIcon(); }

            this.Loaded += (s, e) => _adornerLayer = AdornerLayer.GetAdornerLayer(DesignCanvas);
        }

        #region Atajos de Teclado (Undo, Redo, Delete, Copy, Paste)

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Undo (Ctrl + Z)
            if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Undo();
            }
            // Redo (Ctrl + Y)
            else if (e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Redo();
            }
            // Eliminar
            else if (e.Key == Key.Back || e.Key == Key.Delete)
            {
                RecordUndo(); // Guardar estado antes de borrar
                DeleteSelectedControls();
            }
            // Copiar (Ctrl + C)
            else if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                CopyControls();
            }
            // Pegar (Ctrl + V)
            else if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            {
                RecordUndo(); // Guardar estado antes de pegar
                PasteControls();
            }
        }

        #region Lógica Undo / Redo

        private void RecordUndo()
        {
            // Guardar el estado actual en la pila de deshacer
            _undoStack.Push(GetCurrentState());
            // Al hacer una nueva acción, se limpia la pila de rehacer
            _redoStack.Clear();
        }

        private void Undo()
        {
            if (_undoStack.Count == 0) return;

            // Guardar estado actual en Redo antes de volver atrás
            _redoStack.Push(GetCurrentState());

            // Recuperar y aplicar el estado anterior
            var previousState = _undoStack.Pop();
            RestoreState(previousState);
        }

        private void Redo()
        {
            if (_redoStack.Count == 0) return;

            // Guardar estado actual en Undo antes de avanzar
            _undoStack.Push(GetCurrentState());

            // Recuperar y aplicar el estado futuro
            var nextState = _redoStack.Pop();
            RestoreState(nextState);
        }

        private List<ControlState> GetCurrentState()
        {
            var state = new List<ControlState>();
            foreach (UIElement child in DesignCanvas.Children)
            {
                if (child is FrameworkElement fe && child != SelectionBox)
                {
                    string vbType = MapWpfToVbType(fe);
                    string text = "";

                    if (fe is TextBox txt) text = txt.Text;
                    else if (fe is GroupBox gb) text = gb.Header?.ToString();
                    else if (fe is ContentControl cc) text = cc.Content?.ToString();
                    else if (fe is ComboBox cmb) text = cmb.Text;

                    state.Add(new ControlState
                    {
                        Name = fe.Name,
                        VbType = vbType,
                        Left = Canvas.GetLeft(fe),
                        Top = Canvas.GetTop(fe),
                        Width = fe.Width,
                        Height = fe.Height,
                        Text = text
                    });
                }
            }
            return state;
        }

        private void RestoreState(List<ControlState> state)
        {
            // Limpiar todo (menos el SelectionBox)
            ClearSelection();
            var elementsToRemove = new List<UIElement>();
            foreach (UIElement child in DesignCanvas.Children)
            {
                if (child != SelectionBox) elementsToRemove.Add(child);
            }
            foreach (var el in elementsToRemove) DesignCanvas.Children.Remove(el);

            // Reconstruir controles
            foreach (var item in state)
            {
                FrameworkElement ctrl = CreateElementInstance(item.VbType);
                if (ctrl == null) continue;

                ctrl.Name = item.Name;
                ctrl.Width = item.Width;
                ctrl.Height = item.Height;

                if (ctrl is TextBox txt) txt.Text = item.Text;
                else if (ctrl is GroupBox gb) gb.Header = item.Text;
                else if (ctrl is ContentControl cc) cc.Content = item.Text;
                else if (ctrl is ComboBox cmb) cmb.Text = item.Text;

                // Estilos base
                if (ctrl is Control c && !(ctrl is Label))
                {
                    c.FontFamily = new FontFamily("MS Sans Serif");
                    c.FontSize = 11;
                }

                RegisterControl(ctrl);
                Canvas.SetLeft(ctrl, item.Left);
                Canvas.SetTop(ctrl, item.Top);
            }
        }

        #endregion

        private void DeleteSelectedControls()
        {
            if (_selectedElements.Count == 0) return;

            var toDelete = new List<UIElement>(_selectedElements);
            ClearSelection();

            foreach (var el in toDelete)
            {
                DesignCanvas.Children.Remove(el);
            }
        }

        private void CopyControls()
        {
            if (_selectedElements.Count == 0) return;

            _internalClipboard.Clear();

            double minLeft = double.MaxValue;
            double minTop = double.MaxValue;

            foreach (var el in _selectedElements)
            {
                if (el is FrameworkElement fe)
                {
                    double l = Canvas.GetLeft(fe);
                    double t = Canvas.GetTop(fe);
                    if (l < minLeft) minLeft = l;
                    if (t < minTop) minTop = t;
                }
            }

            foreach (var el in _selectedElements)
            {
                if (el is FrameworkElement fe)
                {
                    string vbType = MapWpfToVbType(fe);
                    if (string.IsNullOrEmpty(vbType)) continue;

                    var data = new ClipboardData
                    {
                        VbType = vbType,
                        Width = fe.Width,
                        Height = fe.Height,
                        Left = Canvas.GetLeft(fe) - minLeft,
                        Top = Canvas.GetTop(fe) - minTop,
                    };

                    if (fe is TextBox txt) data.Text = txt.Text;
                    else if (fe is GroupBox gb) data.Text = gb.Header?.ToString();
                    else if (fe is ContentControl cc) data.Text = cc.Content?.ToString();
                    else if (fe is ComboBox cmb) data.Text = cmb.Text;

                    _internalClipboard.Add(data);
                }
            }
        }

        private void PasteControls()
        {
            if (_internalClipboard.Count == 0) return;

            ClearSelection();
            double pasteOffsetX = 20;
            double pasteOffsetY = 20;

            foreach (var data in _internalClipboard)
            {
                FrameworkElement newCtrl = CreateElementInstance(data.VbType);
                if (newCtrl == null) continue;

                string newName = GetNextNameForType(data.VbType);
                newCtrl.Name = newName;

                newCtrl.Width = data.Width;
                newCtrl.Height = data.Height;

                if (newCtrl is TextBox txt) txt.Text = data.Text;
                else if (newCtrl is GroupBox gb) gb.Header = data.Text;
                else if (newCtrl is ContentControl cc) cc.Content = data.Text;
                else if (newCtrl is ComboBox cmb) cmb.Text = data.Text;

                if (newCtrl is Control c && !(newCtrl is Label))
                {
                    c.FontFamily = new FontFamily("MS Sans Serif");
                    c.FontSize = 11;
                }

                RegisterControl(newCtrl);

                Canvas.SetLeft(newCtrl, 50 + data.Left + pasteOffsetX);
                Canvas.SetTop(newCtrl, 50 + data.Top + pasteOffsetY);

                AddToSelection(newCtrl);
            }
        }

        private string GetNextNameForType(string vbType)
        {
            switch (vbType)
            {
                case "CommandButton": return "Command" + _btnCount++;
                case "Label": return "Label" + _lblCount++;
                case "TextBox": return "Text" + _txtCount++;
                case "CheckBox": return "Check" + _chkCount++;
                case "OptionButton": return "Option" + _optCount++;
                case "Frame": return "Frame" + _frmCount++;
                case "ComboBox": return "Combo" + _cmbCount++;
                case "ListBox": return "List" + _lstCount++;
                case "Timer": return "Timer" + _tmrCount++;
                case "Shape": return "Shape" + _shpCount++;
                case "Image": return "Image" + _imgCount++;
                case "HScrollBar": return "HScroll" + _scrCount++;
                case "VScrollBar": return "VScroll" + _scrCount++;
                case "DriveListBox": return "Drive" + _drvCount++;
                case "DirListBox": return "Dir" + _dirCount++;
                case "FileListBox": return "File" + _filCount++;
                case "Line": return "Line" + _linCount++;
                default: return vbType + DateTime.Now.Ticks;
            }
        }

        #endregion

        #region Interactividad (Selección Múltiple, Arrastre y Lazo)

        private void Control_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Guardar estado ANTES de empezar a arrastrar o seleccionar
            // Esto es crucial para poder deshacer el movimiento después
            _stateBeforeDrag = GetCurrentState();

            var clickedElement = (UIElement)sender;

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                if (_selectedElements.Contains(clickedElement))
                {
                    RemoveFromSelection(clickedElement);
                    e.Handled = true;
                    return;
                }
                else
                {
                    AddToSelection(clickedElement);
                }
            }
            else
            {
                if (!_selectedElements.Contains(clickedElement))
                {
                    ClearSelection();
                    AddToSelection(clickedElement);
                }
            }

            _isDragging = true;
            _startClickPoint = e.GetPosition(DesignCanvas);
            _initialPositions.Clear();

            foreach (var el in _selectedElements)
                _initialPositions[el] = new Point(Canvas.GetLeft(el), Canvas.GetTop(el));

            clickedElement.CaptureMouse();
            e.Handled = true;
        }

        private void DesignCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ClearSelection();
            _isSelecting = true;
            _selectionStartPoint = e.GetPosition(DesignCanvas);

            Canvas.SetLeft(SelectionBox, _selectionStartPoint.X);
            Canvas.SetTop(SelectionBox, _selectionStartPoint.Y);
            SelectionBox.Width = 0;
            SelectionBox.Height = 0;
            SelectionBox.Visibility = Visibility.Visible;

            DesignCanvas.CaptureMouse();
        }

        private void DesignCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                Point currentMousePos = e.GetPosition(DesignCanvas);
                double deltaX = currentMousePos.X - _startClickPoint.X;
                double deltaY = currentMousePos.Y - _startClickPoint.Y;

                foreach (var el in _selectedElements)
                {
                    if (_initialPositions.ContainsKey(el))
                    {
                        Point initPos = _initialPositions[el];
                        double newLeft = Math.Round((initPos.X + deltaX) / 8) * 8;
                        double newTop = Math.Round((initPos.Y + deltaY) / 8) * 8;

                        if (newLeft < 0) newLeft = 0;
                        if (newTop < 0) newTop = 0;

                        Canvas.SetLeft(el, newLeft);
                        Canvas.SetTop(el, newTop);
                    }
                }
                UpdatePropertyPanel();
                return;
            }

            if (_isSelecting)
            {
                Point currentPos = e.GetPosition(DesignCanvas);
                double x = Math.Min(currentPos.X, _selectionStartPoint.X);
                double y = Math.Min(currentPos.Y, _selectionStartPoint.Y);
                double w = Math.Abs(currentPos.X - _selectionStartPoint.X);
                double h = Math.Abs(currentPos.Y - _selectionStartPoint.Y);

                Canvas.SetLeft(SelectionBox, x);
                Canvas.SetTop(SelectionBox, y);
                SelectionBox.Width = w;
                SelectionBox.Height = h;
            }
        }

        private void DesignCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                foreach (var el in _selectedElements) el.ReleaseMouseCapture();

                // Verificar si hubo movimiento real
                var currentState = GetCurrentState();
                if (_stateBeforeDrag != null && !AreStatesEqual(_stateBeforeDrag, currentState))
                {
                    // Si hubo cambios, empujar el estado PREVIO al Undo stack
                    _undoStack.Push(_stateBeforeDrag);
                    _redoStack.Clear();
                }
            }

            if (_isSelecting)
            {
                _isSelecting = false;
                SelectionBox.Visibility = Visibility.Collapsed;
                DesignCanvas.ReleaseMouseCapture();

                Rect selectionRect = new Rect(Canvas.GetLeft(SelectionBox), Canvas.GetTop(SelectionBox), SelectionBox.Width, SelectionBox.Height);
                SelectControlsInRect(selectionRect);
            }
        }

        // Helper para comparar estados y saber si hubo movimiento
        private bool AreStatesEqual(List<ControlState> s1, List<ControlState> s2)
        {
            if (s1.Count != s2.Count) return false;
            // Comparación simple por posición
            for (int i = 0; i < s1.Count; i++)
            {
                if (s1[i].Left != s2[i].Left || s1[i].Top != s2[i].Top) return false;
            }
            return true;
        }

        private void Control_MouseMove(object sender, MouseEventArgs e) { if (_isDragging) DesignCanvas_MouseMove(sender, e); }
        private void Control_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) { if (_isDragging) DesignCanvas_MouseUp(sender, e); }

        #endregion

        #region Gestión de Selección

        private void ClearSelection()
        {
            if (_adornerLayer == null) return;
            foreach (var el in _selectedElements)
            {
                var adorners = _adornerLayer.GetAdorners(el);
                if (adorners != null) foreach (var ad in adorners) _adornerLayer.Remove(ad);
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
                    var adorners = _adornerLayer.GetAdorners(element);
                    if (adorners != null) foreach (var ad in adorners) _adornerLayer.Remove(ad);
                }
            }
            UpdatePropertyPanel();
        }

        private void SelectControlsInRect(Rect rect)
        {
            foreach (UIElement child in DesignCanvas.Children)
            {
                if (child == SelectionBox) continue;
                if (!(child is FrameworkElement fe)) continue;

                double left = Canvas.GetLeft(fe);
                double top = Canvas.GetTop(fe);
                Rect childRect = new Rect(left, top, fe.Width, fe.Height);

                if (rect.IntersectsWith(childRect)) AddToSelection(fe);
            }
        }

        private void UpdatePropertyPanel()
        {
            if (_selectedElements.Count == 0)
            {
                PropHeaderTitle.Text = "Properties - Form1";
                PropName.Text = "Form1";
                PropCaption.Text = "Form1";
                PropLeft.Text = "0"; PropTop.Text = "0";
            }
            else if (_selectedElements.Count == 1)
            {
                var el = _selectedElements[0] as FrameworkElement;
                if (el != null)
                {
                    PropHeaderTitle.Text = "Propiedades - " + el.Name;
                    PropName.Text = el.Name;
                    double leftPx = Canvas.GetLeft(el);
                    double topPx = Canvas.GetTop(el);
                    PropLeft.Text = (Math.Round(leftPx) * PixelsToTwips).ToString();
                    PropTop.Text = (Math.Round(topPx) * PixelsToTwips).ToString();

                    if (el is TextBox txt) { LabelPropCaption.Text = "Text"; PropCaption.Text = txt.Text; }
                    else if (el is GroupBox grp) { LabelPropCaption.Text = "Caption"; PropCaption.Text = grp.Header?.ToString(); }
                    else if (el is ContentControl cc) { LabelPropCaption.Text = "Caption"; PropCaption.Text = cc.Content?.ToString(); }
                }
            }
            else
            {
                PropHeaderTitle.Text = "Varios elementos seleccionados";
                PropName.Text = ""; PropCaption.Text = ""; PropLeft.Text = ""; PropTop.Text = "";
            }
        }

        #endregion

        #region Generación de Icono
        private ImageSource CreateAppIcon()
        {
            DrawingGroup drawingGroup = new DrawingGroup();
            drawingGroup.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromRgb(0, 50, 120)), new Pen(Brushes.White, 1), new RectangleGeometry(new Rect(0, 0, 32, 32), 3, 3)));
            FormattedText text = new FormattedText("VB", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal), 18, Brushes.White, 96);
            drawingGroup.Children.Add(new GeometryDrawing(Brushes.White, null, text.BuildGeometry(new Point(3, 5))));
            Geometry body = Geometry.Parse("M 20,20 L 28,12 L 30,14 L 22,22 Z");
            drawingGroup.Children.Add(new GeometryDrawing(Brushes.Gold, new Pen(Brushes.Black, 0.5), body));
            Geometry eraser = Geometry.Parse("M 28,12 L 30,14 L 32,12 L 30,10 Z");
            drawingGroup.Children.Add(new GeometryDrawing(Brushes.HotPink, new Pen(Brushes.Black, 0.5), eraser));
            Geometry tip = Geometry.Parse("M 20,20 L 22,22 L 18,24 Z");
            drawingGroup.Children.Add(new GeometryDrawing(Brushes.Black, null, tip));
            return new DrawingImage(drawingGroup);
        }
        #endregion

        #region Funciones de Archivo
        private void ImportFrm_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VB6 Form (*.frm)|*.frm|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true) try
                {
                    RecordUndo(); // Guardar antes de importar
                    ParseVb6Frm(openFileDialog.FileName);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
        private void ExportCode_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "VB6 Form (*.frm)|*.frm";
            saveFileDialog.FileName = "Form1_New.frm";
            if (saveFileDialog.ShowDialog() == true) try { File.WriteAllText(saveFileDialog.FileName, GenerateVb6Code()); MessageBox.Show("Código generado."); } catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
        private void ExportImage_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PNG Image (*.png)|*.png";
            saveFileDialog.FileName = "Mockup.png";
            if (saveFileDialog.ShowDialog() == true) try { SaveCanvasToImage(saveFileDialog.FileName); MessageBox.Show("Imagen guardada."); } catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
        #endregion

        #region Parsing, Creación y Generación

        private void ParseVb6Frm(string filePath)
        {
            DesignCanvas.Children.Clear();
            DesignCanvas.Children.Add(SelectionBox);

            string[] lines = File.ReadAllLines(filePath);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("Begin VB."))
                {
                    string[] parts = line.Split(' ');
                    if (parts.Length < 3) continue;
                    string type = parts[1].Replace("VB.", "");
                    string name = parts[2];

                    CreateControlFromImport(type, name, lines, i);
                }
            }
        }

        // Nueva factoría centralizada para crear controles por tipo
        private FrameworkElement CreateElementInstance(string vbType)
        {
            switch (vbType)
            {
                case "CommandButton": return new Button { Background = GetVbGray(), BorderThickness = new Thickness(2) };
                case "Label": return new Label { Background = Brushes.Transparent };
                case "TextBox": return new TextBox { Background = Brushes.White, BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) };
                case "CheckBox": return new CheckBox();
                case "OptionButton": return new RadioButton();
                case "Frame": return new GroupBox { BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1) };
                case "ComboBox": return new ComboBox { IsReadOnly = true };
                case "ListBox": return new ListBox { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) };
                case "Timer": return CreateTimerVisual();
                case "Shape": return new Rectangle { Stroke = Brushes.Black, StrokeDashArray = new DoubleCollection(new double[] { 2, 2 }) };
                case "Image": return new Image { Stretch = Stretch.Uniform, Source = null }; // Placeholder
                case "HScrollBar": return new ScrollBar { Orientation = Orientation.Horizontal, Height = 18 };
                case "VScrollBar": return new ScrollBar { Orientation = Orientation.Vertical, Width = 18 };
                case "DriveListBox": return new ComboBox { Text = "C: [System]", IsReadOnly = true };
                case "DirListBox": return new ListBox();
                case "FileListBox": return new ListBox();
                case "Line": return new Line { Stroke = Brushes.Black, StrokeThickness = 1, X2 = 80, Y2 = 80 };
                case "PictureBox": return new Border { Background = GetVbGray(), BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) };
                default: return null;
            }
        }

        private void CreateControlFromImport(string vbType, string name, string[] allLines, int startIndex)
        {
            FrameworkElement ctrl = CreateElementInstance(vbType);
            if (ctrl == null) return;

            ctrl.Name = name;
            double left = 0, top = 0, width = 100, height = 30;
            string caption = name;

            for (int j = startIndex + 1; j < allLines.Length; j++)
            {
                string l = allLines[j].Trim();
                if (l == "End") break;
                if (l.StartsWith("Begin ")) break;
                if (l.Contains("="))
                {
                    string prop = l.Split('=')[0].Trim();
                    string val = l.Split('=')[1].Trim().Replace("\"", "");
                    switch (prop)
                    {
                        case "Caption": case "Text": caption = val; break;
                        case "Left": left = double.Parse(val) / PixelsToTwips; break;
                        case "Top": top = double.Parse(val) / PixelsToTwips; break;
                        case "Width": width = double.Parse(val) / PixelsToTwips; break;
                        case "Height": height = double.Parse(val) / PixelsToTwips; break;
                    }
                }
            }
            ctrl.Width = width; ctrl.Height = height;

            if (ctrl is ContentControl cc) cc.Content = caption;
            if (ctrl is TextBox tb) tb.Text = caption;
            if (ctrl is GroupBox gb) gb.Header = caption;

            RegisterControl(ctrl);
            Canvas.SetLeft(ctrl, Math.Round(left / 8) * 8);
            Canvas.SetTop(ctrl, Math.Round(top / 8) * 8);
        }

        private string GenerateVb6Code()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("VERSION 5.00");
            sb.AppendLine("Begin VB.Form Form1");
            sb.AppendLine("   Caption         =   \"Mockup\"");
            sb.AppendLine($"   ClientHeight    =   {DesignCanvas.ActualHeight * PixelsToTwips}");
            sb.AppendLine($"   ClientWidth     =   {DesignCanvas.ActualWidth * PixelsToTwips}");

            foreach (UIElement child in DesignCanvas.Children)
            {
                if (child is FrameworkElement fe && child != SelectionBox)
                {
                    string vbType = MapWpfToVbType(fe);
                    if (string.IsNullOrEmpty(vbType)) continue;

                    sb.AppendLine($"   Begin VB.{vbType} {fe.Name}");
                    sb.AppendLine($"      Left            =   {Math.Round(Canvas.GetLeft(fe) * PixelsToTwips)}");
                    sb.AppendLine($"      Top             =   {Math.Round(Canvas.GetTop(fe) * PixelsToTwips)}");
                    sb.AppendLine($"      Width           =   {Math.Round(fe.Width * PixelsToTwips)}");
                    sb.AppendLine($"      Height          =   {Math.Round(fe.Height * PixelsToTwips)}");

                    if (fe is TextBox txt) sb.AppendLine($"      Text            =   \"{txt.Text}\"");
                    else if (fe is ContentControl cc) sb.AppendLine($"      Caption         =   \"{cc.Content}\"");
                    else if (fe is GroupBox gb) sb.AppendLine($"      Caption         =   \"{gb.Header}\"");
                    sb.AppendLine("   End");
                }
            }
            sb.AppendLine("End");
            return sb.ToString();
        }

        private string MapWpfToVbType(FrameworkElement fe)
        {
            if (fe is Button) return "CommandButton";
            if (fe is TextBox) return "TextBox";
            if (fe is Label) return "Label";
            if (fe is CheckBox) return "CheckBox";
            if (fe is RadioButton) return "OptionButton";
            if (fe is GroupBox) return "Frame";
            if (fe is ComboBox) return "ComboBox";
            if (fe is ListBox) return "ListBox";
            if (fe is Rectangle) return "Shape";
            if (fe is Line) return "Line";
            if (fe is Image) return "Image";
            if (fe is ScrollBar sb) return sb.Orientation == Orientation.Horizontal ? "HScrollBar" : "VScrollBar";
            if (fe is Border) return "PictureBox"; // Asumiendo Border es PictureBox en este contexto
            // Para el Timer, es un Grid, habría que identificarlo mejor, pero por ahora:
            if (fe is Grid g && g.Children.Count > 0 && g.Children[0] is Ellipse) return "Timer";
            return "";
        }

        private void SaveCanvasToImage(string fileName)
        {
            SelectionBox.Visibility = Visibility.Collapsed;
            Size size = new Size(DesignCanvas.ActualWidth, DesignCanvas.ActualHeight);
            DesignCanvas.Measure(size);
            DesignCanvas.Arrange(new Rect(size));
            RenderTargetBitmap rtb = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96d, 96d, PixelFormats.Pbgra32);
            rtb.Render(DesignCanvas);
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));
                encoder.Save(fs);
            }
        }
        #endregion

        #region Toolbox & Helpers
        // Métodos de botones de la toolbox simplificados usando la nueva factory
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

        private void CreateControlManual(string vbType)
        {
            RecordUndo(); // Guardar estado antes de agregar nuevo control

            FrameworkElement ctrl = CreateElementInstance(vbType);
            if (ctrl == null) return;

            string name = GetNextNameForType(vbType);
            ctrl.Name = name;

            // Valores por defecto para creación manual
            if (ctrl.Width is double.NaN) ctrl.Width = 100;
            if (ctrl.Height is double.NaN) ctrl.Height = 30;
            if (vbType == "Timer" || vbType == "Shape" || vbType == "Image" || vbType.Contains("Scroll")) { /* Size already set in factory */ }
            else { ctrl.Width = 100; ctrl.Height = (vbType == "TextBox" || vbType == "ComboBox") ? 24 : 35; }
            if (vbType == "Frame") { ctrl.Width = 150; ctrl.Height = 100; }
            if (vbType == "ListBox") { ctrl.Width = 100; ctrl.Height = 80; }

            if (ctrl is ContentControl cc) cc.Content = name;
            if (ctrl is TextBox tb) tb.Text = name;
            if (ctrl is GroupBox gb) gb.Header = name;

            RegisterControl(ctrl);

            Canvas.SetLeft(ctrl, 80);
            Canvas.SetTop(ctrl, 80);

            ClearSelection();
            AddToSelection(ctrl);
        }

        private void RegisterControl(FrameworkElement ctrl)
        {
            ctrl.PreviewMouseLeftButtonDown += Control_MouseLeftButtonDown;
            ctrl.PreviewMouseMove += Control_MouseMove;
            ctrl.PreviewMouseLeftButtonUp += Control_MouseLeftButtonUp;
            if (ctrl is Control c && !(ctrl is Label)) { c.FontFamily = new FontFamily("MS Sans Serif"); c.FontSize = 11; }
            DesignCanvas.Children.Add(ctrl);
        }

        private Grid CreateTimerVisual()
        {
            Grid g = new Grid { Width = 32, Height = 32 };
            g.Children.Add(new Ellipse { Stroke = Brushes.Black, StrokeThickness = 1 });
            g.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M16,4 L16,16 L22,22"), Stroke = Brushes.Black, StrokeThickness = 1 });
            return g;
        }
        private Brush GetVbGray() => new SolidColorBrush(Color.FromRgb(212, 208, 200));

        private void PropCaption_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedElements.Count == 1)
            {
                var el = _selectedElements[0];
                if (el is TextBox txt) txt.Text = PropCaption.Text;
                else if (el is GroupBox grp) grp.Header = PropCaption.Text;
                else if (el is ContentControl cc) cc.Content = PropCaption.Text;
            }
        }
        private void PropName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedElements.Count == 1 && _selectedElements[0] is FrameworkElement fe)
            {
                fe.Name = PropName.Text; PropHeaderTitle.Text = "Propiedades - " + fe.Name;
            }
        }
        #endregion
    }

}