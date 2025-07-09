using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading;
using System.Threading.Tasks;

namespace eSearch.Models.AI.MCP.LocalTransport
{
    public class LocalClientTransport : IClientTransport
    {
        private readonly ChannelReader<JsonRpcMessage> _messageReader;
        private readonly ChannelWriter<JsonRpcMessage> _messageWriter;

        public LocalClientTransport(ChannelReader<JsonRpcMessage> messageReader, ChannelWriter<JsonRpcMessage> messageWriter)
        {
            _messageReader = messageReader;
            _messageWriter = messageWriter;
        }

        public string Name => "Local Transport";

        public Task<ITransport> ConnectAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ITransport>(new LocalTransport(_messageReader, _messageWriter));
        }
    }

    public class LocalTransport : ITransport
    {
        private readonly ChannelReader<JsonRpcMessage> _messageReader;
        private readonly ChannelWriter<JsonRpcMessage> _messageWriter;

        public LocalTransport(ChannelReader<JsonRpcMessage> messageReader, ChannelWriter<JsonRpcMessage> messageWriter)
        {
            _messageReader = messageReader;
            _messageWriter = messageWriter;
        }

        public ChannelReader<JsonRpcMessage> MessageReader => _messageReader;

        public Task SendMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken = default)
        {
            return _messageWriter.WriteAsync(message, cancellationToken).AsTask();
        }

        public async ValueTask DisposeAsync()
        {
            // Clean up if needed
        }
    }
}
