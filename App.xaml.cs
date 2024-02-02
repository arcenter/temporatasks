using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Windows;

namespace TemporaTasks
{
    public partial class App : Application
    {
        private static Mutex mutex = null;
        protected override void OnStartup(StartupEventArgs e)
        {
            mutex = new Mutex(true, "TemporaTasks", out bool createdNew);
            if (!createdNew)
            {
                // mutex.ReleaseMutex();
                Application.Current.Shutdown();
            }
            base.OnStartup(e);
        }
    }

}