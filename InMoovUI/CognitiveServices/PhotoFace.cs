using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VWFIANCognitveServices
{
    public class PhotoFace                              // Klasse für Ergebnisse der Gesichtsapi
    {
        public string FaceId { get; set; }              // Eindeutige ID eines jeden Gesichts
        public Guid PersonId { get; set; }              // Eindeutige ID einer jeden gespeicherten Person
        public FaceRectangle Rect { get; set; }         // Maße für Rechteck um Gesicht im zur API geschickten Bild

        public string Name { get; set; }                // Zum Gesicht gehörender Name
        public bool Identified { get; set; }            // Boolscher Wert, zur Anzeige, ob ein Gesicht gefunden wurde oder nich´t
    }
}
