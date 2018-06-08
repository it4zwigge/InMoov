using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InMoov.Views;
using Windows.UI.Xaml.Controls;

namespace InMoov
{
    public class TestClass
    {
        public SpeechHandler SpeechHandler;
        public static event EventHandler<MyEventArgs> SomethingChanged;

        public TestClass(string textToSpeech)
        {
            SpeechPage.Speak(textToSpeech);

            SpeechHandler = new SpeechHandler();
            SpeechHandler.UpdateLed += SpeechHandler_UpdateLed;
            SomethingChanged += TestClass_SomethingChanged;            
        }

        private void TestClass_SomethingChanged(object sender, MyEventArgs e)
        {
            Debug.WriteLine("Hi");
        }

        public void EventRaising(string speech)
        {
            SomethingChanged?.Invoke(this, new MyEventArgs() { Prop1 = "test" });
        }

        private void SpeechHandler_UpdateLed(object sender, SpeechEventArgs e)
        {
            throw new NotImplementedException();
        }

       
    }
}
