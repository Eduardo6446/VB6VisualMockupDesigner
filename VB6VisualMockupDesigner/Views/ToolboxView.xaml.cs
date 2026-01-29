using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using VB6VisualMockupDesigner.Models;

namespace VB6VisualMockupDesigner.Views
{
    /// <summary>
    /// Vista de la Caja de Herramientas (Refactorizada para ser dinámica)
    /// </summary>
    public partial class ToolboxView : UserControl
    {
        // Evento para comunicar al MainWindow qué control se seleccionó (Click simple)
        public event EventHandler<string> OnControlSelected;

        public ToolboxView()
        {
            InitializeComponent();
            LoadTools(); // Cargar la lista en memoria
        }

        // Método público para controlar el estado
        public void EnableTools(bool isEnabled)
        {
            // Ahora controlamos la lista completa en lugar del StackPanel antiguo
            if (ToolsList != null)
                ToolsList.IsEnabled = isEnabled;

            if (TxtSearchTool != null)
                TxtSearchTool.IsEnabled = isEnabled;
        }

        // -----------------------------------------------------------
        // 1. CARGA DE HERRAMIENTAS (Base de Datos en Memoria)
        // -----------------------------------------------------------
        private void LoadTools()
        {
            var tools = new List<ToolboxItem>();

            // --- GRUPO: VB.Runtime (Estándar) ---
            string catStd = "VB.Runtime";

            tools.Add(new ToolboxItem
            {
                Name = "Pointer",
                Category = catStd,
                Type = "Pointer",
                IconData = Geometry.Parse("M2,2 L10,18 L13,13 L18,13 Z")
            });

            tools.Add(new ToolboxItem
            {
                Name = "PictureBox",
                Category = catStd,
                Type = "VB.PictureBox",
                IconData = Geometry.Parse("M0,0 H20 V16 H0 Z M2,14 L8,6 L12,10 L15,4 L18,14")
            });

            tools.Add(new ToolboxItem
            {
                Name = "Label",
                Category = catStd,
                Type = "VB.Label",
                IconData = Geometry.Parse("M4,4 L8,14 L9,14 L10,11 L13,11 L14,14 L15,14 L19,4 L17,4 L15,10 L12,10 L11,6 L8,6 L7,10 L4,10 L2,4 Z")
            });

            tools.Add(new ToolboxItem
            {
                Name = "TextBox",
                Category = catStd,
                Type = "VB.TextBox",
                IconData = Geometry.Parse("M0,0 H20 V14 H0 Z M4,4 H10 M4,7 H16 M4,10 H12")
            });

            tools.Add(new ToolboxItem
            {
                Name = "Frame",
                Category = catStd,
                Type = "VB.Frame",
                IconData = Geometry.Parse("M0,4 H2 V0 H22 V18 H0 Z M2,6 H20 V16 H2 Z")
            });

            tools.Add(new ToolboxItem
            {
                Name = "CommandButton",
                Category = catStd,
                Type = "VB.CommandButton",
                IconData = Geometry.Parse("M2,2 H18 V12 H2 Z")
            }); // Rectángulo simple

            tools.Add(new ToolboxItem
            {
                Name = "CheckBox",
                Category = catStd,
                Type = "VB.CheckBox",
                IconData = Geometry.Parse("M0,4 H10 V14 H0 Z M2,7 L5,10 L10,3")
            });

            tools.Add(new ToolboxItem
            {
                Name = "OptionButton",
                Category = catStd,
                Type = "VB.OptionButton",
                IconData = Geometry.Parse("M6,6 A5,5 0 1 1 6.1,6 Z M8,8 A2,2 0 1 1 8.1,8 Z")
            });

            tools.Add(new ToolboxItem
            {
                Name = "ComboBox",
                Category = catStd,
                Type = "VB.ComboBox",
                IconData = Geometry.Parse("M0,0 H20 V12 H0 Z M13,2 V10 M14,4 L17,4 L15.5,7 Z")
            });

            tools.Add(new ToolboxItem
            {
                Name = "ListBox",
                Category = catStd,
                Type = "VB.ListBox",
                IconData = Geometry.Parse("M0,0 H18 V18 H0 Z M3,4 H15 M3,8 H15 M3,12 H15")
            });

            tools.Add(new ToolboxItem
            {
                Name = "HScrollBar",
                Category = catStd,
                Type = "VB.HScrollBar",
                IconData = Geometry.Parse("M0,0 H22 V8 H0 Z M0,0 H4 V8 H0 M18,0 H4 V8 H18")
            });

            tools.Add(new ToolboxItem
            {
                Name = "VScrollBar",
                Category = catStd,
                Type = "VB.VScrollBar",
                IconData = Geometry.Parse("M0,0 V22 H8 V0 Z M0,0 V4 H8 V0 M0,18 V4 H8 V18")
            });

            tools.Add(new ToolboxItem
            {
                Name = "Timer",
                Category = catStd,
                Type = "VB.Timer",
                IconData = Geometry.Parse("M8,1 A7,7 0 1 1 7.9,1 Z M8,4 V8 H11")
            });

            tools.Add(new ToolboxItem
            {
                Name = "DriveListBox",
                Category = catStd,
                Type = "VB.DriveListBox",
                IconData = Geometry.Parse("M2,12 L16,12 L16,5 L9,5 L7,3 L2,3 Z")
            });

            tools.Add(new ToolboxItem
            {
                Name = "DirListBox",
                Category = catStd,
                Type = "VB.DirListBox",
                IconData = Geometry.Parse("M3,4 L15,4 L15,12 L3,12 Z")
            });

            tools.Add(new ToolboxItem
            {
                Name = "FileListBox",
                Category = catStd,
                Type = "VB.FileListBox",
                IconData = Geometry.Parse("M4,2 L11,2 L14,5 L14,16 L4,16 Z")
            });

            tools.Add(new ToolboxItem
            {
                Name = "Shape",
                Category = catStd,
                Type = "VB.Shape",
                IconData = Geometry.Parse("M1,1 H13 V13 H1 Z")
            }); // Rectángulo simple

            tools.Add(new ToolboxItem
            {
                Name = "Line",
                Category = catStd,
                Type = "VB.Line",
                IconData = Geometry.Parse("M2,14 L16,2")
            });

            tools.Add(new ToolboxItem
            {
                Name = "Image",
                Category = catStd,
                Type = "VB.Image",
                IconData = Geometry.Parse("M1,1 H15 V15 H1 Z M3,3 L13,13 M3,13 L13,3")
            }); // Cuadro con X

            tools.Add(new ToolboxItem
            {
                Name = "Menu",
                Category = catStd,
                Type = "VB.Menu",
                IconData = Geometry.Parse("M0,0 H20 V12 H0 Z M2,2 H18 M2,5 H18")
            });

            // --- GRUPO: Threed32.ocx ---
            string cat3d = "Threed32.ocx";

            tools.Add(new ToolboxItem
            {
                Name = "SSPanel",
                Category = cat3d,
                Type = "Threed.SSPanel",
                IconData = Geometry.Parse("M0,0 H20 V16 H0 Z M3,3 H17 V13 H3 Z")
            });

            tools.Add(new ToolboxItem
            {
                Name = "SSCommand",
                Category = cat3d,
                Type = "Threed.SSCommand",
                IconData = Geometry.Parse("M1,1 H19 V13 H1 Z")
            });

            tools.Add(new ToolboxItem
            {
                Name = "SSCheck",
                Category = cat3d,
                Type = "Threed.SSCheck",
                IconData = Geometry.Parse("M0,4 H10 V14 H0 Z M2,7 L5,10 L10,3")
            });

            tools.Add(new ToolboxItem
            {
                Name = "SSOption",
                Category = cat3d,
                Type = "Threed.SSOption",
                IconData = Geometry.Parse("M6,6 A5,5 0 1 1 6.1,6 Z M8,8 A2,2 0 1 1 8.1,8 Z")
            });

            tools.Add(new ToolboxItem
            {
                Name = "SSFrame",
                Category = cat3d,
                Type = "Threed.SSFrame",
                IconData = Geometry.Parse("M0,4 H2 V0 H22 V18 H0 Z M2,6 H20 V16 H2 Z")
            });

            tools.Add(new ToolboxItem
            {
                Name = "SSRibbon",
                Category = cat3d,
                Type = "Threed.SSRibbon",
                IconData = Geometry.Parse("M2,2 H18 V18 H2 Z M4,4 L16,16 M4,16 L16,4")
            });

            tools.Add(new ToolboxItem
            {
                Name = "SSTab",
                Category = cat3d,
                Type = "SSTab",
                // Icono tipo carpeta con pestañas
                IconData = Geometry.Parse("M2,4 L6,4 L8,6 L18,6 L18,16 L2,16 Z M2,6 L18,6")
            });

            // --- GRUPO: ActiveX / Otros ---
            string catActiveX = "ActiveX / Otros";

            tools.Add(new ToolboxItem
            {
                Name = "Grid",
                Category = catActiveX,
                Type = "Grid",
                IconData = Geometry.Parse("M0,0 H18 V18 H0 Z M6,0 V18 M12,0 V18 M0,6 H18 M0,12 H18")
            });

            tools.Add(new ToolboxItem
            {
                Name = "Graph",
                Category = catActiveX,
                Type = "Graph",
                IconData = Geometry.Parse("M2,16 H18 M2,2 V16 M5,16 V10 H7 V16 M9,16 V6 H11 V16 M13,16 V12 H15 V16")
            });

            tools.Add(new ToolboxItem
            {
                Name = "MaskEdBox",
                Category = catActiveX,
                Type = "MaskEdBox",
                IconData = Geometry.Parse("M0,0 H20 V14 H0 Z M3,5 L5,5 M7,5 L9,5")
            }); // ## simple

            tools.Add(new ToolboxItem
            {
                Name = "CommonDialog",
                Category = catActiveX,
                Type = "CommonDialog",
                IconData = Geometry.Parse("M0,4 H16 V14 H0 Z M4,0 H20 V10 H16")
            });

            tools.Add(new ToolboxItem
            {
                Name = "CrystalReport",
                Category = catActiveX,
                Type = "CrystalReport",
                IconData = Geometry.Parse("M10,2 L18,10 L10,18 L2,10 Z")
            }); // Rombo

            tools.Add(new ToolboxItem
            {
                Name = "Map",
                Category = catActiveX,
                Type = "Map",
                IconData = Geometry.Parse("M2,2 H18 V18 H2 Z M2,8 H18 M8,2 V18")
            }); // Mapa simple

            // 2. Crear la Vista Agrupada
            var view = (CollectionView)CollectionViewSource.GetDefaultView(tools);
            view.GroupDescriptions.Add(new PropertyGroupDescription("Category"));

            // 3. Asignar al UI
            ToolsList.ItemsSource = view;
        }

        // -----------------------------------------------------------
        // 2. BUSCADOR
        // -----------------------------------------------------------
        private void TxtSearchTool_TextChanged(object sender, TextChangedEventArgs e)
        {
            var view = CollectionViewSource.GetDefaultView(ToolsList.ItemsSource);
            if (view == null) return;

            string filter = TxtSearchTool.Text.Trim();
            if (string.IsNullOrEmpty(filter))
            {
                view.Filter = null;
            }
            else
            {
                view.Filter = o =>
                {
                    var item = o as ToolboxItem;
                    return item != null && item.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
                };
            }
        }

        private void BtnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            TxtSearchTool.Text = "";
            TxtSearchTool.Focus();
        }

        // Navegación con teclado desde el buscador
        private void TxtSearchTool_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Flecha Abajo: Ir a la lista
            if (e.Key == Key.Down)
            {
                if (ToolsList.Items.Count > 0)
                {
                    ToolsList.SelectedIndex = 0;
                    var item = ToolsList.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem;
                    item?.Focus();
                }
                e.Handled = true;
            }
            // Tecla Escape: Limpiar búsqueda y QUITAR FOCO
            else if (e.Key == Key.Escape)
            {
                if (string.IsNullOrEmpty(TxtSearchTool.Text))
                {
                    // Si ya está vacío, solo quitamos el foco
                    ToolsList.Focus();
                }
                else
                {
                    // Si tiene texto, lo borramos primero
                    TxtSearchTool.Text = string.Empty;
                }
                e.Handled = true;
            }
        }

        // -----------------------------------------------------------
        // 3. DRAG & DROP (Reemplaza los antiguos Clicks)
        // -----------------------------------------------------------

        // Este método se conecta mediante un Style en el XAML al evento PreviewMouseLeftButtonDown del ListBoxItem
        // El estilo debe tener: <EventSetter Event="PreviewMouseLeftButtonDown" Handler="ToolboxItem_PreviewMouseDown"/>
        public void ToolboxItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Verificamos que sea un ListBoxItem (o lo buscamos)
            DependencyObject dep = (DependencyObject)e.OriginalSource;
            while (dep != null && !(dep is ListBoxItem))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep is ListBoxItem item && item.DataContext is ToolboxItem toolItem)
            {
                // Obtenemos el nombre del control (Type) que es lo que el DesignerCanvas espera
                // Ej: "VB.CommandButton" o "CommandButton" (según cómo esté programada tu factory)
                // Usamos una pequeña lógica de limpieza si tu Factory espera nombres simples
                string controlType = toolItem.Type;

                // Si tu Factory usa nombres sin prefijo "VB.", podemos limpiarlo aquí o dejarlo si la factory lo maneja.
                // En tu código antiguo enviabas "CommandButton". Aquí enviamos "VB.CommandButton".
                // Asegúrate que RetroControlFactory sepa manejar el punto, o limpiamos:
                if (controlType.StartsWith("VB.")) controlType = controlType.Replace("VB.", "");
                if (controlType.StartsWith("Threed.")) controlType = controlType.Replace("Threed.", "");

                // Empaquetamos los datos IGUAL que antes
                DataObject data = new DataObject("ControlToolboxItem", controlType);

                // Iniciamos arrastre
                DragDrop.DoDragDrop(item, data, DragDropEffects.Copy);

                e.Handled = true;
            }
        }

        // Opcional: Si el usuario hace click simple sin arrastrar, también seleccionamos (útil para modo 'Click luego dibujar')
        private void ToolsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ToolsList.SelectedItem is ToolboxItem item)
            {
                string type = item.Type.Replace("VB.", "").Replace("Threed.", "");
                OnControlSelected?.Invoke(this, type);

                // Reseteamos selección para permitir volver a clicar el mismo
                ToolsList.SelectedIndex = -1;
            }
        }


        private void RootGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Si el buscador tiene el foco, pero el mouse NO está sobre él...
            if (TxtSearchTool.IsFocused && !TxtSearchTool.IsMouseOver)
            {
                // Pasamos el foco a la lista (que no captura letras) o limpiamos el foco
                // Esto devuelve el control de atajos (Ctrl+C, etc) a la ventana principal
                ToolsList.Focus();
            }
        }


    }
}