using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InMoov;

namespace InMoov
{
    static byte SABERTOOTH_MOTOR = 0x42;
    static byte SABERTOOTH_REGISTER = 0x43;

    public class Fahrwerk
    {
        private STMotor LVorne { get; set; }
        private STMotor LHinten { get; set; }
        private STMotor RVorne { get; set; }
        private STMotor RHinten { get; set; }

        public Fahrwerk(Arduino arduino)
        {
            LVorne  = new STMotor(arduino, 4, 1);
            LHinten = new STMotor(arduino, 4, 2);
            RVorne  = new STMotor(2, 1);
            RHinten = new STMotor(2, 2);
        }

        public void gerade(byte speed, byte rampe)
        {
            LVorne.STMotor(LVorne.motor, speed, rampe);
            LHinten.STMotor(LHinten.motor, speed, rampe);
            RVorne.STMotor(RVorne.motor, speed, rampe);
            RHinten.STMotor(RHinten.motor, speed, rampe);
        }
    }

    public internal class STMotor
    {
        public Arduino arduino { get; set; }
        private byte pin { get; set; }
        public byte motor { get; set; }

        private byte maxspeed = 127;
        private static byte SABERTOOTH_MOTOR = 0x42;

        public STMotor(Arduino arduino, byte pin, byte motor)
        {
            this.arduino = arduino;
            this.speed = 0;
            this.pin = pin;
            this.motor = motor;
            this.rampe = rampe;
        }

        public void STMotorRegister(byte pin)
        {
            byte[] message = new byte[1];
            message[0] = (byte)(pin);
            arduino.firmata.sendSysex(SABERTOOTH_REGISTER, message.AsBuffer());
        }

        public void STMotor(byte motor, byte speed, byte rampe)
        {
            if (speed >= this.maxspeed)
            {
                speed = maxspeed;
            }
            byte[] message = new byte[3];
            message[0] = (byte)(motor);
            message[0] = (byte)(speed);
            message[0] = (byte)(rampe);
            arduino.firmata.sendSysex(SABERTOOTH_MOTOR, message.AsBuffer());
        }
    }
}
