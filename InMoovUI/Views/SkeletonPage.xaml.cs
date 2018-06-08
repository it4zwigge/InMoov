using System;
using System.Collections.Generic;
using System.Diagnostics;
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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace InMoov.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SkeletonPage : Page
    {
        public SkeletonPage()
        {
            this.InitializeComponent();
            this.Loaded += SkeletonPage_Loaded;
            servoSlider.Maximum = 60;
            servoSlider.Minimum = 0;
            servoSlider.Value = 30;
            App.ALinks.setPinMode(26, Microsoft.Maker.RemoteWiring.PinMode.SERVO);
        }

        private void SkeletonPage_Loaded(object sender, RoutedEventArgs e)
        {
            double? diagonal = DisplayInformation.GetForCurrentView().DiagonalSizeInInches;

            //move commandbar to page bottom on small screens
            //if (diagonal < 7)
            //{
            //    topbar.Visibility = Visibility.Collapsed;
            //    //pageTitleContainer.Visibility = Visibility.Visible;
            //    bottombar.Visibility = Visibility.Visible;
            //}
            //else
            //{
            //    topbar.Visibility = Visibility.Visible;
            //    //pageTitleContainer.Visibility = Visibility.Collapsed;
            //    bottombar.Visibility = Visibility.Collapsed;
            //}
        }

        private void servoSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
             ushort value = (ushort)servoSlider.Value;
            Debug.WriteLine(value);
            App.ALinks.servoWrite(26, value);
        }
    }
}
