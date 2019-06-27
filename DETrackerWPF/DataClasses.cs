using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Color = System.Drawing.Color;

namespace DETrackerWPF
{
    class DataClasses
    {
    }

    public class SystemsData
    {
        public List<FullFactionData> Factions { get; set; }
        public Int64 Population { get; set; }
        public string PowerplayState { get; set; }
        public List<string> Powers { get; set; }
        public List<double> StarPos { get; set; }
        public string StarSystem { get; set; }
        public Int64 SystemAddress { get; set; }
        public string SystemAllegiance { get; set; }
        public string SystemEconomy { get; set; }
        public SystemFaction SystemFaction { get; set; }
        public string SystemGovernment { get; set; }
        public string SystemSecondEconomy { get; set; }
        public string SystemSecurity { get; set; }
        public DateTime timestamp { get; set; }
    }

    public class SystemFaction
    {
        public string FactionState { get; set; }
        public string Name { get; set; }
    }

    public class FullFactionData
    {
        public string Allegiance { get; set; }
        public string FactionState { get; set; }
        public string Government { get; set; }
        public string Happiness { get; set; }
        public double Influence { get; set; }
        public string Name { get; set; }
        public List<States> ActiveStates { get; set; }
        public List<States> PendingStates { get; set; }
        public List<States> RecoveringStates { get; set; }
    }

    class FactionsPresent
    {
        public int? minor_faction_id { get; set; }
        public int? state_id { get; set; }
        public double? influence { get; set; }
        public string state { get; set; }
    }

    public class States
    {
        public string State { get; set; }
        public int Trend { get; set; }
    }

    public class Jevents
    {
        public string EventType { get; set; }
        public int EventCount { get; set; }
    }


    public class DESystems
    {
        public Int64 SystemAddress { get; set; }
        public List<DESystemsHistory> FactionHistory { get; set; }
        public int Visted { get; set; }
        public DateTime timestamp { get; set; }
        public byte Updated { get; set; }
    }

    public class DESystemsForDisplay
    {
        public string StarSystem { get; set; }
        public Int64 SystemAddress { get; set; }
        public List<DESystemsHistory> FactionHistory { get; set; }
        public List<double> StarPos { get; set; }
        public int Visted { get; set; }
        public DateTime timestamp { get; set; }
        public SystemFaction SysFaction { get; set; }
        public Int64 Population { get; set; }
        public string SystemAllegiance { get; set; }
        public string SystemEconomy { get; set; }
        public string SystemGovernment { get; set; }
        public string SystemSecondEconomy { get; set; }
        public string SystemSecurity { get; set; }
        public bool Updated { get; set; }
    }

    public class DESystemsHistory
    {
        public DateTime timestamp { get; set; }
        public Int64 SystemsAddress { get; set; }
        public List<FullFactionData> Factions { get; set; }
    }

    public class displayData
    {
        public double fi { get; set; }
        public double fiPrev { get; set; }
        public double fiPrevMinus2 { get; set; }
        public double gap { get; set; }
        public string cellText { get; set; }
        public double closest { get; set; }
        public double ourInf { get; set; }
        public int imageIndex { get; set; }
        public string fActState { get; set; }
        public string fInf { get; set; }
        public string fGap { get; set; }
        public string iChange { get; set; }
        public string iChangePrev { get; set; }
        public string distFromDisci { get; set; }
        public string happiness { get; set; }
    }

    public class StarSystemDetail
    {
        public string StarSystem { get; set; }
    }

    public class ChangedSystemData
    {
        public string StarSystem { get; set; }
        public Int64 SystemAddress { get; set; }
        public DateTime timestamp { get; set; }
    }

    public class factionPositionInDisplay
    {
        public string Name { get; set; }
        public int Column { get; set; }
    }


    public class AvgInf
    {
        public DateTime date { get; set; }
        public string starSystem { get; set; }
        public double deInf { get; set; }
    }

    public class TheResults
    {
        public DateTime InfDate { get; set; }
        public double TotalInf { get; set; }
    }

    public class factionPerformance
    {
        public string StarSystem { get; set; }
        public List<DEInfHistory> DEInf { get; set; }
    }

    public class DEInfHistory
    {
        public DateTime Date { get; set; }
        public Double DarkEchoInf { get; set; }
    }

    public class InterfaceColors
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Color ItemColor { get; set; }
    }



    public class VisitHistory
    {
        public Int64 SystemsAddress { get; set; }
        public List<SystemsVisitHistory> Visted { get; set; }
    }

    public class SystemsVisitHistory
    {
        public string StarSystem { get; set; }
        public int Visits { get; set; }
        public DateTime timestamp { get; set; }
    }


    public class ExpansionSystems
    {
        public string SystemName { get; set; }
        public int FactionsInSystem { get; set; }
        public double x { get; set; }
        public double y { get; set; }
        public double z { get; set; }
        public double Distance { get; set; }
        public int id { get; set; }
        public int EddbID { get; set; }
    }

}
