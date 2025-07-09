using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;

namespace eSearch.Models.AI.MCP
{
    public class ProcessTransport : ITransport
    {
        private readonly Process _process;
        private readonly Channel<JsonRpcMessage> _channel = Channel.CreateUnbounded<JsonRpcMessage>();
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private CancellationTokenSource? _cts;
        private bool _started;

        public event EventHandler<string?> OutputDataReceived;

        /// <summary>
        /// The channel reader for incoming JSON-RPC messages (async producer-consumer).
        /// </summary>
        public ChannelReader<JsonRpcMessage> MessageReader => _channel.Reader;

        /// <summary>
        /// Constructs the transport with an already-started Process whose StandardInput/Output are redirected.
        /// </summary>
        public ProcessTransport(Process process)
        {
            _process = process ?? throw new ArgumentNullException(nameof(process));
            if (!process.StartInfo.RedirectStandardInput || !process.StartInfo.RedirectStandardOutput)
            {
                throw new ArgumentException("Process must have StandardInput and StandardOutput redirected.", nameof(process));
            }
        }

        /// <summary>
        /// Starts the background loop that reads incoming messages using OutputDataReceived. Safe to call once.
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_started) return Task.CompletedTask;
            _started = true;

            // Create a CTS that is linked to the provided token, so Stop can cancel the read loop.
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Enable event raising and attach the event handler
            _process.EnableRaisingEvents = true;
            _process.OutputDataReceived += Process_OutputDataReceived;

            // Start asynchronous reading
            // _process.BeginOutputReadLine(); // This is already done by UserConfiguredMCPServer

            // Register cancellation to clean up event handler
            _cts.Token.Register(() =>
            {
                _process.OutputDataReceived -= Process_OutputDataReceived;
                _channel.Writer.TryComplete();
            });

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles data received from the process's StandardOutput.
        /// </summary>
        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                // Check if cancellation was requested
                OutputDataReceived?.Invoke(this, e.Data);
                if (_cts?.Token.IsCancellationRequested == true)
                {
                    _channel.Writer.TryComplete();
                    return;
                }

                // Check for end of stream
                if (e.Data is null)
                {
                    _channel.Writer.TryComplete();
                    return;
                }

                // Deserialize the JSON into a JsonRpcMessage
                JsonRpcMessage message = JsonSerializer.Deserialize<JsonRpcMessage>(e.Data)
                    ?? throw new JsonException("Failed to deserialize message");

                // Publish to the channel for consumers
                _channel.Writer.TryWrite(message);
            }
            catch (Exception ex)
            {
                // On error (parse, IO, etc.), propagate error to channel
                _channel.Writer.TryComplete(ex);
            }
        }

        /// <summary>
        /// Sends a JSON-RPC message by writing it as a JSON line to the process’s standard input.
        /// Uses a lock to prevent interleaved writes from multiple threads.
        /// </summary>
        public async Task SendMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken = default)
        {
            if (message is null) throw new ArgumentNullException(nameof(message));
            // Serialize the message to JSON text.
            string json = JsonSerializer.Serialize(message);

            // Ensure only one writer at a time to avoid interleaving.
            await _writeLock.WaitAsync(cancellationToken);
            try
            {
                // Write the JSON and newline, then flush.
                await _process.StandardInput.WriteLineAsync(json.AsMemory(), cancellationToken);
                await _process.StandardInput.FlushAsync();
            }
            finally
            {
                _writeLock.Release();
            }
        }

        /// <summary>
        /// Stops the listening loop gracefully by cancelling and cleaning up.
        /// </summary>
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (!_started) return;

            // Cancel the listening loop
            _cts?.Cancel();

            // Wait briefly to allow cleanup (ignore cancellation)
            try
            {
                await Task.Delay(100, CancellationToken.None); // Give event handler time to complete
                _process.OutputDataReceived -= Process_OutputDataReceived;
                _channel.Writer.TryComplete();
            }
            catch { /* ignore */ }
        }

        /// <summary>
        /// Asynchronously disposes the transport, stopping the loop.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await StopAsync();
            _cts?.Dispose();
        }
    }
}