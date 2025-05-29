namespace Pingo.Networking.Java.Protocol.Packets;

internal sealed class StatusRequestPacket : IOutgoingPacket
{
    public int Identifier => 0x00;

    public void Write(ref MemoryWriter writer)
    {
        // Empty...
    }
}
