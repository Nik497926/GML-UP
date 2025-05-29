using System.IO;

namespace Pingo.Extensions;

public static class BinaryReaderExtensions
{
    public static bool TryReadExact(this BinaryReader reader, int byteCount, out byte[] payload)
    {
        payload = new byte[byteCount];
        int bytesRead = reader.Read(payload, 0, byteCount);
        return bytesRead == byteCount;
    }
}
