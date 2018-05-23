using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InMoov
{
    class LED
    {
    }
    
    public class Animation
    {
        public static async void StartAnimation(string command)
        {
            switch (command.ToLower())
            {
                case "loading":
                    await Loading(10,10,100);
                    break;
                case "error":
                    await Breathe(100, 0, 0);
                    break;
                case "listening":
                    await Loading(10, 50, 100);
                    break;
                case "succesfully":
                    await Breathe(0, 100, 0);
                    break;
                case "wait":
                    break;
            }
        }

        private static async Task<bool> Loading(byte r, byte g, byte b)
        {
            bool succeeded = false;
            while (!succeeded)
            {
                for (int z = 1; z < 3; z++)
                {
                    for (byte i = 0; i <= 16; i++)
                    {
                        App.neopixel.SetPixelColor(i, r, g, b);
                        Thread.Sleep(50);
                        if (i >= 1)
                        {
                            App.neopixel.SetPixelColor(byte.Parse((i - 1).ToString()), 0, 0, 0);
                            Thread.Sleep(50);
                        }
                    }
                }
            }
            return succeeded;
        }

        private static async Task<bool> Breathe(byte r, byte g, byte b)
        {
            bool succeeded = false;
            while (!succeeded)
            {
                for (byte z = 0; z < 255; z++)
                {
                    for (byte i = 0; i <= 16; i++)
                    {
                        App.neopixel.SetPixelColor(i, r, g, b, z);
                    }
                }
                for (byte z = 255; z >= 0; z--)
                {
                    for (byte i = 0; i <= 16; i++)
                    {
                        App.neopixel.SetPixelColor(i, r, g, b, z);
                        Thread.Sleep(50);
                    }
                }
            }
            return succeeded;
        }
    }
}
