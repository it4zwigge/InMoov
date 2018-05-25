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
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LandingPage : Page
    {
        public static byte readyDevices = 0;


        public LandingPage()
        {
            this.InitializeComponent();
            this.Loading += LandingPage_Loading;
        }

        private void LandingPage_Loading(FrameworkElement sender, object args)
        {
            AutoConnect();
            if (App.readyDevices == 3)
            {
                foreach (Arduino arduino in App.Arduinos.Values)
                {
                    arduino.digitalWrite(13, Microsoft.Maker.RemoteWiring.PinState.HIGH);
                }
            }
        }


        private void AutoConnect()
        {
            Dictionary<string, UsbSerial> devices = new Dictionary<string, UsbSerial>();

            //invoke the listAvailableDevicesAsync method of the correct Serial class. Since it is Async, we will wrap it in a Task and add a llambda to execute when finished
            Task<DeviceInformationCollection> task = null;

            //create a cancellation token which can be used to cancel a task
            CancellationTokenSource cancelTokenSource = new CancellationTokenSource();

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
                            Debug.WriteLine("[Info] Keine Geräte Gefunden!");
                            // TODO: Hier muss zur Conenctpage weitergeleitet werden!
                        }
                        else
                        {
                            foreach (DeviceInformation device in result)
                            {
                                connections.Add(new Connection(device.Name, device));
                                App.Arduinos.Add(device.Id, new Arduino(device));
                                devices.Add(device.Name, new UsbSerial(device));
                            }
                        }
                    }));
                });
            }
        }
    }
}
