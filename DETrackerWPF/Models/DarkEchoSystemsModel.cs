using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;

namespace DETrackerWPF.Models
{
  public class DarkEchoSystemsModel : Screen
  {

    private bool _highLight;
    private bool _notControlledSystem;

    public System.Windows.Media.Imaging.BitmapImage InfluenceChangeImage { get; set; }
    public System.Windows.Media.Imaging.BitmapImage TickStatusImage { get; set; }
    public System.Windows.Media.Imaging.BitmapImage InfoImage { get; set; }
    public string ToolTipText { get; set; }
    public Int64 SystemAddress { get; set; }

    public string StarSystemName { get; set; }
    public string DistanceFromDisci { get; set; }
    public double DarkEchoInfluence { get; set; }
    public double InfluenceChange { get; set; }
    public double InfluenceChangePrev { get; set; }
    public double GapToNextFaction { get; set; }
    public string ActiveStates { get; set; }
    public string PendingStates { get; set; }
    public string RecoveringStates { get; set; }
    public string happiness { get; set; }
    public DateTime Updated { get; set; }
    public int VisitsToday { get; set; }
    public int VisitsTotal { get; set; }
    public int FactionsInSystem { get; set; }
    public bool HighLight
    {
      get { return _highLight; }
      set
      {
        _highLight = value;
        NotifyOfPropertyChange(() => HighLight);
      }
    }
    public bool NotControlledSystem
    {
      get { return _notControlledSystem; }
      set
      {
        _notControlledSystem = value;
        NotifyOfPropertyChange(() => NotControlledSystem);
      }
    }
  }
}
