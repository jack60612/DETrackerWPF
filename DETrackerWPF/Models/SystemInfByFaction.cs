using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DETrackerWPF.Models
{
  class SystemInfByFaction
  {
    public string FactionName { get; set; }
    public List<double> FactionInf { get; set; }
  }

  class SystemInfByFactionOxy
  {
    public string FactionName { get; set; }
    public List<FactionDateInf> DateNAndInf { get; set; }
  }


  public class FactionDateInf
  {
    public double FactionInf { get; set; }
    public DateTime FactionInfDate { get; set; }
  }
}
