using Windows.UI.Xaml.Controls;
using Windows.Foundation;
using System;

namespace Communication
{
    public class Connection
    {
        public string DisplayName { get; set; }
        public object Source { get; set; }
        public string ImageUri { get; set; }
       

        public Connection(string displayName, object source)
        {
            this.ImageUri = "ms-appx:///Assets/wrong.png";
            this.DisplayName = displayName;
            this.Source = source;
        }
    }
}