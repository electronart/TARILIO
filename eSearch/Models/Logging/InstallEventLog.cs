using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;

namespace eSearch.Models.Logging
{

    [RunInstaller(true)]
    public class InstallEventLog : Installer
    {
        public const string EventSource = "eSearch";

        /// <summary>
        /// This is invoked by eSearch Installer and TARILIO Installer projects
        /// Administrative permission is needed to add an event source to windows event viewer.
        /// So this task must be carried out during eSearch installation.
        /// </summary>
        public InstallEventLog()
        {
            var eventLogInstaller = new EventLogInstaller();
            eventLogInstaller.Source = EventSource;
            Installers.Add(eventLogInstaller);
        }
    }
}
