using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Pingo.Networking.Bedrock.Protocol;
using Pingo.Networking.Bedrock.Protocol.Packets;
using Pingo.Status;

namespace Pingo.Networking.Bedrock;

internal sealed class BedrockClient(Socket socket) : IDisposable
{
    public async Task<BedrockStatus> PingAsync(CancellationToken cancellationToken)
    {
        await socket.WriteAsync(
            new UnconnectedPingPacket
            {
                Time = DateTime.UtcNow.Millisecond,
                Client = new Random().Next()
            },
            cancellationToken);

        var message = await socket.ReadAsync(cancellationToken);
        var pong = message.As(new UnconnectedPongPacket());

        var format = pong.Message.Split(';');

        return new BedrockStatus
        {
            Edition = format[0],
            MessagesOfTheDay =
            [
                format[1],
                format[7]
            ],
            Protocol = int.Parse(format[2]),
            Version = format[3],
            OnlinePlayers = int.Parse(format[4]),
            MaximumPlayers = int.Parse(format[5]),
            ServerIdentifier = long.Parse(format[6]),
            GameMode = format[8]
        };
    }

    public void Dispose()
    {
        socket.Dispose();
    }
}
