using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.ApplicationInsights;

namespace InMoov
{
    using BodyParts;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.Maker.Firmata;
    using Microsoft.Maker.RemoteWiring;
    using Microsoft.Maker.Serial;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Views;
    using Windows.Media.SpeechRecognition;
    using Windows.Storage;
    using Windows.UI;
    using Windows.UI.ViewManagement;
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        public static int readyDevices = 0;
        public static int noDevice = 0;

        public static Dictionary<string, Arduino> Arduinos = new Dictionary<string, Arduino>();
        public static Dictionary<string, BodyPart> BodyParts = new Dictionary<string, BodyPart>();

        public static Arduino ARechts   { get; set; }
        public static Arduino ALinks    { get; set; }
        public static Arduino Leonardo  { get; set; }

        public static NeoPixel neopixel { get; set; }

        public static bool ArduinosReady()
        {
            if (readyDevices == App.Arduinos.Values.Count)
            {
                foreach (Arduino arduino in App.Arduinos.Values)
                {
                    if (arduino.id.Substring(26,20) == "756303137363513071D1" || arduino.id.Substring(26, 20) == "55639303834351D0F191" || arduino.id.Substring(26, 20) == "85539313931351C09082" || arduino.id.Substring(26, 20) == "95530343634351901162" || arduino.id.Substring(26, 20) == "955303430353518062E0")   
                    {
                        App.Leonardo = arduino;
                        Debug.WriteLine("Leonardo wurde das gerät " + arduino.name + " zugeteilt!");
                    }
                    else if (arduino.id.Substring(26, 20) == "75533353038351313212")
                    {
                        App.ARechts = arduino;
                        Debug.WriteLine("ARechts wurde das gerät " + arduino.name + " zugeteilt!");

                    }
                    else if (arduino.id.Substring(26, 20) == "85531303231351812120")
                    {
                        App.ALinks = arduino;
                        Debug.WriteLine("ALinks wurde das gerät " + arduino.name + " zugeteilt!");

                    }
                }
                //Alles zugeteilt, Roboter kann aufwachen.
                ReadyDevices();
                return true;
            }
            else return false;
        }
        public static async void ReadyDevices()
        {
            try
            {
                await PairDevices();
            }
            catch
            {
                Debug.WriteLine("Arduinos sind nicht instanziert");
            }
        }

        private static async Task<bool> PairDevices()
        {
            bool succeeded = false;
            while (!succeeded)
            {
                if (Leonardo.ready == true)
                {
                    Views.LedRingPage.InitializeNeoPixel();
                    succeeded = true;
                }
            }
            return succeeded;
        }

        public static async Task<bool> turnConnected()
        {
            bool succeeded = false;
            while (!succeeded)
            {
                for (byte pixel = 0; pixel < 6 * readyDevices; pixel++)
                {
                    neopixel.SetPixelColor(pixel, 0, 100, 0);
                    await Task.Delay(100);
                }
                for (byte pixel = byte.Parse((readyDevices * 6).ToString()); pixel < 16; pixel++)
                {
                    neopixel.SetPixelColor(pixel, 100, 0, 0);
                    await Task.Delay(100);
                }
                if (readyDevices == 3)
                {
                    await Task.Delay(2000);
                    for (byte i = 0; i < 16; i++)
                    {
                        neopixel.SetPixelColor(i, 0, 0, 0);
                        await Task.Delay(100);
                    }
                    await Views.ConnectPage.Startup();
                    succeeded = true;
                }}
            return succeeded;
        }

        public static void InitializeBodyParts()
        {
            BodyParts.Add("Rechten Arm", new BodyPart(ARechts, 8, 0, 70, 0));
            BodyParts.Add("Linken Arm", new BodyPart(ALinks, 8, 0, 70, 0));
        }

        public static IStream Connection
        {
            get;
            set;
        }

        public static UwpFirmata Firmata
        {
            get;
            set;
        }

        public static RemoteDevice Arduino
        {
            get;
            set;
        }

        public static TelemetryClient Telemetry
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;

            Telemetry = new TelemetryClient();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected async override void OnLaunched(LaunchActivatedEventArgs e)
        {
            //NEU
            Frame rootFrame = Window.Current.Content as Frame;

            // App-Initialisierung nicht wiederholen, wenn das Fenster bereits Inhalte enthält.
            // Nur sicherstellen, dass das Fenster aktiv ist.
            if (rootFrame == null)
            {
                // Frame erstellen, der als Navigationskontext fungiert und zum Parameter der ersten Seite navigieren
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Zustand von zuvor angehaltener Anwendung laden
                }

                // Den Frame im aktuellen Fenster platzieren
                Window.Current.Content = rootFrame;

                try
                {
                    // Install the main VCD. Since there's no simple way to test that the VCD has been imported, or that it's your most recent
                    // version, it's not unreasonable to do this upon app load.
                    StorageFile vcdStorageFile = await Package.Current.InstalledLocation.GetFileAsync(@"CortanaCommands.xml");
                    Debug.WriteLine(vcdStorageFile.Path);
                    await Windows.ApplicationModel.VoiceCommands.VoiceCommandDefinitionManager.InstallCommandDefinitionsFromStorageFileAsync(vcdStorageFile);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Installing Voice Commands Failed: " + ex.ToString());
                }
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // Wenn der Navigationsstapel nicht wiederhergestellt wird, zur ersten Seite navigieren
                    // und die neue Seite konfigurieren, indem die erforderlichen Informationen als Navigationsparameter
                    // übergeben werden
                    rootFrame.Navigate(typeof(AppShell), e.Arguments);
                }
                // Sicherstellen, dass das aktuelle Fenster aktiv ist
                Window.Current.Activate();
            }
            //[ENDE]

#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // This just gets in the way.
                //this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif
            // Change minimum window size
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(320, 200));

            // Darken the window title bar using a color value to match app theme
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            if (titleBar != null)
            {
                Color titleBarColor = (Color)App.Current.Resources["SystemChromeMediumColor"];
                titleBar.BackgroundColor = titleBarColor;
                titleBar.ButtonBackgroundColor = titleBarColor;
            }

            if (SystemInformationHelpers.IsTenFootExperience)
            {
                // Apply guidance from https://msdn.microsoft.com/windows/uwp/input-and-devices/designing-for-tv
                ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);

                this.Resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri("ms-appx:///Styles/TenFootStylesheet.xaml")
                });
            }

            AppShell shell = Window.Current.Content as AppShell;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (shell == null)
            {
                // Create a AppShell to act as the navigation context and navigate to the first page
                shell = new AppShell();

                // Set the default language
                shell.Language = Windows.Globalization.ApplicationLanguages.Languages[0];

                shell.AppFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }
            }

            // Place our app shell in the current Window
            Window.Current.Content = shell;

            if (shell.AppFrame.Content == null)
            {
                // When the navigation stack isn't restored, navigate to the first page
                // suppressing the initial entrance animation.
                shell.AppFrame.Navigate(typeof(LandingPage), e.Arguments, new Windows.UI.Xaml.Media.Animation.SuppressNavigationTransitionInfo());
            }

            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        /// <summary>
        /// Returns the semantic interpretation of a speech result. Returns null if there is no interpretation for
        /// that key.
        /// </summary>
        /// <param name="interpretationKey">The interpretation key.</param>
        /// <param name="speechRecognitionResult">The result to get an interpretation from.</param>
        /// <returns></returns>
        private string SemanticInterpretation(string interpretationKey, SpeechRecognitionResult speechRecognitionResult)
        {
            return speechRecognitionResult.SemanticInterpretation.Properties[interpretationKey].FirstOrDefault();
        }
    }
}
