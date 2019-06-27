using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using DETrackerWPF.ViewModels;
using Newtonsoft.Json;
using System.Configuration;
using System.Security.Cryptography;
using System.Threading;
using Caliburn.Micro;
using DETrackerWPF.Models;

namespace DETrackerWPF
{
    public class DataAccess : Screen
    {

        // Backing Fields
        private int _TickTimeHour;
        private int _tickTimeMin;
        private string _remoteConnectionString;
        private string _localConnectionString;
        private string _totalFactionInfluence;
        private string _factionInfluenceChange;
        private double _factionInfluenceChangeValue;

        // The Global SQL Connections stuff
        private string connectionStringRemote = string.Empty;
        private string connectionStringLocal = string.Empty;

        public static string connectionString = string.Empty;

        // Data Lists
        //public List<DESystemsForDisplay> displayDESystems = new List<DESystemsForDisplay>();
        public List<ChangedSystemData> updatedData = new List<ChangedSystemData>();
        SystemOverviewModel _systemOverviewModel = new SystemOverviewModel();

        GetDistance getDistance = new GetDistance();
        Helper helper = new Helper();

        private double[] disciLocation = new double[] { 16.03125, 97.59375, -29.59375 };

        

        // ------------------------- Methods -----------------------------------

        /// <summary>
        /// If this tick has not happened this will calculate the information for the previous tick
        /// will only be called once as the program starts up
        /// </summary>
        public void CalculateAvfValuesPreTick()
        {
            double dayMinus1Inf = 0.0;
            double dayMinus2Inf = 0.0;
            double todaysTotalInf = 0.0;

            

            // Tick Not Happened so we just display data for the previous tick.
            foreach (var sd in displayDESystems)
            {
                // Have we got any history records, catch for when moved into new System
                if (sd.FactionHistory.FindIndex(x => x.timestamp.Date == DateTime.UtcNow.AddDays(-1).Date) >= 0)
                {
                    DESystemsHistory histRec = sd.FactionHistory[sd.FactionHistory.FindIndex(x => x.timestamp.Date == DateTime.UtcNow.AddDays(-1).Date)];
                    dayMinus1Inf = dayMinus1Inf + (histRec.Factions[histRec.Factions.FindIndex(x => x.Name == "Dark Echo")].Influence * 100);

                    // Have we 2 days history?
                    if (sd.FactionHistory.FindIndex(x => x.timestamp.Date == DateTime.UtcNow.AddDays(-2).Date) >= 0)
                    {
                        histRec = sd.FactionHistory[sd.FactionHistory.FindIndex(x => x.timestamp.Date == DateTime.UtcNow.AddDays(-2).Date)];
                        dayMinus2Inf = dayMinus2Inf + (histRec.Factions[histRec.Factions.FindIndex(x => x.Name == "Dark Echo")].Influence * 100);
                    }
                }
                else
                {
                    DESystemsHistory histRec = sd.FactionHistory[sd.FactionHistory.FindIndex(x => x.timestamp.Date == DateTime.UtcNow.Date)];
                    dayMinus1Inf = dayMinus1Inf + (histRec.Factions[histRec.Factions.FindIndex(x => x.Name == "Dark Echo")].Influence * 100);
                }
            }

            var avgInfForDayMinus1 = dayMinus1Inf / displayDESystems.Count;
            var infChange = dayMinus1Inf - dayMinus2Inf;

            TotalFactionInfluence = string.Format("Average Faction Influence : {0:0.##}%", avgInfForDayMinus1);
            FactionInfluenceChange = string.Format("Total Influence Change : {0:0.##}%", infChange);
            FactionInfluenceChangeValue = infChange;

        }
        /// <summary>
        /// Get the average influence change across all systems between now and previous day
        /// </summary>
        public void CalculateAverageChange()
        {
            double dayMinus1Inf = 0.0;
            double dayMinus2Inf = 0.0;
            double todaysTotalInf = 0.0;

            // Are we past tick time?
            if (DateTime.Compare(DateTime.UtcNow, TickTime) > 0)
            {
                // Tick has happened
                foreach (var sd in displayDESystems)
                {
                    //do we have as record for today
                    if (sd.FactionHistory.FindIndex(x => x.timestamp.Date == DateTime.UtcNow.Date) >= 0)
                    {
                        //seems to, lets grab it
                        DESystemsHistory histRec = sd.FactionHistory[sd.FactionHistory.FindIndex(x => x.timestamp.Date == DateTime.UtcNow.Date)];

                        // is the update time after the tick time
                        if (DateTime.Compare(histRec.timestamp, TickTime) > 0)
                        {
                            // Yes, add to inf total for today
                            todaysTotalInf = todaysTotalInf + (histRec.Factions[histRec.Factions.FindIndex(x => x.Name == "Dark Echo")].Influence * 100);
                        }
                        else
                        {
                            // No, use previous days value
                            if (sd.FactionHistory.FindIndex(x => x.timestamp.Date == DateTime.UtcNow.AddDays(-1).Date) >= 0)
                            {
                                histRec = sd.FactionHistory[sd.FactionHistory.FindIndex(x => x.timestamp.Date == DateTime.UtcNow.AddDays(-1).Date)];
                                todaysTotalInf = todaysTotalInf + (histRec.Factions[histRec.Factions.FindIndex(x => x.Name == "Dark Echo")].Influence * 100);
                            }
                            else
                            {
                                dayMinus1Inf = dayMinus1Inf + (histRec.Factions[histRec.Factions.FindIndex(x => x.Name == "Dark Echo")].Influence * 100);
                            }
                        }
                    }
                    else
                    {
                        // No record for today so use yesterdays value
                        if (sd.FactionHistory.FindIndex(x => x.timestamp.Date == DateTime.UtcNow.AddDays(-1).Date) >= 0)
                        {
                            DESystemsHistory histRec = sd.FactionHistory[sd.FactionHistory.FindIndex(x => x.timestamp.Date == DateTime.UtcNow.AddDays(-1).Date)];
                            todaysTotalInf = todaysTotalInf + (histRec.Factions[histRec.Factions.FindIndex(x => x.Name == "Dark Echo")].Influence * 100);
                        }
                    }
                    // Get yesterdays avg inf
                    if (sd.FactionHistory.FindIndex(x => x.timestamp.Date == DateTime.UtcNow.AddDays(-1).Date) >= 0)
                    {
                        DESystemsHistory histRecMinus1 = sd.FactionHistory[sd.FactionHistory.FindIndex(x => x.timestamp.Date == DateTime.UtcNow.AddDays(-1).Date)];
                        dayMinus1Inf = dayMinus1Inf + (histRecMinus1.Factions[histRecMinus1.Factions.FindIndex(x => x.Name == "Dark Echo")].Influence * 100);
                    }
                }
                // Calculate the values
                var avgInfForToday = todaysTotalInf / displayDESystems.Count;
                var infChange = todaysTotalInf - dayMinus1Inf;
                // Display the data

                TotalFactionInfluence = string.Format("Average Faction Influence : {0:0.##}%", avgInfForToday);
                FactionInfluenceChange = string.Format("Total Influence Change : {0:0.##}%", infChange);
                FactionInfluenceChangeValue = infChange;
            }
        }
        /// <summary>
        /// Read tick time from Parms table  and return a string representation
        /// </summary>
        /// <returns></returns>
        public void GetTickTime()
        {
            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                SqlCommand sqlCmd = new SqlCommand("SELECT tickTimeHour, tickTimeMin FROM Params", sqlConnection);
                sqlConnection.Open();
                using (sqlCmd)
                {
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            TickTimeHour = reader.GetInt32(0);
                            TickTimeMin = reader.GetInt32(1);
                        }
                    }
                }
                sqlConnection.Close();
            }
            var tickTime = new TimeSpan(TickTimeHour, TickTimeMin, 00);
            TickTime = DateTime.UtcNow.Date + tickTime;
        }
        /// <summary>
        /// Setup SqlDependecy to check for DB updates
        /// </summary>
        public void RegisterForChanges()
        {
            using (SqlConnection sqlConnectionSub = new SqlConnection(connectionString))
            {

                if (sqlConnectionSub.State == ConnectionState.Closed)
                    sqlConnectionSub.Open();

                SqlCommand sqlCmd = new SqlCommand("Queue_Dependecy", sqlConnectionSub);
                sqlCmd.CommandType = CommandType.StoredProcedure;
                SqlDependency sqlDependency = new SqlDependency(sqlCmd);
                sqlDependency.OnChange += new OnChangeEventHandler(SqlDependecy_OnChange);
                SqlDataReader reader = sqlCmd.ExecuteReader();
                while (reader.Read())
                {
                }
            }
        }

        /// <summary>
        /// Fired when a DB update is detected, copy the existing listview source list to be used for compare later, set dbUpdated to true (this is checked in the timer code)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SqlDependecy_OnChange(object sender, SqlNotificationEventArgs e)
        {
            // Unsubscribe the event
            SqlDependency dependency = sender as SqlDependency;
            dependency.OnChange -= SqlDependecy_OnChange;

            // Re-Subscribe to  the event
            RegisterForChanges();

            // Do what we are here do do
            GetChangedSystems();
        }
        /// <summary>
        /// 
        /// </summary>
        private void GetChangedSystems()
        {
            updatedData.Clear();

            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                SqlCommand sqlCmd = new SqlCommand("Get_Changed_Systems", sqlConnection);
                sqlCmd.CommandType = CommandType.StoredProcedure;

                sqlConnection.Open();
                using (sqlCmd)
                {
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            updatedData.Add(MapDEUpdatedData(reader));
                        }
                    }
                }
                sqlConnection.Close();
            }
            foreach (var changedSystemData in updatedData)
            {
                var index = displayDESystems.IndexOf(displayDESystems.Find(x => x.StarSystem == changedSystemData.StarSystem));

                if (DateTime.Compare(changedSystemData.timestamp, displayDESystems[index].timestamp) > 0)
                {
                    GetDailyVisitsBySystem(changedSystemData.SystemAddress);

                    DESystemsForDisplay updatedSystem = ReadUpdatedSystem(changedSystemData.SystemAddress.ToString());

                    // Update system list with new data
                    updatedSystem.Updated = true;
                    displayDESystems[displayDESystems.FindIndex(m => m.SystemAddress == updatedSystem.SystemAddress)] = updatedSystem;

                    SystemUpdated = true;
                }
            }

        }
        /// <summary>
        /// Read in all systems where DE is present and store in list
        /// </summary>
        public DESystemsForDisplay ReadUpdatedSystem(string sysAddr)
        {
            DESystemsForDisplay changedSystem = new DESystemsForDisplay();

            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                SqlCommand sqlCmd = new SqlCommand("Read_Updated_System", sqlConnection);
                sqlCmd.CommandType = CommandType.StoredProcedure;
                sqlCmd.Parameters.AddWithValue("@sysAddress", sysAddr);
                sqlConnection.Open();
                using (sqlCmd)
                {
                    using (SqlDataReader reader = sqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            changedSystem = MapDESystemsForDisplay(reader);
                        }
                    }
                }
                sqlConnection.Close();
            }
            return changedSystem;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private ChangedSystemData MapDEUpdatedData(SqlDataReader reader)
        {
            ChangedSystemData cd = new ChangedSystemData
            {
                StarSystem = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                SystemAddress = reader.GetInt64(1), timestamp = reader.GetDateTime(2)
            };
            return cd;
        }
        /// <summary>
        /// Read in Dark Echo Systems
        /// </summary>
        /// <returns></returns>
        public List<DESystemsForDisplay> ReadDeSystemsTable()
        {
            displayDESystems = new List<DESystemsForDisplay>();
            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                using (SqlCommand sqlCmd = new SqlCommand("Read_DE_Systems", sqlConnection))
                {
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlConnection.Open();
                    using (sqlCmd)
                    {
                        using (SqlDataReader reader = sqlCmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                displayDESystems.Add(MapDESystemsForDisplay(reader));
                            }
                        }
                    }
                    sqlConnection.Close();
                }
            }
            GetDailyVisits();
            return displayDESystems;
        }

        /// <summary>
        /// Get the daily visit history
        /// </summary>
        public void GetDailyVisits()
        {
            DeVisitsHistory = new List<VisitHistory>();
            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                using (SqlCommand sqlCmd = new SqlCommand("GetDailyVisits", sqlConnection))
                {
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlConnection.Open();
                    using (sqlCmd)
                    {
                        using (SqlDataReader reader = sqlCmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                MapVisitHistory(reader);
                            }
                        }
                    }
                    sqlConnection.Close();
                }
            }
        }
        /// <summary>
        /// Get the daily visit history
        /// </summary>
        private void GetDailyVisitsBySystem(Int64 sysAddr)
        {
            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                using (SqlCommand sqlCmd = new SqlCommand("GetDailyVisitsForSystem", sqlConnection))
                {
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    sqlCmd.Parameters.AddWithValue("@sysAddress", sysAddr);

                    sqlConnection.Open();
                    using (sqlCmd)
                    {
                        using (SqlDataReader reader = sqlCmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                UpdateVisitHistory(reader, sysAddr);
                            }
                        }
                    }
                    sqlConnection.Close();
                }
            }
        }
        /// <summary>
        /// Update the number of visits to this system
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="sysAddr"></param>
        private void UpdateVisitHistory(SqlDataReader reader, Int64 sysAddr)
        {
            // Grab a copy of the visit history list for this system
            VisitHistory VistHistoryList = DeVisitsHistory.Find(x => x.SystemsAddress == sysAddr);

            // If same day just update the count
            if (VistHistoryList.Visted.Last().timestamp.Date == DateTime.UtcNow.Date)
                VistHistoryList.Visted.Last().Visits = reader.GetInt32(2);
            else
            {
                // else create new record for today
                SystemsVisitHistory visitRec = new SystemsVisitHistory();
                visitRec.StarSystem = reader.GetString(1);
                visitRec.Visits = reader.GetInt32(2);
                visitRec.timestamp = reader.GetDateTime(3);
                VistHistoryList.Visted.Add(visitRec);
            }
        }
        /// <summary>
        /// Map the systems visted record into a list
        /// </summary>
        /// <param name="reader"></param>
        private void MapVisitHistory(SqlDataReader reader)
        {
            VisitHistory vHist = new VisitHistory();
            SystemsVisitHistory visitRec = new SystemsVisitHistory();
            vHist.Visted = new List<SystemsVisitHistory>();

            Int64 sysAddr = reader.GetInt64(0);

            if (DeVisitsHistory.FirstOrDefault(x => x.SystemsAddress == sysAddr) == null)
            {
                vHist.SystemsAddress = sysAddr;
                vHist.Visted.Add(visitRec);
                DeVisitsHistory.Add(vHist);
            }
            else
            {
                vHist = DeVisitsHistory.FirstOrDefault(x => x.SystemsAddress == sysAddr);
                vHist.Visted.Add(visitRec);
            }

            visitRec.StarSystem = reader.GetString(1);
            visitRec.Visits = reader.GetInt32(2);
            visitRec.timestamp = reader.GetDateTime(3);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sd"></param>
        /// <returns></returns>
        public DarkEchoSystemsModel GetUpdatedSystem(DESystemsForDisplay sd)
        {
            return BuildDisplayLine(sd);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="systemName"></param>
        public SystemOverviewModel GetSystemInfo(string systemName)
        {
            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                using (SqlCommand SqlCmd = new SqlCommand("GetSystemInfo", sqlConnection))
                {
                    SqlCmd.CommandType = CommandType.StoredProcedure;
                    SqlCmd.Parameters.AddWithValue("@systemName", systemName);

                    sqlConnection.Open();
                    using (SqlCmd)
                    {
                        using (SqlDataReader reader = SqlCmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                _systemOverviewModel = (MapSystemData(reader));
                            }
                        }
                    }
                    sqlConnection.Close();
                }
            }
            GetStations(systemName);
            return _systemOverviewModel;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        SystemOverviewModel MapSystemData(SqlDataReader reader)
        {
            SystemOverviewModel so = new SystemOverviewModel
            {
                SystemName = reader.GetString(0),
                Allegiance = reader.GetString(1),
                Government = Helper.Clean(reader.GetString(2)),
                Population = reader.GetInt64(3),
                ControllingFaction = JsonConvert.DeserializeObject<SystemFaction>(reader.GetString(4)).Name,
                SecurityLevel = Helper.Clean(reader.GetString(5)),
                EconomyPrimary = Helper.Clean(reader.GetString(6)),
                EconomySecondary = Helper.Clean(reader.GetString(7)),
                StarPos = (List<double>) JsonConvert.DeserializeObject(reader.GetString(8), typeof(List<double>))
            };

            return so;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="systemName"></param>
        void GetStations(string systemName)
        {
            _systemOverviewModel.StationsInSystem = new List<StationList>();

            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                using (SqlCommand SqlCmd = new SqlCommand("GetStations", sqlConnection))
                {
                    SqlCmd.CommandType = CommandType.StoredProcedure;
                    SqlCmd.Parameters.AddWithValue("@systemName", systemName);

                    sqlConnection.Open();
                    using (SqlDataReader reader = SqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _systemOverviewModel.StationsInSystem.Add(MapStations(reader));
                        }
                    }
                    sqlConnection.Close();
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        StationList MapStations(SqlDataReader reader)
        {
            StationList s = new StationList
            {
                StationName = reader.GetString(0),
                ControllingFaction = reader.IsDBNull(1) ? "Screw up" : reader.GetString(1),
                StationType = reader.GetString(2),
                IsPlantery = Convert.ToBoolean(reader.GetString(3)),
                MaxLandingPad = reader.GetString(4),
                DistanceFromStar = reader.GetInt32(5)
            };

            return s;
        }
        /// <summary>
        /// 
        /// </summary>
        public async Task<string> GetClosePlayerFactions(SystemOverviewModel _systemOverview, List<ClosePlayFactions> _closestPlayerFactions, List<ExpansionSystems> _expansionSystems)
        {


            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                using (SqlCommand SqlCmd = new SqlCommand("GetPlayerFactions", sqlConnection))
                {
                    SqlCmd.CommandType = CommandType.StoredProcedure;

                    await sqlConnection.OpenAsync();
                    using (SqlDataReader reader = await SqlCmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            var pf = MapClosePlayFactions(reader);
                            if (pf.SystemName == "Dark Echo")
                                continue;

                            var distance = getDistance.Distance3D(_systemOverview.StarPos[0], _systemOverview.StarPos[1], _systemOverview.StarPos[2], pf.StarPos[0], pf.StarPos[1], pf.StarPos[2]);
                            if (distance < 30)
                            {
                                pf.Distance = distance;
                                _closestPlayerFactions.Add(pf);
                            }
                        }
                        reader.Close();
                    }
                    sqlConnection.Close();
                }
            }
            GetExpansionTargets(_systemOverview, _closestPlayerFactions, _expansionSystems);
            return "Complete";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private ClosePlayFactions MapClosePlayFactions(SqlDataReader reader)
        {
            ClosePlayFactions cpf = new ClosePlayFactions
            {
                StarPos = new List<double> {reader.GetDouble(1), reader.GetDouble(2), reader.GetDouble(3)},
                SystemName = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                EddbID = reader.GetInt32(4),
                ControllingFaction = reader.GetString(5),
                ControllingFactionID = reader.GetInt32(6)
            };

            return cpf;
        }
        void GetExpansionTargets(SystemOverviewModel _systemOverview, List<ClosePlayFactions> _closestPlayerFactions, List<ExpansionSystems> _expansionSystems)
        {
            List<ExpansionSystems> _allExpansionTargets = new List<ExpansionSystems>();

            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                using (SqlCommand SqlCmd = new SqlCommand("GetPopulatedSystems", sqlConnection))
                {
                    SqlCmd.CommandType = CommandType.StoredProcedure;

                    sqlConnection.Open();
                    using (SqlDataReader reader = SqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var exp = MapExpansionSystems(reader);
                            if (getDistance.Within3DManhattanDistance(_systemOverview.StarPos[0], _systemOverview.StarPos[1], _systemOverview.StarPos[2], exp.x, exp.y, exp.z, 20))
                            {
                                exp.Distance = getDistance.Distance3D(_systemOverview.StarPos[0], _systemOverview.StarPos[1], _systemOverview.StarPos[2], exp.x, exp.y, exp.z);
                                _allExpansionTargets.Add(exp);
                            }
                        }

                        reader.Close();
                    }

                    sqlConnection.Close();
                }
            }

            foreach (var exp in _allExpansionTargets)
            {
                List<FactionsPresent> ffd = GetSystemsFactions(exp.EddbID);

                if ((ffd.Count - 1) > 6)
                    continue;

                if (ffd.Exists(x => x.minor_faction_id == 11217))
                    continue;

                // Valid expansion target
                exp.FactionsInSystem = (ffd.Count - 1);
                _expansionSystems.Add(exp);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="systemID"></param>
        /// <returns></returns>
        List<FactionsPresent> GetSystemsFactions(int systemID)
        {

            List<FactionsPresent> ffd = new List<FactionsPresent>();

            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                using (SqlCommand SqlCmd = new SqlCommand("GetFactionsInSystems", sqlConnection))
                {
                    SqlCmd.CommandType = CommandType.StoredProcedure;
                    SqlCmd.Parameters.AddWithValue("@systemID", systemID);
                    sqlConnection.Open();
                    using (SqlDataReader reader = SqlCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ffd = (List<FactionsPresent>)JsonConvert.DeserializeObject(reader.GetString(0), typeof(List<FactionsPresent>));
                        }
                        reader.Close();
                    }
                    sqlConnection.Close();
                }
            }

            return ffd;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private ExpansionSystems MapExpansionSystems(SqlDataReader reader)
        {
            ExpansionSystems expSys = new ExpansionSystems
            {
                SystemName = reader.GetString(0),
                x = reader.GetDouble(1),
                y = reader.GetDouble(2),
                z = reader.GetDouble(3),
                EddbID = reader.GetInt32(4)
            };

            return expSys;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sd"></param>
        /// <param name="dd"></param>
        public DarkEchoSystemsModel BuildDisplayLine(DESystemsForDisplay sd)
        {

            displayData dd = new displayData();
            DarkEchoSystemsModel lvd = new DarkEchoSystemsModel();

            foreach (var f in sd.FactionHistory[sd.FactionHistory.Count - 1].Factions)
            {
                if (f.Name == "Dark Echo")
                {
                    dd.fi = f.Influence;
                    dd.happiness = FactionHappiness(f.Happiness);
                }
            }
            // Get yesterdays Influence
            if (sd.FactionHistory.Count > 1)
            {
                foreach (var f in sd.FactionHistory[sd.FactionHistory.Count - 2].Factions)
                {
                    if (f.Name == "Dark Echo")
                        dd.fiPrev = f.Influence;
                }
            }
            else
                dd.fiPrev = dd.fi;

            //Get day before yesterdays influence
            if (sd.FactionHistory.Count > 2)
            {
                foreach (var f in sd.FactionHistory[sd.FactionHistory.Count - 3].Factions)
                {
                    if (f.Name == "Dark Echo")
                        dd.fiPrevMinus2 = f.Influence;
                }
            }
            else
                dd.fiPrevMinus2 = dd.fi;

            // set the bool used in the Style trigger for row foreground color, used to highlight a systems we are in but do not yet control
            lvd.NotControlledSystem = sd.SysFaction.Name == "Dark Echo";

            List<double> factionsInfs = sd.FactionHistory[sd.FactionHistory.Count - 1].Factions.Select(factions => factions.Influence).ToList();
            try
            {
                // Try and get the faction that is closest to our influence, if errors likely we are monitoring a system we are not yet in...
                dd.closest = factionsInfs.OrderBy(y => Math.Abs(sd.FactionHistory[sd.FactionHistory.Count - 1].Factions[sd.FactionHistory[sd.FactionHistory.Count - 1].Factions.FindIndex(x => x.Name == "Dark Echo")].Influence - y)).ElementAt(1);
            }
            catch (Exception e)
            {
                dd.closest = 0.0f;
            }

            if (dd.closest != 0.0)
            {
                dd.ourInf = factionsInfs.OrderBy(y => Math.Abs(sd.FactionHistory[sd.FactionHistory.Count - 1].Factions[sd.FactionHistory[sd.FactionHistory.Count - 1].Factions.FindIndex(x => x.Name == "Dark Echo")].Influence - y)).ElementAt(0);
                List<double> factionsInfsSorted = factionsInfs.OrderByDescending(x => x).ToList();
                int factionPosition = factionsInfsSorted.FindIndex(x => x == dd.ourInf) + 1;
            }

            dd.fInf = (Convert.ToSingle(dd.fi, new CultureInfo("en-US")) * 100.0f).ToString("F");
            dd.fGap = (Convert.ToSingle((dd.ourInf - dd.closest), new CultureInfo("en-US")) * 100.0f).ToString("F");
            dd.iChange = (Convert.ToSingle((dd.fi - dd.fiPrev), new CultureInfo("en-US")) * 100.0f).ToString("F");
            dd.iChangePrev = (Convert.ToSingle((dd.fiPrev - dd.fiPrevMinus2), new CultureInfo("en-US")) * 100.0f).ToString("F");

            // Set image index for Inf Change
            if (Math.Abs((dd.fiPrev - dd.fi)) < 0.0001)
                dd.imageIndex = 2;
            else if ((dd.fiPrev - dd.fi) < 0)
                dd.imageIndex = 1;
            else if ((dd.fiPrev - dd.fi) > 0)
                dd.imageIndex = 0;

            dd.distFromDisci = Convert.ToSingle(getDistance.Distance3D(disciLocation[0], disciLocation[1], disciLocation[2], sd.StarPos[0], sd.StarPos[1], sd.StarPos[2]), new CultureInfo("en-US")).ToString("F");

            Helper helper = new Helper();

            if (Convert.ToDouble(dd.iChange) == 0.0)
                lvd.InfluenceChangeImage = helper.Convert(DETrackerWPF.Properties.Resources.InfNoChange);

            if (Convert.ToDouble(dd.iChange) > 0.0)
                lvd.InfluenceChangeImage = helper.Convert(DETrackerWPF.Properties.Resources.Up_Green);

            if (Convert.ToDouble(dd.iChange) < 0.0)
                lvd.InfluenceChangeImage = helper.Convert(DETrackerWPF.Properties.Resources.down_Red);

            if (DateTime.Compare(TickTime, sd.timestamp) > 0)
                lvd.TickStatusImage = helper.Convert(DETrackerWPF.Properties.Resources.ok_checkmark_red_T);
            else
                lvd.TickStatusImage = helper.Convert(DETrackerWPF.Properties.Resources.ok_checkmark_green_T);

            var visitsToday = DeVisitsHistory.Find(x => x.SystemsAddress == sd.SystemAddress);
            var visitedToday = 0;
            if (visitsToday.Visted.Last().timestamp.Date == DateTime.UtcNow.Date)
                visitedToday = visitsToday.Visted.Last().Visits;

            lvd.StarSystemName = sd.StarSystem;
            lvd.DistanceFromDisci = dd.distFromDisci;
            lvd.DarkEchoInfluence = Convert.ToDouble(dd.ourInf) * 100;
            lvd.InfluenceChange = Convert.ToDouble(dd.iChange);
            lvd.InfluenceChangePrev = Convert.ToDouble(dd.iChangePrev);
            lvd.GapToNextFaction = Convert.ToDouble(dd.fGap);
            lvd.ActiveStates = ActiveStates(sd);
            lvd.PendingStates = PendingStates(sd);
            lvd.RecoveringStates = RecoveringStates(sd);
            lvd.happiness = dd.happiness;
            lvd.Updated = sd.timestamp;
            lvd.VisitsToday = visitedToday;
            lvd.VisitsTotal = sd.Visted;
            lvd.FactionsInSystem = sd.FactionHistory[sd.FactionHistory.Count - 1].Factions.Count - 1;

            lvd.SystemAddress = sd.SystemAddress;
            lvd.InfoImage = helper.Convert(DETrackerWPF.Properties.Resources.InfoBlue);
            lvd.ToolTipText = String.Format("System: {0}\r\nPopulation: {1:###,###,###} \r\nSecurity: {2}\r\nEconomy: {3}/{4}",sd.StarSystem, sd.Population, Helper.Clean(sd.SystemSecurity), Helper.Clean(sd.SystemEconomy), Helper.Clean(sd.SystemSecondEconomy));
            return lvd;

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="happinessBand"></param>
        /// <returns></returns>
        string FactionHappiness(string happinessBand)
        {
            //Happiness values are: (Elated, Happy, Discontented, Unhappy, Despondent)
            var howHappy = "Unknown";

            switch (happinessBand)
            {
                case "$Faction_HappinessBand1;":
                    howHappy = "Elated";
                    break;
                case "$Faction_HappinessBand2;":
                    howHappy = "Happy";
                    break;
                case "$Faction_HappinessBand3;":
                    howHappy = "Discontented";
                    break;
                case "$Faction_HappinessBand4;":
                    howHappy = "Unhappy";
                    break;
                case "$Faction_HappinessBand5;":
                    howHappy = "Despondent";
                    break;
                default:
                    howHappy = "Happy";
                    break;
            }

            return howHappy;
        }
        /// <summary>
        /// Retrun string of pending states
        /// </summary>
        /// <returns></returns>
        string ActiveStates(DESystemsForDisplay sd)
        {
            string fState = string.Empty;

            foreach (var f in sd.FactionHistory[sd.FactionHistory.Count - 1].Factions)
            {
                if (f.Name == "Dark Echo")
                {
                    if (f.ActiveStates != null)
                    {
                        for (int i = 0; i < f.ActiveStates.Count; i++)
                        {
                            fState += f.ActiveStates[i].State;
                            if (i < f.ActiveStates.Count - 1)
                                fState += ", ";
                        }
                    }
                    else
                        fState = "None";
                }
            }

            return fState;
        }
        /// <summary>
        /// Retrun string of pending states
        /// </summary>
        /// <returns></returns>
        string PendingStates(DESystemsForDisplay sd)
        {
            string fState = string.Empty;

            foreach (var f in sd.FactionHistory[sd.FactionHistory.Count - 1].Factions)
            {
                if (f.Name == "Dark Echo")
                {
                    if (f.PendingStates != null)
                    {
                        for (int i = 0; i < f.PendingStates.Count; i++)
                        {
                            fState += f.PendingStates[i].State;
                            if (i < f.PendingStates.Count - 1)
                                fState += ", ";
                        }
                    }
                    else
                        fState = "None";
                }
            }

            return fState;
        }
        /// <summary>
        /// Retrun string of pending states
        /// </summary>
        /// <returns></returns>
        string RecoveringStates(DESystemsForDisplay sd)
        {
            string fState = string.Empty;

            foreach (var f in sd.FactionHistory[sd.FactionHistory.Count - 1].Factions)
            {
                if (f.Name == "Dark Echo")
                {
                    if (f.RecoveringStates != null)
                    {
                        for (int i = 0; i < f.RecoveringStates.Count; i++)
                        {
                            fState += f.RecoveringStates[i].State;
                            if (i < f.RecoveringStates.Count - 1)
                                fState += ", ";
                        }
                    }
                    else
                        fState = "None";
                }
            }

            return fState;
        }
        /// <summary>
        /// 
        /// </summary>
        public class GetDistance
        {
            public GetDistance()
            {
            }


            /// <summary>
            /// Get the Distance between two points in 3D space
            /// http://www.math.usm.edu/lambers/mat169/fall09/lecture17.pdf
            /// </summary>
            /// <param name="x1"></param>
            /// <param name="y1"></param>
            /// <param name="z1"></param>
            /// <param name="x2"></param>
            /// <param name="y2"></param>
            /// <param name="z2"></param>
            /// <returns></returns>
            public double Distance3D(double x1, double y1, double z1, double x2, double y2, double z2)
            {
                //     __________________________________
                //d = √ (x2-x1)^2 + (y2-y1)^2 + (z2-z1)^2
                //
                //Our end result
                double result = 0;
                //Take x2-x1, then square it
                double part1 = Math.Pow((x2 - x1), 2);
                //Take y2-y1, then sqaure it
                double part2 = Math.Pow((y2 - y1), 2);
                //Take z2-z1, then square it
                double part3 = Math.Pow((z2 - z1), 2);
                //Add both of the parts together
                double underRadical = part1 + part2 + part3;
                //Get the square root of the parts
                result = (double)Math.Sqrt(underRadical);
                //Return our result
                return result;
            }
            public bool Within3DManhattanDistance(double h1, double h2, double h3, double t1, double t2, double t3, double distance)
            {
                double dx = Math.Abs(t1 - h1);
                double dy = Math.Abs(t2 - h2);
                double dz = Math.Abs(t3 - h3);

                if (dx > distance) return false; // too far in x direction
                if (dy > distance) return false; // too far in y direction
                if (dz > distance) return false; // too far in z direction

                return true; // we're within the cube
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private DESystemsForDisplay MapDESystemsForDisplay(SqlDataReader reader)
        {
            DESystemsForDisplay DESystem = new DESystemsForDisplay();

            DESystem.StarSystem = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
            DESystem.SystemAddress = reader.GetInt64(1);
            DESystem.FactionHistory = (List<DESystemsHistory>)JsonConvert.DeserializeObject(reader.GetString(2), typeof(List<DESystemsHistory>));
            DESystem.StarPos = (List<double>)JsonConvert.DeserializeObject(reader.GetString(3), typeof(List<double>));
            DESystem.Visted = reader.GetInt32(4);
            DESystem.timestamp = reader.GetDateTime(5);
            if (!reader.IsDBNull(7))
                DESystem.SysFaction = JsonConvert.DeserializeObject<SystemFaction>(reader.GetString(7));
            DESystem.Population = reader.GetInt64(8);
            DESystem.SystemAllegiance = reader.IsDBNull(9) ? string.Empty : reader.GetString(9);
            DESystem.SystemEconomy = reader.IsDBNull(10) ? string.Empty : reader.GetString(10);
            DESystem.SystemGovernment = reader.IsDBNull(11) ? string.Empty : reader.GetString(11);
            DESystem.SystemSecondEconomy = reader.IsDBNull(12) ? string.Empty : reader.GetString(12);
            DESystem.SystemSecurity = reader.IsDBNull(13) ? string.Empty : reader.GetString(13);
            // On inital read set update flag to false
            DESystem.Updated = false;

            //splashMessage = "Retrieving Systems: " + DESystem.StarSystem;
            return DESystem;
        }
        /// <summary>
        /// 
        /// </summary>
        public void DecryptCheck()
        {
            //Configuration config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);

            ConnectionStringSettingsCollection connections = ConfigurationManager.ConnectionStrings;

            #region Encrypt Connection Strings

            // ===============================================================================================
            // This is a run once code segment - it was here to encrypt the connection strings
            // ===============================================================================================
            //var deServer = config.ConnectionStrings.ConnectionStrings["deServer"].ToString();
            //var devServer = config.ConnectionStrings.ConnectionStrings["devServer"].ToString();

            //var deServerEncrypted = CryptorEngine.Encrypt(deServer, true);
            //var devServerEncrypted = CryptorEngine.Encrypt(devServer, true);

            //config.ConnectionStrings.ConnectionStrings["deServer"].ConnectionString = deServerEncrypted;
            //config.ConnectionStrings.ConnectionStrings["devServer"].ConnectionString = devServerEncrypted;

            //config.Save(ConfigurationSaveMode.Minimal);
            //ConfigurationManager.RefreshSection("connectionStrings");

            // ===============================================================================================

            #endregion

            // Get end decrypt the connection strings
            RemoteConnectionString = CryptorEngine.Decrypt(connections[1].ConnectionString, true);
            LocalConnectionString = CryptorEngine.Decrypt(connections[2].ConnectionString, true);

            connectionString = RemoteConnectionString;

            // Start the dependency process
            SqlDependency.Start(connectionString);
        }

        private List<VisitHistory> _deVisitHistory;
        public List<VisitHistory> DeVisitsHistory
        {
            get { return _deVisitHistory; }
            set { _deVisitHistory = value; }
        }
        private List<DESystemsForDisplay> _displayDeSystems;
        public List<DESystemsForDisplay> displayDESystems
        {
            get { return _displayDeSystems; }
            set { _displayDeSystems = value; }
        }
        public int TickTimeHour
        {
            get { return _TickTimeHour; }
            set { _TickTimeHour = value; }
        }
        public int TickTimeMin
        {
            get { return _tickTimeMin; }
            set { _tickTimeMin = value; }
        }
        private bool _systemUpdated;
        public bool SystemUpdated
        {
            get { return _systemUpdated; }
            set { _systemUpdated = value; }
        }
        public string RemoteConnectionString
        {
            get { return _remoteConnectionString; }
            set { _remoteConnectionString = value; }
        }
        public string LocalConnectionString
        {
            get { return _localConnectionString; }
            set { _localConnectionString = value; }
        }
        public string TotalFactionInfluence
        {
            get { return _totalFactionInfluence; }
            set
            {
                _totalFactionInfluence = value;
            }
        }
        public string FactionInfluenceChange
        {
            get { return _factionInfluenceChange; }
            set
            {
                _factionInfluenceChange = value;
            }
        }
        public double FactionInfluenceChangeValue
        {
            get { return _factionInfluenceChangeValue; }
            set { _factionInfluenceChangeValue = value; }
        }
        private ObservableCollection<FactionHistData> _faction1;
        public ObservableCollection<FactionHistData> Faction1
        {
            get { return _faction1; }
            set { _faction1 = value; }
        }
        private DateTime _tickTime;
        public DateTime TickTime
        {
            get { return _tickTime; }
            set { _tickTime = value; }
        }

        private string _closelayFactionStatus;

        public string ClosePMFStatus
        {
            get { return _closelayFactionStatus; }
            set
            {
                _closelayFactionStatus = value;
                NotifyOfPropertyChange(() => ClosePMFStatus);
            }
        }

    }
}
