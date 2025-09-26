using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.ViewModels.StatusUI
{
    public class LLMStatusControlViewModel : StatusControlViewModel, IDisposable
    {
        private readonly Timer _timer;
        private bool _disposed;

        public LLMStatusControlViewModel()
        {
            _timer = new Timer(TimeSpan.FromSeconds(30));
            _timer.Elapsed += _timer_Elapsed;
            _timer.AutoReset = true;
            _timer.Start();
        }

        private void _timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            StringBuilder mcpServerList = new StringBuilder();
            
            int numMCPServersInErrorState = 0;
            if (Program.GetMainWindow()?.DataContext is MainWindowViewModel mwvm)
            {
                var enabledServers = Program.ProgramConfig.GetAllAvailableMCPServers()
                    .Where(
                        server => Program.ProgramConfig.EnabledMCPServerNames
                                                .Contains(server.DisplayName)
                    );
                if (enabledServers.Any())
                {
                    mcpServerList.AppendLine(S.Get("Tools:"));
                    foreach (var enabledServer in enabledServers)
                    {
                        
                        if (enabledServer.IsErrorState)
                        {
                            ++numMCPServersInErrorState;
                        } else
                        {
                            mcpServerList.AppendLine($"• {enabledServer.DisplayName}");
                        }
                    }
                }
            }
            if (numMCPServersInErrorState == 0)
            {
                StatusError = null; // Hide this, no errors.
            }
            else
            {
                StatusError = String.Format(S.Get("{0} MCP Server(s) failed to start."), numMCPServersInErrorState);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _timer?.Stop();
                _timer?.Dispose();
                _disposed = true;
            }
        }
    }
}
