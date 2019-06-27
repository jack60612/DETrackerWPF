using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using DETrackerWPF.ViewModels;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;

namespace DETrackerWPF
{
  public class Bootstrapper : BootstrapperBase
  {
    public Bootstrapper()
    {
      Initialize();
    }

    protected override void OnStartup(object sender, StartupEventArgs e)
    {
      DisplayRootViewFor<ShellViewModel>();
    }

    // Window Open Code
    // http://www.mindscapehq.com/blog/index.php/2012/03/13/caliburn-micro-part-5-the-window-manager/

    private CompositionContainer container;

    protected override void Configure()
    {
      container = new CompositionContainer(new AggregateCatalog(AssemblySource.Instance.Select(x => new AssemblyCatalog(x)).OfType<ComposablePartCatalog>()));

      CompositionBatch batch = new CompositionBatch();

      batch.AddExportedValue<IWindowManager>(new WindowManager());
      batch.AddExportedValue<IEventAggregator>(new EventAggregator());
      batch.AddExportedValue(container);

      container.Compose(batch);
    }

    protected override object GetInstance(Type serviceType, string key)
    {
      string contract = string.IsNullOrEmpty(key) ? AttributedModelServices.GetContractName(serviceType) : key;
      var exports = container.GetExportedValues<object>(contract);

      if (exports.Count() > 0)
      {
        return exports.First();
      }

      throw new Exception(string.Format("Could not locate any instances of contract {0}.", contract));
    }

  }
}
