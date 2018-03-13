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
                            var handside = voiceCommand.Properties["handside"][0];
                            await SpeechContainsHandside(handside);
                            break;
                        case "arms_without_handside":
                            var bodypart = voiceCommand.Properties["bodypart"][0];
                            await DisambiguateHandsides(bodypart);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    var test = ex.Message;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task<string> DisambiguateHandsides(string bodypart)
        {
            string[] handsides = new string[] { "rechter", "linker" };
            var userPrompt = new VoiceCommandUserMessage();

            userPrompt.DisplayMessage = userPrompt.SpokenMessage = $"Welchen {bodypart} soll ich heben? {handsides[0]} oder {handsides[1]} {bodypart}?";
            var userReprompt = new VoiceCommandUserMessage();

            userReprompt.DisplayMessage = userReprompt.SpokenMessage = $"Könntest Du dich für einen {bodypart} entscheiden.";
            var destinationContentTiles = new List<VoiceCommandContentTile>();

            foreach (string sides in handsides)
            {
                var destinationTile = new VoiceCommandContentTile();
                
                destinationTile.AppContext = $"{sides}";
                destinationTile.Title = $"{sides} {bodypart}";
                //destinationTile.TextLine1 = $"{sides} Description";

                destinationContentTiles.Add(destinationTile);
            }
            var response = VoiceCommandResponse.CreateResponseForPrompt(userPrompt, userReprompt, destinationContentTiles);

            var voiceCommandDisambiguationResult = await voiceServiceConnection.RequestDisambiguationAsync(response);
            if (voiceCommandDisambiguationResult != null)
            {
                var userMessage = new VoiceCommandUserMessage();
                userMessage.DisplayMessage = userMessage.SpokenMessage = $"Alles klar, {voiceCommandDisambiguationResult.SelectedItem.AppContext} {bodypart} wird gehoben.";
                response = VoiceCommandResponse.CreateResponse(userMessage);
                await voiceServiceConnection.ReportSuccessAsync(response);
            }
            return String.Empty;
        }

        private async Task SpeechContainsHandside(string destination)
        {
            var userPrompt = new VoiceCommandUserMessage();
            userPrompt.DisplayMessage = userPrompt.SpokenMessage = $"Sind Sie sicher das ich den {destination} Arm heben soll?";

            // Wird noch nicht angezeigt...
            var userReprompt = new VoiceCommandUserMessage();
            userReprompt.DisplayMessage = userReprompt.SpokenMessage = $"Hallo, soll ich nun den {destination} Arm heben, oder nicht?";

            var response = VoiceCommandResponse.CreateResponseForPrompt(userPrompt, userReprompt);

            var voiceCommandConfirmation = await voiceServiceConnection.RequestConfirmationAsync(response);
            if (voiceCommandConfirmation != null)
            {
                if (voiceCommandConfirmation.Confirmed == true)
                {
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
