using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.ViewManagement;
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
    public sealed partial class BasicPage : Page
    {
        public BasicPage()
        {
            this.InitializeComponent();
            var view = ApplicationView.GetForCurrentView();

            // active
            view.TitleBar.BackgroundColor = Color.FromArgb(255, 8, 87, 180);
            view.TitleBar.ForegroundColor = Colors.White;

            // inactive  
            view.TitleBar.InactiveBackgroundColor = Color.FromArgb(255, 8, 87, 180);
            view.TitleBar.InactiveForegroundColor = Colors.Black;

            // button
            view.TitleBar.ButtonBackgroundColor = Color.FromArgb(255, 8, 87, 180);
            view.TitleBar.ButtonForegroundColor = Colors.White;

            view.TitleBar.ButtonHoverBackgroundColor = Colors.Blue;
            view.TitleBar.ButtonHoverForegroundColor = Colors.White;

            view.TitleBar.ButtonPressedBackgroundColor = Colors.Blue;
            view.TitleBar.ButtonPressedForegroundColor = Colors.White;

            view.TitleBar.ButtonInactiveBackgroundColor = Colors.DarkGray;
            view.TitleBar.ButtonInactiveForegroundColor = Colors.Gray;
        }
    }
}
