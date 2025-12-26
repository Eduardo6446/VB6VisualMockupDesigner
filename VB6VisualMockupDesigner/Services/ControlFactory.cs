using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using VB6VisualMockupDesigner.Helpers;

namespace VB6VisualMockupDesigner.Services
{
    public class ControlFactory
    {
        // Contadores encapsulados
        private int _btnCount = 1; private int _lblCount = 1;
        private int _txtCount = 1; private int _chkCount = 1;
        private int _optCount = 1; private int _frmCount = 1;
        private int _lstCount = 1; private int _cmbCount = 1;
        private int _picCount = 1; private int _tmrCount = 1;
        private int _shpCount = 1; private int _linCount = 1;
        private int _scrCount = 1; private int _drvCount = 1;
        private int _dirCount = 1; private int _filCount = 1;
        private int _imgCount = 1; private int _mnuCount = 1;
        private int _sbrCount = 1;

        public FrameworkElement CreateElementInstance(string vbType)
        {
            switch (vbType)
            {
                case "CommandButton": return new Button { Background = VbHelpers.GetVbGray(), BorderThickness = new Thickness(2) };
                case "Label": return new Label { Background = Brushes.Transparent };
                case "TextBox": return new TextBox { Background = Brushes.White, BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) };
                case "CheckBox": return new CheckBox();
                case "OptionButton": return new RadioButton();
                case "Frame": return new GroupBox { BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1) };
                case "ComboBox": return new ComboBox { IsReadOnly = true };
                case "ListBox": return new ListBox { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) };
                case "Timer": return VbHelpers.CreateTimerVisual();
                case "Shape": return new Rectangle { Stroke = Brushes.Black, StrokeDashArray = new DoubleCollection(new double[] { 2, 2 }) };
                case "Image": return new Image { Stretch = Stretch.Uniform, Source = null };
                case "HScrollBar": return new ScrollBar { Orientation = Orientation.Horizontal, Height = 18 };
                case "VScrollBar": return new ScrollBar { Orientation = Orientation.Vertical, Width = 18 };
                case "DriveListBox": return new ComboBox { Text = "C: [System]", IsReadOnly = true };
                case "DirListBox": return new ListBox();
                case "FileListBox": return new ListBox();
                case "Line": return new Line { Stroke = Brushes.Black, StrokeThickness = 1, X2 = 80, Y2 = 80 };
                case "PictureBox": return new Border { Background = VbHelpers.GetVbGray(), BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) };

                case "Menu":
                    var m = new Menu { Background = Brushes.LightGray, IsMainMenu = true };
                    m.Items.Add(new MenuItem { Header = "File" });
                    return m;
                case "StatusBar":
                    var s = new StatusBar { Background = VbHelpers.GetVbGray() };
                    s.Items.Add(new StatusBarItem { Content = "Status" });
                    return s;

                default: return null;
            }
        }

        public string GetNextNameForType(string vbType)
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
                case "PictureBox": return "Picture" + _picCount++;
                case "Menu": return "Menu" + _mnuCount++;
                case "StatusBar": return "StatusBar" + _sbrCount++;
                default: return vbType + DateTime.Now.Ticks;
            }
        }
    }
}