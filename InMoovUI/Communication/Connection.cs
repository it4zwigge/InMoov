using Windows.UI.Xaml.Controls;
using Windows.Foundation;
using System;
using InMoov;
using System.Linq;
using Windows.Devices.Enumeration;

namespace Communication
{
    public class Connection
    {
        public string DisplayName { get; set; }
        public DeviceInformation Source { get; set; }
        public string ImageUri { get; set; }


        public Connection(string displayName, DeviceInformation source)
        {
            this.ImageUri = "ms-appx:///Assets/wrong.png";
            this.DisplayName = displayName;
            this.Source = source;
        }

        public void connectDevice()
        {
            bool vorhanden = false;
            foreach (Arduino arduino in App.Arduinos.Values.ToList())
            {
                if (arduino.name == Source.Name)
                {
                    vorhanden = true;
                }
            }
            if (vorhanden == false)
            {
                App.Arduinos.Add(Source.Id, new Arduino(Source));
            }
        }
    }
}