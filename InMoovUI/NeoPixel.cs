using Microsoft.Maker.Firmata;
using System.ComponentModel;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

namespace InMoov
{
    public class NeoPixel
    {
        UwpFirmata firmata;
        byte pin;
        byte leds;

        public event PropertyChangedEventHandler PropertyChanged;

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

        public void SetAnimation(AnimationID animationID)
        {
            Animation.SetAnimation(animationID);
        }

        public void StopAnimation()
        {
            Animation.stop = true;
            Task.Delay(100);
            for (byte i = 0;i< 16; i++)
            {
                SetPixelColor(i, 0, 0, 0, 0);
                Task.Delay(2);
            }
            //Animation.ResetAnimation();
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

    public enum AnimationID
    {
        Wait = 1,
        Facedetection = 2,
        Error = 3,
        Listening = 4,
        Succesfully = 5,       
    }

    public class Animation
    {
        public static AnimationID aniID = 0;
        public static bool stop = false;

        public static async void SetAnimation(AnimationID animationID)
        {
            aniID = animationID;
            switch (animationID)
            {
                case AnimationID.Error:
                    await Error();
                    break;
                case AnimationID.Facedetection:
                    await Facedetection();
                    break;
                case AnimationID.Succesfully:
                    break;
                case AnimationID.Wait:
                    break;
            }
        }

        private static async Task<bool> Error() // noch nicht fertig
        {
            while (!stop)
            {
                for (byte y = 0; y < 255; y += 1)
                {
                    for (byte i = 0; i <= 16; i++)
                    {
                        App.neopixel.SetPixelColor(i, y, 0, 0);
                    }
                    Task.Delay(2).Wait();
                }
                for (byte z = 255; z >= 0; z -= 1)
                {
                    for (byte i = 0; i <= 16; i++)
                    {
                        App.neopixel.SetPixelColor(i, z, 0, 0);
                    }
                    Task.Delay(2).Wait();
                }
            }
            stop = false;
            return stop;
        }

        private static async Task<bool> Facedetection()
        {
            while (!stop)
            {
                for (byte pixel = 0; pixel < 16; pixel++)
                {
                    App.neopixel.SetPixelColor(pixel, 255, 50, 0);
                    await Task.Delay(50);
                }
                for (byte pixel = 0; pixel < 16; pixel++)
                {
                    App.neopixel.SetPixelColor(pixel, 0, 0, 0);
                    await Task.Delay(50);
                }
            }
            stop = false;
            return stop;
        }
    }
}
