using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


//HIER MUSS DIE XML DATEI HINGESCHOBEN WERDEN!
//C:\Users\_Administrator\source\repos\Test_UWP_schreiben\Test_UWP_schreiben\bin\x64\Debug\AppX

namespace Test_UWP_schreiben
{
    public class XML_Data
    {
        public string XMLPath;
        public string test;

        public XML_Data ()
        {

            XMLPath = Path.Combine(Package.Current.InstalledLocation.Path, "XMLFile1.xml");
            XDocument loadedData = XDocument.Load(XMLPath);
            test = loadedData.Document.ToString();
            XmlDocument xml = new XmlDocument();
           
        }
        public string GetVorName(string id)
        {
            string result = "";
            var xmlstr = test;
            using (var reader = XmlReader.Create(new StringReader(xmlstr)))
            {
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        var attr = reader["id"];
                        if (attr != null && attr == id)
                        {
                            if (reader.ReadToDescendant("vorname"))
                            {
                                reader.Read();//this moves reader to next node which is text 
                                result = reader.Value; //this might give value than 
                                break;
                            }
                        }
                    }
                }
            }


            Debug.WriteLine(result);
            return result;
        }
        public string GetNachName(string id)
        {
            string result = "";
            var xmlstr = test;
            using (var reader = XmlReader.Create(new StringReader(xmlstr)))
            {
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        var attr = reader["id"];
                        if (attr != null && attr == id)
                        {
                            if (reader.ReadToDescendant("nachname"))
                            {
                                reader.Read();//this moves reader to next node which is text 
                                result = reader.Value; //this might give value than 
                                break;
                            }
                        }
                    }
                }
            }


            Debug.WriteLine(result);
            return result;
        }
        public string GetGeburtstag(string id)
        {
            string result = "";
            var xmlstr = test;
            using (var reader = XmlReader.Create(new StringReader(xmlstr)))
            {
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        var attr = reader["id"];
                        if (attr != null && attr == id)
                        {
                            if (reader.ReadToDescendant("geburtstag"))
                            {
                                reader.Read();//this moves reader to next node which is text 
                                result = reader.Value; //this might give value than 
                                break;
                            }
                        }
                    }
                }
            }


            Debug.WriteLine(result);
            return result;
        }
    }
}
