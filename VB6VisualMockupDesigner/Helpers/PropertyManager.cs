using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VB6VisualMockupDesigner.Models;
using VB6VisualMockupDesigner.Controls; // Necesario para VB6Data

namespace VB6VisualMockupDesigner.Helpers
{
    public static class PropertyManager
    {
        // ==========================================
        // LISTAS DE OPCIONES (ENUMS SIMULADOS)
        // ==========================================
        private static List<string> BoolOptions = new List<string> { "True", "False" };
        private static List<string> AlignOptions = new List<string> { "0 - Left Justify", "1 - Right Justify", "2 - Center" };
        private static List<string> BorderStyleOptions = new List<string> { "0 - None", "1 - Fixed Single" };
        private static List<string> BackStyleOptions = new List<string> { "0 - Transparent", "1 - Opaque" };
        private static List<string> AppearanceOptions = new List<string> { "0 - Flat", "1 - 3D" };
        private static List<string> SSTabStyleOptions = new List<string> { "0 - Tabbed Dialog", "1 - Property Page" };
        private static List<string> SSTabOrientationOptions = new List<string> { "0 - Top", "1 - Bottom", "2 - Left", "3 - Right" };

        // NUEVO: Opciones para ScrollBars (TextBox)
        private static List<string> ScrollBarOptions = new List<string> { "0 - None", "1 - Horizontal", "2 - Vertical", "3 - Both" };

        // NUEVO: Opciones para ComboBox Style
        private static List<string> ComboStyleOptions = new List<string> { "0 - Dropdown Combo", "1 - Simple Combo", "2 - Dropdown List" };

        // ==========================================
        // 1. OBTENER PROPIEDADES (LECTURA)
        // ==========================================
        public static List<PropertyItem> GetPropertiesFor(FrameworkElement ctrl)
        {
            var list = new List<PropertyItem>();
            string passChar = VB6Data.GetPasswordChar(ctrl) ?? "";

            if (ctrl == null) return list;

            // --- CATEGORÍA: MISC ---
            list.Add(new PropertyItem
            {
                Name = "(Name)",
                Value = ctrl.Name,
                Category = "Misc",
                Description = "Devuelve o establece el nombre utilizado en el código para identificar un objeto."
            });

            // --- CATEGORÍA: POSITION ---
            list.Add(new PropertyItem { Name = "Left", Value = (int)Canvas.GetLeft(ctrl), Category = "Position", Type = PropertyType.Number });
            list.Add(new PropertyItem { Name = "Top", Value = (int)Canvas.GetTop(ctrl), Category = "Position", Type = PropertyType.Number });
            list.Add(new PropertyItem { Name = "Width", Value = (int)ctrl.Width, Category = "Position", Type = PropertyType.Number });
            list.Add(new PropertyItem { Name = "Height", Value = (int)ctrl.Height, Category = "Position", Type = PropertyType.Number });

            // --- CATEGORÍA: APPEARANCE ---

            // Caption / Text
            if (ctrl is ContentControl cc)
            {
                list.Add(new PropertyItem { Name = "Caption", Value = cc.Content, Category = "Appearance", Description = "Texto que se muestra en el control." });
            }
            else if (ctrl is TextBox tb)
            {
                list.Add(new PropertyItem { Name = "Text", Value = tb.Text, Category = "Appearance", Description = "Texto contenido en el control." });

                // NUEVO: Propiedades Específicas de TextBox
                list.Add(new PropertyItem { Name = "MaxLength", Value = tb.MaxLength, Category = "Behavior", Type = PropertyType.Number, Description = "Número máximo de caracteres." });

                list.Add(new PropertyItem
                {
                    Name = "MultiLine",
                    Value = (tb.AcceptsReturn).ToString(),
                    Category = "Behavior",
                    Type = PropertyType.Boolean,
                    Options = BoolOptions,
                    Description = "Permite múltiples líneas de texto."
                });

                list.Add(new PropertyItem { Name = "PasswordChar", Value = passChar, Category = "Behavior", Description = "Carácter para ocultar contraseña (ej: *)." });

                // ScrollBars
                string currentScroll = "0 - None";
                if (tb.VerticalScrollBarVisibility == ScrollBarVisibility.Visible && tb.HorizontalScrollBarVisibility == ScrollBarVisibility.Visible) currentScroll = ScrollBarOptions[3];
                else if (tb.VerticalScrollBarVisibility == ScrollBarVisibility.Visible) currentScroll = ScrollBarOptions[2];
                else if (tb.HorizontalScrollBarVisibility == ScrollBarVisibility.Visible) currentScroll = ScrollBarOptions[1];

                list.Add(new PropertyItem
                {
                    Name = "ScrollBars",
                    Value = currentScroll,
                    Category = "Appearance",
                    Type = PropertyType.Enum,
                    Options = ScrollBarOptions,
                    Description = "Indica si el control tiene barras de desplazamiento."
                });
            }
            // Agregamos soporte de lectura para TextBlocks (Timer, SSPanel, etc.)
            else if (ctrl is TextBlock txt)
            {
                list.Add(new PropertyItem { Name = "Caption", Value = txt.Text, Category = "Appearance", Description = "Texto que se muestra en el control." });
            }
            // Soporte para Border que contiene un TextBlock (algunos controles retro)
            else if (ctrl is Border b && b.Child is TextBlock tbInside)
            {
                list.Add(new PropertyItem { Name = "Caption", Value = tbInside.Text, Category = "Appearance", Description = "Texto que se muestra en el control." });
            }

            // Alignment (Para Labels y TextBoxes)
            if (ctrl is Label || ctrl is TextBox)
            {
                list.Add(new PropertyItem
                {
                    Name = "Alignment",
                    Value = GetAlignFromControl(ctrl),
                    Category = "Appearance",
                    Type = PropertyType.Enum,
                    Options = AlignOptions,
                    Description = "Alineación del texto."
                });
            }

            // BackStyle (Transparente vs Opaco - Clave para Labels)
            if (ctrl is Label)
            {
                var bg = (ctrl as Control).Background;
                // Si es nulo o Transparente -> 0. Si tiene color -> 1.
                string currentStyle = (bg == null || bg == Brushes.Transparent) ? BackStyleOptions[0] : BackStyleOptions[1];

                list.Add(new PropertyItem
                {
                    Name = "BackStyle",
                    Value = currentStyle,
                    Category = "Appearance",
                    Type = PropertyType.Enum,
                    Options = BackStyleOptions,
                    Description = "Indica si un control es transparente u opaco."
                });
            }

            // BackColor & ForeColor
            if (ctrl is Control c)
            {
                string colorHex = "#FFFFFF";
                if (c.Background is SolidColorBrush sb) colorHex = sb.Color.ToString();

                list.Add(new PropertyItem
                {
                    Name = "BackColor",
                    Value = colorHex,
                    Category = "Appearance",
                    Type = PropertyType.Color,
                    Description = "Color de fondo."
                });

                string foreHex = "#000000";
                if (c.Foreground is SolidColorBrush sf) foreHex = sf.Color.ToString();

                list.Add(new PropertyItem
                {
                    Name = "ForeColor",
                    Value = foreHex,
                    Category = "Appearance",
                    Type = PropertyType.Color,
                    Description = "Color del texto."
                });
            }
            // Soporte lectura de color para TextBlock sueltos (Timer, etc)
            else if (ctrl is TextBlock t)
            {
                string foreHex = "#000000";
                if (t.Foreground is SolidColorBrush sf) foreHex = sf.Color.ToString();
                list.Add(new PropertyItem { Name = "ForeColor", Value = foreHex, Category = "Appearance", Type = PropertyType.Color });
            }
            else if (ctrl is Border b)
            {
                string bgHex = "#D4D0C8";
                if (b.Background is SolidColorBrush sb) bgHex = sb.Color.ToString();
                list.Add(new PropertyItem { Name = "BackColor", Value = bgHex, Category = "Appearance", Type = PropertyType.Color });

                // Si el Border tiene un hijo TextBlock, leemos su ForeColor
                if (b.Child is TextBlock tbChild)
                {
                    string foreHex = "#000000";
                    if (tbChild.Foreground is SolidColorBrush sf) foreHex = sf.Color.ToString();
                    list.Add(new PropertyItem { Name = "ForeColor", Value = foreHex, Category = "Appearance", Type = PropertyType.Color });
                }
            }

            // BorderStyle
            if (ctrl is Control cBorder)
            {
                string currentBorder = (cBorder.BorderThickness.Top > 0) ? BorderStyleOptions[1] : BorderStyleOptions[0];
                list.Add(new PropertyItem
                {
                    Name = "BorderStyle",
                    Value = currentBorder,
                    Category = "Appearance",
                    Type = PropertyType.Enum,
                    Options = BorderStyleOptions,
                    Description = "Estilo del borde."
                });
            }

            // Appearance (Flat vs 3D)
            if (ctrl is Control cApp)
            {
                string currentApp = AppearanceOptions[1]; // Default 3D (Gris)
                if (cApp.BorderBrush is SolidColorBrush bBrush && bBrush.Color == Colors.Black)
                {
                    currentApp = AppearanceOptions[0]; // Flat (Negro)
                }

                list.Add(new PropertyItem
                {
                    Name = "Appearance",
                    Value = currentApp,
                    Category = "Appearance",
                    Type = PropertyType.Enum,
                    Options = AppearanceOptions,
                    Description = "Determina si se dibuja con efectos 3D."
                });
            }

            if (ctrl is TabControl tc)
            {

                string currentOrient = SSTabOrientationOptions[0];
                switch (tc.TabStripPlacement)
                {
                    case Dock.Bottom: currentOrient = SSTabOrientationOptions[1]; break;
                    case Dock.Left: currentOrient = SSTabOrientationOptions[2]; break;
                    case Dock.Right: currentOrient = SSTabOrientationOptions[3]; break;
                }
                list.Add(new PropertyItem { Name = "Orientation", Value = currentOrient, Category = "Appearance", Type = PropertyType.Enum, Options = SSTabOrientationOptions });

                // Propiedad: Tabs (Cantidad)
                list.Add(new PropertyItem { Name = "Tabs", Value = tc.Items.Count, Category = "Behavior", Type = PropertyType.Number, Description = "Número total de pestañas." });

                // Propiedad: Tab (Selección actual)
                list.Add(new PropertyItem { Name = "Tab", Value = tc.SelectedIndex, Category = "Behavior", Type = PropertyType.Number, Description = "Índice de la pestaña actual (empieza en 0)." });

                // Propiedad: TabsPerRow (Guardada en VB6Data porque WPF lo maneja diferente)
                // Usamos el helper SetTag/GetTag o creamos uno nuevo en VB6Data si prefieres ser estricto. 
                // Por simplicidad, usaremos un valor por defecto o leido de VB6Data si existe.
                string tabsPerRow = VB6Data.GetTag(tc) ?? "3"; // Usaremos Tag temporalmente para esto o VB6Data
                                                               // NOTA: Para hacerlo bien, deberías agregar TabsPerRowProperty en VB6Data.cs igual que PasswordChar.
                                                               // Asumiremos que existe o usaremos un valor dummy por ahora.
                list.Add(new PropertyItem { Name = "TabsPerRow", Value = 3, Category = "Appearance", Type = PropertyType.Number });

                // Propiedad: Style
                // Mapeamos visualmente: Si tiene borde grueso es Dialog, si es plano es PropertyPage (simulado)
                list.Add(new PropertyItem { Name = "Style", Value = SSTabStyleOptions[0], Category = "Appearance", Type = PropertyType.Enum, Options = SSTabStyleOptions });

                // Propiedad: TabHeight
                // En WPF, esto se puede mapear a ItemContainerStyle -> Height, pero es complejo leerlo. 
                // Devolveremos un estimado visual.
                list.Add(new PropertyItem { Name = "TabHeight", Value = 20, Category = "Appearance", Type = PropertyType.Number });

                if (tc.SelectedItem is TabItem currentTab)
                {
                    list.Add(new PropertyItem { Name = "TabCaption", Value = currentTab.Header, Category = "Appearance" });

                    // LEER DE LA MEMORIA (VB6Data), NO DE LA UI
                    bool isEnabled = VB6Data.GetTabEnabled(currentTab);
                    list.Add(new PropertyItem { Name = "TabEnabled", Value = isEnabled.ToString(), Category = "Behavior", Type = PropertyType.Boolean, Options = BoolOptions });

                    bool isVisible = VB6Data.GetTabVisible(currentTab);
                    list.Add(new PropertyItem { Name = "TabVisible", Value = isVisible.ToString(), Category = "Behavior", Type = PropertyType.Boolean, Options = BoolOptions });
                }
            }

            // --- LIST & COMBOS ---
            if (ctrl is ListBox || ctrl is ComboBox)
            {
                // 1. Extraer ítems actuales a una lista de strings
                var itemsList = new List<string>();
                System.Collections.IEnumerable currentItems = (ctrl as ItemsControl).Items; // Usamos ItemsControl para cubrir ambos

                foreach (var item in currentItems)
                {
                    itemsList.Add(item.ToString());
                }

                // 2. Unirlos con saltos de línea para el editor
                string itemsStr = string.Join("\r\n", itemsList);

                list.Add(new PropertyItem
                {
                    Name = "List",
                    Value = itemsStr,
                    Category = "Data",
                    Type = PropertyType.StringList,
                    Description = "Devuelve o establece los elementos contenidos en la lista."
                });

                // NUEVO: ComboBox Style
                if (ctrl is ComboBox combo)
                {
                    // Mapeo simple: Editable = Dropdown(0), No Editable = DropdownList(2)
                    string styleVal = combo.IsEditable ? ComboStyleOptions[0] : ComboStyleOptions[2];
                    list.Add(new PropertyItem
                    {
                        Name = "Style",
                        Value = styleVal,
                        Category = "Appearance",
                        Type = PropertyType.Enum,
                        Options = ComboStyleOptions,
                        Description = "Devuelve o establece un valor que indica el tipo de visualización y comportamiento del control."
                    });
                }
            }

            // Font
            if (ctrl is Control cFont)
            {
                string fontInfo = $"{cFont.FontFamily}; {cFont.FontSize}pt";
                if (cFont.FontWeight == FontWeights.Bold) fontInfo += "; Bold";

                list.Add(new PropertyItem
                {
                    Name = "Font",
                    Value = fontInfo,
                    Category = "Appearance",
                    Type = PropertyType.Font,
                    Description = "Fuente del texto."
                });
            }
            else if (ctrl is TextBlock tbFont) // TextBlock suelto
            {
                string fontInfo = $"{tbFont.FontFamily}; {tbFont.FontSize}pt";
                list.Add(new PropertyItem { Name = "Font", Value = fontInfo, Category = "Appearance", Type = PropertyType.Font, Description = "Fuente del texto." });
            }

            // Picture
            if (ctrl is Image img || (ctrl is Border && (ctrl as Border).Child is Canvas)) // PictureBox mock
            {
                list.Add(new PropertyItem
                {
                    Name = "Picture",
                    Value = "",
                    Category = "Appearance",
                    Type = PropertyType.File,
                    Description = "Gráfico mostrado en el control."
                });
            }

            // --- CATEGORÍA: BEHAVIOR ---

            list.Add(new PropertyItem { Name = "Visible", Value = (ctrl.Opacity > 0.5).ToString(), Category = "Behavior", Type = PropertyType.Boolean, Options = BoolOptions });

            // LÓGICA ENABLED (Simulación visual)
            bool isLogicallyEnabled = ctrl.Opacity > 0.9;

            list.Add(new PropertyItem
            {
                Name = "Enabled",
                Value = isLogicallyEnabled.ToString(),
                Category = "Behavior",
                Type = PropertyType.Boolean,
                Options = BoolOptions,
                Description = "Habilita o deshabilita el control (Visualmente en modo diseño)."
            });

            if (ctrl is Control cTab)
            {
                list.Add(new PropertyItem { Name = "TabIndex", Value = cTab.TabIndex, Category = "Behavior", Type = PropertyType.Number });
            }

            list.Add(new PropertyItem { Name = "Tag", Value = ctrl.Tag, Category = "Misc" });

            int? idx = VB6Data.GetIndex(ctrl);
            list.Add(new PropertyItem
            {
                Name = "Index",
                Value = idx.HasValue ? idx.ToString() : "",
                Category = "Misc",
                Description = "Indica el índice en una matriz de controles."
            });

            return list;
        }

        // ==========================================
        // 2. APLICAR PROPIEDADES (ESCRITURA)
        // ==========================================
        public static void ApplyProperty(FrameworkElement ctrl, PropertyItem item)
        {
            string val = item.Value?.ToString();

            switch (item.Name)
            {
                // --- POSITION ---
                case "Left": Canvas.SetLeft(ctrl, ParseDouble(val)); break;
                case "Top": Canvas.SetTop(ctrl, ParseDouble(val)); break;
                case "Width": ctrl.Width = Math.Max(10, ParseDouble(val)); break;
                case "Height": ctrl.Height = Math.Max(10, ParseDouble(val)); break;

                // --- CONTENT ---
                case "Caption":
                    if (ctrl is ContentControl cc) cc.Content = val;
                    if (ctrl is TextBlock txt) txt.Text = val;
                    if (ctrl is GroupBox gb) gb.Header = val;
                    // Caso especial: Border que envuelve TextBlock (ej: Timer, SSPanel)
                    if (ctrl is Border b && b.Child is TextBlock tChild) tChild.Text = val;
                    break;

                case "Text":
                    if (ctrl is TextBox t) t.Text = val;
                    if (ctrl is ComboBox cb) cb.Text = val; // ComboBox también tiene Text
                    break;

                // --- NUEVO: TEXTBOX PROPERTIES ---
                case "MaxLength":
                    if (ctrl is TextBox tbMax) tbMax.MaxLength = (int)ParseDouble(val);
                    break;

                case "MultiLine":
                    if (ctrl is TextBox tbMulti)
                    {
                        bool isMulti = (val == "True");
                        tbMulti.AcceptsReturn = isMulti;
                        tbMulti.TextWrapping = isMulti ? TextWrapping.Wrap : TextWrapping.NoWrap;
                    }
                    break;

                case "PasswordChar":
                    // Solo guardamos el valor (no funcional visualmente en TextBox estándar)
                    VB6Data.SetPasswordChar(ctrl, val);
                    break;

                case "ScrollBars":
                    if (ctrl is TextBox tbScroll)
                    {
                        // "0 - None", "1 - Horizontal", "2 - Vertical", "3 - Both"
                        if (val.Contains("0")) { tbScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden; tbScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden; }
                        else if (val.Contains("1")) { tbScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto; tbScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden; }
                        else if (val.Contains("2")) { tbScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden; tbScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto; }
                        else if (val.Contains("3")) { tbScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto; tbScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto; }
                    }
                    break;

                // --- ALIGNMENT ---
                case "Alignment":
                    ApplyAlignment(ctrl, val);
                    break;

                // --- NUEVO: COMBOBOX STYLE ---
                case "Style":
                    if (ctrl is ComboBox cbo)
                    {
                        if (val.Contains("2")) cbo.IsEditable = false; // Dropdown List
                        else cbo.IsEditable = true; // Dropdown Combo
                    }
                    else if (ctrl is TabControl tcStyle)
                    {
                        // Lógica para SSTab Style (Visualmente no hacemos mucho en WPF por ahora, pero guardamos el dato)
                        // Podrías cambiar el Template si tuvieras estilos diferentes definidos.

                    }
                    break;

                // --- BACKSTYLE (Label Transparent vs Opaque) ---
                case "BackStyle":
                    if (ctrl is Control cStyle)
                    {
                        if (val != null && val.Contains("0")) // Transparent
                        {
                            cStyle.Background = Brushes.Transparent;
                        }
                        else // Opaque
                        {
                            cStyle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D4D0C8"));
                        }
                    }
                    break;

                // --- COLORS ---
                case "BackColor":
                    if (ctrl is Control cColor) cColor.Background = ParseColor(val);
                    if (ctrl is Border bColor) bColor.Background = ParseColor(val);
                    // TextBlock tiene Background también, por si acaso
                    if (ctrl is TextBlock tColor) tColor.Background = ParseColor(val);
                    break;

                case "ForeColor":
                    if (ctrl is Control cFore) cFore.Foreground = ParseColor(val);
                    if (ctrl is TextBlock tFore) tFore.Foreground = ParseColor(val); // <--- AQUI ESTÁ LA LÍNEA RESTAURADA
                    // Caso especial: Border que envuelve TextBlock
                    if (ctrl is Border bFore && bFore.Child is TextBlock tbFore) tbFore.Foreground = ParseColor(val);
                    break;

                // --- BORDER STYLE ---
                case "BorderStyle":
                    if (ctrl is Control cBorder)
                    {
                        if (val != null && val.Contains("1")) // Fixed Single
                        {
                            cBorder.BorderThickness = new Thickness(1);
                            cBorder.BorderBrush = Brushes.Gray;
                            cBorder.Padding = new Thickness(2);
                        }
                        else // None
                        {
                            cBorder.BorderThickness = new Thickness(0);
                            cBorder.Padding = new Thickness(0);
                        }
                    }
                    break;

                case "Tabs":
                    if (ctrl is TabControl tcTabs)
                    {
                        int newCount = (int)ParseDouble(val);
                        newCount = Math.Max(1, newCount); // Mínimo 1 pestaña

                        int currentCount = tcTabs.Items.Count;

                        if (newCount > currentCount)
                        {
                            // AGREGAR PESTAÑAS
                            for (int i = currentCount; i < newCount; i++)
                            {
                                var newTab = new TabItem
                                {
                                    Header = $"Tab {i}",
                                    // IMPORTANTE: Crear el Canvas interno para que reciba Drop
                                    Content = new Canvas
                                    {
                                        HorizontalAlignment = HorizontalAlignment.Stretch,
                                        VerticalAlignment = VerticalAlignment.Stretch,
                                        Background = Brushes.Transparent,
                                        ClipToBounds = true,
                                        MinWidth = 10,
                                        MinHeight = 10
                                    }
                                };

                                VB6Data.SetTabEnabled(newTab, true);
                                VB6Data.SetTabVisible(newTab, true);

                                tcTabs.Items.Add(newTab);
                            }
                        }
                        else if (newCount < currentCount)
                        {
                            // ELIMINAR PESTAÑAS (Desde la última)
                            for (int i = currentCount - 1; i >= newCount; i--)
                            {
                                tcTabs.Items.RemoveAt(i);
                            }
                        }
                    }
                    break;

                case "TabCaption":
                    if (ctrl is TabControl tc && tc.SelectedItem is TabItem tItem)
                    {
                        tItem.Header = val;
                    }
                    break;

                case "Tab": // Cambiar pestaña activa
                    if (ctrl is TabControl tcSel)
                    {
                        int idx = (int)ParseDouble(val);
                        if (idx >= 0 && idx < tcSel.Items.Count)
                        {
                            tcSel.SelectedIndex = idx;
                        }
                    }
                    break;

                case "TabHeight":
                    if (ctrl is TabControl tcHeight)
                    {
                        double h = ParseDouble(val);
                        // En WPF cambiar la altura de los headers requiere estilo. 
                        // Si tienes un estilo dinámico podrías hacer:
                        // tcHeight.Resources["TabItemHeight"] = h; 
                        // Por ahora, lo guardamos para el generador de código sin efecto visual inmediato
                        // o podrías iterar los TabItems y fijar Height (si no lo bloquea el estilo).
                    }
                    break;

                case "TabsPerRow":
                    // VB6 organiza pestañas en filas. WPF TabControl hace Wrap automático si tiene ancho fijo.
                    // Solo guardamos el dato para la exportación.
                    VB6Data.SetTag(ctrl, $"TabsPerRow:{val}");
                    break;

                case "TabEnabled":
                    if (ctrl is TabControl tcEn && tcEn.SelectedItem is TabItem tItemEn)
                    {
                        bool enable = (val == "True");

                        // 1. Persistencia: Guardamos el dato real
                        VB6Data.SetTabEnabled(tItemEn, enable);

                        // 2. Feedback Visual Inmediato:
                        // NO usamos tItemEn.IsEnabled = false porque eso impediría seleccionarla de nuevo.
                        // En su lugar, cambiamos el color del texto manualmente para simularlo.

                        if (enable)
                        {
                            // Restaurar color normal (asumiendo negro o el de tu tema)
                            tItemEn.ClearValue(Control.ForegroundProperty);
                        }
                        else
                        {
                            // Poner gris ("Inhabilitado")
                            tItemEn.Foreground = Brushes.Gray;
                        }
                    }
                    break;

                case "Orientation":
                    if (ctrl is TabControl tcOrient)
                    {
                        // 0 - Top, 1 - Bottom, 2 - Left, 3 - Right
                        if (val.Contains("0")) tcOrient.TabStripPlacement = Dock.Top;
                        else if (val.Contains("1")) tcOrient.TabStripPlacement = Dock.Bottom;
                        else if (val.Contains("2")) tcOrient.TabStripPlacement = Dock.Left;
                        else if (val.Contains("3")) tcOrient.TabStripPlacement = Dock.Right;
                    }
                    break;

                case "TabVisible":
                    if (ctrl is TabControl tcVis && tcVis.SelectedItem is TabItem tItemVis)
                    {
                        bool visible = (val == "True");

                        // 1. Persistencia
                        VB6Data.SetTabVisible(tItemVis, visible);

                        // 2. Feedback Visual Inmediato:
                        // Usamos Opacidad para "Modo Fantasma". 
                        // 1.0 = Visible, 0.4 = Oculto (pero seleccionable)
                        tItemVis.Opacity = visible ? 1.0 : 0.4;
                    }
                    break;
                case "List":
                    // 1. Obtener la colección de ítems del control WPF
                    System.Collections.IList itemsCollection = null;

                    if (ctrl is ListBox lb) itemsCollection = lb.Items;
                    else if (ctrl is ComboBox cbx) itemsCollection = cbx.Items;

                    if (itemsCollection != null)
                    {
                        // 2. Limpiar lista actual
                        itemsCollection.Clear();

                        // 3. Separar el string que viene del editor y rellenar
                        if (!string.IsNullOrEmpty(val))
                        {
                            // Normalizar saltos de línea y separar
                            var lines = val.Replace("\r\n", "\n").Split('\n');

                            foreach (var line in lines)
                            {
                                // Opcional: Ignorar líneas vacías si lo deseas, VB6 permite vacías.
                                itemsCollection.Add(line); // Agregamos tal cual string
                            }
                        }

                        // ComboBox Hack: Si es un ComboBox y tenía texto seleccionado,
                        // intentar restaurar o seleccionar el primero.
                        if (ctrl is ComboBox combo && combo.Items.Count > 0 && string.IsNullOrEmpty(combo.Text))
                        {
                            combo.SelectedIndex = 0;
                        }
                    }
                    break;

                // --- APPEARANCE ---
                case "Appearance":
                    if (ctrl is Control cApp && cApp.BorderThickness.Top > 0)
                    {
                        if (val != null && val.Contains("0")) // Flat
                            cApp.BorderBrush = Brushes.Black;
                        else // 3D
                            cApp.BorderBrush = Brushes.Gray;
                    }
                    break;

                // --- FONT ---
                case "Font":
                    if (ctrl is Control f)
                    {
                        ApplyFontToControl(f, val);
                    }
                    // Soporte para TextBlock sueltos
                    else if (ctrl is TextBlock tx)
                    {
                        ApplyFontToTextBlock(tx, val);
                    }
                    break;

                // --- PICTURE ---
                case "Picture":
                    try
                    {
                        if (ctrl is Image img && !string.IsNullOrEmpty(val) && System.IO.File.Exists(val))
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.UriSource = new Uri(val);
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            img.Source = bitmap;
                        }
                    }
                    catch { /* Ignorar imagen inválida */ }
                    break;

                // --- BEHAVIOR ---
                case "Visible":
                    ctrl.Opacity = (val == "True") ? 1.0 : 0.4;
                   break;

                case "Enabled":
                    // Simulación visual de Enabled
                    ctrl.IsEnabled = (val == "True");
                    break;

                case "TabIndex":
                    if (ctrl is Control cTab) cTab.TabIndex = (int)ParseDouble(val);
                    break;

                case "Tag":
                    ctrl.Tag = val;
                    break;

                case "Index":
                    if (string.IsNullOrWhiteSpace(val)) VB6Data.SetIndex(ctrl, null);
                    else if (int.TryParse(val, out int i)) VB6Data.SetIndex(ctrl, i);
                    break;
            }
        }

        // ==========================================
        // HELPERS PRIVADOS
        // ==========================================

        private static void ApplyFontToControl(Control f, string val)
        {
            try
            {
                var parts = val.Split(';');
                if (parts.Length == 0) return;

                string fullString = parts[0].Trim();

                if (fullString.Contains("#"))
                {
                    string rawPath = fullString.Replace("file:///", "").Replace("file://", "");
                    string cleanPath = Uri.UnescapeDataString(rawPath);
                    string[] fontInfo = cleanPath.Split('#');
                    string filePath = fontInfo[0];
                    string familyName = fontInfo[1];

                    if (System.IO.File.Exists(filePath))
                    {
                        try
                        {
                            string directory = System.IO.Path.GetDirectoryName(filePath);
                            string fileName = System.IO.Path.GetFileName(filePath);
                            Uri folderUri = new Uri(directory + "\\");
                            f.FontFamily = new FontFamily(folderUri, "./" + fileName + "#" + familyName);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"[ERROR CRÍTICO] Falló el constructor de FontFamily:\n{ex.Message}", "Error WPF");
                        }
                    }
                    else
                    {
                        f.FontFamily = new FontFamily("Microsoft Sans Serif");
                    }
                }
                else
                {
                    try { f.FontFamily = new FontFamily(fullString); } catch { }
                }

                if (parts.Length > 1)
                {
                    string sizeStr = parts[1].ToLower().Replace("pt", "").Trim();
                    if (double.TryParse(sizeStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double s))
                    {
                        f.FontSize = Math.Max(s * 1.333, 1);
                    }
                }

                f.FontWeight = FontWeights.Normal;
                f.FontStyle = FontStyles.Normal;
                string styleString = val.ToLower();
                if (styleString.Contains("bold")) f.FontWeight = FontWeights.Bold;
                if (styleString.Contains("italic")) f.FontStyle = FontStyles.Italic;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"[ERROR GENERAL]: {ex.Message}", "Excepción no controlada");
            }
        }

        private static void ApplyFontToTextBlock(TextBlock f, string val)
        {
            try
            {
                var parts = val.Split(';');
                if (parts.Length > 0) f.FontFamily = new FontFamily(parts[0].Trim());
                if (parts.Length > 1)
                {
                    string sizeStr = parts[1].ToLower().Replace("pt", "").Trim();
                    if (double.TryParse(sizeStr, out double size)) f.FontSize = Math.Max(1, size * 1.33);
                }

                f.FontWeight = val.Contains("Bold") ? FontWeights.Bold : FontWeights.Normal;
                f.FontStyle = val.Contains("Italic") ? FontStyles.Italic : FontStyles.Normal;

                var decos = new TextDecorationCollection();
                if (val.Contains("Underline")) decos.Add(TextDecorations.Underline);
                if (val.Contains("Strikethrough") || val.Contains("Strikeout")) decos.Add(TextDecorations.Strikethrough);
                f.TextDecorations = decos;
            }
            catch { }
        }

        private static string GetAlignFromControl(FrameworkElement ctrl)
        {
            HorizontalAlignment ha = HorizontalAlignment.Left;
            if (ctrl is Control c) ha = c.HorizontalContentAlignment;
            if (ctrl is TextBox tb) ha = (HorizontalAlignment)tb.TextAlignment;

            if (ha == HorizontalAlignment.Right) return AlignOptions[1];
            if (ha == HorizontalAlignment.Center) return AlignOptions[2];
            return AlignOptions[0];
        }

        private static void ApplyAlignment(FrameworkElement ctrl, string val)
        {
            HorizontalAlignment align = HorizontalAlignment.Left;
            if (val.Contains("1")) align = HorizontalAlignment.Right;
            if (val.Contains("2")) align = HorizontalAlignment.Center;

            if (ctrl is Control c) c.HorizontalContentAlignment = align;

            if (ctrl is TextBox tb)
            {
                if (align == HorizontalAlignment.Left) tb.TextAlignment = TextAlignment.Left;
                if (align == HorizontalAlignment.Right) tb.TextAlignment = TextAlignment.Right;
                if (align == HorizontalAlignment.Center) tb.TextAlignment = TextAlignment.Center;
            }
        }

        private static double ParseDouble(string s)
        {
            if (double.TryParse(s, out double d)) return d;
            return 0;
        }

        private static Brush ParseColor(string s)
        {
            try
            {
                return (SolidColorBrush)(new BrushConverter().ConvertFrom(s));
            }
            catch
            {
                return Brushes.White;
            }
        }

        public static void ApplyProperty(FrameworkElement control, string propName, string value)
        {
            var tempItem = new PropertyItem
            {
                Name = propName,
                Value = value
            };
            ApplyProperty(control, tempItem);
        }
    }
}