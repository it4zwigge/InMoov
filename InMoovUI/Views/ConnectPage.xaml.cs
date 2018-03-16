﻿using Communication;
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

        const string Computer = "";       // Hier den Namen des Computers eintragen
        const string ComL = "COM4";       // COM Port für Leonardo [ Fahrwerk ]
        const string ComA = "COM3";       // COM Port für Mega A   [ Rechts ]
        const string ComB = "COM9";       // COM Port für Mega B   [ Links ]

        public ConnectPage()
        {
            this.InitializeComponent();
            this.Loaded += ConnectPage_Loaded;
            RefreshDeviceList();
        }

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

            //determine the selected baud rate
            //uint baudRate = Convert.ToUInt32((BaudRateComboBox.SelectedItem as string));

            //connection properties dictionary, used only for telemetry data
            var properties = new Dictionary<string, string>();

            //use the selected device to create our communication object
            
            //send telemetry about this connection attempt
            properties.Add("Device_Name", device.Name);
            properties.Add("Device_ID", device.Id);
            properties.Add("Device_Kind", device.Kind.ToString());
            App.Telemetry.TrackEvent("USB_Connection_Attempt", properties);

            /*******************************************************************************************/

            App.Connection = new UsbSerial(device);//usb
            App.firmata = new UwpFirmata();
            App.Arduino = new RemoteDevice(App.firmata);
            App.firmata.begin(App.Connection);
            App.Connection.begin(57600, SerialConfig.SERIAL_8N1);

            App.Arduino.DeviceReady += OnDeviceReady;
            App.Arduino.DeviceConnectionFailed += OnDeviceConnectionFailed;

            App.firmata.FirmataConnectionReady += Firmata_FirmataConnectionReady;

            connectionAttemptStartedTime = DateTime.UtcNow;

            //start a timer for connection timeout
            timeout = new DispatcherTimer();
            timeout.Interval = new TimeSpan(0, 0, 30);
            timeout.Tick += Connection_TimeOut;
            timeout.Start();
        }

        /*************************
         *        Events
         ************************/

        private void OnDeviceReady()
        {
            //var action = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
            //{
            //    timeout.Stop();

            //    //telemetry
            //    App.Telemetry.TrackRequest("Connection_Success_Event", DateTimeOffset.UtcNow, DateTime.UtcNow - connectionAttemptStartedTime, string.Empty, true);
            //    App.Telemetry.TrackMetric("Connection_Page_Time_Spent_In_Seconds", (DateTime.UtcNow - timePageNavigatedTo).TotalSeconds);

            //    //this.Frame.Navigate(typeof(MainPage));
            //}));
            //ConnectMessage.Text = "Arduino: " + App.Arduino.ToString() + "wurde erfolgreich verbunden!";
            Debug.WriteLine("Arduino: wurde erfolgreich verbunden!");
        }

        private void Firmata_FirmataConnectionReady()
        {
            Debug.WriteLine("Firmata: wurde erfolgreich verbunden!");
        }

        /*************************
         *        Methoden
         ************************/

        private void RefreshDeviceList()
        {
            Dictionary<string, UsbSerial> devices = new Dictionary<string, UsbSerial>();
            Connections connections = new Connections();
            //invoke the listAvailableDevicesAsync method of the correct Serial class. Since it is Async, we will wrap it in a Task and add a llambda to execute when finished
            Task<DeviceInformationCollection> task = null;
            //if (ConnectionMethodComboBox.SelectedItem == null)
            //{
            //    ConnectMessage.Text = "Select a connection method to continue.";
            //    return;
            //}

            
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
                        var result = listTask.Result;
                        if (result == null || result.Count == 0)
                        {
                            Debug.WriteLine("[Info] Keine Geräte Gefunden!");
                        }
                        else
                        {
                            Debug.WriteLine("[Info] Geräte Gefunden: " + result.Count.ToString());
                            foreach (DeviceInformation device in result)
                            {
                                if (device.Name != Computer)
                                {                           
                                    connections.Add(new Connection(device.Name, device));
                                    devices.Add(device.Name,new UsbSerial(device));
                                }
                            }
                            ConnectMessage.Text = "Wählen sie einen Arduino aus und.";
                            Task.Delay(100).Wait();
                            ConnectionList.ItemsSource = connections; // anzeige der Geräte als Liste
                            Task.Delay(100).Wait();
                        }
                    }));
                });
            }
        }


        /// <summary>
        /// This function is invoked if a cancellation is invoked for any reason on the connection task
        /// </summary>
        private void OnConnectionCancelled()
        {
            ConnectMessage.Text = "Connection attempt cancelled.";
            App.Telemetry.TrackRequest("Connection_Cancelled_Event", DateTimeOffset.UtcNow, DateTime.UtcNow - connectionAttemptStartedTime, string.Empty, true);

            if (App.Connection != null)
            {
                App.Connection.ConnectionEstablished -= OnDeviceReady;
                App.Connection.ConnectionFailed -= OnDeviceConnectionFailed;
            }

            if (cancelTokenSource != null)
            {
                cancelTokenSource.Dispose();
            }

            App.Connection = null;
            App.Arduino = null;
            cancelTokenSource = null;

            SetUiEnabled(true);
        }

        private void SetUiEnabled(bool enabled)
        {
            RefreshButton.IsEnabled = enabled;
            ConnectButton.IsEnabled = enabled;
            CancelButton.IsEnabled = !enabled;
        }

        private void OnDeviceConnectionFailed(string message)
        {
            var action = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
            {
                timeout.Stop();

                //telemetry
                App.Telemetry.TrackRequest("Connection_Failed_Event", DateTimeOffset.UtcNow, DateTime.UtcNow - connectionAttemptStartedTime, message, true);

                ConnectMessage.Text = "Connection attempt failed: " + message;
                SetUiEnabled(true);
            }));
        }

        private void Connection_TimeOut(object sender, object e)
        {
            var action = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
            {
                timeout.Stop();

                //telemetry
                App.Telemetry.TrackRequest("Connection_Timeout_Event", DateTimeOffset.UtcNow, DateTime.UtcNow - connectionAttemptStartedTime, string.Empty, true);

                ConnectMessage.Text = "Connection attempt timed out.";
                SetUiEnabled(true);
            }));
        }
    }
}
