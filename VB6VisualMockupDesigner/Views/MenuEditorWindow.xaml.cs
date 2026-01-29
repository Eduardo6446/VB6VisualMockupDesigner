using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VB6VisualMockupDesigner.Models;

namespace VB6VisualMockupDesigner.Views
{
    public partial class MenuEditorWindow : Window
    {
        // Esta colección avisa al ListBox cuando se agregan/quitan ítems
        public ObservableCollection<MenuModel> MenuItems { get; set; } = new ObservableCollection<MenuModel>();

        // Bandera para evitar bucles al actualizar TextBoxes
        private bool _isUpdatingUi = false;

        public MenuEditorWindow()
        {
            InitializeComponent();

            // Conectar datos
            LstMenu.ItemsSource = MenuItems;

            // Agregar un ítem inicial vacío si la lista está vacía
            this.Loaded += (s, e) => {
                if (MenuItems.Count == 0)
                {
                    MenuItems.Add(new MenuModel { Caption = "", Name = "" });
                }
                LstMenu.SelectedIndex = 0;
            };
        }

        // =========================================================
        // 1. SINCRONIZACIÓN UI <-> MODELO
        // =========================================================

        private void LstMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstMenu.SelectedItem is MenuModel item)
            {
                _isUpdatingUi = true;

                // Cargar datos del ítem en los TextBoxes
                TxtCaption.Text = item.Caption;
                TxtName.Text = item.Name;
                ChkChecked.IsChecked = item.Checked;
                ChkEnabled.IsChecked = item.Enabled;
                ChkVisible.IsChecked = item.Visible;
                CboShortcut.Text = item.Shortcut; // Simple binding por texto

                _isUpdatingUi = false;
            }
        }

        // Guardar cambios mientras escribes (Eventos TextChanged/Click)
        // NOTA: Conecta estos eventos en el XAML o usa Bindings. 
        // Para simplificar, aquí lo haremos al pulsar "Siguiente" o cambiar selección,
        // pero lo ideal es actualizar el modelo en tiempo real:

        private void UpdateCurrentItem()
        {
            if (_isUpdatingUi || LstMenu.SelectedIndex == -1) return;
            if (!(LstMenu.SelectedItem is MenuModel item)) return;

            item.Caption = TxtCaption.Text;
            item.Name = TxtName.Text;
            item.Checked = ChkChecked.IsChecked == true;
            item.Enabled = ChkEnabled.IsChecked == true;
            item.Visible = ChkVisible.IsChecked == true;
            item.Shortcut = CboShortcut.Text;
        }

        // Conecta este evento a los TextBoxes en XAML: TextChanged="Input_Changed"
        private void Input_Changed(object sender, RoutedEventArgs e) => UpdateCurrentItem();


        // =========================================================
        // 2. BOTONES DE ACCIÓN (Next, Insert, Delete)
        // =========================================================

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            UpdateCurrentItem(); // Guardar el actual

            // Si estamos en el último, crear uno nuevo
            if (LstMenu.SelectedIndex == MenuItems.Count - 1)
            {
                var newItem = new MenuModel { Caption = "", Name = "" };
                MenuItems.Add(newItem);
                LstMenu.SelectedItem = newItem;
                LstMenu.ScrollIntoView(newItem);
                TxtCaption.Focus();
            }
            else
            {
                // Si no, solo bajar al siguiente
                LstMenu.SelectedIndex++;
            }
        }

        private void BtnInsert_Click(object sender, RoutedEventArgs e)
        {
            int index = LstMenu.SelectedIndex;
            if (index == -1) index = MenuItems.Count;

            var newItem = new MenuModel { Caption = "", Name = "" };
            MenuItems.Insert(index, newItem);
            LstMenu.SelectedItem = newItem;
            TxtCaption.Focus();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            int index = LstMenu.SelectedIndex;
            if (index == -1) return;

            MenuItems.RemoveAt(index);

            // Seleccionar el siguiente (o el anterior si era el último)
            if (MenuItems.Count > 0)
            {
                LstMenu.SelectedIndex = (index < MenuItems.Count) ? index : MenuItems.Count - 1;
            }
        }

        // =========================================================
        // 3. LÓGICA DE FLECHAS (JERARQUÍA Y ORDEN)
        // =========================================================

        private void BtnRight_Click(object sender, RoutedEventArgs e) // Indentar (-->)
        {
            if (LstMenu.SelectedItem is MenuModel item)
            {
                int index = LstMenu.SelectedIndex;
                if (index > 0)
                {
                    // Regla: No puedes ser más de 1 nivel más profundo que el anterior
                    var prevItem = MenuItems[index - 1];
                    if (item.Level <= prevItem.Level)
                    {
                        item.Level++;
                    }
                }
                else
                {
                    // El primer ítem no puede tener indentación
                    // (Opcional: VB6 a veces lo permite, pero es raro)
                }
            }
        }

        private void BtnLeft_Click(object sender, RoutedEventArgs e) // Desindentar (<--)
        {
            if (LstMenu.SelectedItem is MenuModel item)
            {
                if (item.Level > 0) item.Level--;

                // Opcional: También desindentar a todos los hijos para mantener consistencia?
                // VB6 no lo hace automáticamente, te deja el caos. Lo dejaremos simple.
            }
        }

        private void BtnUp_Click(object sender, RoutedEventArgs e) // Mover Arriba
        {
            int index = LstMenu.SelectedIndex;
            if (index > 0)
            {
                MenuItems.Move(index, index - 1);
            }
        }

        private void BtnDown_Click(object sender, RoutedEventArgs e) // Mover Abajo
        {
            int index = LstMenu.SelectedIndex;
            if (index < MenuItems.Count - 1 && index != -1)
            {
                MenuItems.Move(index, index + 1);
            }
        }

        // =========================================================
        // 4. CIERRE Y RETORNO
        // =========================================================

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            UpdateCurrentItem(); // Asegurar guardar último cambio
            this.DialogResult = true; // Cierra la ventana devolviendo Success
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
    }
}