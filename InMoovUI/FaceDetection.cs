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

        private MediaCapture mediaCapture;                                                                                          //Speicher für Bilder
        private VideoEncodingProperties videoProperties;                                                                            //Video Eigenschaften des Streams
        private FaceTracker faceTracker;                                                                                            //lokale Erkennung ob Gesichter im Bild
        private ThreadPoolTimer frameProcessingTimer;
        public bool status = false;                                                                                                 //Beschreibt ob Erkennung an oderr aus

        public TrackFacesInWebcam()                                                                                                 //Konstruktor
        {
            
        }

        public async void StarteWebcam()
        {
            status = true;

            if (status)
            {
                if (this.faceTracker == null)                           // Initialisieren des FaceTrackers zur lokalen Gesichtserkennung - falls nicht aktiv
                    faceTracker = await FaceTracker.CreateAsync();

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

                }
                catch (Exception ex)                                // Catch für alle anderen Fehler / Exceptions
                {

                }
            }
        }


        private async void ProcessCurrentVideoFrame(ThreadPoolTimer timer)
        {
            if (status)
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
                            FaceDetect(previewFrame);                                           //Aufruf der API-Methode
                        }

                    }
                }
                catch (Exception ex)
                {

                }
                finally
                { }
            }
        }        // Verarbeitung des aktuellen Bildes

        public string facedetected = "";
        private async void FaceDetect(VideoFrame frame)
        {
            IdentifyResult[] results = null;  // Erkennnungsergebnisse

            using (var inputStream = new InMemoryRandomAccessStream())
            {
                using (var converted = SoftwareBitmap.Convert(frame.SoftwareBitmap, BitmapPixelFormat.Rgba16))  // kompr. Videoframe -> unkompr. Bitmap
                {
                    // InputStream im PNG-Format erzeugen
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, inputStream);     //Encoder für PNG
                    encoder.SetSoftwareBitmap(converted);                                                       //Quelle für Daten
                    await encoder.FlushAsync();                                                                 //Daten umwandeln

                    Face[] faces = null;
                    try
                    {
                        faces = await faceServiceClient.DetectAsync(inputStream.AsStream());                    //Daten zur API schicken
                    }
                    catch (FaceAPIException er)
                    {
                        Debug.WriteLine("Exception: " + er.ErrorMessage);
                        return;
                    }

                    if (await CheckIfGroupExistsAsync())
                    {
                        results = await faceServiceClient.IdentifyAsync(personGroupId, faces.Select(f => f.FaceId).ToArray());      //Personen zu Gesichtern von API holen
                    }
                    for (var i = 0; i < faces.Length; i++)                                                                          //Identifizierung mit Name und PersonID
                    {
                        var face = faces[i];

                        var photoFace = new PhotoFace()                                                                             //Koordinaten zum Gesicht im Bild
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
                                photoFace.Name = _personList.Where(p => p.PersonId == result.Candidates[0].PersonId).FirstOrDefault()?.Name;    //Verknüpfen des Namens
                                photoFace.Identified = true;
                                facedetected = photoFace.Name.ToString();                                                                       //Schreiben des Namens auf globale Variable
                                //Debug.WriteLine(photoFace.Name.ToString());

                            }
                        }

                    }
                }
            }
        }

        private async Task<bool> CheckIfGroupExistsAsync()
        {
            PersonGroup group = null;

            try
            {
                group = await faceServiceClient.GetPersonGroupAsync(personGroupId);                                     //PersonGroup validieren
                _personList = await faceServiceClient.GetPersonsAsync(personGroupId);                                   //Liste der Personen holen
                return true;
            }
            catch (FaceAPIException ex)
            {
                return false;
            }
        }

        public string GetFaceName()                                                 // Methode zum Abrufen des aktuellen Names
        {
            return facedetected;
        }

    }
}
