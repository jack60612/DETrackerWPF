using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;

namespace DETrackerWPF.Models
{
  public class SystemsFactionsHistory
  {
    public string FactionName { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ObservableCollection<FactionHistData> FactionHist { get; set; }
  }


  public class FactionHistData 
  {
    public double fInf { get; set; }
    public double fChg { get; set; }
    public ObservableCollection<States> ActiveStates { get; set; }
    public ObservableCollection<States> PendingStates { get; set; }
    public ObservableCollection<States> RecoveringStates { get; set; }
  }

  public class DisplayFactionHist :Screen
  {
    public double FactionInfluence { get; set; }
    public double InfluenceChange { get; set; }

    private ObservableCollection<States> _displayStates;

    public ObservableCollection<States> DisplayStates
    {
      get { return _displayStates; }
      set
      {
        _displayStates = value; 
        NotifyOfPropertyChange(() => DisplayStates);
      }
    }
    public ObservableCollection<States> ActiveStates { get; set; }
    public ObservableCollection<States> PendingStates { get; set; }
    public ObservableCollection<States> RecoveringStates { get; set; }
  }

  public class HistoryDatesList
  {
    public string HistDate { get; set; }
  }


  public class FactionHeadersModel :Screen
  {
    public string InfHeader { get; set; }
    public string ChgHeader { get; set; }

    private string _stateHeader;

    public string StateHeader
    {
      get
      {
        return _stateHeader; }
      set
      {
        _stateHeader = value;
        NotifyOfPropertyChange(() => StateHeader);
      }
    }

 }
}
