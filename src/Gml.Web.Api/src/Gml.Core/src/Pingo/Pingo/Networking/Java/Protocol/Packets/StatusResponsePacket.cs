namespace Pingo.Networking.Java.Protocol.Packets;

internal sealed class StatusResponsePacket : IIngoingPacket<StatusResponsePacket>
{
    public int Identifier => 0x00;

    public string Status { get; set; }

    public StatusResponsePacket Read(MemoryReader reader)
    {
        return new StatusResponsePacket
        {
            Status = reader.ReadVariableString(true)
        };
    }
}
