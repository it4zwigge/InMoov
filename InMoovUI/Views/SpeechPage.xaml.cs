using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Globalization;
using Windows.Graphics.Display;
using Windows.Media.SpeechRecognition;
using Windows.Media.SpeechSynthesis;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace InMoov.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SpeechPage : Page
    {
        /// <summary>
        /// This HResult represents the scenario where a user is prompted to allow in-app speech, but 
        /// declines. This should only happen on a Phone device, where speech is enabled for the entire device,
        /// not per-app.
        /// </summary>
        private static uint HResultPrivacyStatementDeclined = 0x80045509;

        /// <summary>
        /// the HResult 0x8004503a typically represents the case where a recognizer for a particular language cannot
        /// be found. This may occur if the language is installed, but the speech pack for that language is not.
        /// See Settings -> Time & Language -> Region & Language -> *Language* -> Options -> Speech Language Options.
        /// </summary>
        private static uint HResultRecognizerNotFound = 0x8004503a;

        #region Variablen
        private bool isListening;
        private SpeechRecognizer speechRecognizer;
        private static StringBuilder dictatedTextBuilder;
        private int[] facedetect = new int[2];          //Handles the facedetection start/stop Commands
        private int[] numbers = new int[50];            //Numbers which are getting captured by speechrecognition
        private bool ledCaptured;                       //Handles if Word LED is captured
        private string colorCaptured;                   //Handles which color is picked by user
        //Handles the amount of colors the User can pick
        private static List<string> colorlist = new List<string>() { "grün", "rot", "blau", "gelb", "schwarz", "aus" };
        //Handles the amount of Numbers the User can pick
        private static List<string> numberlist = new List<string>() { "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16" };
        //Handles Keywords the user can say to pick Facedetection
        private static List<string> facelist = new List<string>() { "Gesichtserkennung", "gesichtserkennung", "Gesicht", "gesicht" };
        //Handles basic start/stop Keys
        private static List<string> openList = new List<string>() { "starte", "erkenne", "öffne", "benutze" };
        private static List<string> closeList = new List<string>() { "schließe", "stoppe", "stoppen", "verhindere" };

        private static bool isSpeeking = false;

        //Needed to give NeoPixel the RGBs of picked Color
        //private byte[] color = new byte[3];
        private static string uebergabeText;
        #endregion

        //Initialize a new FacesPage
        FacesPage fp = new FacesPage();

        private static MediaElement mediaElement;

        public SpeechPage()
        {
            this.InitializeComponent();
            this.Loaded += SpeechPage_Loaded;
            isListening = false;
            dictatedTextBuilder = new StringBuilder();

            mediaElement = new MediaElement();
            mediaElement.MediaEnded += MediaElement_MediaEnded;
            mediaElement.AutoPlay = false;

        }

        private void MediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            /*App.ALinks.servoWrite(26, 60);*/
            mediaElement.AutoPlay = false;
            mediaElement.Stop();
            uebergabeText = null;
        }

        private void SpeechPage_Loaded(object sender, RoutedEventArgs e)
        {
            double? diagonal = DisplayInformation.GetForCurrentView().DiagonalSizeInInches;
            isSpeeking = true;

            TestClass testClass = new TestClass("Hallo Welt");

            //move commandbar to page bottom on small screens
            //if (diagonal < 7)
            //{
            //    topbar.Visibility = Visibility.Collapsed;
            //    //pageTitleContainer.Visibility = Visibility.Visible;
            //    bottombar.Visibility = Visibility.Visible;
            //}
            //else
            //{
            //    topbar.Visibility = Visibility.Visible;
            //    //pageTitleContainer.Visibility = Visibility.Collapsed;
            //    bottombar.Visibility = Visibility.Collapsed;
            //}
        }

        /// <summary>
        /// When activating the scenario, ensure we have permission from the user to access their microphone, and
        /// provide an appropriate path for the user to enable access to the microphone if they haven't
        /// given explicit permission for it. 
        /// </summary>
        /// <param name="e">The navigation event details</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            bool permissionGained = await AudioCapturePermissions.RequestMicrophonePermission();
            if (permissionGained)
            {
                await InitializeRecognizer();
            }
            else
            {
                resultTextBlock.Visibility = Visibility.Visible;
                resultTextBlock.Text = "Permission to access capture resources was not given by the user; please set the application setting in Settings->Privacy->Microphone.";
            }
        }

        /// <summary>
        /// Initialize Speech Recognizer and compile constraints.
        /// </summary>
        /// <param name="recognizerLanguage">Language to use for the speech recognizer</param>
        /// <returns>Awaitable task.</returns>
        private async Task InitializeRecognizer()
        {
            if (speechRecognizer != null)
            {
                //Clean old Eventhandlers if possible
                speechRecognizer.StateChanged -= SpeechRecognizer_StateChanged;
                speechRecognizer.ContinuousRecognitionSession.Completed -= ContinuousRecognitionSession_Completed;
                speechRecognizer.ContinuousRecognitionSession.ResultGenerated -= ContinuousRecognitionSession_ResultGenerated;
                speechRecognizer.HypothesisGenerated -= SpeechRecognizer_HypothesisGenerated;

                this.speechRecognizer.Dispose();
                this.speechRecognizer = null;
            }

            //Initialize new Speechrecognizer
            this.speechRecognizer = new SpeechRecognizer(SpeechRecognizer.SystemSpeechLanguage);

            // Provide feedback to the user about the state of the recognizer. This can be used to provide visual feedback in the form
            // of an audio indicator to help the user understand whether they're being heard.
            // Apply the dictation topic constraint to optimize for dictated freeform speech.
            var dictationConstraint = new SpeechRecognitionTopicConstraint(SpeechRecognitionScenario.Dictation, "dictation");
            speechRecognizer.Constraints.Add(dictationConstraint);
            SpeechRecognitionCompilationResult result = await speechRecognizer.CompileConstraintsAsync();

            //Create Continuous-Dictation Handler
            speechRecognizer.ContinuousRecognitionSession.ResultGenerated += ContinuousRecognitionSession_ResultGenerated;
            speechRecognizer.HypothesisGenerated += SpeechRecognizer_HypothesisGenerated;
            speechRecognizer.ContinuousRecognitionSession.Completed += ContinuousRecognitionSession_Completed;

            try
            {
                // Provide feedback to the user about the state of the recognizer.
                speechRecognizer.StateChanged += SpeechRecognizer_StateChanged;

                //Text before continuous recognize is enabled
                string uiOptionsText = "Hallo ich bin das InMoov Sprachprogramm\n\nIch bin noch nicht fertig, tut mir leid :( \nAber ich gebe mir mühe :)\n\nIch bin noch nicht fertig, tut mir leid :(";
                speechRecognizer.UIOptions.ExampleText = uiOptionsText;
                // Compile the constraint.
                SpeechRecognitionCompilationResult compilationResult = await speechRecognizer.CompileConstraintsAsync();


                resultTextBlock.Visibility = Visibility.Visible;

                RecognizeWithoutUIListConstraint_Toggle(this, new RoutedEventArgs());
            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == HResultRecognizerNotFound)
                {

                    resultTextBlock.Visibility = Visibility.Visible;
                    resultTextBlock.Text = "Speech Language pack for selected language not installed.";
                }
                else
                {
                    var messageDialog = new Windows.UI.Popups.MessageDialog(ex.Message, "Exception");
                    await messageDialog.ShowAsync();
                }
            }
        }

        /// <summary>
        /// Handle SpeechRecognizer state changed events by updating a UI component.
        /// </summary>
        /// <param name="sender">Speech recognizer that generated this status event</param>
        /// <param name="args">The recognizer's status</param>
        private async void SpeechRecognizer_StateChanged(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                //Hello, I am an Method
            });
        }

        /// <summary>
        /// Begin recognition, or finish the recognition session. 
        /// </summary>
        /// <param name="sender">The button that generated this event</param>
        /// <param name="e">Unused event details</param>
        public async void RecognizeWithoutUIListConstraint_Toggle(object sender, RoutedEventArgs e)
        {
            if (isListening == false && ToggleSpeech.IsOn == true)
            {
                // The recognizer can only start listening in a continuous fashion if the recognizer is currently idle.
                // This prevents an exception from occurring.
                if (speechRecognizer.State == SpeechRecognizerState.Idle)
                {
                    try
                    {
                        isListening = true;
                        System.Diagnostics.Debug.WriteLine("HELLO");
                        await speechRecognizer.ContinuousRecognitionSession.StartAsync();
                    }
                    catch (Exception ex)
                    {
                        if ((uint)ex.HResult == HResultPrivacyStatementDeclined)
                        {
                            //Empty
                        }
                        else
                        {
                            var messageDialog = new Windows.UI.Popups.MessageDialog(ex.Message, "Exception");
                            await messageDialog.ShowAsync();
                        }
                        isListening = false;
                    }
                }
            }
            else
            {
                isListening = false;

                if (speechRecognizer.State != SpeechRecognizerState.Idle)
                {
                    // Cancelling recognition prevents any currently recognized speech from
                    // generating a ResultGenerated event. StopAsync() will allow the final session to 
                    // complete.
                    try
                    {
                        await speechRecognizer.ContinuousRecognitionSession.StopAsync();

                        // Ensure we don't leave any hypothesis text behind
                        resultTextBlock.Text = dictatedTextBuilder.ToString();
                    }
                    catch (Exception exception)
                    {
                        var messageDialog = new Windows.UI.Popups.MessageDialog(exception.Message, "Exception");
                        await messageDialog.ShowAsync();
                    }
                }
            }
        }

        /// <summary>
        /// Handle events fired when error conditions occur, such as the microphone becoming unavailable, or if
        /// some transient issues occur.
        /// </summary>
        /// <param name="sender">The continuous recognition session</param>
        /// <param name="args">The state of the recognizer</param>
        private async void ContinuousRecognitionSession_Completed(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionCompletedEventArgs args)
        {
            //Normalerweise nicht in Benutzung
            Debug.WriteLine("Session is Completed!");
            if (args.Status != SpeechRecognitionResultStatus.Success)
            {
                if (args.Status == SpeechRecognitionResultStatus.TimeoutExceeded)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        //cbLanguageSelection.IsEnabled = true;
                        resultTextBlock.Text = dictatedTextBuilder.ToString();
                        isListening = false;
                    });
                }
                else
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        resultTextBlock.Text = " Dictate";
                        //cbLanguageSelection.IsEnabled = true;
                        isListening = false;
                    });
                }
            }
        }

        bool colorIsDetected = false;
        bool numberIsDetected = false;
        byte[] detectedColor = null;
        int detectedNumber = 0;

        /// <summary>
        /// While the user is speaking, update the textbox with the partial sentence of what's being said for user feedback.
        /// </summary>
        /// <param name="sender">The recognizer that has generated the hypothesis</param>
        /// <param name="args">The hypothesis formed</param>
        private async void SpeechRecognizer_HypothesisGenerated(SpeechRecognizer sender, SpeechRecognitionHypothesisGeneratedEventArgs args)
        {
            if (uebergabeText == null)
            {
                //Create the Text
                string recognizedText = dictatedTextBuilder.ToString() + " " + args.Hypothesis.Text + " ...";

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    //LED Search-Algorithm
                    #region LED
                    if (recognizedText.ToLower().Contains("led") && (!colorIsDetected || !numberIsDetected))
                    {
                        //Search if captured Word is any of the given colors
                        if (!colorIsDetected)
                          colorIsDetected = String2Color(recognizedText, out detectedColor);
                        if (!numberIsDetected)
                          numberIsDetected = String2Number(recognizedText, out detectedNumber);
                    }

                    if (colorIsDetected && numberIsDetected)
                    {
                        var test1 = detectedColor;
                        var test2 = detectedNumber;

                        SpeechHandler speechhandler = new SpeechHandler();
                        speechhandler.HandleDetectedSpeech("start");

                        dictatedTextBuilder.Clear();
                        colorIsDetected = numberIsDetected = false;
                    }

                    ////Change Color of the LED
                    //if (/*ledCaptured == true && */colorCaptured != null)
                    //{
                    //    UpdateLed(colorCaptured);

                    //    resultTextBlock.Text = "Die LED" + newNum.ToString() + " ist jetzt " + colorCaptured.ToString();
                    //    byte.TryParse(newNum.ToString(), out byte ExitNumber);
                    //    ExitNumber--;                   //Convert numbervalue 1-16 to 0-15
                    //    Debug.WriteLine($"LED: {ledCaptured}, Color: {colorCaptured}, Number: {newNum}");
                    //    //App.neopixel.SetPixelColor((NexNum), color[0], color[1], color[2]);           //Set Pixelcolor to RGB-Value

                    //    recognizedText = "LED " + newNum + " " + colorCaptured;
                    //    uebergabeText = "Alles klar, die LED " + newNum + " ist jetzt " + colorCaptured;
                    //    resultTextBlock.Text = recognizedText;

                    //    //Reset Variables
                    //    #region
                    //    ledCaptured = false;
                    //    colorCaptured = null;
                    //    isSpeeking = true;
                    //    facedetect[0] = facedetect[1] = 0;
                    //    for (int i = 0; i < numbers.Length; i++)
                    //    {
                    //        numbers[i] = 0;
                    //    }
                    //    Speak(uebergabeText);
                    //    dictatedTextBuilder.Clear();
                    //    #endregion
                    //}
                    //else
                        resultTextBlock.Text = recognizedText;

                    #endregion
                });
            }
        }

        private bool String2Color(string text, out byte[] color)
        {
            byte[] detectedColor = new byte[3];

            foreach (string colorS in colorlist)
            {
                if (text.Contains(colorS))
                {
                    colorCaptured = colorS;

                    switch (colorCaptured)
                    {
                        //Set RGB-Colors to red
                        case "rot":
                            detectedColor[0] = 255;
                            detectedColor[1] = 0;
                            detectedColor[2] = 0;
                            break;
                        //Set RGB-Colors to blau
                        case "blau":
                            detectedColor[0] = 0;
                            detectedColor[1] = 0;
                            detectedColor[2] = 255;
                            break;
                        //Set RGB-Colors to green
                        case "grün":
                            detectedColor[0] = 0;
                            detectedColor[1] = 255;
                            detectedColor[2] = 0;
                            break;
                        //Set RGB-Colors to yellow
                        case "gelb":
                            detectedColor[0] = 255;
                            detectedColor[1] = 255;
                            detectedColor[2] = 0;
                            break;
                        //Set RGB-Colors to off/black
                        default:
                            detectedColor[0] = 0;
                            detectedColor[1] = 0;
                            detectedColor[2] = 0;
                            break;
                    }
                }
            }

            color = detectedColor;

            return detectedColor == null ? false : true;
        }

        private bool String2Number(string text, out int zahl)
        {
            //Get Number out of the recognized string
            int newNum = 0;
            int pointer = 0;                //"points" at the Numberarray

            foreach (string number in numberlist)
            {
                if (text.Contains(number))
                {
                    int.TryParse(number, out newNum);
                    //numbers[pointer] = newNum;              //Sets the number at a new Array Value for each time one is recognized -> better number capturing
                    //pointer++;
                }
                else if (text.Contains("eins"))   //Replace number 1 with string "eins" because of bad recognition of the number "1" 
                {
                    //numbers[pointer] = 1;
                    //pointer++;
                    newNum = 1;
                }
            }

            //newNum = GetMostUsedValue(numbers, out newNum);          //Get the most used numbervalue from Array -> better number recognition

            zahl = newNum;

            return newNum == 0 ? false : true;
        }

        /// <summary>
        /// Handle events fired when a result is generated. Check for high to medium confidence, and then append the
        /// string to the end of the stringbuffer, and replace the content of the textbox with the string buffer, to
        /// remove any hypothesis text that may be present.
        /// </summary>
        /// <param name="sender">The Recognition session that generated this result</param>
        /// <param name="args">Details about the recognized speech</param>
        private async void ContinuousRecognitionSession_ResultGenerated(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            //Normalerweise nicht ausgelößt
            System.Diagnostics.Debug.WriteLine("Result is generated!");
            if (args.Result.Confidence == SpeechRecognitionConfidence.Medium ||
                args.Result.Confidence == SpeechRecognitionConfidence.High)
            {
                dictatedTextBuilder.Append(args.Result.Text + " ");
            }
            else
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    string discardedText = args.Result.Text;
                });
            }
        }

        /// <summary>
        /// Function which gives the most used Value in an integer Array
        /// </summary>
        /// <param name="ArrayOfNumbers">Array in which you will search the most used number</param>
        /// <param name="MostUsedNumber">The Most used number in the Array</param>
        /// <returns>The number of times the most used number was used</returns>
        public static int GetMostUsedValue(int[] ArrayOfNumbers, out int MostUsedNumber)
        {
            int max_Countnumber = 0;        //Zähler wie oft der Wert im Array benutzt wurde
            MostUsedNumber = 0;

            for (int i = 0; i < ArrayOfNumbers.Length; i++)
            {
                int l_count = 0;                //Der meist verwendete Wertes
                for (int j = 0; j < ArrayOfNumbers.Length; j++)
                {
                    if (ArrayOfNumbers[i] == ArrayOfNumbers[j])
                    {
                        l_count++;
                    }
                    if (ArrayOfNumbers[i] >= 10 && i < 25)
                    {
                        for (int k = 25; k < ArrayOfNumbers.Length; k++)
                        {
                            //Füllt letzte 25 Stellen mit Zahl wenn 11 - 16 erkannt worden ist
                            //Das ist nötig weil gute Erkennung ansonsten nicht möglich ist. 
                            ArrayOfNumbers[k] = ArrayOfNumbers[i];
                        }
                    }
                }

                if (l_count > max_Countnumber)
                {
                    max_Countnumber = l_count;
                    if (ArrayOfNumbers[i] != 0)
                    {
                        MostUsedNumber = ArrayOfNumbers[i];
                    }
                }
            }

            return max_Countnumber;
        }

        /// <summary>
        /// This is invoked when the user clicks on the speak/stop button.
        /// </summary>
        /// <param name="sender">Button that triggered this event</param>
        /// <param name="e">State information about the routed event</param>
        public static async void Speak(string Text)
        {
            SpeechSynthesizer synthesizer = new SpeechSynthesizer();
            // If the media is playing, the user has pressed the button to stop the playback.
            if (mediaElement.CurrentState == MediaElementState.Playing)
            {
                mediaElement.Stop();
            }
            else
            {
                if (!String.IsNullOrEmpty(Text))
                {
                    // Change the button label. You could also just disable the button if you don't want any user control.

                    try
                    {
                        if (isSpeeking)
                        {
                            /*App.ALinks.setPinMode(26, Microsoft.Maker.RemoteWiring.PinMode.SERVO);
                            App.ALinks.servoWrite(26, 0);*/
                            isSpeeking = false;
                            // Create a stream from the text. This will be played using a media element.
                            SpeechSynthesisStream synthesisStream = await synthesizer.SynthesizeTextToStreamAsync(Text);

                            // Set the source and start playing the synthesized audio stream.
                            mediaElement.AutoPlay = true;
                            mediaElement.SetSource(synthesisStream, synthesisStream.ContentType);
                            mediaElement.Play();
                            dictatedTextBuilder.Clear();
                            Text = null;
                            uebergabeText = null;
                        }
                    }
                    catch (FileNotFoundException)
                    {
                        // If media player components are unavailable, (eg, using a N SKU of windows), we won't
                        // be able to start media playback. Handle this gracefully
                        var messageDialog = new Windows.UI.Popups.MessageDialog("Media player components unavailable");
                        await messageDialog.ShowAsync();
                    }
                    catch (Exception)
                    {
                        // If the text is unable to be synthesized, throw an error message to the user.
                        mediaElement.AutoPlay = false;
                        var messageDialog = new Windows.UI.Popups.MessageDialog("Unable to synthesize text");
                        await messageDialog.ShowAsync();
                    }
                }
            }
        }

        public static async void Speaking(string Text, bool isSpeeking)
        {
            SpeechSynthesizer synthesizer = new SpeechSynthesizer();
            MediaElement mediaElement = new MediaElement();
            mediaElement.AutoPlay = false;
            mediaElement.MediaEnded += media_MediaEnded;
            // If the media is playing, the user has pressed the button to stop the playback.
            if (mediaElement.CurrentState == MediaElementState.Playing)
            {
                mediaElement.Stop();
            }
            else
            {
                if (!String.IsNullOrEmpty(Text))
                {
                    // Change the button label. You could also just disable the button if you don't want any user control.

                    try
                    {
                        if (isSpeeking)
                        {
                            /*App.ALinks.setPinMode(26, Microsoft.Maker.RemoteWiring.PinMode.SERVO);
                            App.ALinks.servoWrite(26, 0);*/
                            isSpeeking = false;
                            // Create a stream from the text. This will be played using a media element.
                            SpeechSynthesisStream synthesisStream = await synthesizer.SynthesizeTextToStreamAsync(Text);

                            // Set the source and start playing the synthesized audio stream.
                            mediaElement.AutoPlay = true;
                            mediaElement.SetSource(synthesisStream, synthesisStream.ContentType);
                            mediaElement.Play();
                            Text = null;
                        }
                    }
                    catch (FileNotFoundException)
                    {
                        // If media player components are unavailable, (eg, using a N SKU of windows), we won't
                        // be able to start media playback. Handle this gracefully
                        var messageDialog = new Windows.UI.Popups.MessageDialog("Media player components unavailable");
                        await messageDialog.ShowAsync();
                    }
                    catch (Exception)
                    {
                        // If the text is unable to be synthesized, throw an error message to the user.
                        mediaElement.AutoPlay = false;
                        var messageDialog = new Windows.UI.Popups.MessageDialog("Unable to synthesize text");
                        await messageDialog.ShowAsync();
                    }
                }
            }
        }

        private static void media_MediaEnded(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }



    public class SpeechHandler
    {
        private EventRegistrationTokenTable<EventHandler<SpeechEventArgs>> m_SpeechTokenTable = new EventRegistrationTokenTable<EventHandler<SpeechEventArgs>>();

        public SpeechHandler()
        {

        }

        public void HandleDetectedSpeech(string text)
        {
            if (text.Equals("start"))
                OnUpdateLed(new SpeechEventArgs("grün"));
        }

        public event EventHandler<SpeechEventArgs>UpdateLed
        {
            add
            {
                EventRegistrationTokenTable<EventHandler<SpeechEventArgs>>.GetOrCreateEventRegistrationTokenTable(ref m_SpeechTokenTable).AddEventHandler(value);
            }
            remove
            {
                EventRegistrationTokenTable<EventHandler<SpeechEventArgs>>.GetOrCreateEventRegistrationTokenTable(ref m_SpeechTokenTable).RemoveEventHandler(value);
            }
        }

        internal void OnUpdateLed(SpeechEventArgs e)
        {
            EventRegistrationTokenTable<EventHandler<SpeechEventArgs>>
            .GetOrCreateEventRegistrationTokenTable(ref m_SpeechTokenTable)
            .InvocationList?.Invoke(this, new SpeechEventArgs(e.Color));
        }
    }

    public class SpeechEventArgs : EventArgs
    {
        public string Color { get; private set; }

        public SpeechEventArgs(string color)
        {
            Color = color;
        }
    }
}