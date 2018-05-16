using VWFIANCognitveServices;
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
using Microsoft.ProjectOxford.Face.Contract;

namespace InMoov.Views
{
    public sealed partial class FacesPage : Page
    {
        public FacesPage()
        {
            this.InitializeComponent();
            this.Loaded += FacesPage_Loaded;
            ToogleFace.Toggled += ToogleFace_Toggled;

        }

        private void ToogleFace_Toggled(object sender, RoutedEventArgs e)
        {
            if(ToogleFace.IsOn)
            {
                _faceTimer.Tick += _faceTimer_Tick;
                _faceTimer.Interval = new TimeSpan(0, 0, 3);
                _faceTimer.Start();
            }
            else
            {
                _faceTimer.Stop();
                FaceDetect.status = false;
            }
        }

        private void FacesPage_Loaded(object sender, RoutedEventArgs e)
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
        TrackFacesInWebcam FaceDetect = new TrackFacesInWebcam();
        DispatcherTimer _faceTimer = new DispatcherTimer();
        public string nameface_voice = null;
        private void _faceTimer_Tick(object sender, object e)
        {
            FaceDetect.StarteWebcam();
            FaceName_TextBlock.Text = "Hallo " + FaceDetect.GetFaceName();
            nameface_voice = FaceName_TextBlock.Text;
            if (FaceName_TextBlock.Text == "") { }
            else { Task.Delay(500); }
        }

        private void Button_ON_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Button_OFF_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}
