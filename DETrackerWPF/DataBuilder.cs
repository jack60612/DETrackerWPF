using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DETrackerWPF.Models;

namespace DETrackerWPF
{
  public class DataBuilder
  {

    public void DisplayHistoryData(DESystemsForDisplay DESystem)
    {
      int fIndex;

      // Get the history list and reverse sort so latest is first.
      List<DESystemsHistory> sysHist = new List<DESystemsHistory>(DESystem.FactionHistory);
      sysHist = sysHist.OrderByDescending(r => r.timestamp).ToList();


      foreach (var fhr in sysHist)
      {

        TimeSpan span = DateTime.Today.Subtract(fhr.timestamp.Date);
        if ((int)span.TotalDays > 30)
          continue;

        SystemsFactionsHistory sfh = new SystemsFactionsHistory();
        sfh.FactionHist = new ObservableCollection<FactionHistData>();

        sfh.UpdatedAt = fhr.timestamp;

        // Get the index to the current history record
        var histIndex = sysHist.FindIndex(m => m.timestamp == fhr.timestamp);

        // Create temp list of factions and sort in influence order 
        List<FullFactionData> fhs = new List<FullFactionData>(fhr.Factions);

        // Dump "Pilots Federation Local Branch" from the new list
        fhs.RemoveAll(x => x.Name.Contains("Federation Local Branch"));

      }

    }

  }
}
