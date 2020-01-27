using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Caliburn.Micro;
using DETrackerWPF.Models;
using Newtonsoft.Json;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using FontWeights = OxyPlot.FontWeights;


namespace DETrackerWPF.ViewModels
{
  public class SystemDetailViewModel : Screen
  {

    public List<DESystemsForDisplay> _displayDESystems = new List<DESystemsForDisplay>();

    DataAccess dataAccess = new DataAccess();

    string[] hexColours = new string[] { "#002A64","#7BC141","#767731","#97b8fd",
                                         "#40e0d0","#d31145","#9a504c","#2e2c2e" };

    List<PlotColour> PlotColours = new List<PlotColour>();

    public SystemDetailViewModel(List<DESystemsForDisplay> deSystemsForDisplay, string sysName)
    {

      VisibilityState = true;
      ShowPMFData = true;
      ProgressRingActive = true;

      // Set up line graph
      PlotModel = new PlotModel();
      PlotModel.LegendPlacement = LegendPlacement.Outside;
      PlotModel.LegendPosition = LegendPosition.BottomCenter;
      PlotModel.LegendOrientation = LegendOrientation.Horizontal;
      PlotModel.LegendBackground = OxyColor.FromAColor(200, OxyColors.White);
      PlotModel.LegendBorder = OxyColors.Black;

      // Setup system influence Pie Chart
      FactionPlot = new PlotModel();
      FactionPlot.Title = "System Influence";
      FactionPlot.TitleColor =  OxyColors.Cornsilk;
      FactionPlot.TitleFontSize = 24;
      FactionPlot.PlotMargins = new OxyThickness(25,25,25,25);

      var SelectSystem = deSystemsForDisplay[deSystemsForDisplay.FindIndex(x => x.StarSystem == sysName)];
      var CurrentFactionHistory = SelectSystem.FactionHistory.Last();
      var deInf = CurrentFactionHistory.Factions[CurrentFactionHistory.Factions.FindIndex(x => x.Name == Helper.FactionName)].Influence * 100;

      SystemOverview = new SystemOverviewModel();
      _displayDESystems = deSystemsForDisplay;
      _closePlayerFactions = new List<ClosePlayFactions>();
      _expansionSystems = new List<ExpansionSystems>();

      SystemOverview = dataAccess.GetSystemInfo(sysName);

      InfluenceDisplay = string.Format("Current Influence : {0:##.##}% / Max Influence Possible : {1:##.##}%", deInf, Helper.MaxInfluence(deInf, SystemOverview.Population));

      SystemEconomy = string.Format("{0} ({1})", SystemOverview.EconomyPrimary, SystemOverview.EconomySecondary);
      OrbitalStations = new ObservableCollection<StationList>(SystemOverview.StationsInSystem.Where(x => x.IsPlantery == false).ToList());
      PlanetaryStations = new ObservableCollection<StationList>(SystemOverview.StationsInSystem.Where(x => x.IsPlantery).ToList());

      DisplayDarkEchoSystem();
      FactionPieChart();

      // Fire off the retrieval of close PMFs and Expansion targets as background task 
      PMFStatus = "Loading Closest PMFs and Expansion Systems";
      ExpansionRange = 20;
      ExpansionRangeText = "Expansion Targets (20ly Cube)";
      Task task = Task.Run(async () => await dataAccess.GetClosePlayerFactions(SystemOverview, _closePlayerFactions, _expansionSystems, ExpansionRange));
      task.ContinueWith(DataRetrived);
    }

    /// <summary>
    /// Data retrieval complete, set some properties
    /// </summary>
    /// <param name="obj"></param>
    private void DataRetrived(Task obj)
    {
      ProgressRingActive = false;
      VisibilityState = false;
      ClosePMFs = new ObservableCollection<ClosePlayFactions>(_closePlayerFactions.OrderBy(x => x.Distance));
      ExpansionTargetSystems = new ObservableCollection<ExpansionSystems>(_expansionSystems.OrderBy(x => x.Distance));
      ShowPMFData = true;
    }
    /// <summary>
    /// 
    /// </summary>
    public void FactionPieChart()
    {


      var todaysRec = _displayDESystems[_displayDESystems.FindIndex(x => x.StarSystem == SystemOverview.SystemName)].FactionHistory.Last();
      todaysRec.Factions.RemoveAll(x => x.Name.Contains("Federation Local Branch"));

      var ps = new PieSeries
      {
        StrokeThickness = 0.25,
        AngleSpan = 360,
        StartAngle = 0,
        InnerDiameter = 0.4,
        InsideLabelFormat = "",
        OutsideLabelFormat = "{0:##.##}%",
        TickHorizontalLength = 10,
        TickRadialLength = 6,
        FontWeight = FontWeights.Bold,
        TextColor =  OxyColors.Cornsilk,
        ExplodedDistance = 0.1
    };

      foreach (var faction in todaysRec.Factions)
      {
        var HexCode = PlotColours[PlotColours.FindIndex(x => x.FactionName == faction.Name)].HexColour;
        if (faction.Name == Helper.FactionName)
          ps.Slices.Add(new PieSlice(faction.Name, faction.Influence * 100) { IsExploded = true,  Fill = OxyColor.Parse(HexCode)} );
        else
          ps.Slices.Add(new PieSlice(faction.Name, faction.Influence * 100){ IsExploded = false, Fill = OxyColor.Parse(HexCode) });

      }
      FactionPlot.Series.Add(ps);
    }

    /// <summary>
    /// Load system performance graph
    /// </summary>
    public void DisplayDarkEchoSystem()
    {
      var tempData = new List<double>();
      string FactionName = String.Empty;
      var tmpFactionData = new List<SystemInfByFactionOxy>();

      PlotModel.Series.Clear();
      PlotModel.Axes.Clear();
      PlotColours.Clear();

      foreach (var histRec in _displayDESystems[_displayDESystems.FindIndex(x => x.StarSystem == SystemOverview.SystemName)].FactionHistory)
      {
        histRec.Factions.RemoveAll(x => x.Name.Contains("Federation Local Branch"));
        foreach (var f in histRec.Factions)
        {
          var sysInfByFaction = new SystemInfByFactionOxy();
          sysInfByFaction.DateNAndInf = new List<FactionDateInf>();

          if (tmpFactionData.FirstOrDefault(x => x.FactionName == f.Name) == null)
          {
            var dai = new FactionDateInf();
            sysInfByFaction.FactionName = f.Name;
            dai.FactionInf = (f.Influence * 100);
            dai.FactionInfDate = histRec.timestamp;
            sysInfByFaction.DateNAndInf.Add(dai);
            tmpFactionData.Add(sysInfByFaction);
          }
          else
          {
            var dai = new FactionDateInf();
            dai.FactionInf = (f.Influence * 100);
            dai.FactionInfDate = histRec.timestamp;
            tmpFactionData[tmpFactionData.FindIndex(x => x.FactionName == f.Name)].DateNAndInf.Add(dai);
          }
        }
      }

      var MaxHistoryDays = 0;
      var index = 0;

      foreach (var sf in tmpFactionData)
      {
        LineSeries ls = new LineSeries() {Color = OxyColor.Parse(hexColours[index])};
        PlotColours.Add(new PlotColour() {FactionName = sf.FactionName, HexColour = hexColours[index]});

        ls.Title = sf.FactionName;

        if (MaxHistoryDays < sf.DateNAndInf.Count)
          MaxHistoryDays = sf.DateNAndInf.Count;

        foreach (var dai in sf.DateNAndInf)
        {
          ls.Points.Add(new DataPoint(DateTimeAxis.ToDouble(dai.FactionInfDate), dai.FactionInf) );
        }

        PlotModel.Series.Add(ls);
        index++;
      }

      AddAxis();
      PlotModel.InvalidatePlot(true);
    }

    /// <summary>
    /// Add the axes
    /// </summary>
    /// <param name="yAxes"></param>
    private void AddAxis()
    {
      PlotModel.Axes.Add(new DateTimeAxis
      {
        Position = AxisPosition.Bottom,
        StringFormat = "d/MMM",
        MajorGridlineStyle = LineStyle.Solid,
        MinorGridlineStyle = LineStyle.Dot,
        FontWeight = FontWeights.Bold,
        TextColor = OxyColors.Cornsilk,
        IntervalLength = 80
      });

      PlotModel.Axes.Add(new LinearAxis
      {
        Position = AxisPosition.Left,
        Title = "Influence %",
        AxisTitleDistance = 10,
        TitleFontWeight = FontWeights.Bold,
        TitleFontSize = 14,
        TitleColor = OxyColors.Cornsilk,
        MajorGridlineStyle = LineStyle.Solid,
        MinorGridlineStyle = LineStyle.Dot,
        TextColor = OxyColors.Cornsilk,
        FontWeight = FontWeights.Bold
      });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="SelectedSystem"></param>
    public void RowSelectClosePMF(ClosePlayFactions SelectedSystem)
    {
      System.Diagnostics.Process.Start(string.Format("https://eddb.io/system/{0}", SelectedSystem.EddbID));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="SelectedSystem"></param>
    public void RowSelectExpansion(ExpansionSystems SelectedSystem)
    {
      System.Diagnostics.Process.Start(string.Format("https://eddb.io/system/{0}", SelectedSystem.EddbID));
    }

    /// <summary>
    /// 
    /// </summary>
    public bool ArePlanetary
    {
      get { return PlanetaryStations.Count > 0; }
    }

    public void ExpRange()
    {
      ClosePMFs.Clear();
      ExpansionTargetSystems.Clear();

      VisibilityState = true;
      ShowPMFData = true;
      ProgressRingActive = true;

      if (ExpansionRange == 20)
      {
        ExpansionRange = 40;
        ExpansionRangeText = "Expansion Targets (40ly Cube)";
      }
      else
      {
        ExpansionRange = 20;
        ExpansionRangeText = "Expansion Targets (20ly Cube)";
      }
      Task task = Task.Run(async () => await dataAccess.GetClosePlayerFactions(SystemOverview, _closePlayerFactions, _expansionSystems, ExpansionRange));
      task.ContinueWith(DataRetrived);
    }


    // -----------------------------------------------------------------------------------------------------------------
    // --------------------------------------------       Properties     -----------------------------------------------
    // -----------------------------------------------------------------------------------------------------------------

    private string _expansionRangeText;
    private int _expansionRange;
    private bool _showLoadText;
    private bool _progressRingActive;
    private bool _showPMFData;
    private SystemOverviewModel _systemOverview;
    private string _systemEconomy;
    private string _pmfStatus;
    private ObservableCollection<ClosePlayFactions> _closePMFs;
    private ObservableCollection<ExpansionSystems> _expansionTargetSystems;

    public string ExpansionRangeText
    {
      get { return _expansionRangeText; }
      set
      {
        _expansionRangeText = value;
        NotifyOfPropertyChange(() => ExpansionRangeText);
      }
    }

    public int ExpansionRange
    {
      get { return _expansionRange; }
      set
      {
        _expansionRange = value;
        NotifyOfPropertyChange(() => ExpansionRange);
      }
    }

    public bool VisibilityState
    {
      get { return _showLoadText; }
      set
      {
        _showLoadText = value;
        NotifyOfPropertyChange(() => VisibilityState);
      }
    }

    public bool ProgressRingActive
    {
      get { return _progressRingActive; }
      set
      {
        _progressRingActive = value;
        NotifyOfPropertyChange(() => ProgressRingActive);
      }
    }

    public bool ShowPMFData
    {
      get { return _showPMFData; }
      set
      {
        _showPMFData = value;
        NotifyOfPropertyChange(() => ShowPMFData);
      }
    }

    public SystemOverviewModel SystemOverview
    {
      get { return _systemOverview; }
      set { _systemOverview = value; }
    }

    public string SystemEconomy
    {
      get { return _systemEconomy; }
      set
      {
        _systemEconomy = value;
        NotifyOfPropertyChange(() => SystemEconomy);
      }
    }

    public string PMFStatus
    {
      get { return _pmfStatus; }
      set
      {
        _pmfStatus = value;
        NotifyOfPropertyChange(() => PMFStatus);
      }
    }

    public ObservableCollection<ClosePlayFactions> ClosePMFs
    {
      get { return _closePMFs; }
      set
      {
        _closePMFs = value;
        NotifyOfPropertyChange(() => ClosePMFs);
      }
    }

    public ObservableCollection<ExpansionSystems> ExpansionTargetSystems
    {
      get { return _expansionTargetSystems; }
      set
      {
        _expansionTargetSystems = value;
        NotifyOfPropertyChange(() => ExpansionTargetSystems);
      }
    }

    public ObservableCollection<StationList> OrbitalStations { get; set; }
    public ObservableCollection<StationList> PlanetaryStations { get; set; }
    public List<ClosePlayFactions> _closePlayerFactions { get; set; }
    public List<ExpansionSystems> _expansionSystems { get; set; }
    public PlotModel PlotModel { get; private set; }
    public  PlotModel FactionPlot { get; set; }
    public string InfluenceDisplay { get; set; }

    public class PlotColour
    {
      public string FactionName { get; set; }
      public string HexColour { get; set; }
    }
  }
}
