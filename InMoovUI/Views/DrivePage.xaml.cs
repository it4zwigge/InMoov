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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace InMoov.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DrivePage : Page
    {
        byte NEOPIXEL = 0x72;
        byte NEOPIXEL_REGISTER  = 0x74;
        byte SABERTOOTH_MOTOR = 0x42;
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            STMotor(1, 2, 2);
            //NeoPixelRegister(9, 16);
            //Task.Delay(1000).Wait();
            //SetPixelColor(1, 255, 0, 0);
        }

        public void NeoPixelRegister(byte pin, byte count)
        {
            byte[] message = new byte[2];
            message[0] = (byte)(pin);
            message[1] = (byte)(count);
            App.Firmata.sendSysex(NEOPIXEL_REGISTER, message.AsBuffer());
        }

        public void SetPixelColor(byte index, byte r, byte g, byte b)
        {
            byte[] message = new byte[4];
            message[0] = (byte)(index);
            message[1] = (byte)(r);
            message[2] = (byte)(g);
            message[3] = (byte)(b);
            App.Firmata.sendSysex(NEOPIXEL, message.AsBuffer());
        }

        public void STMotor (byte motor, byte speed, byte rampe)
        {
            byte[] message = new byte[3];
            message[0] = (byte)(motor);
            message[1] = (byte)(speed);
            message[2] = (byte)(rampe);
            App.Firmata.sendSysex(SABERTOOTH_MOTOR, message.AsBuffer());
        }
    }
}
