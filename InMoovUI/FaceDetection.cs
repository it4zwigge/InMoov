using System;
using Windows.Media;
using Windows.Graphics.Imaging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.FaceAnalysis;
using Windows.Media.MediaProperties;
using Windows.System.Threading;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Linq;
using System.IO;

namespace VWFIANCognitveServices
{
    public sealed partial class TrackFacesInWebcam : Page
    {
        public FaceServiceClient faceServiceClient = new FaceServiceClient(Keys.FaceServiceKey, Keys.FaceAPI_rootstring);           // Client der API mit Authentifizierungsschlüssel und URL
        private Person[] _personList;                                                                                               // Array für alle Gesichter

        public string personGroupId = "6b727d764cb5469f9e822361c545d058";                                                           // Name der Gesichtsdatenbank
        TimeSpan timerInterval;                                                                                                     // Timer für Erzeugung der einzelnen Frames/Bilder

        private MediaCapture mediaCapture;
        private VideoEncodingProperties videoProperties;
        private FaceTracker faceTracker;
        private ThreadPoolTimer frameProcessingTimer;


        public TrackFacesInWebcam()
        {
        }

        public async void OnNavigatedTo(NavigationEventArgs e)
        {
            await StartWebcamStreaming();                           // Starten der Kamera
            if (this.faceTracker == null)                           // Initialisieren des FaceTrackers zur lokalen Gesichtserkennung - falls nicht aktiv
                faceTracker = await FaceTracker.CreateAsync();
        }

        private async Task<bool> StartWebcamStreaming()
        {
            bool successful = true;

            try
            {
                this.mediaCapture = new MediaCapture();                                                 // Erstellen eines neuen MediaCaptues
                var captureElement = new CaptureElement();

                MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();     //MediaCapture Settings erstellen
                settings.StreamingCaptureMode = StreamingCaptureMode.Video;                                 //auf Video Modus setzten
                await this.mediaCapture.InitializeAsync(settings);                                          // Settings auf das MediaCapture schreiben
                //this.mediaCapture.Failed += this.MediaCapture_CameraStreamFailed;

                var deviceController = this.mediaCapture.VideoDeviceController;                             //Device Controller für Video-Kamera festlegen
                this.videoProperties = deviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;  //VideoProperties festschreiben
                captureElement.Source = mediaCapture;                                                       //MediaCapture auf CaptureElement anzeigen
                await this.mediaCapture.StartPreviewAsync();                                                //Media Capture Vorschau starten

                timerInterval = TimeSpan.FromMilliseconds(5000);                                            // Timer Interval festsetzten - auf 5 Sekunden
                this.frameProcessingTimer = Windows.System.Threading.ThreadPoolTimer.CreatePeriodicTimer(new Windows.System.Threading.TimerElapsedHandler(ProcessCurrentVideoFrame), timerInterval);
            }
            catch (System.UnauthorizedAccessException)          // Catch für fehlende Zugriffsrechte
            {
                successful = false;
            }
            catch (Exception ex)                                // Catch für alle anderen Fehler / Exceptions
            {
                successful = false;
            }

            return successful;                                  // Zurückgabe des Wertes successfull - true oder false
        }                           // Webcam aktivieren und Bild vorbereiten

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
                IList<DetectedFace> faces = null;                                   // erkannte Gesichter auf null setzten

                const BitmapPixelFormat InputPixelFormat = BitmapPixelFormat.Nv12;  // Pixelformat auf NV12 festlegen
                using (VideoFrame previewFrame = new VideoFrame(InputPixelFormat, 1280, 720))   //VideoFrame erstellen - mit den angegebenen Properties
                {
                    await this.mediaCapture.GetPreviewFrameAsync(previewFrame);                 //Aktuelles Bild aus MediaCapture in das Freame schreiben

                    if (FaceDetector.IsBitmapPixelFormatSupported(previewFrame.SoftwareBitmap.BitmapPixelFormat)) // Prüfen ob VideoFormt gleich NV12 ist - wenn nicht funktioniert der FaceTracker nicht
                    {
                        faces = await this.faceTracker.ProcessNextFrameAsync(previewFrame);    // lokale Auswertung ob Gesichter auf Bild vorhanden, wenn ja, wieviele und wo
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
        }                           //  Konvertieren des aktuellen Bildes, transfer zur API und Entgegennahme der Ergebnisse

        public string GetFaceName()                                                 // Methode zum Abrufen des aktuellen Names
        {
            return facedetected;
        }

    }
}
