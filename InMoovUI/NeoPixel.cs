using Microsoft.Maker.Firmata;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;

namespace InMoov
{
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

        public void SetAnimation(AnimationID animationID)
        {
            Animation.SetAnimation(animationID);
        }

        public void StopAnimation()
        {
            Animation.stop = true;
            Task.Delay(100);
            clear();
            //Animation.ResetAnimation();
        }

        public async void clear()
        {
            for (byte pixel = 0; pixel < 16; pixel++)
            {
                App.neopixel.SetPixelColor(pixel, 0, 0, 0);
                await Task.Delay(50);
            }
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
        Ready = 5,
    }

    public class Animation
    {
        public static bool stop = false;

        public static async void SetAnimation(AnimationID animationID)
        {
            switch (animationID)
            {
                case AnimationID.Error:
                    await Error();
                    break;
                case AnimationID.Facedetection:
                    await Facedetection();
                    break;
                case AnimationID.Ready:
                    await Ready();
                    break;
                case AnimationID.Wait:
                    break;
                case AnimationID.Listening:
                    await Listening();
                    break;
            }
        }

        private static async Task<bool> Error() // noch nicht fertig
        {
            while (!stop)
            {
                try
                {
                    for (byte fade = 0; fade < 125; fade += 5)
                    {
                        for (byte i = 0; i < 16; i++)
                        {
                            if (!stop)
                            {
                                App.neopixel.SetPixelColor(i, fade, 0, 0);
                                await Task.Delay(1);
                            }
                            else
                                break;
                        }
                        if (stop) { break; }
                    }
                    for (byte fade = 125; fade > 0; fade -= 5)
                    {
                        for (byte i = 0; i < 16; i++)
                        {
                            if (!stop)
                            {
                                App.neopixel.SetPixelColor(i, fade, 0, 0);
                                await Task.Delay(1);
                            }
                            else
                                break;
                        }
                        if (stop) { break; }
                    }
                }
                catch
                { stop = true; }
            }
            stop = false;
            App.neopixel.clear();
            return stop;
        }

        private static async Task<bool> Ready() // noch nicht fertig
        {
            while (!stop)
            {
                try
                {
                    for (byte fade = 0; fade < 125; fade += 5)
                    {
                        for (byte i = 0; i < 16; i++)
                        {
                            if (!stop)
                            {
                                App.neopixel.SetPixelColor(i, 0, 0, fade);
                                await Task.Delay(1);
                            }
                            else
                                break;
                        }
                        if (stop) { break; }
                    }
                    for (byte fade = 125; fade > 0; fade -= 10)
                    {
                        for (byte i = 0; i < 16; i++)
                        {
                            if (!stop)
                            {
                                App.neopixel.SetPixelColor(i, 0, 0, fade);
                                await Task.Delay(01);
                            }
                            else
                                break;
                        }
                        if (stop) { break; }
                    }
                }
                catch { stop = true; }
            }
            stop = false;
            App.neopixel.clear();
            return stop;
        }

        private static async Task<bool> Listening() // noch nicht fertig
        {
            while (!stop)
            {
                try
                {
                    byte r = 0;
                    byte g = 0;
                    byte b = 255;

                    for (byte i = 0; i <= 16; i++)
                    {
                        App.neopixel.SetPixelColor(i, r, g, b);
                        await Task.Delay(100);

                        App.neopixel.SetPixelColor(byte.Parse((i - 1).ToString()), 0, 0, 0);
                        await Task.Delay(100);
                    }
                }
                catch
                {
                    stop = true;
                }
            }
            stop = false;
            App.neopixel.clear();
            return stop;
        }

        private static async Task<bool> Facedetection()
        {
            while (!stop)
            {
                try
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
                catch { stop = true; }
            }
            stop = false;
            App.neopixel.clear();
            return stop;
        }
    }
}
