using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.VoiceCommands;
using Windows.Storage;
using Windows.Web.Http;

namespace FHEMApp.VoiceCommands
{
    public sealed class FHEMAppVoiceCommandService : IBackgroundTask
    {
        VoiceCommandServiceConnection voiceServiceConnection;
        BackgroundTaskDeferral serviceDeferral;
        StorageFile destinationTileImage = null;

        string voiceCommandName, textSpoken, feedback1, feedback2;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            //destinationTileImage = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///FHEMApp.VoiceCommands/Images/GreyTile.png"));
            this.serviceDeferral = taskInstance.GetDeferral();
            taskInstance.Canceled += OnTaskCanceled;
            var triggerDetails = taskInstance.TriggerDetails as AppServiceTriggerDetails;

            if (triggerDetails != null && triggerDetails.Name == "FHEMAppVoiceCommandService")
            {
                try
                {
                    voiceServiceConnection = VoiceCommandServiceConnection.FromAppServiceTriggerDetails(triggerDetails);
                    voiceServiceConnection.VoiceCommandCompleted += VoiceCommandCompleted;
                    VoiceCommand voiceCommand = await voiceServiceConnection.GetVoiceCommandAsync();

                    //textSpoken = voiceCommand.SpeechRecognitionResult.Text;
                    
                    switch (voiceCommand.CommandName)
                    {
                        case "arms":
                            var destination = voiceCommand.Properties["direction"][0];
                            await Speech_contains_destination(destination);
                            break;
                        case "arms_without_specification":
                            var part = voiceCommand.Properties["bodypart"][0];
                            await Speech_not_contains_direction(part);
                            break;

                    }
                }
                catch(Exception ex)
                {
                    var test = ex.Message;
                }
            }
        }

        /// <summary>
        /// Methode welche uns einen Arm vorschlägt welcher bestätigt oder abgelehnt werden kann, wenn abgelehnt dann wird anderer Arm vorgeschlagen
        /// </summary>
        /// <param name="bodypart">beinhaltet "arm"</param>
        /// <returns></returns>
        private async Task Speech_not_contains_direction(string bodypart)
        {
            //Cortana fragt welcher Arm gehoben werden soll? Sie schlägt uns einen vor.
            var userPrompt = new VoiceCommandUserMessage();
            userPrompt.DisplayMessage = userPrompt.SpokenMessage = $"Welchen {bodypart} soll ich heben? Ich schlage den linken vor.";

            //Nach zu langer Wartezeit wird noch einmal nachgefragt.
            var userReprompt = new VoiceCommandUserMessage();
            userReprompt.DisplayMessage = userReprompt.SpokenMessage = $"Hallo! Welchen {bodypart} soll ich nun heben, wie gesagt mein Vorschlag wäre der linke.";

            //Antwort wird erwartet und überprüft
            var response = VoiceCommandResponse.CreateResponseForPrompt(userPrompt, userReprompt);
            var voiceCommandConfirmation = await voiceServiceConnection.RequestConfirmationAsync(response);
            Debug.WriteLine(voiceCommandConfirmation.Confirmed.ToString());


            if (voiceCommandConfirmation != null)
            {
                //Prüfen wie die Antwort auf die Frage ist.
                if (voiceCommandConfirmation.Confirmed == true)
                {
                    //Linker Arm ist bestätigt worden

                    var userMessage = new VoiceCommandUserMessage();
                    userMessage.DisplayMessage = userMessage.SpokenMessage = $"Alles klar, ich hebe den linken {bodypart}.";
                    response = VoiceCommandResponse.CreateResponse(userMessage);
                    await voiceServiceConnection.ReportSuccessAsync(response);
/*
                             +++ TODO +++ 
                      Befehl für Servos ergänzen    
                Servos ansteuern und Bewegung ausführen
*/

                }
                else
                {
                    //Es soll der Rechte Arm gewählt werden
                    var new_userPrompt = new VoiceCommandUserMessage();
                    new_userPrompt.DisplayMessage = new_userPrompt.SpokenMessage = $"Soll ich dann den rechten {bodypart} heben?";

                    //Nachfrage wenn zu lange gezögert
                    var new_userReprompt = new VoiceCommandUserMessage();
                    new_userReprompt.DisplayMessage = new_userReprompt.SpokenMessage = $"Hallo! Soll ich nun den rechten {bodypart} heben?.";

                    var new_response = VoiceCommandResponse.CreateResponseForPrompt(new_userPrompt, new_userReprompt);

                    var new_voiceCommandConfirmation = await voiceServiceConnection.RequestConfirmationAsync(new_response);
                    Debug.WriteLine(new_voiceCommandConfirmation.Confirmed.ToString());
                    if (new_voiceCommandConfirmation != null)
                    {
                        if (new_voiceCommandConfirmation.Confirmed == true)
                        {
                            //Rechter Arm wird bestätigt
                            var new_userMessage = new VoiceCommandUserMessage();
                            new_userMessage.DisplayMessage = new_userMessage.SpokenMessage = $"Alles klar, ich hebe den rechten {bodypart}.";
                            new_response = VoiceCommandResponse.CreateResponse(new_userMessage);
                            await voiceServiceConnection.ReportSuccessAsync(new_response);
                        }
                        else
                        {
                            //Abbruch weil nicht erfordert.
                            Debug.WriteLine(007);
                            var decline_userMessage = new VoiceCommandUserMessage();
                            decline_userMessage.DisplayMessage = decline_userMessage.SpokenMessage = "Gut dann lasse ich´s halt.";

                            new_response = VoiceCommandResponse.CreateResponse(decline_userMessage);
                            await voiceServiceConnection.ReportSuccessAsync(new_response);
                        }
                    }
                }
            }
        }

        private async Task Speech_contains_destination(string destination)
        {
            var userPrompt = new VoiceCommandUserMessage();
            userPrompt.DisplayMessage = userPrompt.SpokenMessage = $"Sind Sie sicher das ich den {destination} Arm heben soll?";

            // Wird noch nicht angezeigt...
            var userReprompt = new VoiceCommandUserMessage();
            userReprompt.DisplayMessage = userReprompt.SpokenMessage = $"Hallo, soll ich nun den {destination} Arm heben, oder nicht?";

            var response = VoiceCommandResponse.CreateResponseForPrompt(userPrompt, userReprompt);

            var voiceCommandConfirmation = await voiceServiceConnection.RequestConfirmationAsync(response);
            Debug.WriteLine(voiceCommandConfirmation.Confirmed.ToString());
            if (voiceCommandConfirmation != null)
            {
                if (voiceCommandConfirmation.Confirmed == true)
                {
                    //await ShowProgressScreen($"Was willst Du?");

                    var userMessage = new VoiceCommandUserMessage();
                    userMessage.DisplayMessage = userMessage.SpokenMessage = $"Alles klar, ich hebe den {destination} Arm.";
                    response = VoiceCommandResponse.CreateResponse(userMessage);
                    await voiceServiceConnection.ReportSuccessAsync(response);

                    //Befehl Servos
                }
                else
                {
                    var userMessage = new VoiceCommandUserMessage();
                    userMessage.DisplayMessage = userMessage.SpokenMessage = "Na dann eben nicht!";

                    response = VoiceCommandResponse.CreateResponse(userMessage);
                    await voiceServiceConnection.ReportSuccessAsync(response);
                }
            }
        }

        private async Task ShowProgressScreen(string message)
        {
            var userProgressMessage = new VoiceCommandUserMessage();
            userProgressMessage.DisplayMessage = userProgressMessage.SpokenMessage = message;

            VoiceCommandResponse response = VoiceCommandResponse.CreateResponse(userProgressMessage);
            await voiceServiceConnection.ReportProgressAsync(response);
        }

        private void VoiceCommandCompleted(VoiceCommandServiceConnection sender, VoiceCommandCompletedEventArgs args)
        {
            //Debug.WriteLine("sender: "+sender.ToString()+"args:"+args.ToString());
        }

        private async void LaunchAppInForeground()
        {
            Debug.WriteLine("LaunchAppInForeground");
            var userMessage = new VoiceCommandUserMessage();
            userMessage.SpokenMessage = "Starte FHEMApp";

            var response = VoiceCommandResponse.CreateResponse(userMessage);

            // When launching the app in the foreground, pass an app 
            // specific launch parameter to indicate what page to show.
            //response.AppLaunchArgument = "showAllTrips=true";

            await voiceServiceConnection.RequestAppLaunchAsync(response);
        }

        /// <summary>
        /// When the background task is cancelled, clean up/cancel any ongoing long-running operations.
        /// This cancellation notice may not be due to Cortana directly. The voice command connection will
        /// typically already be destroyed by this point and should not be expected to be active.
        /// </summary>
        /// <param name="sender">This background task instance</param>
        /// <param name="reason">Contains an enumeration with the reason for task cancellation</param>
        private void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            Debug.WriteLine("OnTaskCanceled");
            System.Diagnostics.Debug.WriteLine("Task cancelled, clean up");
            if (this.serviceDeferral != null)
            {
                //Complete the service deferral
                this.serviceDeferral.Complete();
            }
        }
    }
}
