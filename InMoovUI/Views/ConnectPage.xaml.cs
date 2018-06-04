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
using Windows.Media.Playback;
using Windows.Media.Core;

namespace InMoov.Views
{
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
        public static async Task<bool> Startup()
        {
            bool var = true;
            playSound("Assets/sounds/startup1.mp3");
            App.neopixel.SetAnimation(AnimationID.Succesfully);
            await Task.Delay(9000);
            App.neopixel.StopAnimation();
            return true;
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

        public static void playSound(string path)
        {
            MediaPlayer sound = new MediaPlayer();
            sound.Source = MediaSource.CreateFromUri(new Uri("ms-appx:///" + path)); 
            sound.Play();
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
                //RefreshDeviceList();
                Aktuallisieren();
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Aktuallisieren();
            //RefreshDeviceList();
            this.Frame.Navigate(this.GetType());
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            Verbinden();
            //Arduino device = null;

            //if (devicesList.SelectedItem != null)
            //{
            //    var selectedConnection = devicesList.SelectedItem as Arduino;
            //    device = selectedConnection;

            //    var properties = new Dictionary<string, string>
            //    {
            //        { "Device_Name", device.name },
            //        { "Device_ID", device.id },
            //        { "Device_Kind", device.kind.ToString() }
            //    };

            //    App.Telemetry.TrackEvent("USB_Connection_Attempt", properties);

            //    foreach (string key in App.Arduinos.Keys)
            //    {
            //        if (key == device.id)
            //        {
            //            device = null;
            //            ConnectMessage.Text = "Der Arduino ist bereits verbunden";
            //            break;
            //        }
            //    }
            //    if (device != null)
            //    {
            //        App.Arduinos.Add(device.id, device);
            //    }

            //    this.Frame.Navigate(this.GetType());
            //}
            //else
            //    ConnectMessage.Text = "Wählen sie einen Arduino aus!";
        }
        #endregion UI events

        #region Helper

        private void RefreshDeviceList()
        {
            Task<DeviceInformationCollection> task = null;

            devicesList.Visibility = Visibility.Visible;

            cancelTokenSource = new CancellationTokenSource();

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

        #endregion Helper


        private void Verbinden()
        {
            if (devicesList.SelectedItem != null)
            {
                var selc = devicesList.SelectedItem as Arduino;
                selc.startConnection();
                //this.Frame.Navigate(this.GetType());
            }
        }

        private void Aktuallisieren()
        {
            Task<DeviceInformationCollection> task = null;

            devicesList.Visibility = Visibility.Visible;
            cancelTokenSource = new CancellationTokenSource();
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

                        var result = listTask.Result;
                        if (result == null || result.Count == 0)
                        {
                            ConnectMessage.Text = "[Info] Keine Geräte Gefunden!";
                        }
                        else
                        {
                            foreach (DeviceInformation device in result)
                            {
                                if (result.ToList().Count < App.Arduinos.Values.Count)
                                {
                                    foreach (Arduino arduino in App.Arduinos.Values.ToList())
                                    {
                                        if (arduino.ready == false && arduino.id != device.Id)
                                        {
                                            App.Arduinos.Remove(arduino.id);
                                        }
                                    }
                                }

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
                            task = null;
                        }
                    }));
                });
            }
        }
    }
}
