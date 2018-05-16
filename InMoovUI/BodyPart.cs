using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InMoov;
using Microsoft.Maker.RemoteWiring;
using Windows.UI.Xaml.Controls;

namespace BodyParts
{
    public class BodyPart
    {
        public Arduino side { get; set; }
        public Slider slider { get; set; }
        public byte Pin { get; set; }
        public ushort minValue { get; set; }
        public ushort maxValue { get; set; }
        public ushort startValue { get; set; }

        public BodyPart(Arduino seite, byte Pin, ushort minValue, ushort maxValue, ushort startValue)
        {
            this.side = seite;
            this.Pin = Pin;
            this.minValue = minValue;
            this.maxValue = maxValue;
            this.startValue = startValue;
            seite.setPinMode(Pin, PinMode.SERVO);
        }

        public void SetSlider (Slider slider)
        {
            this.slider = slider;
            this.slider.Maximum = Convert.ToDouble(maxValue);
            this.slider.Minimum = Convert.ToDouble(minValue);
            this.slider.Value = Convert.ToDouble(startValue);
        }
    }
}
