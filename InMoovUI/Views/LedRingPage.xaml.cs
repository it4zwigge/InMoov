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

            if (diagonal < 7)
            {
                topbar.Visibility = Visibility.Collapsed;
                bottombar.Visibility = Visibility.Visible;
            }
            else
            {
                topbar.Visibility = Visibility.Visible;
                bottombar.Visibility = Visibility.Collapsed;
            }
        }

        // wenn der Leonardo verbunden ist wird diese Methode einmalig aufgerufen um den Ledring frühzeitig ansteuern zu können und diesen anzulegen
        public static void InitializeNeoPixel()
        {
            App.neopixel = new NeoPixel(App.Leonardo.firmata, 9, 16);
            App.neopixel.clear();
        }

        public static async Task<bool> turnConnected()
        {
            bool succeeded = false;
            byte readyDevices;
            while (!succeeded)
            {
                readyDevices = 0;
                foreach (Arduino arduino in App.Arduinos.Values)
                {
                    if (arduino.ready == true)
                    {
                        readyDevices++;
                    }
                }
                for (byte pixel = 0; pixel < 6 * readyDevices; pixel++)
                {
                    App.neopixel.SetPixelColor(pixel, 0, 100, 0);
                    await Task.Delay(62);
                }
                for (byte pixel = byte.Parse((readyDevices * 6).ToString()); pixel < 16; pixel++)
                {
                    App.neopixel.SetPixelColor(pixel, 100, 0, 0);
                    await Task.Delay(62);
                }
                if (readyDevices > 1)
                {
                    Views.ConnectPage.Startup();
                    succeeded = true;
                }
            }
            return succeeded;
        }

        private void Neopixel_Reset_Click(object sender, RoutedEventArgs e)
        {
            App.neopixel.StopAnimation();
        }

        private void Facedetection_Click(object sender, RoutedEventArgs e)
        {
            App.neopixel.SetAnimation(AnimationID.Facedetection);
        }

        private void Error_Click(object sender, RoutedEventArgs e)
        {
            App.neopixel.SetAnimation(AnimationID.Error);
        }
    }
}
