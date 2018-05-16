﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

        private SpeechRecognizer speechRecognizer;
        private ResourceContext speechContext;
        private ResourceMap speechResourceMap;
        private bool isPopulatingLanguages = false;
        private IAsyncOperation<SpeechRecognitionResult> recognitionOperation;

        public SpeechPage()
        {
            this.InitializeComponent();
            this.Loaded += SpeechPage_Loaded;
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
                btnRecognizeWithUI.IsEnabled = true;
                btnRecognizeWithoutUI.IsEnabled = true;

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
                btnRecognizeWithUI.IsEnabled = false;
                btnRecognizeWithoutUI.IsEnabled = false;
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
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (speechRecognizer != null)
            {
                if (speechRecognizer.State != SpeechRecognizerState.Idle)
                {
                    if (recognitionOperation != null)
                    {
                        recognitionOperation.Cancel();
                        recognitionOperation = null;
                    }
                }

                speechRecognizer.StateChanged -= SpeechRecognizer_StateChanged;

                this.speechRecognizer.Dispose();
                this.speechRecognizer = null;
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

                this.speechRecognizer.Dispose();
                this.speechRecognizer = null;
            }
            try
            {
                // Create an instance of SpeechRecognizer.
                speechRecognizer = new SpeechRecognizer(recognizerLanguage);

                // Provide feedback to the user about the state of the recognizer.
                speechRecognizer.StateChanged += SpeechRecognizer_StateChanged;

                // Add a list constraint to the recognizer.
                speechRecognizer.Constraints.Add(
                    new SpeechRecognitionListConstraint(
                        new List<string>()
                        {
                        speechResourceMap.GetValue("ListGrammarRobotName1", speechContext).ValueAsString,
                        speechResourceMap.GetValue("ListGrammarRobotName2", speechContext).ValueAsString,
                        speechResourceMap.GetValue("ListGrammarRobotName3", speechContext).ValueAsString
                        }, "RobotName"));
                speechRecognizer.Constraints.Add(
                    new SpeechRecognitionListConstraint(
                        new List<string>()
                        {
                        speechResourceMap.GetValue("Gesicht1", speechContext).ValueAsString,
                        speechResourceMap.GetValue("Gesicht2", speechContext).ValueAsString,
                        speechResourceMap.GetValue("Gesicht3", speechContext).ValueAsString
                        }, "GesichtStart"));
                speechRecognizer.Constraints.Add(
                    new SpeechRecognitionListConstraint(
                        new List<string>()
                        {
                        speechResourceMap.GetValue("GesichtStop1", speechContext).ValueAsString,
                        speechResourceMap.GetValue("GesichtStop2", speechContext).ValueAsString,
                        speechResourceMap.GetValue("GesichtStop3", speechContext).ValueAsString
                        }, "GesichtStop"));
                speechRecognizer.Constraints.Add(
                    new SpeechRecognitionListConstraint(
                        new List<string>()
                        {
                        speechResourceMap.GetValue("ListGrammarGoToContosoStudio", speechContext).ValueAsString
                        }, "GoToContosoStudio"));
                speechRecognizer.Constraints.Add(
                    new SpeechRecognitionListConstraint(
                        new List<string>()
                        {
                        speechResourceMap.GetValue("ListGrammarShowMessage", speechContext).ValueAsString,
                        speechResourceMap.GetValue("ListGrammarOpenMessage", speechContext).ValueAsString
                        }, "Message"));
                speechRecognizer.Constraints.Add(
                    new SpeechRecognitionListConstraint(
                        new List<string>()
                        {
                        speechResourceMap.GetValue("ListGrammarSendEmail", speechContext).ValueAsString,
                        speechResourceMap.GetValue("ListGrammarCreateEmail", speechContext).ValueAsString
                        }, "Email"));
                speechRecognizer.Constraints.Add(
                    new SpeechRecognitionListConstraint(
                        new List<string>()
                        {
                        speechResourceMap.GetValue("ListGrammarCallNitaFarley", speechContext).ValueAsString,
                        speechResourceMap.GetValue("ListGrammarCallNita", speechContext).ValueAsString
                        }, "CallNita"));
                speechRecognizer.Constraints.Add(
                    new SpeechRecognitionListConstraint(
                        new List<string>()
                        {
                        speechResourceMap.GetValue("ListGrammarCallWayneSigmon", speechContext).ValueAsString,
                        speechResourceMap.GetValue("ListGrammarCallWayne", speechContext).ValueAsString
                        }, "CallWayne"));

                // RecognizeWithUIAsync allows developers to customize the prompts.
                string uiOptionsText = string.Format("Try saying '{0}', '{1}' or '{2}'",
                    speechResourceMap.GetValue("ListGrammarRobotName3", speechContext).ValueAsString,
                    speechResourceMap.GetValue("ListGrammarGoToContosoStudio", speechContext).ValueAsString,
                    speechResourceMap.GetValue("ListGrammarShowMessage", speechContext).ValueAsString);
                speechRecognizer.UIOptions.ExampleText = uiOptionsText;
                helpTextBlock.Text = string.Format("{0}\n{1}",
                    speechResourceMap.GetValue("ListGrammarHelpText", speechContext).ValueAsString,
                    uiOptionsText);

                // Compile the constraint.
                SpeechRecognitionCompilationResult compilationResult = await speechRecognizer.CompileConstraintsAsync();

                // Check to make sure that the constraints were in a proper format and the recognizer was able to compile it.
                if (compilationResult.Status != SpeechRecognitionResultStatus.Success)
                {
                    // Disable the recognition buttons.
                    btnRecognizeWithUI.IsEnabled = false;
                    btnRecognizeWithoutUI.IsEnabled = false;

                    // Let the user know that the grammar didn't compile properly.
                    resultTextBlock.Visibility = Visibility.Visible;
                    resultTextBlock.Text = "Unable to compile grammar.";
                }
                else
                {
                    btnRecognizeWithUI.IsEnabled = true;
                    btnRecognizeWithoutUI.IsEnabled = true;

                    resultTextBlock.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == HResultRecognizerNotFound)
                {
                    btnRecognizeWithUI.IsEnabled = false;
                    btnRecognizeWithoutUI.IsEnabled = false;

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
                //Wenn Fehler behoben -> App funktioniert
                //MainPage.Current.NotifyUser("Speech recognizer state: " + args.State.ToString(), NotifyType.StatusMessage);
            });
        }

        /// <summary>
        /// Uses the recognizer constructed earlier to listen for speech from the user before displaying 
        /// it back on the screen. Uses the built-in speech recognition UI.
        /// </summary>
        /// <param name="sender">Button that triggered this event</param>
        /// <param name="e">State information about the routed event</param>
        FacesPage fp = new FacesPage();
        private async void RecognizeWithUIListConstraint_Click(object sender, RoutedEventArgs e)
        {
            heardYouSayTextBlock.Visibility = resultTextBlock.Visibility = Visibility.Collapsed;

            // Start recognition.
            try
            {
                recognitionOperation = speechRecognizer.RecognizeWithUIAsync();

                SpeechRecognitionResult speechRecognitionResult = await recognitionOperation;
                // If successful, display the recognition result.
                if (speechRecognitionResult.Status == SpeechRecognitionResultStatus.Success)
                {
                    string tag = "unknown";
                    if (speechRecognitionResult.Constraint != null)
                    {
                        // Only attempt to retreive the tag if we didn't hit the garbage rule.
                        tag = speechRecognitionResult.Constraint.Tag;
                    }

                    heardYouSayTextBlock.Visibility = resultTextBlock.Visibility = Visibility.Visible;
                    /*
                    * Hier kann weitergearbeitet werden
                    * ++++++++++++++++++++++++++++++++++++++
                    */

                    if (tag == "RobotName")
                    {
                        heardYouSayTextBlock.Text = "Hallo ich bin InMoov, der humanoide Roboter des Volkswagen Bildungsinstitutes";
                        resultTextBlock.Text = string.Format("Heard: '{0}', (Tag: '{1}', Confidence: {2})", speechRecognitionResult.Text, tag, speechRecognitionResult.Confidence.ToString());

                    }
                    else if (tag == "GesichtStart")
                    {
                        string name = fp.nameface_voice;
                        heardYouSayTextBlock.Text = "Starte Gesichtserkennung";
                        resultTextBlock.Text = string.Format("Heard: '{0}', (Tag: '{1}', Confidence: {2})", speechRecognitionResult.Text, tag, speechRecognitionResult.Confidence.ToString());
                    }
                    else if (tag == "GesichtStop")
                    {
                        heardYouSayTextBlock.Text = "Stope Gesichtserkennung";
                        resultTextBlock.Text = string.Format("Heard: '{0}', (Tag: '{1}', Confidence: {2})", speechRecognitionResult.Text, tag, speechRecognitionResult.Confidence.ToString());
                    }
                    else
                    {
                        heardYouSayTextBlock.Text = "Heard you say";
                        resultTextBlock.Text = string.Format("Heard: '{0}', (Tag: '{1}', Confidence: {2})", speechRecognitionResult.Text, tag, speechRecognitionResult.Confidence.ToString());
                    }

                    /*
                     * ++++++++++++++++++++++++++++++++++++++
                     */
                }
                else
                {
                    resultTextBlock.Visibility = Visibility.Visible;
                    resultTextBlock.Text = string.Format("Speech Recognition Failed, Status: {0}", speechRecognitionResult.Status.ToString());
                }
            }
            catch (TaskCanceledException exception)
            {
                // TaskCanceledException will be thrown if you exit the scenario while the recognizer is actively
                // processing speech. Since this happens here when we navigate out of the scenario, don't try to 
                // show a message dialog for this exception.
                System.Diagnostics.Debug.WriteLine("TaskCanceledException caught while recognition in progress (can be ignored):");
                System.Diagnostics.Debug.WriteLine(exception.ToString());
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
        }

        /// <summary>
        /// Uses the recognizer constructed earlier to listen for speech from the user before displaying 
        /// it back on the screen. Uses developer-provided UI for user feedback.
        /// </summary>
        /// <param name="sender">Button that triggered this event</param>
        /// <param name="e">State information about the routed event</param>
        private async void RecognizeWithoutUIListConstraint_Click(object sender, RoutedEventArgs e)
        {
            heardYouSayTextBlock.Visibility = resultTextBlock.Visibility = Visibility.Collapsed;

            // Disable the UI while recognition is occurring, and provide feedback to the user about current state.
            btnRecognizeWithUI.IsEnabled = false;
            btnRecognizeWithoutUI.IsEnabled = false;
            cbLanguageSelection.IsEnabled = false;
            listenWithoutUIButtonText.Text = " listening for speech...";

            // Start recognition.
            try
            {
                // Save the recognition operation so we can cancel it (as it does not provide a blocking
                // UI, unlike RecognizeWithAsync()
                recognitionOperation = speechRecognizer.RecognizeAsync();

                SpeechRecognitionResult speechRecognitionResult = await recognitionOperation;

                // If successful, display the recognition result. A cancelled task should do nothing.
                if (speechRecognitionResult.Status == SpeechRecognitionResultStatus.Success)
                {
                    string tag = "unknown";
                    if (speechRecognitionResult.Constraint != null)
                    {
                        // Only attempt to retreive the tag if we didn't hit the garbage rule.
                        tag = speechRecognitionResult.Constraint.Tag;
                    }

                    heardYouSayTextBlock.Visibility = resultTextBlock.Visibility = Visibility.Visible;

                    /*
                     * +++++++++++++++NEU+++++++++++++++
                     */

                    if (tag == "RobotName")
                    {
                        heardYouSayTextBlock.Text = "Hallo ich bin InMoov, der humanoide Roboter des Volkswagen Bildungsinstitutes";
                        resultTextBlock.Text = string.Format("Heard: '{0}', (Tag: '{1}', Confidence: {2})", speechRecognitionResult.Text, tag, speechRecognitionResult.Confidence.ToString());
                    }
                    else if (tag == "GesichtStart")
                    {
                        string name = fp.nameface_voice;
                        heardYouSayTextBlock.Text = "Starte Gesichtserkennung";
                        resultTextBlock.Text = string.Format("Heard: '{0}', (Tag: '{1}', Confidence: {2})", speechRecognitionResult.Text, tag, speechRecognitionResult.Confidence.ToString());
                    }
                    else if (tag == "GesichtStop")
                    {
                        heardYouSayTextBlock.Text = "Stope Gesichtserkennung";
                        resultTextBlock.Text = string.Format("Heard: '{0}', (Tag: '{1}', Confidence: {2})", speechRecognitionResult.Text, tag, speechRecognitionResult.Confidence.ToString());
                    }
                    else
                    {
                        heardYouSayTextBlock.Text = "Heard you say";
                        resultTextBlock.Text = string.Format("Heard: '{0}', (Tag: '{1}', Confidence: {2})", speechRecognitionResult.Text, tag, speechRecognitionResult.Confidence.ToString());
                    }

                    /*
                     * +++++++++++++++++++++++++++++++++
                     */
                }
                else
                {
                    resultTextBlock.Visibility = Visibility.Visible;
                    resultTextBlock.Text = string.Format("Speech Recognition Failed, Status: {0}", speechRecognitionResult.Status.ToString());
                }
            }
            catch (TaskCanceledException exception)
            {
                // TaskCanceledException will be thrown if you exit the scenario while the recognizer is actively
                // processing speech. Since this happens here when we navigate out of the scenario, don't try to 
                // show a message dialog for this exception.
                System.Diagnostics.Debug.WriteLine("TaskCanceledException caught while recognition in progress (can be ignored):");
                System.Diagnostics.Debug.WriteLine(exception.ToString());
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
            listenWithoutUIButtonText.Text = " without UI";
            cbLanguageSelection.IsEnabled = true;
            btnRecognizeWithUI.IsEnabled = true;
            btnRecognizeWithoutUI.IsEnabled = true;
        }
    }
}