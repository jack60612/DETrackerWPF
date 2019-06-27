using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DETrackerWPF.Models
{
  public class FactionModel
  {
    public string FactionName { get; set; }
    public string GovernmentaAllegiance { get; set; }
    public ObservableCollection<DisplayFactionHist> FactionHistory { get; set; }
  }
}
