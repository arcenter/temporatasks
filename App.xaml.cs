using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using TemporaTasks.Core;

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

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Exception ex = (Exception)e.ExceptionObject;
                File.AppendAllText($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\TemporaTasks\\crashLogs.txt", $"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}\n{ex.Message}\n{ex.StackTrace}\n\n");
                Shutdown();
            };
        }
    }

}