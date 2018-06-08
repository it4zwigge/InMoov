using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Benutzersteuerelement" wird unter https://go.microsoft.com/fwlink/?LinkId=234236 dokumentiert.

namespace App1
{
    public partial class MotorControl : UserControl
    {
        const int MINSPEED = -127;
        const int MAXSPEED = 127;

        public MotorControl()
        {
            this.InitializeComponent();
            DataContext = this;
        }

        public int MinSpeed
        {
            get
            {
                return MINSPEED;
            }
        }
        public int MaxSpeed
        {
            get
            {
                return MAXSPEED;
            }
        }

        public event EventHandler<HoldingRoutedEventArgs> ButtonHold;

        private void ButtonMotor_Holding(object sender, HoldingRoutedEventArgs e)
        {
            //bubble the event up to the parent
            if (this.ButtonHold != null)
                this.ButtonHold(this, e);
        }
    }
}
