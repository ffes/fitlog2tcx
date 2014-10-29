using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Xml;
using System.Diagnostics;
using System.Globalization;

namespace fitlog2tcx
{
    public class MyBase
    {
        protected double ParseDoubleAttribute(XmlAttribute attr)
        {
            if (attr == null)
                return 0;
            return double.Parse(attr.Value, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        protected long ParseLongAttribute(XmlAttribute attr)
        {
            if (attr == null)
                return 0;
            return long.Parse(attr.Value);
        }

        protected void AddDateAttribute(XmlElement elem, string name, DateTime date)
        {
            elem.SetAttribute(name, date.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));
        }

        protected void AddDateTime(XmlDocument doc, XmlElement parent, string element, DateTime value)
        {
            string tmp = value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            AddString(doc, parent, element, tmp);
        }

        protected void AddLong(XmlDocument doc, XmlElement parent, string element, long value)
        {
            string tmp = value.ToString();
            AddString(doc, parent, element, tmp);
        }

        protected void AddDouble(XmlDocument doc, XmlElement parent, string element, double value)
        {
            string tmp = value.ToString();
            AddString(doc, parent, element, tmp);
        }

        protected void AddString(XmlDocument doc, XmlElement parent, string element, string value)
        {
            XmlElement childNode = doc.CreateElement(element);
            if (value.Length > 0)
                childNode.InnerText = value;
            parent.AppendChild(childNode);
        }
    }
 
    public class TrackPoint : MyBase
    {
        public DateTime _time;
        public double _latitude;
        public double _longitude;
        public double _elevation;
        public long _heartRate;

        public void ParseXML(XmlNode node, DateTime trackStart)
        {
            _time = trackStart.AddSeconds(ParseLongAttribute(node.Attributes["tm"]));
            _latitude = ParseDoubleAttribute(node.Attributes["lat"]);
            _longitude = ParseDoubleAttribute(node.Attributes["lon"]);
            _elevation = ParseDoubleAttribute(node.Attributes["ele"]);
            _heartRate = ParseLongAttribute(node.Attributes["hr"]);
        }

        public void WriteXML(XmlDocument doc, XmlElement parent)
        {
        }
    }

    public class Lap : MyBase
    {
        public DateTime _startTime;
        public double _duration = 0;
        public double _calories = 0;
		public double _startDistance = 0;
        public List<TrackPoint> _track = new List<TrackPoint>();

        public void ParseXML(XmlNode node)
        {
            _startTime = DateTime.Parse(node.Attributes["StartTime"].Value);
            _duration = ParseDoubleAttribute(node.Attributes["DurationSeconds"]);

            foreach (XmlNode calNode in node.ChildNodes)
            {
                if (calNode.Name == "Calories")
                {
                    _calories = ParseDoubleAttribute(calNode.Attributes["TotalCal"]);
                }
            }
        }

        public void Update()
        {
        }

        public void WriteXML(XmlDocument doc, XmlElement parent)
        {
            XmlElement lapElem = doc.CreateElement("Lap");

            AddDateAttribute(lapElem, "StartTime", _startTime);
            AddLong(doc, lapElem, "TotalTimeSeconds", (long)_duration);
            AddLong(doc, lapElem, "Calories", (long)_calories);

            // Add the trackpoints
            if (_track.Count > 0)
            {
                XmlElement trackElem = doc.CreateElement("Track");

                foreach (TrackPoint tp in _track)
                {
                    tp.WriteXML(doc, trackElem);
                }

                lapElem.AppendChild(trackElem);
            }

            parent.AppendChild(lapElem);
        }
    }

    public class Activity : MyBase
    {
        private Guid _guid;
        private double _totalDuration = 0;  // seconds
        private double _totalDistance = 0;  // meters
        private double _totalCalories = 0;
        private DateTime _startTime;
        private DateTime _trackStartTime;
        private List<Lap> _laps = new List<Lap>();
        private List<TrackPoint> _track = new List<TrackPoint>();
        private string _category = "";

        public void ParseXML(XmlNode node)
        {
            _startTime = DateTime.Parse(node.Attributes["StartTime"].Value);
            _guid = new Guid(node.Attributes["Id"].Value);
            Debug.WriteLine("Activity: " + _guid.ToString() + ", " + _startTime.ToString());

            // Loop through child nodes
            foreach (XmlNode curNode in node.ChildNodes)
            {
                switch (curNode.Name)
                {
                    case "Duration":
                        _totalDuration = ParseDoubleAttribute(curNode.Attributes["TotalSeconds"]);
                        break;
                    case "Distance":
                        _totalDistance = ParseDoubleAttribute(curNode.Attributes["TotalMeters"]);
                        break;
                    case "Calories":
                        _totalCalories = ParseDoubleAttribute(curNode.Attributes["TotalCal"]);
                        break;
                    case "Category":
                        _category = curNode.Attributes["Name"].Value;
                        break;
                    case "Track":
                    {
                        _trackStartTime = DateTime.Parse(curNode.Attributes["StartTime"].Value);
                        foreach (XmlNode tpNode in curNode.ChildNodes)
                        {
                            TrackPoint pt = new TrackPoint();
                            pt.ParseXML(tpNode, _trackStartTime);
                            _track.Add(pt);
                        }
                        break;
                    }
                    case "Laps":
                    {
                        foreach (XmlNode lapNode in curNode.ChildNodes)
                        {
                            Lap lap = new Lap();
                            lap.ParseXML(lapNode);
                            _laps.Add(lap);
                        }
                        break;
                    }
                    case "DistanceMarkers":
                    {
						// Sla de DistanceMarkers bij de desbetreffende ronde op
						int markerCount = 1;
                        foreach (XmlNode markerNode in curNode.ChildNodes)
                        {
							if (markerNode.Name == "Marker")
							{
								double distance = ParseDoubleAttribute(markerNode.Attributes["dist"]);
								_laps[markerCount]._startDistance = distance;
							}
                        }
                        break;
                    }
                }
            }
            Debug.WriteLine("Distance: " + _totalDistance.ToString() + " m");
        }

        private void AddCreator(XmlDocument doc, XmlElement parent)
        {
            XmlElement creator = doc.CreateElement("Creator");
            creator.SetAttribute("xsi:type", "Device_t");
            AddString(doc, creator, "Name", "Forerunner 305");
            AddLong(doc, creator, "UnitId", 3396510648);
            AddLong(doc, creator, "ProductID", 484);
            XmlElement version = doc.CreateElement("Version");
            AddLong(doc, version, "VersionMajor", 2);
            AddLong(doc, version, "VersionMinor", 9);
            AddLong(doc, version, "BuildMajor", 0);
            AddLong(doc, version, "BuildMinor", 0);
            creator.AppendChild(version);
            parent.AppendChild(creator);
        }

        public void ConvertTracksToTCX()
        {
            // Add all existing track point to the right lap
            foreach (TrackPoint tp in _track)
            {
            }

            // Let the lap update all its data based upon the new track points
            foreach (Lap lap in _laps)
                lap.Update();
        }

        public void WriteXML(XmlDocument doc, XmlElement elem)
        {
            switch (_category)
            {
                case "Hardlopen":
                case "Mijn activiteiten":
                case "Wedstrijd":
                    elem.SetAttribute("Sport", "Running");
                    break;
                case "Fietsen":
                    elem.SetAttribute("Sport", "Biking");
                    break;
                case "Schaatsen":
                    elem.SetAttribute("Sport", "Other");
                    break;
            }
            AddDateTime(doc, elem, "Id", _startTime);

            // Voeg nu onder de Activities alle Activity toe
            if (_laps.Count > 0)
            {
                foreach (Lap lap in _laps)
                {
                    lap.WriteXML(doc, elem);
/*
                    AddDateAttribute(lapElem, "StartTime", lap._startTime);
                    AddLong(doc, lapElem, "TotalTimeSeconds", (long) lap._duration);
                    AddLong(doc, lapElem, "Calories", (long) lap._calories);

                    XmlElement trackElem = doc.CreateElement("Track");

                    lapElem.AppendChild(trackElem);

                    elem.AppendChild(lapElem);
*/
                }
            }

            //AddCreator(doc, elem);
        }
    }

	public class Converter
	{
        public List<Activity> _activities = new List<Activity>();

        public void Convert()
        {
            if (_activities.Count > 0)
                foreach (Activity act in _activities)
                    act.ConvertTracksToTCX();
        }

        public void ReadFitlog(string filename)
        {
            // Open the XML document and load it
            XmlDocument doc = new XmlDocument();
            doc.Load(filename);

            // Move to the first node of DOM and get some attributes.
            XmlElement root = doc.DocumentElement;

            // Loop through child nodes
            foreach (XmlNode curNode in root.ChildNodes)
            {
                if (curNode.Name == "AthleteLog")
                {
                    // Loop through child nodes
                    foreach (XmlNode node in curNode.ChildNodes)
                    {
                        if (node.Name == "Activity")
                        {
                            Activity act = new Activity();
                            act.ParseXML(node);
                            _activities.Add(act);
                        }
                    }
                }
            }
        }

        public void WriteTCX(string filename)
        {
            CultureInfo culture = new CultureInfo("en-US");
            XmlDocument doc = new XmlDocument();

            // Create the root element
            XmlElement rootNode = doc.CreateElement("TrainingCenterDatabase");
            doc.AppendChild(rootNode);

            // Voeg Activities toe
            XmlElement activities = doc.CreateElement("Activities");
            rootNode.AppendChild(activities);

            // Voeg nu onder de Activities alle Activity toe
            if (_activities.Count > 0)
            {
                foreach (Activity act in _activities)
                {
                    XmlElement actElem = doc.CreateElement("Activity");
                    act.WriteXML(doc, actElem);
                    activities.AppendChild(actElem);
                }
            }

            doc.Save(filename);
        }
	}

}
