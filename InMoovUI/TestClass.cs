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
        public SpeechHandler speechHandler;

        public TestClass(string textToSpeech)
        {
            //SpeechPage.Speak(textToSpeech);

            speechHandler = new SpeechHandler();
            speechHandler.UpdateLed += SpeechHandler_UpdateLed;
        }

        private void SpeechHandler_UpdateLed(object sender, SpeechEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
