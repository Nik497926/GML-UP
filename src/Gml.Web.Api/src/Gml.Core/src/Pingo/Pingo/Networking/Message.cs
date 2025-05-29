using System;

namespace Pingo.Networking;

internal sealed record Message
{
    public int Identifier { get; }
    public ReadOnlyMemory<byte> Memory { get; }

    public Message(int identifier, ReadOnlyMemory<byte> memory)
    {
        Identifier = identifier;
        Memory = memory;
    }

    public T As<T>(T item) where T : IIngoingPacket<T>
    {
        if (item.Identifier != Identifier)
        {
            throw new InvalidOperationException($"Tried to read as 0x{Identifier:X2}.");
        }

        var reader = new MemoryReader(Memory);
        return item.Read(reader);
    }
}
