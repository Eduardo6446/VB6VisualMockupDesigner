using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;

namespace VB6VisualMockupDesigner.Controls
{
    internal class RetroStyles
    {

        private static Style _threed32ButtonStyle;
        public static Style GetCustomThreed32Style()
        {
            if (_threed32ButtonStyle != null) return _threed32ButtonStyle;

            // Aquí pegamos TU XAML. 
            // Nota: Hemos añadido los xmlns necesarios al principio del string para que el parser funcione.
            string xaml = @"
            <ResourceDictionary 
                xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>

                <Color x:Key='BtnFace'>#FFE0E0E0</Color>
                <Color x:Key='BtnHighlight'>#FFFFFFFF</Color>
                <Color x:Key='BtnShadow'>#FFA0A0A0</Color>
                <Color x:Key='BtnDarkShadow'>#FF696969</Color>

                <SolidColorBrush x:Key='BtnFaceBrush' Color='{StaticResource BtnFace}' />
                <SolidColorBrush x:Key='BtnHighlightBrush' Color='{StaticResource BtnHighlight}' />
                <SolidColorBrush x:Key='BtnShadowBrush' Color='{StaticResource BtnShadow}' />
                <SolidColorBrush x:Key='BtnDarkShadowBrush' Color='{StaticResource BtnDarkShadow}' />

                <Style x:Key='Threed32Button' TargetType='Button'>
                    <Setter Property='Background' Value='{StaticResource BtnFaceBrush}' />
                    <Setter Property='Foreground' Value='Black' />
                    <Setter Property='BorderThickness' Value='1' />
                    <Setter Property='Padding' Value='8,3' />
                    <Setter Property='FontFamily' Value='Microsoft Sans Serif' />
                    <Setter Property='FontSize' Value='12' />
                    <Setter Property='Template'>
                        <Setter.Value>
                            <ControlTemplate TargetType='Button'>
                                <Grid>
                                    <Border Background='{TemplateBinding Background}' BorderThickness='1'>
                                        <Border.BorderBrush>
                                            <LinearGradientBrush StartPoint='0,0' EndPoint='1,1'>
                                                <GradientStop Color='{StaticResource BtnHighlight}' Offset='0'/>
                                                <GradientStop Color='{StaticResource BtnDarkShadow}' Offset='1'/>
                                            </LinearGradientBrush>
                                        </Border.BorderBrush>

                                        <Border Margin='1' BorderThickness='1'>
                                            <Border.BorderBrush>
                                                <LinearGradientBrush StartPoint='0,0' EndPoint='1,1'>
                                                    <GradientStop Color='{StaticResource BtnFace}' Offset='0'/>
                                                    <GradientStop Color='{StaticResource BtnShadow}' Offset='1'/>
                                                </LinearGradientBrush>
                                            </Border.BorderBrush>

                                            <ContentPresenter 
                                                HorizontalAlignment='Center' 
                                                VerticalAlignment='Center' 
                                                RecognizesAccessKey='True'/>
                                        </Border>
                                    </Border>
                                </Grid>
                                <ControlTemplate.Triggers>
                                    <Trigger Property='IsPressed' Value='True'>
                                        <Setter Property='RenderTransform'>
                                            <Setter.Value>
                                                <TranslateTransform X='1' Y='1'/>
                                            </Setter.Value>
                                        </Setter>
                                    </Trigger>
                                    <Trigger Property='IsEnabled' Value='False'>
                                        <Setter Property='Foreground' Value='#FF808080'/>
                                    </Trigger>
                                </ControlTemplate.Triggers>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>
            </ResourceDictionary>";

            try
            {
                // Parseamos el string a un ResourceDictionary real
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml)))
                {
                    var resources = (ResourceDictionary)XamlReader.Load(stream);
                    _threed32ButtonStyle = (Style)resources["Threed32Button"];
                }
            }
            catch (System.Exception ex)
            {
                // Fallback por si acaso falla el parseo
                System.Diagnostics.Debug.WriteLine("Error parsing Style: " + ex.Message);
                _threed32ButtonStyle = new Style(typeof(Button));
            }

            return _threed32ButtonStyle;
        }

        private static Style _threed32CheckStyle;

        public static Style GetCustomThreed32CheckStyle()
        {
            if (_threed32CheckStyle != null) return _threed32CheckStyle;

            string xaml = @"
            <ResourceDictionary 
                xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>

                <Style x:Key='Threed32CheckBox' TargetType='CheckBox'>
                    <Setter Property='FontFamily' Value='Microsoft Sans Serif'/>
                    <Setter Property='FontSize' Value='11'/>
                    <Setter Property='Foreground' Value='Black'/>
                    <Setter Property='HorizontalContentAlignment' Value='Left'/> 
                    <Setter Property='Template'>
                        <Setter.Value>
                            <ControlTemplate TargetType='CheckBox'>
                                <DockPanel SnapsToDevicePixels='True' Background='Transparent' LastChildFill='True'>
                                    
                                    <Grid x:Name='CheckBoxContainer' Width='13' Height='13' VerticalAlignment='Center' DockPanel.Dock='Left'>
                                        <Border BorderThickness='1'>
                                            <Border.BorderBrush>
                                                <LinearGradientBrush StartPoint='0,0' EndPoint='1,1'>
                                                    <GradientStop Color='#808080' Offset='0.5'/> 
                                                    <GradientStop Color='White' Offset='0.51'/>
                                                </LinearGradientBrush>
                                            </Border.BorderBrush>
                                        </Border>
                                        <Border Margin='1' Background='White' BorderThickness='1,1,0,0' BorderBrush='Black'>
                                            <Path x:Name='CheckMark' 
                                                  Data='M 2,4 L 4,6 L 8,1' 
                                                  Stroke='Black'
                                                  StrokeThickness='1.5'
                                                  Visibility='Collapsed'
                                                  HorizontalAlignment='Center'
                                                  VerticalAlignment='Center'/>
                                        </Border>
                                    </Grid>

                                    <ContentPresenter x:Name='ContentObj'
                                                      Margin='5,0,0,0' 
                                                      VerticalAlignment='Center' 
                                                      HorizontalAlignment='{TemplateBinding HorizontalContentAlignment}'
                                                      RecognizesAccessKey='True'/>
                                </DockPanel>

                                <ControlTemplate.Triggers>
                                    <Trigger Property='IsChecked' Value='True'>
                                        <Setter TargetName='CheckMark' Property='Visibility' Value='Visible'/>
                                    </Trigger>

                                    <Trigger Property='IsEnabled' Value='False'>
                                        <Setter Property='Foreground' Value='#808080'/>
                                        <Setter TargetName='CheckMark' Property='Stroke' Value='#808080'/>
                                    </Trigger>

                                    <DataTrigger Binding='{Binding Tag, RelativeSource={RelativeSource TemplatedParent}}' Value='Right'>
                                        <Setter TargetName='CheckBoxContainer' Property='DockPanel.Dock' Value='Right'/>
                                        <Setter TargetName='ContentObj' Property='Margin' Value='0,0,5,0'/>
                                        <Setter Property='HorizontalContentAlignment' Value='Right'/>
                                    </DataTrigger>

                                </ControlTemplate.Triggers>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>
            </ResourceDictionary>";

            try
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml)))
                {
                    var resources = (ResourceDictionary)XamlReader.Load(stream);
                    _threed32CheckStyle = (Style)resources["Threed32CheckBox"];
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error parsing CheckBox Style: " + ex.Message);
                _threed32CheckStyle = new Style(typeof(CheckBox));
            }

            return _threed32CheckStyle;
        }

        private static Style _threed32OptionStyle;

        public static Style GetCustomThreed32OptionStyle()
        {
            if (_threed32OptionStyle != null) return _threed32OptionStyle;

            string xaml = @"
            <ResourceDictionary 
                xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>

                <Style x:Key='Threed32OptionButton' TargetType='RadioButton'>
                    <Setter Property='FontFamily' Value='Microsoft Sans Serif'/>
                    <Setter Property='FontSize' Value='11'/>
                    <Setter Property='Foreground' Value='Black'/>
                    <Setter Property='HorizontalContentAlignment' Value='Left'/>
                    <Setter Property='Template'>
                        <Setter.Value>
                            <ControlTemplate TargetType='RadioButton'>
                                <DockPanel SnapsToDevicePixels='True' Background='Transparent' LastChildFill='True'>
                                    
                                    <Grid x:Name='OptionContainer' Width='13' Height='13' VerticalAlignment='Center' DockPanel.Dock='Left'>
                                        
                                        <Ellipse StrokeThickness='1'>
                                            <Ellipse.Stroke>
                                                <LinearGradientBrush StartPoint='0,0' EndPoint='1,1'>
                                                    <GradientStop Color='#808080' Offset='0.5'/> 
                                                    <GradientStop Color='White' Offset='0.55'/>
                                                </LinearGradientBrush>
                                            </Ellipse.Stroke>
                                        </Ellipse>

                                        <Ellipse Margin='1' StrokeThickness='1' Fill='White'>
                                            <Ellipse.Stroke>
                                                <LinearGradientBrush StartPoint='0,0' EndPoint='1,1'>
                                                    <GradientStop Color='Black' Offset='0.5'/> 
                                                    <GradientStop Color='Transparent' Offset='0.55'/>
                                                </LinearGradientBrush>
                                            </Ellipse.Stroke>
                                        </Ellipse>

                                        <Ellipse x:Name='OptionMark' 
                                                 Fill='Black' 
                                                 Width='5' Height='5' 
                                                 Visibility='Collapsed'/>
                                    </Grid>

                                    <ContentPresenter x:Name='ContentObj'
                                                      Margin='5,0,0,0' 
                                                      VerticalAlignment='Center'
                                                      HorizontalAlignment='{TemplateBinding HorizontalContentAlignment}'
                                                      RecognizesAccessKey='True'/>
                                </DockPanel>

                                <ControlTemplate.Triggers>
                                    <Trigger Property='IsChecked' Value='True'>
                                        <Setter TargetName='OptionMark' Property='Visibility' Value='Visible'/>
                                    </Trigger>

                                    <Trigger Property='IsEnabled' Value='False'>
                                        <Setter Property='Foreground' Value='#808080'/>
                                        <Setter TargetName='OptionMark' Property='Fill' Value='#808080'/>
                                    </Trigger>

                                    <DataTrigger Binding='{Binding Tag, RelativeSource={RelativeSource TemplatedParent}}' Value='Right'>
                                        <Setter TargetName='OptionContainer' Property='DockPanel.Dock' Value='Right'/>
                                        <Setter TargetName='ContentObj' Property='Margin' Value='0,0,5,0'/>
                                        <Setter Property='HorizontalContentAlignment' Value='Right'/>
                                    </DataTrigger>
                                </ControlTemplate.Triggers>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>
            </ResourceDictionary>";

            try
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml)))
                {
                    var resources = (ResourceDictionary)XamlReader.Load(stream);
                    _threed32OptionStyle = (Style)resources["Threed32OptionButton"];
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error parsing OptionButton Style: " + ex.Message);
                _threed32OptionStyle = new Style(typeof(RadioButton));
            }

            return _threed32OptionStyle;
        }

        private static Style _vb6TextBoxStyle;

        public static Style GetVB6TextBoxStyle()
        {
            if (_vb6TextBoxStyle != null) return _vb6TextBoxStyle;

            string xaml = @"
            <ResourceDictionary 
                xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>

                <Style x:Key='VB6TextBox' TargetType='TextBox'>
                    <Setter Property='FontFamily' Value='Microsoft Sans Serif'/>
                    <Setter Property='FontSize' Value='11'/>
                    <Setter Property='Background' Value='White'/>
                    <Setter Property='Foreground' Value='Black'/>
                    <Setter Property='BorderThickness' Value='0'/>
                    <Setter Property='Padding' Value='2,1'/>
                    <Setter Property='Template'>
                        <Setter.Value>
                            <ControlTemplate TargetType='TextBox'>
                                <Grid SnapsToDevicePixels='True'>
                                    
                                    <Border BorderThickness='1'>
                                        <Border.BorderBrush>
                                            <LinearGradientBrush StartPoint='0,0' EndPoint='1,1'>
                                                <GradientStop Color='#808080' Offset='0.5'/> 
                                                <GradientStop Color='#808080' Offset='0.51'/>
                                            </LinearGradientBrush>
                                        </Border.BorderBrush>
                                        
                                        <Border BorderThickness='1'>
                                            <Border.BorderBrush>
                                                <LinearGradientBrush StartPoint='0,0' EndPoint='1,1'>
                                                    <GradientStop Color='#808080' Offset='0.5'/> 
                                                    <GradientStop Color='#808080' Offset='0.51'/>
                                                </LinearGradientBrush>
                                            </Border.BorderBrush>

                                            <Border Background='{TemplateBinding Background}'>
                                                <ScrollViewer x:Name='PART_ContentHost' 
                                                              Margin='{TemplateBinding Padding}'
                                                              VerticalScrollBarVisibility='Auto'/>
                                            </Border>
                                        </Border>
                                    </Border>
                                </Grid>
                                
                                <ControlTemplate.Triggers>
                                    <Trigger Property='IsEnabled' Value='False'>
                                        <Setter Property='Background' Value='#D4D0C8'/>
                                        <Setter Property='Foreground' Value='#808080'/>
                                    </Trigger>
                                </ControlTemplate.Triggers>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>
            </ResourceDictionary>";

            try
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml)))
                {
                    var resources = (ResourceDictionary)XamlReader.Load(stream);
                    _vb6TextBoxStyle = (Style)resources["VB6TextBox"];
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error parsing TextBox Style: " + ex.Message);
                _vb6TextBoxStyle = new Style(typeof(TextBox));
            }

            return _vb6TextBoxStyle;
        }


        // =========================================================
        // SCROLLBARS (HScrollBar / VScrollBar)
        // =========================================================

        private static Style _vb6ScrollStyleH;
        private static Style _vb6ScrollStyleV;

        public static Style GetVB6ScrollBarStyle(Orientation orientation)
        {
            if (orientation == Orientation.Horizontal && _vb6ScrollStyleH != null) return _vb6ScrollStyleH;
            if (orientation == Orientation.Vertical && _vb6ScrollStyleV != null) return _vb6ScrollStyleV;

            string targetType = orientation == Orientation.Horizontal ? "VB6HScrollBar" : "VB6VScrollBar";

            // Definimos las flechas según la orientación
            string arrow1 = orientation == Orientation.Horizontal ? "M 4,0 L 4,7 L 0,3.5 Z" : "M 0,4 L 7,4 L 3.5,0 Z"; // Izq o Arriba
            string arrow2 = orientation == Orientation.Horizontal ? "M 0,0 L 0,7 L 4,3.5 Z" : "M 0,0 L 7,0 L 3.5,4 Z"; // Der o Abajo

            // Márgenes para centrar las flechas
            string arrowMargin1 = orientation == Orientation.Horizontal ? "5,4,0,0" : "4,5,0,0";
            string arrowMargin2 = orientation == Orientation.Horizontal ? "6,4,0,0" : "4,6,0,0";

            string xaml = $@"
            <ResourceDictionary 
                xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>

                <SolidColorBrush x:Key='Face' Color='#D4D0C8'/>
                <SolidColorBrush x:Key='Shadow' Color='#808080'/>
                <SolidColorBrush x:Key='Light' Color='White'/>
                <SolidColorBrush x:Key='Dark' Color='Black'/>

                <Style x:Key='ScrollButton' TargetType='RepeatButton'>
                    <Setter Property='Background' Value='{{StaticResource Face}}'/>
                    <Setter Property='Focusable' Value='False'/>
                    <Setter Property='Template'>
                        <Setter.Value>
                            <ControlTemplate TargetType='RepeatButton'>
                                <Grid>
                                    <Border BorderThickness='1'>
                                        <Border.BorderBrush>
                                            <LinearGradientBrush StartPoint='0,0' EndPoint='1,1'>
                                                <GradientStop Color='White' Offset='0.5'/>
                                                <GradientStop Color='Black' Offset='0.51'/>
                                            </LinearGradientBrush>
                                        </Border.BorderBrush>
                                        <Border BorderThickness='1'>
                                            <Border.BorderBrush>
                                                <LinearGradientBrush StartPoint='0,0' EndPoint='1,1'>
                                                    <GradientStop Color='#D4D0C8' Offset='0.5'/>
                                                    <GradientStop Color='#808080' Offset='0.51'/>
                                                </LinearGradientBrush>
                                            </Border.BorderBrush>
                                            <Border Background='{{TemplateBinding Background}}'>
                                                <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>
                                            </Border>
                                        </Border>
                                    </Border>
                                </Grid>
                                <ControlTemplate.Triggers>
                                    <Trigger Property='IsPressed' Value='True'>
                                        <Setter Property='RenderTransform'>
                                            <Setter.Value>
                                                <TranslateTransform X='1' Y='1'/>
                                            </Setter.Value>
                                        </Setter>
                                    </Trigger>
                                </ControlTemplate.Triggers>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>

                <Style x:Key='ScrollThumb' TargetType='Thumb'>
                    <Setter Property='Background' Value='{{StaticResource Face}}'/>
                    <Setter Property='Template'>
                        <Setter.Value>
                            <ControlTemplate TargetType='Thumb'>
                                <Border BorderThickness='1'>
                                    <Border.BorderBrush>
                                        <LinearGradientBrush StartPoint='0,0' EndPoint='1,1'>
                                            <GradientStop Color='White' Offset='0.5'/>
                                            <GradientStop Color='Black' Offset='0.51'/>
                                        </LinearGradientBrush>
                                    </Border.BorderBrush>
                                    <Border BorderThickness='1'>
                                        <Border.BorderBrush>
                                            <LinearGradientBrush StartPoint='0,0' EndPoint='1,1'>
                                                <GradientStop Color='#D4D0C8' Offset='0.5'/>
                                                <GradientStop Color='#808080' Offset='0.51'/>
                                            </LinearGradientBrush>
                                        </Border.BorderBrush>
                                        <Border Background='{{TemplateBinding Background}}'/>
                                    </Border>
                                </Border>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>

                <Style x:Key='{targetType}' TargetType='ScrollBar'>
                    <Setter Property='Background' Value='{{StaticResource Face}}'/> <Setter Property='Template'>
                        <Setter.Value>
                            <ControlTemplate TargetType='ScrollBar'>
                                <Grid SnapsToDevicePixels='True'>
                                    <Grid.ColumnDefinitions>
                                        {(orientation == Orientation.Horizontal ?
                                            "<ColumnDefinition Width='17'/><ColumnDefinition Width='*'/><ColumnDefinition Width='17'/>" :
                                            "<ColumnDefinition Width='*'/>")}
                                    </Grid.ColumnDefinitions>
                                    <Grid.RowDefinitions>
                                        {(orientation == Orientation.Vertical ?
                                            "<RowDefinition Height='17'/><RowDefinition Height='*'/><RowDefinition Height='17'/>" :
                                            "<RowDefinition Height='*'/>")}
                                    </Grid.RowDefinitions>

                                    <RepeatButton Style='{{StaticResource ScrollButton}}'
                                                  Grid.Column='0' Grid.Row='0'
                                                  Command='ScrollBar.LineUpCommand'>
                                        <Path Data='{arrow1}' Fill='Black' Margin='{arrowMargin1}'/>
                                    </RepeatButton>

                                    <Track x:Name='PART_Track' 
                                           Grid.Column='{(orientation == Orientation.Horizontal ? "1" : "0")}'
                                           Grid.Row='{(orientation == Orientation.Vertical ? "1" : "0")}'
                                           IsDirectionReversed='true'>
                                        <Track.Thumb>
                                            <Thumb Style='{{StaticResource ScrollThumb}}'/>
                                        </Track.Thumb>
                                        <Track.DecreaseRepeatButton>
                                            <RepeatButton Command='ScrollBar.PageUpCommand' Opacity='0' Background='Transparent'/>
                                        </Track.DecreaseRepeatButton>
                                        <Track.IncreaseRepeatButton>
                                            <RepeatButton Command='ScrollBar.PageDownCommand' Opacity='0' Background='Transparent'/>
                                        </Track.IncreaseRepeatButton>
                                    </Track>

                                    <RepeatButton Style='{{StaticResource ScrollButton}}'
                                                  Grid.Column='{(orientation == Orientation.Horizontal ? "2" : "0")}'
                                                  Grid.Row='{(orientation == Orientation.Vertical ? "2" : "0")}'
                                                  Command='ScrollBar.LineDownCommand'>
                                        <Path Data='{arrow2}' Fill='Black' Margin='{arrowMargin2}'/>
                                    </RepeatButton>
                                </Grid>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>
            </ResourceDictionary>";

            try
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml)))
                {
                    var resources = (ResourceDictionary)XamlReader.Load(stream);
                    var style = (Style)resources[targetType];

                    if (orientation == Orientation.Horizontal) _vb6ScrollStyleH = style;
                    else _vb6ScrollStyleV = style;

                    return style;
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing ScrollStyle {orientation}: " + ex.Message);
                return new Style(typeof(ScrollBar));
            }
        }

        private static Style _vb6FixedLabelStyle;

        public static Style GetVB6FixedLabelStyle()
        {
            if (_vb6FixedLabelStyle != null) return _vb6FixedLabelStyle;

            string xaml = @"
            <ResourceDictionary 
                xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>

                <Style x:Key='VB6FixedLabel' TargetType='Label'>
                    <Setter Property='FontFamily' Value='Microsoft Sans Serif'/>
                    <Setter Property='FontSize' Value='11'/>
                    <Setter Property='Foreground' Value='Black'/>
                    <Setter Property='Background' Value='#D4D0C8'/> 
                    <Setter Property='Padding' Value='2,1'/>
                    <Setter Property='VerticalContentAlignment' Value='Center'/>
                    <Setter Property='Template'>
                        <Setter.Value>
                            <ControlTemplate TargetType='Label'>
                                <Grid SnapsToDevicePixels='True'>
                                    
                                    <Border BorderThickness='1'>
                                        <Border.BorderBrush>
                                            <LinearGradientBrush StartPoint='0,0' EndPoint='1,1'>
                                                <GradientStop Color='#808080' Offset='0.5'/> 
                                                <GradientStop Color='White' Offset='0.51'/>
                                            </LinearGradientBrush>
                                        </Border.BorderBrush>
                                        
                                        <Border BorderThickness='1'>
                                            <Border.BorderBrush>
                                                <LinearGradientBrush StartPoint='0,0' EndPoint='1,1'>
                                                    <GradientStop Color='#404040' Offset='0.5'/> 
                                                    <GradientStop Color='#F0F0F0' Offset='0.51'/>
                                                </LinearGradientBrush>
                                            </Border.BorderBrush>

                                            <Border Background='{TemplateBinding Background}'>
                                                <ContentPresenter HorizontalAlignment='{TemplateBinding HorizontalContentAlignment}' 
                                                                  VerticalAlignment='{TemplateBinding VerticalContentAlignment}'
                                                                  Margin='{TemplateBinding Padding}'/>
                                            </Border>
                                        </Border>
                                    </Border>
                                </Grid>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>
            </ResourceDictionary>";

            try
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml)))
                {
                    var resources = (ResourceDictionary)XamlReader.Load(stream);
                    _vb6FixedLabelStyle = (Style)resources["VB6FixedLabel"];
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error parsing FixedLabel Style: " + ex.Message);
                _vb6FixedLabelStyle = new Style(typeof(Label));
            }

            return _vb6FixedLabelStyle;
        }

        private static Style _vb6MenuStyle;

        public static Style GetVB6MenuStyle()
        {
            if (_vb6MenuStyle != null) return _vb6MenuStyle;

            string xaml = @"
            <ResourceDictionary 
                xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>

                <SolidColorBrush x:Key='MenuBack' Color='#E0E0E0'/>
                <SolidColorBrush x:Key='BorderGray' Color='#A0A0A0'/>
                <SolidColorBrush x:Key='BorderWhite' Color='#FFFFFF'/>
                <SolidColorBrush x:Key='Highlight' Color='#000080'/> <SolidColorBrush x:Key='HighlightText' Color='White'/>

                <Style x:Key='{x:Static MenuItem.SeparatorStyleKey}' TargetType='Separator'>
                    <Setter Property='Template'>
                        <Setter.Value>
                            <ControlTemplate TargetType='Separator'>
                                <Border Height='2' Margin='4,2' BorderThickness='1,1,0,0' BorderBrush='#A0A0A0' Background='White'/>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>

                <Style TargetType='MenuItem'>
                    <Setter Property='FontFamily' Value='Microsoft Sans Serif'/>
                    <Setter Property='FontSize' Value='11'/>
                    <Setter Property='Foreground' Value='Black'/>
                    <Setter Property='OverridesDefaultStyle' Value='True'/>
                    <Setter Property='Template'>
                        <Setter.Value>
                            <ControlTemplate TargetType='MenuItem'>
                                <Grid>
                                    <Border x:Name='Bg' Background='Transparent'/>
                                    
                                    <Grid>
                                        <ContentPresenter x:Name='ContentHost'
                                                          ContentSource='Header'
                                                          RecognizesAccessKey='True'
                                                          Margin='6,2'/>
                                        
                                        <Popup x:Name='Popup' Placement='Bottom' IsOpen='{TemplateBinding IsSubmenuOpen}' AllowsTransparency='True' Focusable='False' PopupAnimation='None'>
                                            <Border Background='#E0E0E0' BorderThickness='1' BorderBrush='#A0A0A0' SnapsToDevicePixels='True'>
                                                <Border Margin='1' BorderThickness='1' BorderBrush='#FFFFFF'>
                                                    <StackPanel IsItemsHost='True' KeyboardNavigation.DirectionalNavigation='Cycle'/>
                                                </Border>
                                            </Border>
                                        </Popup>
                                    </Grid>
                                </Grid>

                                <ControlTemplate.Triggers>
                                    <Trigger Property='Role' Value='TopLevelHeader'>
                                        <Setter TargetName='ContentHost' Property='Margin' Value='6,2'/>
                                        <Setter TargetName='Popup' Property='Placement' Value='Bottom'/>
                                    </Trigger>

                                    <Trigger Property='Role' Value='SubmenuItem'>
                                        <Setter TargetName='ContentHost' Property='Margin' Value='24,2,10,2'/> </Trigger>
                                    
                                    <Trigger Property='Role' Value='SubmenuHeader'>
                                        <Setter TargetName='ContentHost' Property='Margin' Value='24,2,10,2'/>
                                        <Setter TargetName='Popup' Property='Placement' Value='Right'/>
                                        </Trigger>

                                    <Trigger Property='IsHighlighted' Value='True'>
                                        <Setter TargetName='Bg' Property='Background' Value='{StaticResource Highlight}'/>
                                        <Setter Property='Foreground' Value='{StaticResource HighlightText}'/>
                                    </Trigger>

                                    <Trigger Property='IsEnabled' Value='False'>
                                        <Setter Property='Foreground' Value='#808080'/>
                                    </Trigger>
                                </ControlTemplate.Triggers>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>

                <Style x:Key='VB6Menu' TargetType='Menu'>
                    <Setter Property='Background' Value='#E0E0E0'/>
                    <Setter Property='FontFamily' Value='Microsoft Sans Serif'/>
                    <Setter Property='FontSize' Value='11'/>
                    <Setter Property='Template'>
                        <Setter.Value>
                            <ControlTemplate TargetType='Menu'>
                                <Border Background='{TemplateBinding Background}' BorderThickness='0,0,0,1' BorderBrush='#A0A0A0'>
                                    <StackPanel IsItemsHost='True' ClipToBounds='True' Orientation='Horizontal' Margin='2,2,0,0'/>
                                </Border>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>
            </ResourceDictionary>";

            try
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml)))
                {
                    var resources = (ResourceDictionary)XamlReader.Load(stream);
                    _vb6MenuStyle = (Style)resources["VB6Menu"];
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error parsing Menu Style: " + ex.Message);
                _vb6MenuStyle = new Style(typeof(Menu));
            }

            return _vb6MenuStyle;
        }

        // Dentro de la clase RetroStyles :

        private static Style _ssTabStyle;

        public static Style GetSSTabStyle()
        {
            if (_ssTabStyle != null) return _ssTabStyle;

            string xaml = @"
    <ResourceDictionary 
        xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>

        <SolidColorBrush x:Key='Face' Color='#D4D0C8'/>
        <SolidColorBrush x:Key='Shadow' Color='#808080'/>
        <SolidColorBrush x:Key='Light' Color='White'/>
        <SolidColorBrush x:Key='Dark' Color='Black'/>

        <Style x:Key='SSTabItem' TargetType='TabItem'>
            <Setter Property='Template'>
                <Setter.Value>
                    <ControlTemplate TargetType='TabItem'>
                        <Grid>
                            <Border x:Name='Bd' 
                                    Background='{StaticResource Face}'
                                    BorderBrush='{StaticResource Shadow}' 
                                    BorderThickness='1,1,1,0' 
                                    Margin='0,0,2,0'
                                    Padding='6,2'>
                                <ContentPresenter x:Name='Content' 
                                                ContentSource='Header' 
                                                HorizontalAlignment='Center' 
                                                VerticalAlignment='Center'
                                                RecognizesAccessKey='True'/>
                            </Border>
                        </Grid>
                        <ControlTemplate.Triggers>
                            <Trigger Property='IsSelected' Value='True'>
                                <Setter TargetName='Bd' Property='Margin' Value='-2,-2,0,-1'/>
                                <Setter TargetName='Bd' Property='Padding' Value='8,4'/>
                                <Setter Property='Panel.ZIndex' Value='100'/>
                                <Setter TargetName='Bd' Property='BorderBrush' Value='{StaticResource Shadow}'/>
                                <Setter TargetName='Bd' Property='BorderThickness' Value='1,1,2,0'/>
                            </Trigger>
                            <Trigger Property='IsSelected' Value='False'>
                                <Setter TargetName='Bd' Property='Background' Value='#C0C0C0'/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
            <Setter Property='Header' Value='Tab'/>
            <Setter Property='FontFamily' Value='Microsoft Sans Serif'/>
            <Setter Property='FontSize' Value='11'/>
        </Style>

        <Style x:Key='SSTabControl' TargetType='TabControl'>
            <Setter Property='Background' Value='{StaticResource Face}'/>
            <Setter Property='BorderThickness' Value='1'/>
            <Setter Property='BorderBrush' Value='{StaticResource Shadow}'/>
            <Setter Property='ItemContainerStyle' Value='{StaticResource SSTabItem}'/>
            <Setter Property='Template'>
                <Setter.Value>
                    <ControlTemplate TargetType='TabControl'>
                        <Grid ClipToBounds='True' SnapsToDevicePixels='true' KeyboardNavigation.TabNavigation='Local'>
                            <Grid.RowDefinitions>
                                <RowDefinition Height='Auto'/>
                                <RowDefinition Height='*'/>
                            </Grid.RowDefinitions>
                            
                            <TabPanel x:Name='HeaderPanel' 
                                      Grid.Row='0' 
                                      Panel.ZIndex='1' 
                                      Margin='2,2,2,0' 
                                      IsItemsHost='true'
                                      KeyboardNavigation.TabIndex='1' 
                                      Background='Transparent'/>

                            <Border x:Name='Border' 
                                    Grid.Row='1' 
                                    Background='{TemplateBinding Background}' 
                                    BorderBrush='{TemplateBinding BorderBrush}' 
                                    BorderThickness='1'>
                                    
                                    <Border BorderThickness='1' BorderBrush='White'>
                                        <ContentPresenter x:Name='PART_SelectedContentHost' 
                                                        ContentSource='SelectedContent' 
                                                        Margin='2'/>
                                    </Border>
                            </Border>
                        </Grid>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
    </ResourceDictionary>";

            try
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml)))
                {
                    var resources = (ResourceDictionary)XamlReader.Load(stream);
                    _ssTabStyle = (Style)resources["SSTabControl"];
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error parsing SSTab Style: " + ex.Message);
                _ssTabStyle = new Style(typeof(TabControl));
            }

            return _ssTabStyle;
        }


    }
}
