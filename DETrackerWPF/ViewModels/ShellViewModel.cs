using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Xml.Serialization;
using AutoUpdaterDotNET;
using Caliburn.Micro;
using DETrackerWPF.Models;
using MahApps.Metro.Controls;
using Newtonsoft.Json;


namespace DETrackerWPF.ViewModels
{


  [Export(typeof(ShellViewModel))]
  public class ShellViewModel : Screen
  {

    DataAccess dataAccess = new DataAccess();

    public List<DESystemsForDisplay> displayDESystems = new List<DESystemsForDisplay>();
    public List<DESystemsForDisplay> UpdatedDisplayDESystems = new List<DESystemsForDisplay>();

    System.Windows.Threading.DispatcherTimer UpdateTimer = new DispatcherTimer();

    Helper helper = new Helper();

    private readonly int DefaultWidth = 1315;

    // Test stuff - Remember to remove
    private readonly IWindowManager _windowManager;

    [ImportingConstructor]
    public ShellViewModel(IWindowManager windowManager)
    {

      _windowManager = windowManager;

      // Now Check for update
      //AutoUpdater.ReportErrors = true;
      AutoUpdater.Start("https://www.darkecho.space/update/DETracker/DETracker.xml");

      // Convert the .png images to something we can use in the grid
      UpTriangle = helper.Convert(DETrackerWPF.Properties.Resources.UpTriangle);
      DownTriangle = helper.Convert(DETrackerWPF.Properties.Resources.DownTriangle);
      NoChange = helper.Convert(DETrackerWPF.Properties.Resources.NoChange);
      SysInfo = helper.Convert(DETrackerWPF.Properties.Resources.InfoBlue);
      SysHistory = helper.Convert(DETrackerWPF.Properties.Resources.analytics);
      WebImage = helper.Convert(DETrackerWPF.Properties.Resources.WebImage);

      //Get and Load the connect strings
      dataAccess.DecryptCheck();

      // Read the time of the tick
      dataAccess.GetTickTime();

      DBAccessMode = "Access Mode : Live DB";
      DisplayTickTime = "Configured Tick Time : " + dataAccess.TickTime.ToString(@"H:mm UTC");

      // Get the file version
      System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
      FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
      string version = fvi.FileVersion;
      WindowTitle = string.Format("Dark Echo Influence Tracker v{0} (Built {1})", version, DETrackerWPF.Properties.Resources.BuildDate);

      // Get DE systems, size main screen to suit
      displayDESystems = dataAccess.ReadDeSystemsTable();

      // sort out the screen size
      double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
      double screenhight = System.Windows.SystemParameters.PrimaryScreenHeight;
      Helper.TransformToPixels(screenhight, screenWidth, out int ScreenHeight, out int ScreenWidth);

      Width = DefaultWidth;
      MaxWidth = Width;
      if (ScreenHeight < (displayDESystems.Count * 16) + 259)
        Height = ScreenHeight;
      else
        Height = ((displayDESystems.Count * 16) + 259);

      // Frig to stop scroll bars appearing when a systems updates and sorting on update time
      Height = Height + 40;

      MaxHeight = Height;

      // Build the summary line

      var systemsControlled = 0;
      foreach (var des in displayDESystems)
      {
        if (des.SysFaction.Name == Helper.FactionName)
          systemsControlled++;
      }

      //var systemsControlled = displayDESystems.Count(x => x.SysFaction.Name == Helper.FactionName);
      var popControlled = displayDESystems.Where(x => x.SysFaction.Name == Helper.FactionName).Sum(x => x.Population);
      FactionSummary = string.Format("Population Controlled : {0:###,###,###} / Systems Controlled {1} / Present in {2} Systems", popControlled, systemsControlled, displayDESystems.Count);

      GetListViewData();
      CalculateSystemMovements();

      // Display the avg inf and change
      if (DateTime.Compare(DateTime.UtcNow, dataAccess.TickTime) > 0)
        dataAccess.CalculateAverageChange();
      else
        dataAccess.CalculateAvfValuesPreTick();
      // Adn display it
      DisplayFactionInfAvgAndChange();

      // Register with SQL Dependency to we get updates when one of our systems is visited
      dataAccess.RegisterForChanges();

      // Timer to fire an event every 100ms to check if a system has been visited
      UpdateTimer.Tick += UpdateTimer_Tick;
      UpdateTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
      dataAccess.SystemUpdated = false;
      UpdateTimer.Start();
    }

    /// <summary>
    /// Grab all systems that we control or have a presence in and build the Datagrid line 
    /// </summary>
    /// <param name="_deSystemsForDisplay"></param>
    /// <returns></returns>
    private void GetListViewData()
    {
      BindableCollection<DarkEchoSystemsModel> lvdList = new BindableCollection<DarkEchoSystemsModel>();
      foreach (var sd in dataAccess.displayDESystems)
      {
        DarkEchoSystems.Add(dataAccess.BuildDisplayLine(sd));
      }
    }

    /// <summary>
    /// Fires every 100ms to check if a DB update has been received.
    /// Would be better to deal with this in the dependency event but proved unreliable
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void UpdateTimer_Tick(object sender, EventArgs e)
    {
      if (dataAccess.SystemUpdated)
      {
        UpdatedDisplayDESystems = displayDESystems.FindAll(x => x.Updated);

        foreach (var deSystem in UpdatedDisplayDESystems)
        {
          var index = DarkEchoSystems.IndexOf(DarkEchoSystems.First(x => x.StarSystemName == deSystem.StarSystem));

          DarkEchoSystems[index] = dataAccess.BuildDisplayLine(deSystem);
          DarkEchoSystems[index].HighLight = true;
          Task.Delay(7500).ContinueWith(t => ClearHightlight(index));
        }

        foreach (var deSystem in UpdatedDisplayDESystems)
        {
          dataAccess.displayDESystems[dataAccess.displayDESystems.FindIndex(x => x.StarSystem == deSystem.StarSystem)].Updated = false;
        }

        dataAccess.SystemUpdated = false;
        dataAccess.CalculateAverageChange();
        DisplayFactionInfAvgAndChange();

        CalculateSystemMovements();
      }
    }

    /// <summary>
    /// Work out number of systems where our influence moved up, down or remained unchanged
    /// </summary>
    void CalculateSystemMovements()
    {

      var SystemsUpdatedSinceTick = DarkEchoSystems.Where(x => x.VisitsToday > 0).ToList();
      var SystemsNotUpdatedSinceTick = DarkEchoSystems.Where(x => x.VisitsToday == 0).ToList();

      if (DateTime.Compare(DateTime.UtcNow, dataAccess.TickTime) > 0)
      {
        SystemsUpdatedSinceTick.Clear();
        SystemsUpdatedSinceTick = DarkEchoSystems.Where(x => x.Updated > dataAccess.TickTime).ToList();
        SystemsUp = SystemsUpdatedSinceTick.Count(x => x.InfluenceChange > 0.0);
        SystemsDown = SystemsUpdatedSinceTick.Count(x => x.InfluenceChange < 0.0);
      }
      else
      {
        // For systems that have not been updated we can use the displayed influence change value
        SystemsUp = SystemsNotUpdatedSinceTick.Count(x => x.InfluenceChange > 0.0);
        SystemsDown = SystemsNotUpdatedSinceTick.Count(x => x.InfluenceChange < 0.0);

        // For systems that have been updated use the calculated hidden change value. 
        SystemsUp = SystemsUp + SystemsUpdatedSinceTick.Count(x => x.InfluenceChangePrev > 0.0);
        SystemsDown = SystemsDown + SystemsUpdatedSinceTick.Count(x => x.InfluenceChangePrev < 0.0);
      }

      SystemsNoChange = DarkEchoSystems.Count - (SystemsUp + SystemsDown);
    }

    /// <summary>
    /// Display the average influence and influence movement across all systems
    /// </summary>
    void DisplayFactionInfAvgAndChange()
    {
      TotalFactionInfluence = dataAccess.TotalFactionInfluence;
      FactionInfluenceChange = dataAccess.FactionInfluenceChange;
      InfChangeBackgroundColour = dataAccess.FactionInfluenceChangeValue < 0.0 ? "Red" : "DarkGreen";
    }

    /// <summary>
    /// Open the graph screen
    /// </summary>
    public void Analytics()
    {
      _windowManager.ShowWindow(new OxyPlotChartViewModel(displayDESystems, ""), null, null);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="SelectedSystem"></param>
    public void RowSelect(DarkEchoSystemsModel SelectedSystem)
    {
      Int64 SystemAddress = displayDESystems[displayDESystems.IndexOf(displayDESystems.Find(x => x.StarSystem == SelectedSystem.StarSystemName))].SystemAddress;
      _windowManager.ShowWindow(new SystemHistoryViewModel(SystemAddress, displayDESystems), null, null);

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    public void SystemOverviewClick(object sender)
    {
      WindowManager windowManager = new WindowManager();
      windowManager.ShowWindow(new SystemDetailViewModel(displayDESystems, GetSystemName(sender)), null, null);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    public void SystemHistoryClick(object sender)
    {
      WindowManager windowManager = new WindowManager();
      windowManager.ShowWindow(new SystemHistoryViewModel(GetSystemAddress(sender), displayDESystems), null, null);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    public void EDDBClick(object sender)
    {
      System.Diagnostics.Process.Start($"https://eddb.io/system/{dataAccess.GetSystemEDDBID(GetSystemName(sender))}");
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    public void EliteBGSClick(object sender)
    {
      WebRequest webRequest = WebRequest.Create(string.Format("https://elitebgs.app/api/ebgs/v4/systems?name={0}", GetSystemName(sender).Replace("+", "%2B")));
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
    /// 
    /// </summary>
    /// <param name="SelectedSystem"></param>
    public void History(DarkEchoSystemsModel SelectedSystem)
    {
      RowSelect(SelectedSystem);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="SelectedSystem"></param>
    public void SystemInfo(DarkEchoSystemsModel SelectedSystem)
    {
      WindowManager windowManager = new WindowManager();
      windowManager.ShowWindow(new SystemDetailViewModel(displayDESystems, SelectedSystem.StarSystemName), null, null);
    }
    public void CheckChanged(object CheckedState)
    {
      bool cs = (bool) CheckedState;
      DisplayFullHistory = cs;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <returns></returns>
    private string GetSystemName(object sender)
    {
      MenuItem ReturnedMenuItem = (MenuItem) sender;
      ContextMenu cm = (ContextMenu) ReturnedMenuItem.Parent;
      DataGrid dg = (DataGrid) cm.PlacementTarget;
      DarkEchoSystemsModel selectedItem = (DarkEchoSystemsModel) dg.SelectedItem;
      return selectedItem.StarSystemName;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <returns></returns>
    private Int64 GetSystemAddress(object sender)
    {
      MenuItem ReturnedMenuItem = (MenuItem) sender;
      ContextMenu cm = (ContextMenu) ReturnedMenuItem.Parent;
      DataGrid dg = (DataGrid) cm.PlacementTarget;
      DarkEchoSystemsModel selectedItem = (DarkEchoSystemsModel) dg.SelectedItem;
      return selectedItem.SystemAddress;
    }

    /// <summary>
    /// 
    /// </summary>
    public void ClearHightlight(int row)
    {
      DarkEchoSystems[row].HighLight = false;
    }

    /// <summary>
    /// 
    /// </summary>
    public void Grid_SizeChanged()
    {
      if (Height < MaxHeight)
      {
        if ((Width + 2) >= DefaultWidth && Width != (DefaultWidth + 18))
        {
          Width = (DefaultWidth + 18);
          MaxWidth = Width;
        }
      }

      if (Height == MaxHeight && Width == (DefaultWidth + 18))
      {
        Width = DefaultWidth;
        MaxWidth = Width;
      }
    }

    // -----------------------------------------------------------------------------------------------------------------
    // --------------------------------------------       Properties     -----------------------------------------------
    // -----------------------------------------------------------------------------------------------------------------

    private BindableCollection<DarkEchoSystemsModel> _darkEchoSystems = new BindableCollection<DarkEchoSystemsModel>();
    private string _displayTickTime;
    private string _dBAccessMode;
    private string _factionSummary;
    private int _height;
    private int _width;
    private int _maxHeight;
    private int _maxWidth;
    private string _totalFactionInfluence;
    private string _factionInfluenceChange;
    private string _infChangeBackgroundColour;
    private int _systemsUp;
    private int _systemsDown;
    private int _systemsNoChange;

    public BindableCollection<DarkEchoSystemsModel> DarkEchoSystems
    {
      get { return _darkEchoSystems; }
      set
      {
        _darkEchoSystems = value;
        NotifyOfPropertyChange(() => DarkEchoSystems);
      }
    }

    public string DisplayTickTime
    {
      get { return _displayTickTime; }
      set
      {
        _displayTickTime = value;
        NotifyOfPropertyChange(() => DisplayTickTime);
      }
    }

    public string DBAccessMode
    {
      get { return _dBAccessMode; }
      set
      {
        _dBAccessMode = value;
        NotifyOfPropertyChange(() => DBAccessMode);
      }
    }

    public string FactionSummary
    {
      get { return _factionSummary; }
      set
      {
        _factionSummary = value;
        NotifyOfPropertyChange(() => FactionSummary);
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

    public string TotalFactionInfluence
    {
      get { return _totalFactionInfluence; }
      set
      {
        _totalFactionInfluence = value;
        NotifyOfPropertyChange(() => TotalFactionInfluence);
      }
    }

    public string FactionInfluenceChange
    {
      get { return _factionInfluenceChange; }
      set
      {
        _factionInfluenceChange = value;
        NotifyOfPropertyChange(() => FactionInfluenceChange);
      }
    }

    public string InfChangeBackgroundColour
    {
      get { return _infChangeBackgroundColour; }
      set
      {
        _infChangeBackgroundColour = value;
        NotifyOfPropertyChange(() => InfChangeBackgroundColour);
      }
    }

    public int SystemsUp
    {
      get { return _systemsUp; }
      set
      {
        _systemsUp = value;
        NotifyOfPropertyChange(() => SystemsUp);
      }
    }

    public int SystemsDown
    {
      get { return _systemsDown; }
      set
      {
        _systemsDown = value;
        NotifyOfPropertyChange(() => SystemsDown);
      }
    }

    public int SystemsNoChange
    {
      get { return _systemsNoChange; }
      set
      {
        _systemsNoChange = value;
        NotifyOfPropertyChange(() => SystemsNoChange);
      }
    }


    public System.Windows.Media.Imaging.BitmapImage UpTriangle { get; set; }
    public System.Windows.Media.Imaging.BitmapImage DownTriangle { get; set; }
    public System.Windows.Media.Imaging.BitmapImage NoChange { get; set; }
    public System.Windows.Media.Imaging.BitmapImage SysInfo { get; set; }
    public System.Windows.Media.Imaging.BitmapImage SysHistory { get; set; }
    public System.Windows.Media.Imaging.BitmapImage WebImage { get; set; }
    public string WindowTitle { get; set; }
    public static bool DisplayFullHistory { get; set; }
  }
}
