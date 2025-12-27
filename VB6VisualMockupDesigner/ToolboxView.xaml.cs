using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace VB6VisualMockupDesigner
{
    /// <summary>
    /// Interaction logic for ToolboxView.xaml
    /// </summary>
    public partial class ToolboxView : UserControl
    {
        // Evento para comunicar al MainWindow qué control se seleccionó
        public event System.EventHandler<string> OnControlSelected;

        public ToolboxView()
        {
            InitializeComponent();
        }

        // Método público para controlar el estado
        public void EnableTools(bool isEnabled)
        {
            ControlsGrid.IsEnabled = isEnabled;
        }

        // Métodos de acción (simplificados para redirigir a un evento común)
        private void SendControl(string controlName)
        {
            OnControlSelected?.Invoke(this, controlName);
        }

        private void ToolboxButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button btn && btn.ToolTip != null)
            {
                // Obtenemos el nombre del control (ej: "CommandButton") desde el ToolTip
                string controlType = btn.ToolTip.ToString();

                // Empaquetamos los datos para el viaje
                DataObject data = new DataObject("ControlToolboxItem", controlType);

                // Iniciamos la operación de arrastre
                DragDrop.DoDragDrop(btn, data, DragDropEffects.Copy);

                // Marcamos el evento como manejado para que no interfiera con el Click normal si lo hubiera
                e.Handled = true;
            }
        }

        private void AddPictureBox_Click(object sender, RoutedEventArgs e) => SendControl("PictureBox");
        private void AddLabel_Click(object sender, RoutedEventArgs e) => SendControl("Label");
        private void AddTextBox_Click(object sender, RoutedEventArgs e) => SendControl("TextBox");
        private void AddFrame_Click(object sender, RoutedEventArgs e) => SendControl("Frame");
        private void AddButton_Click(object sender, RoutedEventArgs e) => SendControl("CommandButton");
        private void AddCheckBox_Click(object sender, RoutedEventArgs e) => SendControl("CheckBox");
        private void AddOptionButton_Click(object sender, RoutedEventArgs e) => SendControl("OptionButton");
        private void AddComboBox_Click(object sender, RoutedEventArgs e) => SendControl("ComboBox");
        private void AddListBox_Click(object sender, RoutedEventArgs e) => SendControl("ListBox");
        private void AddHScrollBar_Click(object sender, RoutedEventArgs e) => SendControl("HScrollBar");
        private void AddVScrollBar_Click(object sender, RoutedEventArgs e) => SendControl("VScrollBar");
        private void AddTimer_Click(object sender, RoutedEventArgs e) => SendControl("Timer");
        private void AddDriveListBox_Click(object sender, RoutedEventArgs e) => SendControl("DriveListBox");
        private void AddDirListBox_Click(object sender, RoutedEventArgs e) => SendControl("DirListBox");
        private void AddFileListBox_Click(object sender, RoutedEventArgs e) => SendControl("FileListBox");
        private void AddShape_Click(object sender, RoutedEventArgs e) => SendControl("Shape");
        private void AddLine_Click(object sender, RoutedEventArgs e) => SendControl("Line");
        private void AddImage_Click(object sender, RoutedEventArgs e) => SendControl("Image");
        private void AddMenu_Click(object sender, RoutedEventArgs e) => SendControl("Menu");
        private void AddStatusBar_Click(object sender, RoutedEventArgs e) => SendControl("StatusBar");
    }
}
