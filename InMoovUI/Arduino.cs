using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Maker.Firmata;
using Microsoft.Maker.RemoteWiring;
using Microsoft.Maker.Serial;
using Windows.Devices.Enumeration;
using System.Collections;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace InMoov
{
    public class Arduino
    {
        private IStream connection { get; set; }
        private RemoteDevice arduino { get; set; }
        public UwpFirmata firmata { get; private set; }
        public string ImageUri { get; set; }

        public string name { get; set; }
        public string id { get; set; }
        public string kind { get; set; }
        public bool ready { get; set; }

        //Firmata Ansteuerungsbytes
        private const byte SABERTOOTH_MOTOR_VOR = 0x42;
        private const byte SABERTOOTH_MOTOR_STOP = 0x43;
        private const byte SABERTOOTH_MOTOR_ZURUECK = 0x44;
        private const byte SABERTOOTH_MOTOR_STOP_ZURUECK = 0x45;
        private const byte SABERTOOTH_MOTOR_DREHUNG_RECHTS = 0x46;

        public Arduino(DeviceInformation device) // Konstruktor
        {
            this.connection = new UsbSerial(device);
            this.arduino = new RemoteDevice(connection);
            this.ImageUri = "ms-appx:///Assets/disconnected.png";

            arduino.DeviceReady += Arduino_DeviceReady;
            arduino.DeviceConnectionFailed += Arduino_DeviceConnectionFailed;
            arduino.DeviceConnectionLost += Arduino_DeviceConnectionLost;

            this.firmata = new UwpFirmata();
            this.firmata.begin(this.connection);
            this.ready = false;
            this.name = device.Name;
            this.id = device.Id;
            this.kind = device.Kind.ToString();

            //Events
            this.firmata.FirmataConnectionReady += Firmata_FirmataConnectionReady;
            this.firmata.FirmataConnectionFailed += Firmata_FirmataConnectionFailed;
            this.firmata.FirmataConnectionLost += Firmata_FirmataConnectionLost;
        }

        public void connect() // startet die Verbindung
        {
            try
            {
                this.connection.begin(57600, SerialConfig.SERIAL_8N1);
            }
            catch { }
        }

        public void setPinMode(byte pin, PinMode pinMode) // setzt den Pin mode auf dem Arduino
        {
            try
            {
                this.arduino.pinMode(pin, pinMode);
            }
            catch { }
        }

        public void analogWrite(byte pin, ushort value) //schreib auf einen analogen pin einen Wert zwichen 0 und 255
        {
            try
            {
                this.arduino.analogWrite(pin, value);
            }
            catch { }
        }

        public ushort analogRead (byte pin) // liest einen wert aus einem Analogen pin (gut für sensorig)
        {
            try
            {
                return this.arduino.analogRead(pin.ToString());
            }
            catch { return 0; }
        }

        public void digitalWrite(byte pin, PinState pinstate) // schreibt einen digitalen wert (HIGH/LOW) auf einen digitalen Pin
        {
            try
            {
                this.arduino.digitalWrite(pin, pinstate);
            }
            catch { }
        }

        public PinState digitalRead(byte pin) // liest einen digitalen Wert aus einem digitalen Pin
        {
            try
            {
                return this.arduino.digitalRead(pin);
            }
            catch { return 0; }
        }

        public void servoWrite (byte pin, int angle) // schreibt einen winkel auf einen Servo (pwm pin)
        {
            try {
                byte ANALOG_MESSAGE = 0xE0;
                byte START_SYSEX = 0xF0;
                byte[] message = null;
                if (pin <= 15)
                {
                    message = new byte[3];
                    message[0] = (byte)(ANALOG_MESSAGE | (pin & 0x0F));
                    message[1] = (byte)(angle & 0x7F);
                    message[2] = (byte)(angle >> 7);
                    //_serialPort.Write(message, 0, 3);
                    connection.write(message);
                }
                else
                {
                    message = new byte[12];
                    int len = 4;
                    message[0] = START_SYSEX;           // 0xF0, 240, start a MIDI SysEx message
                    message[1] = 0x6F;                  // 111, ?
                    message[2] = (byte)pin;             // Pin (z.B. 22)
                    message[3] = (byte)(angle & 0x7F);
                    if (angle > 0x00000080) message[len++] = (byte)((angle >> 7) & 0x7F);
                    if (angle > 0x00004000) message[len++] = (byte)((angle >> 14) & 0x7F);
                    if (angle > 0x00200000) message[len++] = (byte)((angle >> 21) & 0x7F);
                    if (angle > 0x10000000) message[len++] = (byte)((angle >> 28) & 0x7F);
                    message[len++] = 0xF7;
                    //_serialPort.Write(message, 0, len);
                    connection.write(message);
                }
            }
            catch { }
        }

        #region InMoov Firmata

        // Hier befinden sich die Firamta ansteuerungsbefehle
        public void STMotor_Vor(byte speed)
        {
            byte[] message = new byte[3];
            message[0] = (byte)(speed);
            message[1] = (byte)(speed);
            message[2] = (byte)(speed);
            firmata.sendSysex(SABERTOOTH_MOTOR_VOR,message.AsBuffer());
        }

        public void STMotor_Stop()
        {
            byte[] message = new byte[3];
            message[0] = (byte)(0);
            message[1] = (byte)(0);
            message[2] = (byte)(0);
            firmata.sendSysex(SABERTOOTH_MOTOR_STOP, message.AsBuffer());
        }

        public void STMotor_Zurueck(byte speed)
        {
            byte[] message = new byte[3];
            message[0] = (byte)(speed);
            message[1] = (byte)(speed);
            message[2] = (byte)(speed);
            firmata.sendSysex(SABERTOOTH_MOTOR_ZURUECK, message.AsBuffer());
        }


        public void STMotor_Stop_Zurueck()
        {
            byte[] message = new byte[3];
            message[0] = (byte)(0);
            message[1] = (byte)(0);
            message[2] = (byte)(0);
            firmata.sendSysex(SABERTOOTH_MOTOR_STOP_ZURUECK, message.AsBuffer());
        }

        public void STMotor_Drehung()
        {
            byte[] message = new byte[3];
            message[0] = (byte)(0);
            message[1] = (byte)(0);
            message[2] = (byte)(0);
            firmata.sendSysex(SABERTOOTH_MOTOR_DREHUNG_RECHTS, message.AsBuffer());
        }

        #endregion

        #region Events

        #region arduino
            private void Arduino_DeviceConnectionLost(string message)
            {
                Debug.WriteLine("[" + this.name + "] Verbindung verloren : " + message);
                Views.ConnectPage.playSound("Assets/sounds/disconnected.wav");
                App.Arduinos.Remove(id);
        }

            private void Arduino_DeviceConnectionFailed(string message)
            {
                Debug.WriteLine("[" + this.name + "] konnte nicht verbinden : " + message);
                Views.ConnectPage.playSound("Assets/sounds/disconnected.wav");
                App.Arduinos.Remove(id);
            }

            private void Arduino_DeviceReady()
            {
                //this.analog_pins = this.arduino.DeviceHardwareProfile.AnalogPins.ToArray();
                this.ImageUri = "ms-appx:///Assets/connected.png";
                Debug.WriteLine("[" + this.name + "] erolgreich verbunden" + id);
                Views.ConnectPage.playSound("Assets/sounds/connected.wav");
                this.ready = true;
                Views.ConnectPage.sortArduinos();
                Views.ConnectPage.checkDevices();
            }
            #endregion

        #region Firmata
            private void Firmata_FirmataConnectionLost(string message)
            {
                Debug.WriteLine("[" + this.name + "] Firmata antwortet nicht mehr : " + message);
            }

            private void Firmata_FirmataConnectionFailed(string message)
            {
                Debug.WriteLine("[" + this.name + "] Firmata nicht bereit : " + message);
            }

            private void Firmata_FirmataConnectionReady()
            {
                Debug.WriteLine("[" + this.name + "] Firmata bereit!");
            }
            #endregion

        #endregion
    }
}
