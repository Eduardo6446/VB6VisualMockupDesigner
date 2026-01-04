using System;
using System.Diagnostics; // Para Process y System Info
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading; // Para el Timer
namespace VB6VisualMockupDesigner
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        private DispatcherTimer _timer;

        public AboutWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Iniciamos el timer para actualizar la RAM cada segundo
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // Primera actualización inmediata
            UpdateMemoryUsage();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateMemoryUsage();
        }

        private void UpdateMemoryUsage()
        {
            // Obtiene la memoria usada por el proceso actual en MB
            long memory = Process.GetCurrentProcess().PrivateMemorySize64;
            double mb = memory / 1024.0 / 1024.0;
            txtMemory.Text = $"{mb:F2} MB (Privada)";
        }

        // --- Easter Egg: Doble Click en el Logo ---
        private void AppLogo_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Verifica que sea doble click (aunque el evento ya lo sugiere, aseguramos)
            if (e.ClickCount == 2)
            {
                // El clásico sonido de error de Windows
                System.Media.SystemSounds.Hand.Play();

                MessageBox.Show(
                    "Run-time error '429':\n\nActiveX component can't create object.",
                    "Microsoft Visual Basic",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // --- Botón System Info (Clásico) ---
        private void BtnSysInfo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("msinfo32.exe") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo abrir System Info: " + ex.Message);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Evento para mover la ventana al hacer clic en el fondo
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Solo permitir mover si se presiona el clic izquierdo
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
    }
}
