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
        private string textCaptured;
        private int[] facedetect = new int[2];
        private bool ledCaptured;
        private string colorCaptured;
        private int numberCaptured = 99;
        private List<string> colorlist = new List<string>() { "grün", "rot", "blau", "gelb" };
        private List<string> numberlist = new List<string>() { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15" };
        private List<string> facelist = new List<string>() { "Gesichtserkennung", "gesichtserkennung", "Gesicht", "gesicht" };
        private List<string> openList = new List<string>() { "starte", "erkenne", "öffne" };
        private List<string> closeList = new List<string>() { "schließe", "stoppe", "stoppen" };

        public SpeechPage()
        {
            this.InitializeComponent();
            this.Loaded += SpeechPage_Loaded;
            isListening = false;
            dictatedTextBuilder = new StringBuilder();
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
                // Enable the recognition buttons.

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
                Debug.WriteLine("ErrorLine264");
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
                // Add a list constraint to the recognizer.

                // RecognizeWithUIAsync allows developers to customize the prompts.
                string uiOptionsText = "Ich bin noch nicht fertig, tut mir leid :(";
                speechRecognizer.UIOptions.ExampleText = uiOptionsText;
                helpTextBlock.Text = "Hallo ich bin das InMoov Sprachprogramm\n\nIch bin noch nicht fertig, tut mir leid :( \nAber ich gebe mir mühe :)";
                // Compile the constraint.
                SpeechRecognitionCompilationResult compilationResult = await speechRecognizer.CompileConstraintsAsync();

                // Check to make sure that the constraints were in a proper format and the recognizer was able to compile it.
                if (compilationResult.Status != SpeechRecognitionResultStatus.Success)
                {
                    // Disable the recognition buttons.

                    // Let the user know that the grammar didn't compile properly.
                    resultTextBlock.Visibility = Visibility.Visible;
                    resultTextBlock.Text = "Unable to compile grammar.";
                }
                else
                {

                    resultTextBlock.Visibility = Visibility.Collapsed;
                }
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

            });
        }

        /// <summary>
        /// Uses the recognizer constructed earlier to listen for speech from the user before displaying 
        /// it back on the screen. Uses the built-in speech recognition UI.
        /// </summary>
        /// <param name="sender">Button that triggered this event</param>
        /// <param name="e">State information about the routed event</param>
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
                numberCaptured = 99;
                heardYouSayTextBlock.Visibility = resultTextBlock.Visibility = Visibility.Collapsed;
                // Disable the UI while recognition is occurring, and provide feedback to the user about current state.
                cbLanguageSelection.IsEnabled = false;
                // Start recognition.
                try
                {
                    // Save the recognition operation so we can cancel it (as it does not provide a blocking
                    // UI, unlike RecognizeWithAsync()
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
                        Debug.WriteLine("HELLO");
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


            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
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
                #region LED
                if ((textboxContent.Contains("LED") || textboxContent.Contains("led")) && ledCaptured != true)
                {
                    Debug.WriteLine("LED harrapatua!");
                    ledCaptured = true;
                }
                if (colorCaptured == null)
                {
                    foreach (string color in colorlist)
                    {
                        if (textboxContent.Contains(color))
                        {
                            Debug.WriteLine(color);
                            colorCaptured = color;
                        }
                    }
                }
                if (numberCaptured == 99)
                {
                    foreach (string number in numberlist)
                    {
                        if (textboxContent.Contains(number))
                        {
                            Debug.WriteLine("kopurua harrapatua!");
                            Debug.WriteLine(number);
                            int.TryParse(number, out numberCaptured);
                        }
                    }
                }

                if (ledCaptured == true && colorCaptured != null && numberCaptured != 99)
                {
                    resultTextBlock.Text = "Die LED" + numberCaptured.ToString() + " ist jetzt " + colorCaptured.ToString();
                    helpTextBlock.Text = "Die LED" + numberCaptured.ToString() + " ist jetzt " + colorCaptured.ToString();
                    Debug.WriteLine($"LED: {ledCaptured}, Color: {colorCaptured}, Number: {numberCaptured}");
                }
                #endregion


                Debug.WriteLine("helloWorld");
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

        private void StopRecognicing(object sender, RoutedEventArgs e)
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
                Debug.WriteLine("Salbuespen errorea: aitortu ez hasi");
            }
        }
    }
}