using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Pingo.Networking.Bedrock.Protocol;

internal static class SocketExtensions
{
    public static async Task<Message> ReadAsync(
        this Socket socket,
        CancellationToken cancellationToken)
    {
        var memory = new byte[1500].AsMemory();

        var items = await socket.ReceiveAsync(memory, SocketFlags.None, cancellationToken);

        memory = memory[..items];

        return new Message(memory.Span[0], memory[1..]);
    }

    public static async Task WriteAsync(
        this Socket socket,
        IOutgoingPacket packet,
        CancellationToken cancellationToken)
    {
        await socket.SendAsync(Write(packet), SocketFlags.None, cancellationToken);

        return;

        static Memory<byte> Write(IOutgoingPacket packet)
        {
            var memory = new byte[1500].AsMemory();
            var writer = new MemoryWriter(memory);

            writer.WriteByte((byte) packet.Identifier);
            packet.Write(ref writer);

            return memory[..writer.Position];
        }
    }
}
