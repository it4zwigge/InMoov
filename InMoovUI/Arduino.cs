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

        public ICollection analog_pins { get; set; }

        public string name { get; set; }
        public string id { get; set; }
        public string kind { get; set; }
        public bool ready { get; set; }

        private static byte SABERTOOTH_MOTOR = 0x41;
        private static byte SABERTOOTH_MOTOR_VOR = 0x42;
        private static byte SABERTOOTH_MOTOR_STOP = 0x43;
        private static byte SABERTOOTH_MOTOR_ZURUECK = 0x44;
        private static byte SABERTOOTH_MOTOR_STOP_ZURUECK = 0x45;
        private static byte SABERTOOTH_MOTOR_DREHUNG_RECHTS = 0x46;

        DispatcherTimer timeout;

        public Arduino(DeviceInformation device)
        {
            this.connection = new UsbSerial(device);
            this.arduino = new RemoteDevice(connection);

            arduino.DeviceReady += Arduino_DeviceReady;
            arduino.DeviceConnectionFailed += Arduino_DeviceConnectionFailed;
            arduino.DeviceConnectionLost += Arduino_DeviceConnectionLost;

            this.firmata = new UwpFirmata();
            this.firmata.begin(this.connection);

            this.firmata.FirmataConnectionReady += Firmata_FirmataConnectionReady;
            this.firmata.FirmataConnectionFailed += Firmata_FirmataConnectionFailed;
            this.firmata.FirmataConnectionLost += Firmata_FirmataConnectionLost;

            this.connection.begin(57600, SerialConfig.SERIAL_8N1);

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

        public void STMotor()
        {
            byte[] message = new byte[3];
            message[0] = (byte)(0);
            message[1] = (byte)(0);
            message[2] = (byte)(0);
            firmata.sendSysex(SABERTOOTH_MOTOR, message.AsBuffer());
        }
        public void STMotor_Vor()
        {
            byte[] message = new byte[3];
            message[0] = (byte)(0);
            message[1] = (byte)(0);
            message[2] = (byte)(0);
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

        public void STMotor_Zurueck()
        {
            byte[] message = new byte[3];
            message[0] = (byte)(0);
            message[1] = (byte)(0);
            message[2] = (byte)(0);
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

        
        #region Events

        #region arduino
        private void Arduino_DeviceConnectionLost(string message)
            {
                Debug.WriteLine("[" + this.name + "] Verbindung verloren : " + message);
            }

            private void Arduino_DeviceConnectionFailed(string message)
            {
                Debug.WriteLine("[" + this.name + "] konnte nicht verbinden : " + message);
            }

            private void Arduino_DeviceReady()
            {
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

    public class NeoPixel
    {
        UwpFirmata firmata;
        byte pin;
        byte leds;

        static byte NEOPIXEL = 0x72;
        static byte NEOPIXEL_REGISTER = 0x74;

        public NeoPixel(UwpFirmata firmata, byte pin, byte led_count)
        {
            this.firmata = firmata;
            this.pin = pin;
            this.leds = led_count;

            NeoPixelRegister(this.pin, this.leds);
        }

        private void NeoPixelRegister(byte pin, byte count)
        {
            byte[] message = new byte[2];
            message[0] = (byte)(pin);
            message[1] = (byte)(count);
            firmata.sendSysex(NEOPIXEL_REGISTER, message.AsBuffer());
        }

        public void SetPixelColor(byte index, byte r, byte g, byte b)
        {
            byte[] message = new byte[4];
            message[0] = (byte)(index);
            message[1] = (byte)(r);
            message[2] = (byte)(g);
            message[3] = (byte)(b);
            firmata.sendSysex(NEOPIXEL, message.AsBuffer());
        }
        public void SetPixelColor(byte index, byte r, byte g, byte b, byte gamma)
        {
            byte[] message = new byte[5];
            message[0] = (byte)(index);
            message[1] = (byte)(r);
            message[2] = (byte)(g);
            message[3] = (byte)(b);
            message[3] = (byte)(gamma);
            firmata.sendSysex(NEOPIXEL, message.AsBuffer());
        }


    }
}
