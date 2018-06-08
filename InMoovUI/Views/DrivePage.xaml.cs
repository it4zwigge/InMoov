using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;
using System.Threading.Tasks;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace InMoov.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DrivePage : Page
    {
        public DrivePage()
        {
            this.InitializeComponent();

            SliderSTMotor.Maximum = 127;
            SliderSTMotor.Minimum = -127;
            SliderSTMotor.Value = 0;
            this.Loaded += DrivePage_Loaded;
            Vor_Button.Click += Vor_Button_Click;
        }

        private void Vor_Button_Click(object sender, RoutedEventArgs e)
        {
            App.Leonardo.STMotor_Vor();
        }

        private void Stop_Button_Click(object sender, RoutedEventArgs e)
        {
            App.Leonardo.STMotor_Stop();
        }

        private void Rueckwaerts_Button_Click(object sender, RoutedEventArgs e)
        {
            App.Leonardo.STMotor_Zurueck();
        }

        private void Stop_R_Button_Click(object sender, RoutedEventArgs e)
        {
            App.Leonardo.STMotor_Stop_Zurueck();
        }

        private void Drehung_Button_Click(object sender, RoutedEventArgs e)
        {
            App.Leonardo.STMotor_Drehung();
        }
        private void STMotor_Click(object sender, RoutedEventArgs e)
        {
            //App.Leonardo.STMotor();
        }
        private void DrivePage_Loaded(object sender, RoutedEventArgs e)
        {
            double? diagonal = DisplayInformation.GetForCurrentView().DiagonalSizeInInches;

        }

        private void SliderSTMotor_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            App.Leonardo.STMotor((int)SliderSTMotor.Value);
        }
    }
}
