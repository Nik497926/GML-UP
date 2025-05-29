namespace Pingo.Networking.Java.Protocol.Packets;

internal sealed class HandshakePacket : IOutgoingPacket
{
    public int Identifier => 0x00;

    public int ProtocolVersion { get; set; }

    public string Address { get; set; }

    public ushort Port { get; set; }

    public int NextState { get; set; }

    public void Write(ref MemoryWriter writer)
    {
        writer.WriteVariableInteger(ProtocolVersion);
        writer.WriteVariableString(Address);
        writer.WriteUnsignedShort(Port);
        writer.WriteVariableInteger(NextState);
    }
}
