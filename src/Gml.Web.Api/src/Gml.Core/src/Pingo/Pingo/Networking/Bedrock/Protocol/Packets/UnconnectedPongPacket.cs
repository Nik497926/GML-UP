namespace Pingo.Networking.Bedrock.Protocol.Packets;

internal sealed class UnconnectedPongPacket : IIngoingPacket<UnconnectedPongPacket>
{
    public int Identifier => 0x1C;

    public long Time { get; set; }

    public long Server { get; set; }

    public string Message { get; set; }

    public UnconnectedPongPacket Read(MemoryReader reader)
    {
        var time = reader.ReadLong();
        var server = reader.ReadLong();
        reader.ReadMagic();

        return new UnconnectedPongPacket
        {
            Time = time,
            Server = server,
            Message = reader.ReadVariableString(false)
        };
    }
}
