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
using Windows.UI.Xaml.Automation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace InMoov.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SpeechSchedulerPage : Page
    {
        int hour;
        int minute;
        DispatcherTimer timer = new DispatcherTimer();
        public SpeechSchedulerPage()
        {
            this.InitializeComponent();
            this.Loaded += SpeechSchedulerPage_Loaded;
            //timePicker.
            timePicker.TimeChanged += TimeChanged;
            //TimePicker firstTimePicker = new TimePicker();
            //firstTimePicker.Header = "Arrival time";
            //Debug.WriteLine(timePicker.GetValue());
           ;

        }

        private void TimeChanged(object sender, TimePickerValueChangedEventArgs e)
        {
            hour = timePicker.Time.Hours;
            minute = timePicker.Time.Minutes;
            timer.Interval = TimeSpan.FromSeconds(30);
            timer.Start();
            timer.Tick += Ticker;
        }

        private void Ticker(object sender, object e)
        {
            if ((DateTime.Now.Hour == timePicker.Time.Hours) && (DateTime.Now.Minute == timePicker.Time.Minutes))
            {
                SpeechPage.Speaking(textBox1.Text, true);
                timer.Stop();
            }
        }

        private void SpeechSchedulerPage_Loaded(object sender, RoutedEventArgs e)
        {
            double? diagonal = DisplayInformation.GetForCurrentView().DiagonalSizeInInches;

            //move commandbar to page bottom on small screens

        }
    }
}
