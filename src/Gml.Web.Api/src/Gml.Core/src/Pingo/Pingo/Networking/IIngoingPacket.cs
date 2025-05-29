namespace Pingo.Networking;

internal interface IIngoingPacket<out TSelf> where TSelf : IIngoingPacket<TSelf>
{
    public abstract int Identifier { get; }

    public abstract TSelf Read(MemoryReader reader);
}
