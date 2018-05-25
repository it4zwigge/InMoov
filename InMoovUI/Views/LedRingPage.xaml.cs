using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace InMoov.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LedRingPage : Page
    {
        public LedRingPage()
        {
            this.InitializeComponent();
            this.Loaded += LedRingPage_Loaded;
        }

        private void LedRingPage_Loaded(object sender, RoutedEventArgs e)
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

        public static void InitializeNeoPixel()
        {
            App.neopixel = new NeoPixel(App.Leonardo.firmata, 9, 16);
            ReadyNeopixel();
            //Animation.StartAnimation("error");
        }

        public static async void ReadyNeopixel()
        {
            for(byte i = 0; i <=16; i++)
            {
                App.neopixel.SetPixelColor(i ,0, 0, 0);
                Task.Delay(5).Wait();
            }
            await App.turnConnected();
        }
    }
}
