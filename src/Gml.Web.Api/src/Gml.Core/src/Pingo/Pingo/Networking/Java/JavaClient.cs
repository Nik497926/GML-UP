using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Pingo.Networking.Java.Protocol;
using Pingo.Networking.Java.Protocol.Components;
using Pingo.Networking.Java.Protocol.Packets;

namespace Pingo.Networking.Java;

internal sealed class JavaClient(ConnectionContext connection) : IAsyncDisposable
{
    public async Task<ServerStatus?> PingAsync(
        string address,
        ushort port,
        CancellationToken cancellationToken)
    {
        IOutgoingPacket[] initial =
        [
            new HandshakePacket
            {
                ProtocolVersion = -1,
                Address = address,
                Port = port,
                NextState = 1
            },
            new StatusRequestPacket()
        ];

        foreach (var packet in initial)
        {
            await connection.Transport.WriteAsync(packet);
        }

        Message? message = await connection.Transport.ReadAsync(cancellationToken);
        var response = message?.As(new StatusResponsePacket());

        return response is not null
            ? JsonSerializer.Deserialize(
                response.Status,
                SourceGenerationContext.Default.ServerStatus)
            : null;
    }

    public async ValueTask DisposeAsync()
    {
        await connection.DisposeAsync();
    }
}
