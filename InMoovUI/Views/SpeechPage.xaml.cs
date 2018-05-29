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

        private bool isListening;
        private SpeechRecognizer speechRecognizer;
        private ResourceContext speechContext;
        private ResourceMap speechResourceMap;
        private StringBuilder dictatedTextBuilder;
        private bool isPopulatingLanguages = false;
        private IAsyncOperation<SpeechRecognitionResult> recognitionOperation;
        private string textCaptured;                    //Handles the captured Text
        private int[] facedetect = new int[2];          //Handles the facedetection start/stop Commands
        private int[] numbers = new int[50];            //Numbers which are getting captured by speechrecognition
        private bool ledCaptured;                       //Handles if Word LED is captured
        private string colorCaptured;                   //Handles which color is picked by user
        //Handles the amount of colors the User can pick
        private static List<string> colorlist = new List<string>() { "grün", "rot", "blau", "gelb", "schwarz", "aus" };
        //Handles the amount of Numbers the User can pick
        private static List<string> numberlist = new List<string>() { "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16"};
        //Handles Keywords the user can say to pick Facedetection
        private static List<string> facelist = new List<string>() { "Gesichtserkennung", "gesichtserkennung", "Gesicht", "gesicht" };
        //Handles basic start/stop Keys
        private static List<string> openList = new List<string>() { "starte", "erkenne", "öffne" };
        private static List<string> closeList = new List<string>() { "schließe", "stoppe", "stoppen" };
        //Needed to give NeoPixel the RGBs of picked Color
        private byte[] color = new byte[3];

        public SpeechPage()
        {
            this.InitializeComponent();
            this.Loaded += SpeechPage_Loaded;
            isListening = false;
            dictatedTextBuilder = new StringBuilder();

            LedRingPage.InitializeNeoPixel();
        }
        private void SpeechPage_Loaded(object sender, RoutedEventArgs e)
        {
            double? diagonal = DisplayInformation.GetForCurrentView().DiagonalSizeInInches;

            //move commandbar to page bottom on small screens
            if (diagonal < 7)
            {
                topbar.Visibility = Visibility.Collapsed;
                //pageTitleContainer.Visibility = Visibility.Visible;
                bottombar.Visibility = Visibility.Visible;
            }
            else
            {
                topbar.Visibility = Visibility.Visible;
                //pageTitleContainer.Visibility = Visibility.Collapsed;
                bottombar.Visibility = Visibility.Collapsed;
            }
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
                //Initialize basic Speech tiles and Varialbes

                Language speechLanguage = SpeechRecognizer.SystemSpeechLanguage;
                string langTag = speechLanguage.LanguageTag;
                speechContext = ResourceContext.GetForCurrentView();
                speechContext.Languages = new string[] { langTag };

                speechResourceMap = ResourceManager.Current.MainResourceMap.GetSubtree("LocalizationSpeechResources");

                PopulateLanguageDropdown();
                await InitializeRecognizer(SpeechRecognizer.SystemSpeechLanguage);
            }
            else
            {
                resultTextBlock.Visibility = Visibility.Visible;
                resultTextBlock.Text = "Permission to access capture resources was not given by the user; please set the application setting in Settings->Privacy->Microphone.";
                cbLanguageSelection.IsEnabled = false;
            }
        }

        /// <summary>
        /// Look up the supported languages for this speech recognition scenario, 
        /// that are installed on this machine, and populate a dropdown with a list.
        /// </summary>
        private void PopulateLanguageDropdown()
        {
            // disable the callback so we don't accidentally trigger initialization of the recognizer
            // while initialization is already in progress.
            isPopulatingLanguages = true;

            Language defaultLanguage = SpeechRecognizer.SystemSpeechLanguage;
            IEnumerable<Language> supportedLanguages = SpeechRecognizer.SupportedGrammarLanguages;
            foreach (Language lang in supportedLanguages)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Tag = lang;
                item.Content = lang.DisplayName;

                cbLanguageSelection.Items.Add(item);
                if (lang.LanguageTag == defaultLanguage.LanguageTag)
                {
                    item.IsSelected = true;
                    cbLanguageSelection.SelectedItem = item;
                }
            }
            isPopulatingLanguages = false;
        }

        /// <summary>
        /// When a user changes the speech recognition language, trigger re-initialization of the 
        /// speech engine with that language, and change any speech-specific UI assets.
        /// </summary>
        /// <param name="sender">Ignored</param>
        /// <param name="e">Ignored</param>
        private async void cbLanguageSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isPopulatingLanguages)
            {
                return;
            }

            ComboBoxItem item = (ComboBoxItem)(cbLanguageSelection.SelectedItem);
            Language newLanguage = (Language)item.Tag;
            if (speechRecognizer != null)
            {
                if (speechRecognizer.CurrentLanguage == newLanguage)
                {
                    return;
                }
            }

            // trigger cleanup and re-initialization of speech.
            try
            {
                // update the context for resource lookup
                speechContext.Languages = new string[] { newLanguage.LanguageTag };

                await InitializeRecognizer(newLanguage);
            }
            catch (Exception exception)
            {
                var messageDialog = new Windows.UI.Popups.MessageDialog(exception.Message, "Exception");
                await messageDialog.ShowAsync();
            }
        }

        /// <summary>
        /// Ensure that we clean up any state tracking event handlers created in OnNavigatedTo to prevent leaks,
        /// dipose the speech recognizer, and clean up to ensure the scenario is not still attempting to recognize
        /// speech while not in view.
        /// </summary>
        /// <param name="e">Details about the navigation event</param>
        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (this.speechRecognizer != null)
            {
                if (isListening)
                {
                    await this.speechRecognizer.ContinuousRecognitionSession.CancelAsync();
                    isListening = false;
                    resultTextBlock.Text = " Dictate";
                    cbLanguageSelection.IsEnabled = true;
                }

                resultTextBlock.Text = "";

                this.speechRecognizer.Dispose();
                this.speechRecognizer = null;
            }
            // Prompt the user for permission to access the microphone. This request will only happen
            // once, it will not re-prompt if the user rejects the permission.
            bool permissionGained = await AudioCapturePermissions.RequestMicrophonePermission();
            if (permissionGained)
            {

                PopulateLanguageDropdown();
                await InitializeRecognizer(SpeechRecognizer.SystemSpeechLanguage);
            }
            else
            {
                this.resultTextBlock.Text = "Permission to access capture resources was not given by the user, reset the application setting in Settings->Privacy->Microphone.";
                cbLanguageSelection.IsEnabled = false;
            }
        }

        /// <summary>
        /// Initialize Speech Recognizer and compile constraints.
        /// </summary>
        /// <param name="recognizerLanguage">Language to use for the speech recognizer</param>
        /// <returns>Awaitable task.</returns>
        private async Task InitializeRecognizer(Language recognizerLanguage)
        {
            if (speechRecognizer != null)
            {
                // cleanup prior to re-initializing this scenario.
                speechRecognizer.StateChanged -= SpeechRecognizer_StateChanged;
                speechRecognizer.ContinuousRecognitionSession.Completed -= ContinuousRecognitionSession_Completed;
                speechRecognizer.ContinuousRecognitionSession.ResultGenerated -= ContinuousRecognitionSession_ResultGenerated;
                speechRecognizer.HypothesisGenerated -= SpeechRecognizer_HypothesisGenerated;

                this.speechRecognizer.Dispose();
                this.speechRecognizer = null;
            }

            this.speechRecognizer = new SpeechRecognizer(recognizerLanguage);

            // Provide feedback to the user about the state of the recognizer. This can be used to provide visual feedback in the form
            // of an audio indicator to help the user understand whether they're being heard.
            // Apply the dictation topic constraint to optimize for dictated freeform speech.
            var dictationConstraint = new SpeechRecognitionTopicConstraint(SpeechRecognitionScenario.Dictation, "dictation");
            speechRecognizer.Constraints.Add(dictationConstraint);
            SpeechRecognitionCompilationResult result = await speechRecognizer.CompileConstraintsAsync();
            if (result.Status != SpeechRecognitionResultStatus.Success)
            {
                //Soll nicht ausgelößt werden :)
            }

            // Handle continuous recognition events. Completed fires when various error states occur. ResultGenerated fires when
            // some recognized phrases occur, or the garbage rule is hit. HypothesisGenerated fires during recognition, and
            // allows us to provide incremental feedback based on what the user's currently saying.
            speechRecognizer.ContinuousRecognitionSession.ResultGenerated += ContinuousRecognitionSession_ResultGenerated;
            speechRecognizer.HypothesisGenerated += SpeechRecognizer_HypothesisGenerated;
            speechRecognizer.ContinuousRecognitionSession.Completed += ContinuousRecognitionSession_Completed;

            try
            {
                // Provide feedback to the user about the state of the recognizer.
                speechRecognizer.StateChanged += SpeechRecognizer_StateChanged;

                //Text before continuous recognize is enabled
                string uiOptionsText = "Ich bin noch nicht fertig, tut mir leid :(";
                speechRecognizer.UIOptions.ExampleText = uiOptionsText;
                helpTextBlock.Text = "Hallo ich bin das InMoov Sprachprogramm\n\nIch bin noch nicht fertig, tut mir leid :( \nAber ich gebe mir mühe :)";
                // Compile the constraint.
                SpeechRecognitionCompilationResult compilationResult = await speechRecognizer.CompileConstraintsAsync();


                resultTextBlock.Visibility = Visibility.Collapsed;
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

        //Initialize a new FacesPage
        FacesPage fp = new FacesPage();
        /// <summary>
        /// Uses the recognizer constructed earlier to listen for speech from the user before displaying 
        /// it back on the screen. Uses developer-provided UI for user feedback.
        /// </summary>
        /// <param name="sender">Button that triggered this event</param>
        /// <param name="e">State information about the routed event</param>
        private async void RecognizeWithoutUIListConstraint_Click(object sender, RoutedEventArgs e)
        {
            while (ToggleSpeech.IsOn)
            {
                ledCaptured = false;
                colorCaptured = null;
                heardYouSayTextBlock.Visibility = resultTextBlock.Visibility = Visibility.Collapsed;
                cbLanguageSelection.IsEnabled = false;
                try
                {
                    // Start continuous Recognition Session
                    recognitionOperation = speechRecognizer.RecognizeAsync();

                    SpeechRecognitionResult speechRecognitionResult = await recognitionOperation;
                }
                catch (TaskCanceledException exception)
                {
                    Debug.Print(exception.ToString());
                }
                catch (Exception exception)
                {
                    // Handle the speech privacy policy error.
                    if ((uint)exception.HResult == HResultPrivacyStatementDeclined)
                    {
                        resultTextBlock.Visibility = Visibility.Visible;
                        resultTextBlock.Text = "The privacy statement was declined.";
                    }
                    else
                    {
                        var messageDialog = new Windows.UI.Popups.MessageDialog(exception.Message, "Exception");
                        await messageDialog.ShowAsync();
                    }
                }

                // Reset UI state.
                cbLanguageSelection.IsEnabled = true;
            }
            //If the toggle Switch is off, Recognizing will end if user dont speech
            StopRecognicing();
        }

        /// <summary>
        /// Handle events fired when error conditions occur, such as the microphone becoming unavailable, or if
        /// some transient issues occur.
        /// </summary>
        /// <param name="sender">The continuous recognition session</param>
        /// <param name="args">The state of the recognizer</param>
        private async void ContinuousRecognitionSession_Completed(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionCompletedEventArgs args)
        {
            Debug.WriteLine("CRS_C");
            if (args.Status != SpeechRecognitionResultStatus.Success)
            {
                // If TimeoutExceeded occurs, the user has been silent for too long. We can use this to 
                // cancel recognition if the user in dictation mode and walks away from their device, etc.
                // In a global-command type scenario, this timeout won't apply automatically.
                // With dictation (no grammar in place) modes, the default timeout is 20 seconds.
                if (args.Status == SpeechRecognitionResultStatus.TimeoutExceeded)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        helpTextBlock.Text = " Dictate";
                        cbLanguageSelection.IsEnabled = true;
                        helpTextBlock.Text = dictatedTextBuilder.ToString();
                        isListening = false;
                    });
                }
                else
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        helpTextBlock.Text = " Dictate";
                        cbLanguageSelection.IsEnabled = true;
                        isListening = false;
                    });
                }
            }
        }

        /// <summary>
        /// While the user is speaking, update the textbox with the partial sentence of what's being said for user feedback.
        /// </summary>
        /// <param name="sender">The recognizer that has generated the hypothesis</param>
        /// <param name="args">The hypothesis formed</param>
        private async void SpeechRecognizer_HypothesisGenerated(SpeechRecognizer sender, SpeechRecognitionHypothesisGeneratedEventArgs args)
        {
            Debug.WriteLine("SR_HG");
            string hypothesis = args.Hypothesis.Text;

            // Update the textbox with the currently confirmed text, and the hypothesis combined.
            string textboxContent = dictatedTextBuilder.ToString() + " " + hypothesis + " ...";

            int zel = 0;
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                //Search after Face-Detection Phrases/Words
                #region Gesichtserkennung
                foreach (string facedet in facelist)
                {
                    if (textboxContent.Contains(facedet))
                    {
                        facedetect[0] = 1;
                    }
                }
                foreach (string open in openList)
                {
                    if (textboxContent.Contains(open))
                    {
                        facedetect[1] = 1;
                    }
                }
                foreach (string close in closeList)
                {
                    if (textboxContent.Contains(close))
                    {
                        facedetect[1] = 2;
                    }
                }
                if (facedetect[0] == 1 && facedetect[1] == 1)
                {
                    fp.StarteWebcam();
                }
                else if (facedetect[0] == 1 && facedetect[1] == 2)
                {
                    fp.StopWebcam();
                }
                #endregion
                //LED Search-Algorithm
                #region LED
                if ((textboxContent.Contains("LED") || textboxContent.Contains("led")) && ledCaptured != true)
                {
                    Debug.WriteLine("LED captured!");
                    ledCaptured = true;
                }
                //Search if captured Word is any of the given colors
                foreach (string colorS in colorlist)
                {
                    if (textboxContent.Contains(colorS))
                    {
                        Debug.WriteLine("Color captured: " + colorS);
                        colorCaptured = colorS;
                        switch (colorCaptured)
                        {
                            case "rot":
                                color[0] = 255;             //R
                                color[1] = 0;               //G
                                color[2] = 0;               //B
                                break;
                            case "blau":
                                color[0] = 0;               //R
                                color[1] = 0;               //G
                                color[2] = 255;             //B
                                break;
                            case "grün":
                                color[0] = 0;               //R
                                color[1] = 255;             //G
                                color[2] = 0;               //B
                                break;
                            case "gelb":
                                color[0] = 255;             //R
                                color[1] = 255;             //G
                                color[2] = 0;               //B
                                break;
                            case "schwarz":
                            case "aus":
                                color[0] = 0;               //R
                                color[1] = 0;               //G
                                color[2] = 0;               //B
                                break;
                            default:
                                color[0] = color[1] = color[2] = 0;
                                break;
                        }
                    }
                }

                //Get Number out of the recognized string
                int newNum = 0;
                foreach (string number in numberlist)
                {
                    if (textboxContent.Contains(number))
                    {
                        int.TryParse(number, out newNum);
                        numbers[zel] = newNum;
                        zel++;
                    }
                    else if (textboxContent.Contains("eins"))
                    {
                        Debug.WriteLine(1);
                        numbers[zel] = 1;
                        zel++;
                    }
                }

                //Change Color of the LED
                if (ledCaptured == true && colorCaptured != null)
                {
                    newNum = GetMostUsedValue(numbers, out int exNum);
                    resultTextBlock.Text = "Die LED" + exNum.ToString() + " ist jetzt " + colorCaptured.ToString();
                    exNum--;
                    byte.TryParse(exNum.ToString(), out byte NexNum);
                    Debug.WriteLine($"LED: {ledCaptured}, Color: {colorCaptured}, Number: {exNum}");
                    App.neopixel.SetPixelColor((NexNum), color[0], color[1], color[2]);
                    ledCaptured = false;
                    zel = 0;
                    for(int i = 0; i < numbers.Length; i++)
                    {
                        numbers[i] = 0;
                    }
                }
                #endregion
                helpTextBlock.Text = textboxContent;
            });
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
            System.Diagnostics.Debug.WriteLine("CRS_RG");
            // We may choose to discard content that has low confidence, as that could indicate that we're picking up
            // noise via the microphone, or someone could be talking out of earshot.
            if (args.Result.Confidence == SpeechRecognitionConfidence.Medium ||
                args.Result.Confidence == SpeechRecognitionConfidence.High)
            {
                dictatedTextBuilder.Append(args.Result.Text + " ");
            }
            else
            {
                // In some scenarios, a developer may choose to ignore giving the user feedback in this case, if speech
                // is not the primary input mechanism for the application.
                // Here, just remove any hypothesis text by resetting it to the last known good.
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    string discardedText = args.Result.Text;
                });
            }
        }

        private void StopRecognicing()
        {
            if (isListening == true && ToggleSpeech.IsOn == true)
            {
                ToggleSpeech.IsOn = false;
                isListening = false;
                helpTextBlock.Text = "";
                heardYouSayTextBlock.Visibility = Visibility.Collapsed;
                resultTextBlock.Visibility = Visibility.Collapsed;
                dictatedTextBuilder.Clear();
            }
            else
            {
                //AUSNAHMEFEHLER!!!
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
            int max_Countnumber = 0;
            MostUsedNumber = 0;

            for (int i = 0; i < ArrayOfNumbers.Length; i++)
            {
                int l_count = 0;                //Zählt Anzahl des am meisten verwendeten Wertes
                for (int j = 0; j < ArrayOfNumbers.Length; j++)
                {
                    if (ArrayOfNumbers[i] == ArrayOfNumbers[j])
                    {
                        l_count++;
                    }
                    if(ArrayOfNumbers[i] >= 10 && i < 25)
                    {
                        for(int k = 25; k < ArrayOfNumbers.Length; k++)
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
    }
}