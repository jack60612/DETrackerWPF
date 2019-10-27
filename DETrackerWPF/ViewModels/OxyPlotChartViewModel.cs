using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using DETrackerWPF.Models;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace DETrackerWPF.ViewModels
{
  class OxyPlotChartViewModel : Screen
  {


    // Dark Echo Average influance over all systems data source
    List<AvgInf> avgDEInf = new List<AvgInf>();
    List<TheResults> theResults = new List<TheResults>();

    // Dark Echo all systems graph data source
    List<factionPerformance> DEPerformance = new List<factionPerformance>();

    // List of all Dark Echo systems including history
    //List<VisitHistory> _deVisitHistory = new List<VisitHistory>();

    Helper helper = new Helper();
    DataAccess dataAccess = new DataAccess();

    public OxyPlotChartViewModel(List<DESystemsForDisplay> displayDESystems, string SystemToDisplay)
    {
      RightArrow = helper.Convert(DETrackerWPF.Properties.Resources.rightArrow);
      LeftArrow = helper.Convert(DETrackerWPF.Properties.Resources.leftArrow);

      _deSystems = displayDESystems;

      DarkEchoSystems = _deSystems.Select(x => x.StarSystem).ToList();

      GenerateDataLIsts();

      if (SystemToDisplay.Length > 0)
        SelectedSystemIndex = DarkEchoSystems.IndexOf(SystemToDisplay);

      PlotModel = new PlotModel { Title = "Faction Performance" };
      PlotModel.LegendPlacement = LegendPlacement.Outside;
      PlotModel.LegendPosition = LegendPosition.RightTop;
      PlotModel.LegendBackground = OxyColor.FromAColor(200, OxyColors.White);
      PlotModel.LegendBorder = OxyColors.Black;
      PlotModel.TitleHorizontalAlignment = TitleHorizontalAlignment.CenteredWithinView;
      PlotModel.TitleFontSize = 24;
      PlotModel.TitlePadding = 20;
      PlotModel.TitleColor = OxyColors.MidnightBlue;
    }
    /// <summary>
    /// 
    /// </summary>
    public void DisplayDarkEchoSystem()
    {
      var tempData = new List<double>();
      string FactionName = String.Empty;
      var tmpFactionData = new List<SystemInfByFactionOxy>();
      TrafficButtonContent = "Show Traffic";

      PlotModel.Series.Clear();
      PlotModel.Axes.Clear();

      foreach (var histRec in _deSystems[_deSystems.FindIndex(x => x.StarSystem == SelectedSystem)].FactionHistory)
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

      foreach (var sf in tmpFactionData)
      {
        LineSeries ls = new LineSeries();
        ls.Title = sf.FactionName;

        if (MaxHistoryDays < sf.DateNAndInf.Count)
          MaxHistoryDays = sf.DateNAndInf.Count;

        foreach (var dai in sf.DateNAndInf)
        {
          ls.Points.Add(new DataPoint(DateTimeAxis.ToDouble(dai.FactionInfDate), dai.FactionInf));
        }
        PlotModel.Series.Add(ls);
      }
      AddAxis("Inf");
      PlotModel.Title = string.Format("System: {0} - Faction performance over the past {1} days", SelectedSystem, MaxHistoryDays);
      PlotModel.InvalidatePlot(true);
    }

    /// <summary>
    /// Display theAverage Influence
    /// </summary>
    public void AverageInf()
    {
      PlotModel.Series.Clear();
      PlotModel.Axes.Clear();

      LineSeries ls = new LineSeries();
      PlotModel.Title = string.Format("Average Influence Across all Dark Echo Systems for the past {0} days", theResults.Count);

      foreach (var results in theResults)
      {
        ls.Points.Add(new DataPoint(DateTimeAxis.ToDouble(results.InfDate), results.TotalInf));
      }

      PlotModel.Series.Add(ls);
      AddAxis("Inf");
      PlotModel.InvalidatePlot(true);
    }

    /// <summary>
    /// Display Faction performance across ross all systems we are in
    /// </summary>
    public void FactionPerformance()
    {
      PlotModel.Series.Clear();
      PlotModel.Axes.Clear();

      var MaxHistoryDays = 0;
      foreach (var factionPerformance in DEPerformance)
      {
        LineSeries s1 = new LineSeries();
        if (MaxHistoryDays < factionPerformance.DEInf.Count)
          MaxHistoryDays = factionPerformance.DEInf.Count;

        foreach (var deInfHistory in factionPerformance.DEInf)
        {
          s1.Points.Add(new DataPoint(DateTimeAxis.ToDouble(deInfHistory.Date), deInfHistory.DarkEchoInf));
          s1.Title = factionPerformance.StarSystem;
        }

        PlotModel.Series.Add(s1);
      }

      PlotModel.Title = string.Format("Faction performance across all systems for the past {0} days", MaxHistoryDays);
      AddAxis("Inf");
      PlotModel.InvalidatePlot(true);
    }
    /// <summary>
    /// Show daily visits
    /// </summary>
    /// <param name="sysName"></param>
    public void SystemTraffic()
    {
      TrafficButtonContent = "Show Influence";
      PlotModel.Series.Clear();
      PlotModel.Axes.Clear();

      LinearBarSeries l1 = new LinearBarSeries();

      // First extract the traffic record for the system
      dataAccess.GetDailyVisits();
      VisitHistory trafficRec = dataAccess.DeVisitsHistory[dataAccess.DeVisitsHistory.FindIndex(x => x.Visted[0].StarSystem == SelectedSystem)];

      foreach (var dailyTraffic in trafficRec.Visted)
      {
        l1.Points.Add(new DataPoint(DateTimeAxis.ToDouble(dailyTraffic.timestamp.Date), dailyTraffic.Visits));
      }

      l1.BarWidth = 20;
      l1.FillColor = OxyColors.DarkBlue;
      l1.StrokeThickness = 1;
      l1.StrokeColor = OxyColors.Red;
      PlotModel.Series.Add(l1);
      AddAxis("Visits");
      PlotModel.Title = string.Format("Traffic levels for {0}", SelectedSystem);
      PlotModel.InvalidatePlot(true);
    }
    /// <summary>
    /// Handle button click
    /// </summary>
    public void ShowTraffic()
    {
      if (TrafficButtonContent == "Show Traffic")
        SystemTraffic();
      else
        DisplayDarkEchoSystem();
    }

    /// <summary>
    /// Handle click on Right Arrow
    /// </summary>
    public void RightArrowClick()
    {
      if (SelectedSystemIndex < (DarkEchoSystems.Count - 1))
        SelectedSystemIndex++;
      else
        SelectedSystemIndex = 0;
    }
    public void LeftArrowClick()
    {
      if (SelectedSystemIndex > 0)
        SelectedSystemIndex--;
      else
        SelectedSystemIndex = (DarkEchoSystems.Count - 1);
    }

    private void AddAxis(string yAxes)
    {
      PlotModel.Axes.Add(new DateTimeAxis
      {
        Position = AxisPosition.Bottom,
        Title = "Date",
        AxisTitleDistance = 20,
        TitleFontWeight = FontWeights.Bold,
        TitleFontSize = 18,
        StringFormat = "d/MMM",
        MajorGridlineStyle = LineStyle.Solid,
        MinorGridlineStyle = LineStyle.Dot,
        FontWeight = FontWeights.Bold,
        IntervalLength = 80
      });

      if (yAxes == "Inf")
      {
        PlotModel.Axes.Add(new LinearAxis
        {
          Position = AxisPosition.Left,
          Title = "Influence%",
          AxisTitleDistance = 20,
          TitleFontWeight = FontWeights.Bold,
          TitleFontSize = 18,
          MajorGridlineStyle = LineStyle.Solid,
          MinorGridlineStyle = LineStyle.Dot,
          FontWeight = FontWeights.Bold
        });
      }

      if (yAxes == "Visits")
      {
        PlotModel.Axes.Add(new LinearAxis
        {
          Position = AxisPosition.Left,
          Title = "Visits per Day",
          AxisTitleDistance = 20,
          TitleFontWeight = FontWeights.Bold,
          TitleFontSize = 18,
          MajorGridlineStyle = LineStyle.Solid,
          MinorGridlineStyle = LineStyle.Dot,
          FontWeight = FontWeights.Bold
        });
      }
    }
    /// <summary>
    /// 
    /// </summary>
    void GenerateDataLIsts()
    {
      // ************** Systems Inf Graph data ****************
      DEPerformance.Clear();
      foreach (var sys in _deSystems)
      {
        factionPerformance fp = new factionPerformance();
        fp.StarSystem = sys.StarSystem;
        fp.DEInf = new List<DEInfHistory>();

        foreach (var histRec in sys.FactionHistory)
        {
          DEInfHistory dh = new DEInfHistory();
          // Create new record
          dh.Date = histRec.timestamp.Date;
          dh.DarkEchoInf = histRec.Factions[histRec.Factions.FindIndex(y => y.Name == Helper.FactionName)].Influence * 100;
          fp.DEInf.Add(dh);
        }
        DEPerformance.Add(fp);
      }

      // *************** Average Influence ********************

      avgDEInf.Clear();
      foreach (var sys in _deSystems)
      {
        foreach (var histRec in sys.FactionHistory)
        {
          AvgInf ai = new AvgInf();
          ai.date = histRec.timestamp.Date;
          ai.starSystem = sys.StarSystem;
          var index = histRec.Factions.FindIndex(f => f.Name == Helper.FactionName);
          ai.deInf = histRec.Factions[index].Influence;
          avgDEInf.Add(ai);
        }
      }

      var result = avgDEInf
        .GroupBy(d => d.date)
        .Select(g => new
        {
          InfDate = g.Key,
          TotalInf = g.Sum(d => d.deInf),
          systems = g.Count()
        }).ToList();

      foreach (var r in result)
      {
        TheResults tr = new TheResults();
        tr.InfDate = r.InfDate;
        tr.TotalInf = ((r.TotalInf * 100) / r.systems);
        theResults.Add(tr);
      }
      theResults.Sort((x, y) => x.InfDate.CompareTo(y.InfDate));
    }


    // -----------------------------------------------------------------------------------------------------------------
    // --------------------------------------------       Properties     -----------------------------------------------
    // -----------------------------------------------------------------------------------------------------------------

    private System.Windows.Media.Imaging.BitmapImage _leftArrow;
    private System.Windows.Media.Imaging.BitmapImage _rightArrow;
    private string _chartHeaderText;
    private string _legendLocation;
    private List<string> _DarEchoSystems;
    private string _selectedSystem;
    private int _selectedSystemIndex;
    private bool _arrowsEnabled;
    private List<DESystemsForDisplay> _deSystems;


    public string DisplayingSystem { get; set; }

    public System.Windows.Media.Imaging.BitmapImage LeftArrow
    {
      get { return _leftArrow; }
      set
      {
        _leftArrow = value;
        NotifyOfPropertyChange(() => LeftArrow);
      }
    }
    public System.Windows.Media.Imaging.BitmapImage RightArrow
    {
      get { return _rightArrow; }
      set
      {
        _rightArrow = value;
        NotifyOfPropertyChange(() => RightArrow);
      }
    }
    public string ChartHeaderText
    {
      get { return _chartHeaderText; }
      set
      {
        _chartHeaderText = value;
        NotifyOfPropertyChange(() => ChartHeaderText);
      }
    }
    public string LegendLocation
    {
      get { return _legendLocation; }
      set
      {
        _legendLocation = value;
        NotifyOfPropertyChange(() => LegendLocation);
      }
    }
    public List<string> DarkEchoSystems
    {
      get { return _DarEchoSystems; }
      set
      {
        _DarEchoSystems = value;
        NotifyOfPropertyChange(() => DarkEchoSystems);
      }
    }
    public string SelectedSystem
    {
      get { return _selectedSystem; }
      set
      {
        _selectedSystem = value;
        NotifyOfPropertyChange(() => SelectedSystem);
      }
    }
    public int SelectedSystemIndex
    {
      get { return _selectedSystemIndex; }
      set
      {
        _selectedSystemIndex = value;
        NotifyOfPropertyChange(() => SelectedSystemIndex);
      }
    }
    public bool ArrowsEnabled
    {
      get { return _arrowsEnabled; }
      set { _arrowsEnabled = value; }
    }
    public List<DESystemsForDisplay> DisplayDESystems
    {
      get { return _deSystems; }
      set { _deSystems = value; }
    }
    public PlotModel PlotModel { get; private set; }

    private string _trafficButtonContent;

    public string TrafficButtonContent
    {
      get { return _trafficButtonContent; }
      set
      {
        _trafficButtonContent = value; 
        NotifyOfPropertyChange(() => TrafficButtonContent);
      }
    }

  }
}
