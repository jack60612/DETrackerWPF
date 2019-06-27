using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using DETrackerWPF.Models;
using Newtonsoft.Json;

namespace DETrackerWPF.ViewModels
{
  public class SystemHistoryViewModel : Screen
  {

    DataAccess dataAccess = new DataAccess();
    Helper helper = new Helper();


    private List<FactionModel> _factions = new List<FactionModel>();
    private ObservableCollection<SystemsFactionsHistory> _systemHistory = new ObservableCollection<SystemsFactionsHistory>();
    public List<DESystemsForDisplay> _displayDESystems = new List<DESystemsForDisplay>();

    private int _width;
    private int _height;
    private int _maxHeight;
    private int _maxWidth;

    public SystemHistoryViewModel(Int64 systemAddress, List<DESystemsForDisplay> deSystemsForDisplay)
    {

      _displayDESystems = deSystemsForDisplay;

      DeLogo = helper.Convert(DETrackerWPF.Properties.Resources.DELogo);
      Info = helper.Convert(DETrackerWPF.Properties.Resources.InfoBlue);
      Analytics = helper.Convert(DETrackerWPF.Properties.Resources.analytics);
      EliteBGS = helper.Convert(DETrackerWPF.Properties.Resources.EliteBGS);

      DESystemsForDisplay selectedSystem = dataAccess.ReadUpdatedSystem(systemAddress.ToString());
      ActiveStarSystem = selectedSystem.StarSystem;
      DisplayHistoryData(selectedSystem, "");

      HeaderSummary = string.Format("Star System: {0} | Population: {1:###,###,###,###} | Controlling Faction: {2} | Government: {3}\r\nAlligence: {4} | Economies: {5} | Security: {6}",
        selectedSystem.StarSystem,
        selectedSystem.Population,
        selectedSystem.SysFaction.Name, Helper.Clean(selectedSystem.SystemGovernment),
        selectedSystem.SystemAllegiance, Helper.Clean(selectedSystem.SystemEconomy) + "/" + Helper.Clean(selectedSystem.SystemSecondEconomy), Helper.Clean(selectedSystem.SystemSecurity));

      // var FactionsInSystem = selectedSystem.FactionHistory[selectedSystem.FactionHistory.Count - 1].Factions.Count;

      Factions.Clear();
      foreach (FullFactionData Faction in selectedSystem.FactionHistory[selectedSystem.FactionHistory.Count - 1].Factions)
      {
        // Dump Pilots' Federation Local Branch
        if (Faction.Name.Contains("Federation Local Branch"))
          continue;

        FactionModel f = new FactionModel();
        f.FactionName = Faction.Name;
        f.GovernmentaAllegiance = Faction.Government + " / " + Faction.Allegiance;

        Faction1 = new ObservableCollection<SystemsFactionsHistory>(SystemHistory.Where(x => x.FactionName == Faction.Name));
        f.FactionHistory = new ObservableCollection<DisplayFactionHist>(BuildHistory(Faction1));

        Factions.Add(f);

        // Sort the Headers
        FactionHeadersModel fh = new FactionHeadersModel();
        fh.InfHeader = "Inf";
        fh.ChgHeader = "Chg";
        fh.StateHeader = "Active States";
        FactionHeaders.Add(fh);
      }

      // Reverse the faction list so Dark Echo are the first column
      Factions.Reverse();


    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="displayFaction"></param>
    /// <returns></returns>
    ObservableCollection<DisplayFactionHist> BuildHistory(ObservableCollection<SystemsFactionsHistory> displayFaction)
    {
      //FactionNo1 = displayFaction.;
      FactionName = displayFaction[0].FactionName;
      HistDisplay.Clear();
      foreach (var systemsFactionsHistory in displayFaction)
      {
        DisplayFactionHist dfh = new DisplayFactionHist();
        dfh.DisplayStates = new ObservableCollection<States>();
        // Grab states for each factions
        dfh.ActiveStates = systemsFactionsHistory.FactionHist[0].ActiveStates;
        dfh.PendingStates = systemsFactionsHistory.FactionHist[0].PendingStates;
        dfh.RecoveringStates = systemsFactionsHistory.FactionHist[0].RecoveringStates;

        // Update Influence
        dfh.FactionInfluence = systemsFactionsHistory.FactionHist[0].fInf * 100;
        dfh.InfluenceChange = 0;

        // Default to displaying Active states
        dfh.DisplayStates = systemsFactionsHistory.FactionHist[0].ActiveStates;

        HistDisplay.Add(dfh);
      }

      // Calculate the change from previous day
      for (int i = 0; i < HistDisplay.Count - 1; i++)
      {
        HistDisplay[i].InfluenceChange = HistDisplay[i].FactionInfluence - HistDisplay[i + 1].FactionInfluence;
      }

      return HistDisplay;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="DESystem"></param>
    /// <param name="stateToDisplay"></param>
    public void DisplayHistoryData(DESystemsForDisplay DESystem, string stateToDisplay)
    {
      int fIndex;

      // Get the history list and reverse sort so latest is first.
      List<DESystemsHistory> sysHist = new List<DESystemsHistory>(DESystem.FactionHistory);
      sysHist = sysHist.OrderByDescending(r => r.timestamp).ToList();

      foreach (var fhr in sysHist)
      {

        TimeSpan span = DateTime.Today.Subtract(fhr.timestamp.Date);
        if ((int) span.TotalDays > 30)
          continue;

        // Sort the Dates
        HistoryDatesList hdl = new HistoryDatesList();
        hdl.HistDate = fhr.timestamp.ToShortDateString();
        HistDates.Add(hdl);

        SystemsFactionsHistory sfh = new SystemsFactionsHistory();
        sfh.FactionHist = new ObservableCollection<FactionHistData>();

        sfh.UpdatedAt = fhr.timestamp;

        // Get the index to the current history record
        var histIndex = sysHist.FindIndex(m => m.timestamp == fhr.timestamp);

        // Create temp list of factions and sort in influence order 
        List<FullFactionData> fhs = new List<FullFactionData>(fhr.Factions);

        // Dump "Pilots Federation Local Branch" from the new list
        fhs.RemoveAll(x => x.Name.Contains("Federation Local Branch"));


        foreach (var fullFactionData in fhs)
        {

          SystemsFactionsHistory sysFactionHist = new SystemsFactionsHistory();
          sysFactionHist.FactionHist = new ObservableCollection<FactionHistData>();

          sysFactionHist.FactionName = fullFactionData.Name;
          sysFactionHist.UpdatedAt = fhr.timestamp;

          FactionHistData factionHistoryData = new FactionHistData();

          factionHistoryData.fInf = fullFactionData.Influence;
          if (fullFactionData.ActiveStates != null)
          {
            fullFactionData.ActiveStates = AddSpaces(fullFactionData.ActiveStates);
            factionHistoryData.ActiveStates = new ObservableCollection<States>(fullFactionData.ActiveStates);
          }


          if (fullFactionData.PendingStates != null)
          {
            fullFactionData.PendingStates = AddSpaces(fullFactionData.PendingStates);
            factionHistoryData.PendingStates = new ObservableCollection<States>(fullFactionData.PendingStates);
          }


          if (fullFactionData.RecoveringStates != null)
          {
            fullFactionData.RecoveringStates = AddSpaces(fullFactionData.RecoveringStates);
            factionHistoryData.RecoveringStates = new ObservableCollection<States>(fullFactionData.RecoveringStates);
          }

          sysFactionHist.FactionHist.Add(factionHistoryData);

          SystemHistory.Add(sysFactionHist);

        }

        SystemHistory.Add(sfh);
      }
    }

    List<States> AddSpaces(List<States> stateList)
    {
      foreach (var activeState in stateList)
      {
        switch (activeState.State.ToLower())
        {
          case "civilwar":
            activeState.State = "Civil War";
            break;
          case "civilliberty":
            activeState.State = "Civil Liberty";
            break;
          case "civilunrest":
            activeState.State = "Civil Unrest";
            break;
          case "pirateattack":
            activeState.State = "Pirate Attack";
            break;
          default:
            break;
        }
      }

      return stateList;
    }
    public void StateHeader_Click()
    {
      string NewState = string.Empty;

      SystemsFactionsHistory sysFactionHist = new SystemsFactionsHistory();
      sysFactionHist.FactionHist = new ObservableCollection<FactionHistData>();

      if (FactionHeaders[0].StateHeader == "Active States")
        NewState = "Pending States";
      else if ((FactionHeaders[0].StateHeader == "Pending States"))
        NewState = "Recovering States";
      else if ((FactionHeaders[0].StateHeader == "Recovering States"))
        NewState = "Active States";

      foreach (var factionHeadersModel in FactionHeaders)
      {
        factionHeadersModel.StateHeader = NewState;
      }

      foreach (var factionModel in Factions)
      {
        foreach (var displayFactionHist in factionModel.FactionHistory)
        {
          switch (NewState)
          {
            case "Active States":
              displayFactionHist.DisplayStates = displayFactionHist.ActiveStates;
              break;
            case "Pending States":
              displayFactionHist.DisplayStates = displayFactionHist.PendingStates;
              break;
            case "Recovering States":
              displayFactionHist.DisplayStates = displayFactionHist.RecoveringStates;
              break;
          }
        }
      }
    }

    /// <summary>
    /// Handle mousewheel scroll
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
      ScrollViewer scrollViewer = sender as ScrollViewer;

      if (e.Delta > 0)
        scrollViewer.LineUp();
      else
        scrollViewer.LineDown();

      e.Handled = true;
    }

    /// <summary>
    /// Pull up the EliteBGS web page for this system
    /// </summary>
    public void EliteBGSButton()
    {

      WebRequest webRequest = WebRequest.Create(string.Format("https://elitebgs.app/api/ebgs/v4/systems?name={0}", ActiveStarSystem.Replace("+", "%2B")));
      WebResponse webResp = webRequest.GetResponse();

      Stream dataStream = webResp.GetResponseStream();

      StreamReader reader = new StreamReader(dataStream);
      string responseFromServer = reader.ReadToEnd();
      reader.Close();
      var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseFromServer);
      var retDocs = JsonConvert.DeserializeObject<dynamic>(dict["docs"].ToString());
      string sysAddr = retDocs[0]._id.ToString();
      System.Diagnostics.Process.Start(string.Format("https://elitebgs.app/system/{0}", sysAddr));

    }
    /// <summary>
    /// Pull up graph screen and display graph for current system
    /// </summary>
    public void AnalyticsButton()
    {
      WindowManager windowManager = new WindowManager();
      windowManager.ShowWindow(new OxyPlotChartViewModel(_displayDESystems, ActiveStarSystem), null, null);
    }
    /// <summary>
    /// Pull up the systems Overview screen
    /// </summary>
    public void InfoButton()
    {
      WindowManager windowManager = new WindowManager();
      windowManager.ShowWindow(new SystemDetailViewModel(_displayDESystems, ActiveStarSystem), null, null);
    }
    // -----------------------------------------------------------------------------------------------------------------
    // --------------------------------------------       Properties     -----------------------------------------------
    // -----------------------------------------------------------------------------------------------------------------

    private ObservableCollection<SystemsFactionsHistory> _faction1 = new ObservableCollection<SystemsFactionsHistory>();
    private ObservableCollection<HistoryDatesList> _histDates = new ObservableCollection<HistoryDatesList>();
    private string _factionName;
    private List<FactionHeadersModel> _factionHeaders = new List<FactionHeadersModel>();
    private ObservableCollection<DisplayFactionHist> _histDisplay = new ObservableCollection<DisplayFactionHist>();
    private System.Windows.Media.Imaging.BitmapImage _eliteBGS;
    private System.Windows.Media.Imaging.BitmapImage _analytics;
    private System.Windows.Media.Imaging.BitmapImage _info;
    private System.Windows.Media.Imaging.BitmapImage _deLogo;
    private string _headerSummary;

    public ObservableCollection<SystemsFactionsHistory> Faction1
    {
      get { return _faction1; }
      set { _faction1 = value; }
    }

    public ObservableCollection<SystemsFactionsHistory> SystemHistory
    {
      get { return _systemHistory; }
      set { _systemHistory = value; }
    }

    public List<FactionModel> Factions
    {
      get { return _factions; }
      set { _factions = value; }
    }

    public string HeaderSummary
    {
      get { return _headerSummary; }
      set
      {
        _headerSummary = value;
        NotifyOfPropertyChange(() => HeaderSummary);
      }
    }

    public int Width
    {
      get { return _width; }
      set
      {
        _width = value;
        NotifyOfPropertyChange(() => Width);
      }
    }

    public int Height
    {
      get { return _height; }
      set
      {
        _height = value;
        NotifyOfPropertyChange(() => Height);
      }
    }

    public int MaxWidth
    {
      get { return _maxWidth; }
      set
      {
        _maxWidth = value;
        NotifyOfPropertyChange(() => MaxWidth);
      }
    }

    public int MaxHeight
    {
      get { return _maxHeight; }
      set
      {
        _maxHeight = value;
        NotifyOfPropertyChange(() => MaxHeight);
      }
    }

    public System.Windows.Media.Imaging.BitmapImage DeLogo
    {
      get { return _deLogo; }
      set
      {
        _deLogo = value;
        NotifyOfPropertyChange(() => DeLogo);
      }
    }

    public System.Windows.Media.Imaging.BitmapImage Info
    {
      get { return _info; }
      set
      {
        _info = value;
        NotifyOfPropertyChange(() => Info);
      }
    }

    public System.Windows.Media.Imaging.BitmapImage Analytics
    {
      get { return _analytics; }
      set
      {
        _analytics = value;
        NotifyOfPropertyChange(() => Analytics);
      }
    }

    public System.Windows.Media.Imaging.BitmapImage EliteBGS
    {
      get { return _eliteBGS; }
      set
      {
        _eliteBGS = value;
        NotifyOfPropertyChange(() => EliteBGS);
      }
    }

    public ObservableCollection<DisplayFactionHist> HistDisplay
    {
      get { return _histDisplay; }
      set
      {
        _histDisplay = value;
        NotifyOfPropertyChange(() => HistDisplay);
      }
    }

    public ObservableCollection<HistoryDatesList> HistDates
    {
      get { return _histDates; }
      set { _histDates = value; }
    }

    public List<FactionHeadersModel> FactionHeaders
    {
      get { return _factionHeaders; }
      set { _factionHeaders = value; }
    }

    public string FactionName
    {
      get { return _factionName; }
      set { _factionName = value; }
    }

    public string ActiveStarSystem { get; set; }
  }
}
