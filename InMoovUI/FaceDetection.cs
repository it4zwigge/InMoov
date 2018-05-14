//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;

using Windows.Media;
using Windows.Graphics.Imaging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.FaceAnalysis;
using Windows.Media.MediaProperties;
using Windows.System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.ProjectOxford.Common.Contract;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Storage.Pickers;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Storage;
using System.Linq;
using Windows.Media.Core;
using VWFIANCognitveServices;
using System.IO;

namespace VWFIANCognitveServices
{
    public sealed partial class TrackFacesInWebcam : Page
    {

        //SoftwareBitmap convertedSource = null;
        FaceServiceClient faceServiceClient = new FaceServiceClient(Keys.FaceServiceKey, Keys.FaceAPI_rootstring);
        private Person[] _personList;

        string personGroupId = "VW";//"adventure_works_group3";
        TimeSpan timerInterval;

        //private readonly SolidColorBrush lineBrush = new SolidColorBrush(Windows.UI.Colors.Yellow);
        //private readonly double lineThickness = 2.0;
        //private readonly SolidColorBrush fillBrush = new SolidColorBrush(Windows.UI.Colors.Transparent);
        //private ScenarioState currentState;
        private MediaCapture mediaCapture;
        private VideoEncodingProperties videoProperties;
        private FaceTracker faceTracker;
        private ThreadPoolTimer frameProcessingTimer;
        private SemaphoreSlim frameProcessingSemaphore = new SemaphoreSlim(1);


        public TrackFacesInWebcam()
        {
        }

        public async void OnNavigatedTo(NavigationEventArgs e)
        {
            await StartWebcamStreaming();
            if (this.faceTracker == null)
                faceTracker = await FaceTracker.CreateAsync();
        }

        private async Task<bool> StartWebcamStreaming()
        {
            bool successful = true;

            try
            {
                this.mediaCapture = new MediaCapture();
                var captureElement = new CaptureElement();

                MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();
                settings.StreamingCaptureMode = StreamingCaptureMode.Video;
                await this.mediaCapture.InitializeAsync(settings);
                //this.mediaCapture.Failed += this.MediaCapture_CameraStreamFailed;

                var deviceController = this.mediaCapture.VideoDeviceController;
                this.videoProperties = deviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;
                captureElement.Source = mediaCapture;
                await this.mediaCapture.StartPreviewAsync();

                timerInterval = TimeSpan.FromMilliseconds(5000);
                this.frameProcessingTimer = Windows.System.Threading.ThreadPoolTimer.CreatePeriodicTimer(new Windows.System.Threading.TimerElapsedHandler(ProcessCurrentVideoFrame), timerInterval);
            }
            catch (System.UnauthorizedAccessException)
            {
                successful = false;
            }
            catch (Exception ex)
            {
                successful = false;
            }

            return successful;
        }

        private async void ShutdownWebCam()
        {
            if (this.frameProcessingTimer != null)
            {
                this.frameProcessingTimer.Cancel();
            }

            if (this.mediaCapture != null)
            {
                if (this.mediaCapture.CameraStreamState == Windows.Media.Devices.CameraStreamState.Streaming)
                {
                    try
                    {
                        await this.mediaCapture.StopPreviewAsync();
                    }
                    catch (Exception)
                    {
                        ;   // Since we're going to destroy the MediaCapture object there's nothing to do here
                    }
                }
                this.mediaCapture.Dispose();
            }

            this.frameProcessingTimer = null;
            this.mediaCapture = null;

        }                                       // Kamera ausschalten

        private async void ProcessCurrentVideoFrame(ThreadPoolTimer timer)
        {
            try
            {
                IList<DetectedFace> faces = null;

                const BitmapPixelFormat InputPixelFormat = BitmapPixelFormat.Nv12;
                using (VideoFrame previewFrame = new VideoFrame(InputPixelFormat, 1280, 720))
                {
                    await this.mediaCapture.GetPreviewFrameAsync(previewFrame);

                    // The returned VideoFrame should be in the supported NV12 format but we need to verify this.
                    if (FaceDetector.IsBitmapPixelFormatSupported(previewFrame.SoftwareBitmap.BitmapPixelFormat))
                    {
                        faces = await this.faceTracker.ProcessNextFrameAsync(previewFrame);
                    }
                    else
                    {
                        throw new System.NotSupportedException("PixelFormat '" + InputPixelFormat.ToString() + "' is not supported by FaceDetector");
                    }

                    if (faces.Count > 0)
                    {
                        FaceDetect(previewFrame);
                    }

                }
            }
            catch (Exception ex)
            {
               
            }
            finally
            { }

        }        // Verarbeitung des aktuellen Bildes


        public string FileName;                                                      // Dateiname des Bildes 
        private async Task<StorageFile> WriteableBitmapToStorageFile(WriteableBitmap WB, FileFormat fileFormat) // Abspeichern des Bildes im lokalen Speicher
        {
            FileName = "face" + DateTime.Now.ToString("HHSSmm + yyyyMMdd") + ".";
            Guid BitmapEncoderGuid = BitmapEncoder.JpegEncoderId;
            switch (fileFormat)
            {
                case FileFormat.Jpeg:
                    FileName += "jpeg";
                    BitmapEncoderGuid = BitmapEncoder.JpegEncoderId;
                    break;

                case FileFormat.Png:
                    FileName += "png";
                    BitmapEncoderGuid = BitmapEncoder.PngEncoderId;
                    break;

                case FileFormat.Bmp:
                    FileName += "bmp";
                    BitmapEncoderGuid = BitmapEncoder.BmpEncoderId;
                    break;

                case FileFormat.Tiff:
                    FileName += "tiff";
                    BitmapEncoderGuid = BitmapEncoder.TiffEncoderId;
                    break;

                case FileFormat.Gif:
                    FileName += "gif";
                    BitmapEncoderGuid = BitmapEncoder.GifEncoderId;
                    break;
            }

            var file = await Windows.Storage.DownloadsFolder.CreateFileAsync(FileName, CreationCollisionOption.GenerateUniqueName);

            using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoderGuid, stream);
                Stream pixelStream = WB.PixelBuffer.AsStream();
                byte[] pixels = new byte[pixelStream.Length];
                await pixelStream.ReadAsync(pixels, 0, pixels.Length);

                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                                    (uint)WB.PixelWidth,
                                    (uint)WB.PixelHeight,
                                    96.0,
                                    96.0,
                                    pixels);
                await encoder.FlushAsync();
            }





            return file;
        }       

        private enum FileFormat                                                      // Mögliche Dateiformate für das Abspeichern
        {
            Jpeg,
            Png,
            Bmp,
            Tiff,
            Gif
        }


        private async Task<bool> CheckIfGroupExistsAsync()
        {
            PersonGroup group = null;

            // create group if first time
            try
            {
                group = await faceServiceClient.GetPersonGroupAsync(personGroupId);
                _personList = await faceServiceClient.GetPersonsAsync(personGroupId);
                return true;
            }
            catch (FaceAPIException ex)
            {
                return false;
            }
        }                       // Erstellen oder Holen der Personen-Gruppe über die API


        public string facedetected = "";                                             // Name der aktuell erkannten Person
        private async void FaceDetect(VideoFrame frame)
        {
            IdentifyResult[] results = null;  // Erkennnungsergebnisse

            using (var inputStream = new InMemoryRandomAccessStream())
            {
                using (var converted = SoftwareBitmap.Convert(frame.SoftwareBitmap, BitmapPixelFormat.Rgba16))  // kompr. Videoframe -> unkompr. Bitmap
                {
                    // InputStream im PNG-Format erzeugen
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, inputStream);
                    encoder.SetSoftwareBitmap(converted);
                    await encoder.FlushAsync();

                    Face[] faces = null;
                    try
                    {
                        faces = await faceServiceClient.DetectAsync(inputStream.AsStream());
                    }
                    catch (FaceAPIException er)
                    {
                        Debug.WriteLine("Exception: " + er.ErrorMessage);
                        return;
                    }

                    if (await CheckIfGroupExistsAsync())
                    {
                        results = await faceServiceClient.IdentifyAsync(personGroupId, faces.Select(f => f.FaceId).ToArray());
                    }
                    for (var i = 0; i < faces.Length; i++)
                    {
                        var face = faces[i];

                        var photoFace = new PhotoFace()
                        {
                            Rect = face.FaceRectangle,
                            Identified = false
                        };


                        if (results != null)
                        {
                            var result = results[i];
                            if (result.Candidates.Length > 0)
                            {
                                photoFace.PersonId = result.Candidates[0].PersonId;
                                photoFace.Name = _personList.Where(p => p.PersonId == result.Candidates[0].PersonId).FirstOrDefault()?.Name;
                                photoFace.Identified = true;
                                facedetected = photoFace.Name.ToString();
                                //Debug.WriteLine(photoFace.Name.ToString());

                            }
                        }

                    }
                }
            }
        }                          //  Konvertieren des aktuellen Bildes, transfer zur API und Entgegennahme der Ergebnisse

        public string GetFaceName()                                                 // Methode zum Abrufen des aktuellen Names
        {
            return facedetected;
        }



        public void Delete()
        {
            //private async void ChangeScenarioState(ScenarioState newState)
            //{
            //    // Disable UI while state change is in progress

            //    switch (newState)
            //    {
            //        case ScenarioState.Idle:

            //            //   this.ShutdownWebCam();
            //            this.currentState = newState;
            //            break;

            //        case ScenarioState.Streaming:

            //            if (!await this.StartWebcamStreaming())
            //            {
            //                this.ChangeScenarioState(ScenarioState.Idle);
            //                break;
            //            }

            //            this.currentState = newState;
            //            break;
            //    }
            //}


            //private void MediaCapture_CameraStreamFailed(MediaCapture sender, object args)
            //{
            //    // MediaCapture is not Agile and so we cannot invoke its methods on this caller's thread
            //    // and instead need to schedule the state change on the UI thread.
            //    var ignored = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            //    {
            //        ChangeScenarioState(ScenarioState.Idle);
            //    });
            //}



            //private void CameraStreamingButton_Click(object sender, RoutedEventArgs e)
            //{//
            //    if (this.currentState == ScenarioState.Streaming)
            //    {
            //        this.ChangeScenarioState(ScenarioState.Idle);
            //    }
            //    else
            //    {
            //        this.ChangeScenarioState(ScenarioState.Streaming);
            //    }
            //}
            //private enum ScenarioState
            //{
            //    Idle,
            //    Streaming
            //}
            //private void OnSuspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
            //{
            //    if (this.currentState == ScenarioState.Streaming)
            //    {
            //        var deferral = e.SuspendingOperation.GetDeferral();
            //        try
            //        {
            //            this.ChangeScenarioState(ScenarioState.Idle);
            //        }
            //        finally
            //        {
            //            deferral.Complete();
            //        }
            //    }
            //}
        }
    }
}
