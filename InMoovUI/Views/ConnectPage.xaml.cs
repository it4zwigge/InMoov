using Communication;
using Microsoft.Maker.RemoteWiring;
using Microsoft.Maker.Serial;
using Microsoft.Maker.Firmata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
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
using InMoov;
using System.Diagnostics;

namespace InMoov.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ConnectPage : Page
    {
        DispatcherTimer timeout;
        DateTime connectionAttemptStartedTime;
        DateTime timePageNavigatedTo;
        CancellationTokenSource cancelTokenSource;

        public static NeoPixel neopixel;

        public ConnectPage()
        {
            this.InitializeComponent();

            this.Loaded += ConnectPage_Loaded;
            App.Telemetry.TrackEvent("ConnectPage_Launched");
        }

        // Hier müssen alle anzusteuernde funktionen eingetragen werden
        public static void Startup()
        {
            neopixel = new NeoPixel(App.Leonardo.firmata, 9, 16); 
            neopixel.SetPixelColor(2, 0, 255, 0);
            HalloWelt();
            
        }

        public static async void HalloWelt()
        {
            await WaitForItToWork();
        }

        public static async Task<bool> WaitForItToWork()
        {
            byte r = 0;
            byte g = 0;
            byte b = 255;
            bool succeeded = false;
            while (!succeeded)
            {
                for (int z = 1; z < 3; z++) //Wiederholung x2
                {
                    for (byte i = 0; i <= 16; i++)
                    {
                        neopixel.SetPixelColor(i, r, g, b);
                        Thread.Sleep(100);

                        neopixel.SetPixelColor(byte.Parse((i - 1).ToString()), 0, 0, 0);
                        Thread.Sleep(100);
                    }
                }
            }
            return succeeded;
        }

        #region UI events
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConnectPage_Loaded(object sender, RoutedEventArgs e)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Telemetry
            App.Telemetry.TrackPageView("Connection_Page");
            timePageNavigatedTo = DateTime.UtcNow;

            if (ConnectionList.ItemsSource == null)
            {
                ConnectMessage.Text = "Select an item to connect to.";
                RefreshDeviceList();
            }
        }

        /// <summary>
        /// Called if the Refresh button is pressed
        /// </summary>
        /// <param name="sender">The object invoking the event</param>
        /// <param name="e">Arguments relating to the event</param>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshDeviceList();
        }

        /// <summary>
        /// Called if the Cancel button is pressed
        /// </summary>
        /// <param name="sender">The object invoking the event</param>
        /// <param name="e">Arguments relating to the event</param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            OnConnectionCancelled();
        }

        /// <summary>
        /// Called if the Connect button is pressed
        /// </summary>
        /// <param name="sender">The object invoking the event</param>
        /// <param name="e">Arguments relating to the event</param>
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            //disable the buttons and set a timer in case the connection times out
            SetUiEnabled(false);

            DeviceInformation device = null;
            if (ConnectionList.SelectedItem != null)
            {
                var selectedConnection = ConnectionList.SelectedItem as Connection;
                device = selectedConnection.Source as DeviceInformation;
            }

            // Connection properties dictionary, used only for telemetry data
            var properties = new Dictionary<string, string>
            {
                { "Device_Name", device.Name },
                { "Device_ID", device.Id },
                { "Device_Kind", device.Kind.ToString() }
            };
            App.Telemetry.TrackEvent("USB_Connection_Attempt", properties);
            foreach (string key in App.Arduinos.Keys)
            {
                if (key == device.Id)
                {
                    App.Arduinos.Remove(device.Id);
                    break;
                }
            }
            App.Arduinos.Add(device.Id, new Arduino(device));
        }
        #endregion UI events

        #region Helper
        /// <summary>
        /// 
        /// </summary>
        private void RefreshDeviceList()
        {
            Dictionary<string, UsbSerial> devices = new Dictionary<string, UsbSerial>();

            //invoke the listAvailableDevicesAsync method of the correct Serial class. Since it is Async, we will wrap it in a Task and add a llambda to execute when finished
            Task<DeviceInformationCollection> task = null;

            ConnectionList.Visibility = Visibility.Visible;
            NetworkConnectionGrid.Visibility = Visibility.Collapsed;
            BaudRateStack.Visibility = Visibility.Visible;

            //create a cancellation token which can be used to cancel a task
            cancelTokenSource = new CancellationTokenSource();
            cancelTokenSource.Token.Register(() => OnConnectionCancelled());

            task = UsbSerial.listAvailableDevicesAsync().AsTask<DeviceInformationCollection>(cancelTokenSource.Token);

            if (task != null)
            {
                //store the returned DeviceInformation items when the task completes
                task.ContinueWith(listTask =>
                {
                    //store the result and populate the device list on the UI thread
                    var action = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
                    {
                        Connections connections = new Connections();

                        var result = listTask.Result;
                        if (result == null || result.Count == 0)
                        {
                            ConnectMessage.Text = "[Info] Keine Geräte Gefunden!";
                        }
                        else
                        {
                            foreach (DeviceInformation device in result)
                            {
                                connections.Add(new Connection(device.Name, device));
                                //devices.Add(device.Name, new UsbSerial(device));
                            }
                            ConnectMessage.Text = "Wählen sie einen Arduino aus.";

                            ConnectionList.ItemsSource = connections;  // Anzeige der Geräte als Liste
                        }
                    }));
                });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="enabled"></param>
        private void SetUiEnabled(bool enabled)
        {
            RefreshButton.IsEnabled = enabled;
            ConnectButton.IsEnabled = enabled;
            CancelButton.IsEnabled = !enabled;
        }

        /// <summary>
        /// This function is invoked if a cancellation is invoked for any reason on the connection task
        /// </summary>
        private void OnConnectionCancelled()
        {
            ConnectMessage.Text = "Connection attempt cancelled.";
            App.Telemetry.TrackRequest("Connection_Cancelled_Event", DateTimeOffset.UtcNow, DateTime.UtcNow - connectionAttemptStartedTime, string.Empty, true);


            if (cancelTokenSource != null)
            {
                cancelTokenSource.Dispose();
            }

            App.Connection = null;
            App.Arduino = null;
            cancelTokenSource = null;

            SetUiEnabled(true);
        }
        #endregion Helper
    }
}
