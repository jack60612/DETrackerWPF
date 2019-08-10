using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DETrackerWPF.Models
{

    public class SystemOverviewModel
    {
        public string SystemName { get; set; }
        public string Allegiance { get; set; }
        public string Government { get; set; }
        public Int64 Population { get; set; }
        public string ControllingFaction { get; set; }
        public string SecurityLevel { get; set; }
        public string EconomyPrimary { get; set; }
        public string EconomySecondary { get; set; }
        public List<double> StarPos { get; set; }
        public List<StationList> StationsInSystem { get; set; }
    }

    public class StationList
    {
        public string StationName { get; set; }
        public string ControllingFaction { get; set; }
        public string StationType { get; set; }
        public bool IsPlantery { get; set; }
        public string MaxLandingPad { get; set; }
        public int DistanceFromStar { get; set; }

    }

    public class ClosePlayFactions
    {
        public string SystemName { get; set; }
        public List<double> StarPos { get; set; }
        public int EddbID { get; set; }
        public string ControllingFaction { get; set; }
        public int ControllingFactionID { get; set; }
        public double Distance { get; set; }
    }

    public class ExpansionSystems
    {
        public string SystemName { get; set; }
        public int FactionsInSystem { get; set; }
        public double x { get; set; }
        public double y { get; set; }
        public double z { get; set; }
        public double Distance { get; set; }
        public int EddbID { get; set; }
        public bool InvasionTarget { get; set; }
    }
}