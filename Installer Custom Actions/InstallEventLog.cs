using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;

namespace DIEventSourceCreator
{


    [RunInstaller(true)]
    public class InstallEventLog : Installer
    {
        public const string EventSource = "eSearch";

        public InstallEventLog()
        {
            var eventLogInstaller = new EventLogInstaller();
            eventLogInstaller.Source = EventSource;
            Installers.Add(eventLogInstaller);
        }
    }
}