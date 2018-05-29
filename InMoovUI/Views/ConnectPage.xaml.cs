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
using System.Collections.ObjectModel;

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

        public ConnectPage()
        {
            this.InitializeComponent();

            this.Loaded += ConnectPage_Loaded;
            App.Telemetry.TrackEvent("ConnectPage_Launched");
        }

        // Hier müssen alle anzusteuernde funktionen eingetragen werden
        public static void Startup()
        {
            App.neopixel.SetAnimation(AnimationID.Facedetection);
            Task.Delay(10000);
            App.neopixel.StopAnimation();
        }

        #region UI events
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

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Telemetry
            App.Telemetry.TrackPageView("Connection_Page");
            timePageNavigatedTo = DateTime.UtcNow;

            if (devicesList.ItemsSource == null)
            {
                ConnectMessage.Text = "Select an item to connect to.";
                RefreshDeviceList();
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshDeviceList();
            this.Frame.Navigate(this.GetType());
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            SetUiEnabled(false);

            Arduino device = null;
            //DeviceInformation device = null;

            if (devicesList.SelectedItem != null)
            {
                var selectedConnection = devicesList.SelectedItem as Arduino;
                device = selectedConnection;

                var properties = new Dictionary<string, string>
                {
                    { "Device_Name", device.name },
                    { "Device_ID", device.id },
                    { "Device_Kind", device.kind.ToString() }
                };

                App.Telemetry.TrackEvent("USB_Connection_Attempt", properties);

                foreach (string key in App.Arduinos.Keys)
                {
                    if (key == device.id)
                    {
                        device = null;
                        ConnectMessage.Text = "Der Arduino ist bereits verbunden";
                        break;
                    }
                }
                if (device != null)
                {
                    App.Arduinos.Add(device.id, device);
                }

                this.Frame.Navigate(this.GetType());
            }
            else
                ConnectMessage.Text = "Wählen sie einen Arduino aus!";
        }
        #endregion UI events

        #region Helper

        private void RefreshDeviceList()
        {
            Task<DeviceInformationCollection> task = null;

            devicesList.Visibility = Visibility.Visible;

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
                        //ObservableCollection<Connection> connections = new ObservableCollection<Connection>();
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
                                bool vorhanden = false;
                                foreach (Arduino arduino in App.Arduinos.Values.ToList())
                                {
                                    if (arduino.name == device.Name)
                                    {
                                        vorhanden = true;
                                    }
                                }
                                if (vorhanden == false)
                                {
                                    App.Arduinos.Add(device.Id, new Arduino(device));
                                }
                            }
                            ConnectMessage.Text = "Wählen sie einen Arduino aus.";
                            devicesList.ItemsSource = App.Arduinos.Values;
                        }
                    }));
                });
            }
        }

        private void SetUiEnabled(bool enabled)
        {
            RefreshButton.IsEnabled = enabled;
            ConnectButton.IsEnabled = enabled;
        }

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
