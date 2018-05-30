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

namespace InMoov
{
    public class Arduino
    {
        private IStream connection { get; set; }
        private RemoteDevice arduino { get; set; }
        public UwpFirmata firmata { get; private set; }
        public string ImageUri { get; set; }

        public ICollection analog_pins { get; set; }

        public string name { get; set; }
        public string id { get; set; }
        public string kind { get; set; }
        public bool ready { get; set; }

        private static byte SABERTOOTH_MOTOR_VOR = 0x42;
        private static byte SABERTOOTH_MOTOR_STOP = 0x43;
        private static byte SABERTOOTH_MOTOR_ZURUECK = 0x44;
        private static byte SABERTOOTH_MOTOR_STOP_ZURUECK = 0x45;
        private static byte SABERTOOTH_MOTOR_DREHUNG_RECHTS = 0x46;

        public Arduino(DeviceInformation device)
        {
            this.connection = new UsbSerial(device);
            this.arduino = new RemoteDevice(connection);
            this.ImageUri = "ms-appx:///Assets/disconnected.png";

            arduino.DeviceReady += Arduino_DeviceReady;
            arduino.DeviceConnectionFailed += Arduino_DeviceConnectionFailed;
            arduino.DeviceConnectionLost += Arduino_DeviceConnectionLost;

            this.firmata = new UwpFirmata();
            this.firmata.begin(this.connection);

            this.firmata.FirmataConnectionReady += Firmata_FirmataConnectionReady;
            this.firmata.FirmataConnectionFailed += Firmata_FirmataConnectionFailed;
            this.firmata.FirmataConnectionLost += Firmata_FirmataConnectionLost;

            this.connection.begin(115200, SerialConfig.SERIAL_8N1);

            this.ready = false;
            this.name = device.Name;
            this.id = device.Id;
            this.kind = device.Kind.ToString();
        }

        public void setPinMode(byte pin, PinMode pinMode)
        {
            this.arduino.pinMode(pin, pinMode);
        }

        public void analogWrite(byte pin, ushort value)
        {
            this.arduino.analogWrite(pin, value);
        }

        public ushort analogRead (byte pin)
        {
            return this.arduino.analogRead(pin.ToString());
        }

        public void digitalWrite(byte pin, PinState pinstate)
        {
            this.arduino.digitalWrite(pin, pinstate);
        }

        public PinState digitalRead(byte pin)
        {
            return this.arduino.digitalRead(pin);
        }

        public void servoWrite (byte pin, ushort value)
        {
            this.arduino.analogWrite(pin, value);
        }

        #region InMoov Firmata

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
                App.Arduinos.Remove(this.id);
                App.noDevice++;
                App.readyDevices--;
            }

            private void Arduino_DeviceConnectionFailed(string message)
            {
                Debug.WriteLine("[" + this.name + "] konnte nicht verbinden : " + message);
                App.noDevice++;
            }

            private void Arduino_DeviceReady()
            {
                this.ImageUri = "ms-appx:///Assets/connected.png";
                this.analog_pins = this.arduino.DeviceHardwareProfile.AnalogPins.ToArray();
                this.ready = true;
                Debug.WriteLine("[" + this.name + "] erolgreich verbunden" + id);
                App.readyDevices++;
                App.ArduinosReady();
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
