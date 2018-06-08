using Communication;
using Microsoft.Maker.Serial;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Graphics.Display;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace InMoov.Views
{
    public sealed partial class ConnectPage : Page
    {
        DateTime timePageNavigatedTo;
        CancellationTokenSource cancelTokenSource;

        public ConnectPage()
        {
            this.InitializeComponent();
            devicesList.ItemClick += DevicesList_ItemClick;
            this.Loaded += ConnectPage_Loaded;
            App.Telemetry.TrackEvent("ConnectPage_Launched");
        }

        private void DevicesList_ItemClick(object sender, ItemClickEventArgs e)
        {
            ConnectButton.Visibility = Visibility.Visible;
        }

        // Hier müssen alle anzusteuernde funktionen eingetragen werden
        public static void Startup()
        {
            App.neopixel.clear();
            playSound("Assets/sounds/startup.mp3");
            App.neopixel.SetAnimation(AnimationID.Ready);
            Thread.Sleep(9000);
            App.neopixel.StopAnimation();
        }

        #region UI events
        private void ConnectPage_Loaded(object sender, RoutedEventArgs e)
        {
            double? diagonal = DisplayInformation.GetForCurrentView().DiagonalSizeInInches;
        }

        //spiele verschiedene sounds zb. connected oder connection lost
        public static void playSound(string path)
        {
            MediaPlayer sound = new MediaPlayer();
            sound.Source = MediaSource.CreateFromUri(new Uri("ms-appx:///" + path)); 
            sound.Play();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            App.Telemetry.TrackPageView("Connection_Page");
            timePageNavigatedTo = DateTime.UtcNow;

            if (devicesList.ItemsSource == null)
            {
                ConnectMessage.Text = "Wählen sie einen Arduino aus.";
                Aktuallisieren();
            }
        }

        //Aktuallisiere Seite bei click auf refresh button
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Aktuallisieren();
            this.Frame.Navigate(this.GetType());
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            Verbinden();
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


        // startet die Verbindung mit dem ausgewählten arduino bei Knopdruck
        private async Task <bool> Verbinden()
        {
            var selc = devicesList.SelectedItem as Arduino;
            if (devicesList.SelectedItem != null)
            {
                selc.connect();
                while (selc.ready == false)
                {
                    await Task.Delay(100);
                }
                this.Frame.Navigate(this.GetType());
            }
            return selc.ready;
        }


        public void Aktuallisieren()
        {
            //initialisiert den kommenden Vorgang
            Task<DeviceInformationCollection> task = null;
            devicesList.Visibility = Visibility.Visible;
            cancelTokenSource = new CancellationTokenSource();
            task = UsbSerial.listAvailableDevicesAsync().AsTask<DeviceInformationCollection>(cancelTokenSource.Token);

            if (task != null)
            {
                //speichert alle gefunden items vom Typ Deviceinformation wenn der Task fertig ist
                task.ContinueWith(listTask =>
                {
                    //speichert die ergebnisse "result" und füllt die Geräte Liste im UI-Thread
                    var action = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
                    {
                        var result = listTask.Result;
                        //Prüfe ob überhaupt geräte gefunden wurden
                        if (result == null || result.Count == 0)
                        {
                            ConnectMessage.Text = "[Info] Keine Geräte Gefunden!";
                        }
                        else
                        {
                            foreach (DeviceInformation device in result) // Hier der algorythmus zum filtern von vorhandenen geräten
                            {
                                bool vorhanden = false;
                                foreach (Arduino arduino in App.Arduinos.Values.ToList())
                                {
                                    if (arduino.name == device.Name) // sollte ein name übereinstimmen dann ist dieses gerät schon vorhanden
                                    {
                                        vorhanden = true;
                                    }
                                }
                                if (vorhanden == false)
                                {
                                    App.Arduinos.Add(device.Id, new Arduino(device)); //solte das Gerät nicht vorhanden sein, soll dieses zu liste hinzugefügt werden
                                }
                            }
                            devicesList.ItemsSource = App.Arduinos.Values; //zeige alle items aus der App.Arduinos liste (Dictonary)
                            task = null; // lösche speicher
                        }
                    }));
                });
            }
        }

        public static async void sortArduinos()
        {
            foreach (Arduino arduino in App.Arduinos.Values)
             {
                switch (arduino.id.Substring(26, 20))
                {
                    case "85539313931351C09082":
                    case "955303430353518062E0":
                        App.Leonardo = arduino;
                        Debug.WriteLine("Leonardo wurde das gerät " + arduino.name + " zugeteilt!");
                        break;
                    case "75533353038351313212":
                        App.ARechts = arduino;
                        Debug.WriteLine("ARechts wurde das gerät " + arduino.name + " zugeteilt!");
                        break;
                    case "85531303231351812120":
                        App.ALinks = arduino;
                        Debug.WriteLine("ALinks wurde das gerät " + arduino.name + " zugeteilt!");
                        break;
                }
            }
        }
        public static async void checkDevices()
        {
            try
            {
                if (App.Leonardo.ready == true && App.neopixel == null)
                {
                    Views.LedRingPage.InitializeNeoPixel();
                    await Views.LedRingPage.turnConnected();
                }
            }
            catch
            {

            }
            //if (App.ARechts != null && App.ALinks != null && App.Leonardo != null)
            //{
            //    Startup();
            //}
        }
    }
}
