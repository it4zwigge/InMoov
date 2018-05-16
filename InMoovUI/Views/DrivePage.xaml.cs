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
using Microsoft.Maker.Firmata;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

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
            this.Loaded += DrivePage_Loaded;
        }

        private void DrivePage_Loaded(object sender, RoutedEventArgs e)
        {
            double? diagonal = DisplayInformation.GetForCurrentView().DiagonalSizeInInches;

            //move commandbar to page bottom on small screens
            if (diagonal < 7)
            {
                topbar.Visibility = Visibility.Collapsed;
                //pageTitleContainer.Visibility = Visibility.Visible;
                bottombar.Visibility = Visibility.Visible;
            }
            else
            {
                topbar.Visibility = Visibility.Visible;
                //pageTitleContainer.Visibility = Visibility.Collapsed;
                bottombar.Visibility = Visibility.Collapsed;
            }
        }

        public static void allLedsOn()
        {
           foreach (Arduino ard in App.Arduinos.Values)
            {
                ard.digitalWrite(13, Microsoft.Maker.RemoteWiring.PinState.HIGH);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Random rnd = new Random();
            int index = rnd.Next(0, 4);
            App.Arduinos.ElementAt(index).Value.digitalWrite(13, Microsoft.Maker.RemoteWiring.PinState.HIGH);                  
            Thread.Sleep(rnd.Next(50, 180));
            App.Arduinos.ElementAt(index).Value.digitalWrite(13, Microsoft.Maker.RemoteWiring.PinState.LOW);
            //NeoPixelRegister(9, 16);
            //Task.Delay(1000).Wait();
            //SetPixelColor(1, 255, 0, 0);
        }
    }
}
