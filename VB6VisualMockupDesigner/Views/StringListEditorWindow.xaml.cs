using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VB6VisualMockupDesigner.Views
{
    public partial class StringListEditorWindow : Window
    {
        public string ResultText { get; private set; }

        public StringListEditorWindow(string currentContent)
        {
            InitializeComponent();

            // Asignar texto y colocar cursor al final
            TxtContent.Text = currentContent;
            TxtContent.CaretIndex = TxtContent.Text.Length;

            // Evento para actualizar contador
            TxtContent.TextChanged += (s, e) => UpdateLineCount();

            // Foco inicial
            Loaded += (s, e) => {
                TxtContent.Focus();
                UpdateLineCount();
            };
        }

        private void UpdateLineCount()
        {
            int lines = TxtContent.LineCount;
            // A veces LineCount reporta -1 si no ha renderizado
            if (lines < 0) lines = 0;
            // Ajuste visual para texto vacío
            if (string.IsNullOrEmpty(TxtContent.Text)) lines = 0;

            if (LblLineCount != null)
                LblLineCount.Text = $"{lines} elementos";
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            ResultText = TxtContent.Text;
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
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