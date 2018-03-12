<img src="http://h2590701.stratoserver.net/wp-content/uploads/2017/09/inmoov-github-header.png">

# InMoov

InMoov ist eine [UWP](https://de.wikipedia.org/wiki/Universal_Windows_Platform)-App zum Steuern unseres [InMoov](http://www.inmoov.fr)-Roboters.

## Überblick

Der humanoide Roboter mit [Mecanum](https://de.wikipedia.org/wiki/Mecanum-Rad)-Fahrgestell wurde 2016 am [Volkswagen Bildungsintitut](http://vw-bi.de) in Zwickau fertiggestellt. Der Roboter soll, nach seiner programmiertechnischen Fertigstellung, zur sozialen Interaktion im Institutsfojer dienen. Gleichzeitig dient er als Lernträger bei der Ausbildung unserer Fachinformatiker für Anwendungsentwicklung (FIAN).

> [http://h2590701.stratoserver.net/?p=8]
>
> [https://sagwas.net/2017/12/roboter-zum-selbermachen]

## Hardware (Auszug)

- [Microsoft Kinect v2](https://en.wikipedia.org/wiki/Kinect#Kinect_for_Windows_v2_(2014))
- [LattePanda](www.lattepanda.com/)
- 2x Arduino Mega

Die Kinect dient als zentraler Sensor des Projekts. Die ca. 30 Servomotoren werden über die beiden Arduino Mega angesteuert. Die Verbindung der InMoov-App zu den Servomotoren erfolgt mit Hilfe der [Microsoft.Maker](https://github.com/ms-iot/remote-wiring)-Bibliotheken.

## Geplanter Funktionsumfang

- Gesichtserkennung ([Microsoft Cognitive Services](https://azure.microsoft.com/de-de/services/cognitive-services/))
- Sprachsteuerung bzw. ausgabe (Microsoft Cortana)
- Interaktion über hinterlegte Datenbankinformationen
- ...
