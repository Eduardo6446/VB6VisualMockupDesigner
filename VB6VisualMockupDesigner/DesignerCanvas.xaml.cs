using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VB6VisualMockupDesigner
{
    /// <summary>
    /// Interaction logic for DesignerCanvas.xaml
    /// </summary>
    public partial class DesignerCanvas : UserControl
    {

        // VARIABLES DE ESTADO PARA ARRASTRE
        private bool _isDragging = false;
        private Point _clickOffset;       // Dónde hice clic dentro del control
        private UIElement _selectedControl; // El control que tengo seleccionado actualmente

        // CAPA VISUAL DE SELECCIÓN (Simularemos los 8 cuadraditos más tarde)
        private Border _selectionBorder;

        public DesignerCanvas()
        {
            InitializeComponent();
        }

        // Propiedad para establecer el título del formulario simulado
        public string FormTitle
        {
            get { return FormTitleText.Text; }
            set { FormTitleText.Text = value; }
        }

        // Método para obtener el Canvas donde agregaremos controles
        public Canvas GetDesignSurface()
        {
            return DesignSurface;
        }

        public event EventHandler<FrameworkElement> ControlSelected;

        // Deseleccionar al hacer clic fuera (opcional para el futuro)
        private void Grid_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Aquí implementarás la lógica para quitar selección de controles
            // Keyboard.ClearFocus();
        }

        private void DesignSurface_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("ControlToolboxItem"))
            {
                string controlType = e.Data.GetData("ControlToolboxItem") as string;
                Point dropPosition = e.GetPosition(DesignSurface);

                // Crear el control visual
                UIElement newControl = CreateRetroControl(controlType);

                if (newControl != null)
                {
                    // Posicionar en el Canvas
                    Canvas.SetLeft(newControl, SnapToGrid(dropPosition.X));
                    Canvas.SetTop(newControl, SnapToGrid(dropPosition.Y));

                    DesignSurface.Children.Add(newControl);
                }
            }
        }

        // AYUDA: Ajustar a la rejilla de 8px (Típico VB6)
        private double SnapToGrid(double val)
        {
            return Math.Round(val / 8.0) * 8.0;
        }

        // FÁBRICA DE CONTROLES RETRO (Simulación Visual)
        private UIElement CreateRetroControl(string type)
        {
            Control control = null;

            switch (type)
            {
                case "CommandButton":
                    control = new Button
                    {
                        Content = "Command1",
                        Width = 121,
                        Height = 33, // Tamaños default VB6
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D4D0C8")),
                        BorderThickness = new Thickness(2),
                        FontFamily = new FontFamily("Microsoft Sans Serif"),
                        FontSize = 11
                    };
                    break;

                case "TextBox":
                    control = new TextBox
                    {
                        Text = "Text1",
                        Width = 121,
                        Height = 25,
                        FontFamily = new FontFamily("Microsoft Sans Serif"),
                        FontSize = 11,
                        BorderBrush = Brushes.Gray
                    };
                    break;

                case "Label":
                    control = new Label
                    {
                        Content = "Label1",
                        Width = 121,
                        Height = 25,
                        FontFamily = new FontFamily("Microsoft Sans Serif"),
                        FontSize = 11,
                        Padding = new Thickness(0) // Label VB6 no tiene mucho padding
                    };
                    break;

                case "CheckBox":
                    control = new CheckBox
                    {
                        Content = "Check1",
                        Width = 121,
                        Height = 25,
                        FontFamily = new FontFamily("Microsoft Sans Serif"),
                        FontSize = 11
                    };
                    break;

                // Agregar más casos según necesites...
                default:
                    // Placeholder para controles no implementados
                    control = new Button { Content = type, Width = 100, Height = 30, Background = Brushes.Red };
                    break;
            }

            if (control != null)
            {
                // === NUEVO: CONECTAR EVENTOS DE MOVIMIENTO ===
                control.PreviewMouseDown += Control_PreviewMouseDown;
                control.PreviewMouseMove += Control_PreviewMouseMove;
                control.PreviewMouseUp += Control_PreviewMouseUp;

                // Cursor de movimiento al pasar por encima
                control.Cursor = Cursors.SizeAll;
            }

            return control;
        }

        private void ShowSelectionIndicator(UIElement control)
        {
            // 1. Si ya había una selección, la quitamos
            if (_selectionBorder != null)
            {
                DesignSurface.Children.Remove(_selectionBorder);
                _selectionBorder = null;
            }

            if (control == null) return;

            // 2. Crear un borde visual alrededor del control
            // En el futuro, aquí es donde dibujaríamos los 8 cuadraditos blancos de VB6
            _selectionBorder = new Border
            {
                BorderBrush = Brushes.Blue, // Azul moderno para indicar selección
                BorderThickness = new Thickness(1),
                Width = ((FrameworkElement)control).Width + 6,  // Un poco más grande que el control
                Height = ((FrameworkElement)control).Height + 6,
                IsHitTestVisible = false // Importante: Que el clic pase a través de él
            };

            // 3. Posicionar el borde sobre el control
            double left = Canvas.GetLeft(control);
            double top = Canvas.GetTop(control);

            Canvas.SetLeft(_selectionBorder, left - 3);
            Canvas.SetTop(_selectionBorder, top - 3);

            // 4. Agregar al Canvas
            DesignSurface.Children.Add(_selectionBorder);
        }


        // 1. INICIAR ARRASTRE (Al hacer clic)
        private void Control_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var control = sender as UIElement;

            // Guardamos referencia y estado
            _selectedControl = control;
            _isDragging = true;

            // Calculamos dónde hicimos clic RELATIVO al control (para que no salte al moverlo)
            _clickOffset = e.GetPosition(control);

            // Capturamos el mouse para que no se pierda si lo mueves muy rápido fuera del control
            control.CaptureMouse();

            // Mostrar visualmente que está seleccionado
            ShowSelectionIndicator(control);

            // Detenemos la propagación para que no seleccione el fondo
            e.Handled = true;

            ControlSelected?.Invoke(this, control as FrameworkElement);
        
        }

        // 2. MOVER (Mientras arrastras)
        private void Control_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _selectedControl != null)
            {
                // Obtenemos la posición actual del mouse en el CANVAS
                Point currentPos = e.GetPosition(DesignSurface);

                // Calculamos la nueva posición restando el offset inicial
                double newLeft = currentPos.X - _clickOffset.X;
                double newTop = currentPos.Y - _clickOffset.Y;

                // APLICAMOS EL SNAP-TO-GRID (Tu función existente)
                newLeft = SnapToGrid(newLeft);
                newTop = SnapToGrid(newTop);

                // Evitar coordenadas negativas (irse fuera del formulario por arriba/izquierda)
                if (newLeft < 0) newLeft = 0;
                if (newTop < 0) newTop = 0;

                // Movemos el control
                Canvas.SetLeft(_selectedControl, newLeft);
                Canvas.SetTop(_selectedControl, newTop);

                // Actualizamos también el borde de selección para que siga al control
                if (_selectionBorder != null)
                {
                    Canvas.SetLeft(_selectionBorder, newLeft - 3);
                    Canvas.SetTop(_selectionBorder, newTop - 3);
                }
            }
        }

        // 3. TERMINAR ARRASTRE (Al soltar)
        private void Control_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;

                if (_selectedControl != null)
                {
                    _selectedControl.ReleaseMouseCapture();
                }
            }
        }

        private void DesignSurface_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Si hago clic en el vacío, quito la selección
            ShowSelectionIndicator(null);
            _selectedControl = null;

            ControlSelected?.Invoke(this, null);
        }


    }


}
