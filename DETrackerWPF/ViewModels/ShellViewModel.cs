using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using System.Xml.Serialization;
using Caliburn.Micro;
using DETrackerWPF.Models;
using MahApps.Metro.Controls;


namespace DETrackerWPF.ViewModels
{


    [Export(typeof(ShellViewModel))]
    public class ShellViewModel : Screen
    {

        DataAccess dataAccess = new DataAccess();

        private BindableCollection<DarkEchoSystemsModel> _darkEchoSystems = new BindableCollection<DarkEchoSystemsModel>();
        private string _displayTickTime;
        private string _dBAccessMode;
        private string _factionSummary;
        private int _height;
        private int _maxHeight;
        private int _maxWidth;
        private string _totalFactionInfluence;
        private string _factionInfluenceChange;
        private string _infChangeBackgroundColour;

        public List<DESystemsForDisplay> displayDESystems = new List<DESystemsForDisplay>();
        public List<DESystemsForDisplay> UpdatedDisplayDESystems = new List<DESystemsForDisplay>();

        System.Windows.Threading.DispatcherTimer UpdateTimer = new DispatcherTimer();

        Helper helper = new Helper();

        // Test stuff - Remember to remove
        int row = 0;
        private readonly IWindowManager _windowManager;

        [ImportingConstructor]
        public ShellViewModel(IWindowManager windowManager)
        {

            _windowManager = windowManager;

            UpTriangle = helper.Convert(DETrackerWPF.Properties.Resources.UpTriangle);
            DownTriangle = helper.Convert(DETrackerWPF.Properties.Resources.DownTriangle);
            NoChange = helper.Convert(DETrackerWPF.Properties.Resources.NoChange);
            SysInfo = helper.Convert(DETrackerWPF.Properties.Resources.InfoBlue);
            SysHistory = helper.Convert(DETrackerWPF.Properties.Resources.analytics);

            //Get and Load the connect strings
            dataAccess.DecryptCheck();

            // Read the time of the tick
            dataAccess.GetTickTime();

            DBAccessMode = "Access Mode : Local (Development DB)";
            DisplayTickTime = "Configured Tick Time : " + dataAccess.TickTime.ToString(@"H:mm UTC");

            // Get the file version
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;
            WindowTitle = string.Format("Dark Echo Influence Tracker v{0} (Built {1})", version, DETrackerWPF.Properties.Resources.BuildDate);

            // Get DE systems, size main screen to suit
            displayDESystems = dataAccess.ReadDeSystemsTable();
            Width = 1320;
            MaxWidth = Width;
            Height = (displayDESystems.Count * 23) + 6;
            MaxHeight = Height;

            // Build the summary line
            var systemsControlled = displayDESystems.Count(x => x.SysFaction.Name == "Dark Echo");
            var popControlled = displayDESystems.Where(x => x.SysFaction.Name == "Dark Echo").Sum(x => x.Population);
            FactionSummary = string.Format("Population Controlled : {0:###,###,###} / Systems Controlled {1} / Present in {2} Systems", popControlled, systemsControlled, displayDESystems.Count);

            GetListViewData();
            CalculateSystemMovements();

            // Display the avg inf and change
            if (DateTime.Compare(DateTime.UtcNow, dataAccess.TickTime) > 0)
                dataAccess.CalculateAverageChange();
            else
                dataAccess.CalculateAvfValuesPreTick();

            DisplayFactionInfAvgAndChange();

            dataAccess.RegisterForChanges();

            UpdateTimer.Tick += UpdateTimer_Tick;
            UpdateTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            dataAccess.SystemUpdated = false;
            UpdateTimer.Start();
        }

        /// <summary>
        /// 
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
        /// 
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
        /// Work out number of systems that moved up, down ro stayed static
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
        /// 
        /// </summary>
        public void Analytics()
        {
            //_windowManager.ShowWindow(new GraphViewModel(displayDESystems), null, null);
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
            MenuItem ReturnedMenuItem = (MenuItem)sender;
            ContextMenu cm = (ContextMenu)ReturnedMenuItem.Parent;
            DataGrid dg = (DataGrid)cm.PlacementTarget;
            DarkEchoSystemsModel selectedItem = (DarkEchoSystemsModel)dg.SelectedItem;
            return selectedItem.SystemAddress;
        }
        /// <summary>
        /// 
        /// </summary>
        public void ClearHightlight(int row)
        {
            DarkEchoSystems[row].HighLight = false;
        }

        public BindableCollection<DarkEchoSystemsModel> DarkEchoSystems
        {
            get { return _darkEchoSystems; }
            set
            {
                _darkEchoSystems = value;
                NotifyOfPropertyChange(() => DarkEchoSystems);
            }
        }

        /// <summary>
        /// 
        /// </summary>
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

        /// <summary>
        /// 
        /// </summary>
        public void UpdateTickTime()
        {
            DisplayTickTime = "Configured Tick Time : 17:30 UTC";
        }


        private int _width;

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

        private int _systemsUp;

        public int SystemsUp
        {
            get { return _systemsUp; }
            set
            {
                _systemsUp = value;
                NotifyOfPropertyChange(() => SystemsUp);
            }
        }

        private int _systemsDown;

        public int SystemsDown
        {
            get { return _systemsDown; }
            set
            {
                _systemsDown = value;
                NotifyOfPropertyChange(() => SystemsDown);
            }
        }


        private int _systemsNoChange;

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

        public string WindowTitle { get; set; }

        public void Grid_SizeChanged()
        {
            if (Height < MaxHeight)
            {
                if (Width >= 1320 && Width != 1337)
                {
                    Width = 1337;
                    MaxWidth = Width;
                }
            }

            if (Height == MaxHeight && Width == 1337)
            {
                Width = 1320;
                MaxWidth = Width;
            }
        }


    }
}
