using Microsoft.Maker.Serial;
using System.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Communication;
using System.Diagnostics;

namespace InMoov.Views
{
    public sealed partial class LandingPage : Page
    {
        public LandingPage()
        {
            this.InitializeComponent();
            this.Loading += LandingPage_Loading;

            SolidColorBrush Rot = new SolidColorBrush(Windows.UI.Color.FromArgb(250, 153, 0, 0));
            SolidColorBrush Gruen = new SolidColorBrush(Windows.UI.Color.FromArgb(250, 0, 153, 0));
            SpeachRec.Fill = Gruen;
            FaceRec.Fill = Gruen;
            ArduRec.Fill = Gruen;
            DriveRec.Fill = Gruen;
            SkelRec.Fill = Rot;
        }

        private void LandingPage_Loading(FrameworkElement sender, object args)
        {
        }

        private void TextBlock_SelectionChanged(object sender, RoutedEventArgs e)
        {
        }
    }
}