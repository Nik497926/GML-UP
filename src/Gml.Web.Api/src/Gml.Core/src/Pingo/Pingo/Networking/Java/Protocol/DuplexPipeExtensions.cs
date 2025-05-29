using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipelines;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Pingo.Networking.Java.Protocol;


internal static class DuplexPipeExtensions
{
    public static async Task<Message?> ReadAsync(this IDuplexPipe pipe, CancellationToken cancellationToken)
    {
        var reader = pipe.Input;

        while (true)
        {
            var result = await reader.ReadAsync(cancellationToken);
            var buffer = result.Buffer;
            var consumed = buffer.Start;
            var examined = buffer.End;

            try
            {
                if (TryRead(ref buffer, out var message))
                {
                    consumed = buffer.Start;
                    examined = consumed;

                    return message;
                }

                if (result.IsCompleted)
                {
                    if (buffer.Length > 0)
                    {
                        // The message is incomplete and there's no more data to process.
                        throw new InvalidDataException("Incomplete message.");
                    }

                    break;
                }
            }
            finally
            {
                reader.AdvanceTo(consumed, examined);
            }
        }

        return null;

        static bool TryRead(ref ReadOnlySequence<byte> buffer, [NotNullWhen(true)] out Message? message)
        {
            var reader = new SequenceReader<byte>(buffer);
            message = null;

            if (!reader.TryReadVariableInteger(out var length)
                || !reader.TryReadVariableInteger(out var identifier))
            {
                return false;
            }

            var padding = VariableInteger.GetBytesCount(identifier);

            if (!reader.TryReadExact(length - padding, out var payload))
            {
                return false;
            }

            message = new Message(identifier, payload.ToArray());
            buffer = buffer.Slice(length + padding);
            return true;
        }
    }

    public static async Task WriteAsync(this IDuplexPipe pipe, IOutgoingPacket packet)
    {
        var writer = pipe.Output;
        Write(writer, packet);
        await writer.FlushAsync();
        return;

        static void Write(PipeWriter writer, IOutgoingPacket packet)
        {
            var memory = writer.GetMemory(packet.CalculateLength());
            writer.Advance(Write(packet, memory));
            return;

            static int Write(IOutgoingPacket packet, Memory<byte> memory)
            {
                var packetWriter = new MemoryWriter(memory);
                packet.Write(ref packetWriter);

                var position = packetWriter.Position;
                var temporary = memory[..position].ToArray();
                var payloadWriter = new MemoryWriter(memory);

                payloadWriter.WriteVariableInteger(VariableInteger.GetBytesCount(packet.Identifier) + position);
                payloadWriter.WriteVariableInteger(packet.Identifier);
                payloadWriter.Write(temporary);

                return payloadWriter.Position;
            }
        }
    }
}

internal static class SequenceReaderExtensions
{
    public static bool TryReadExact(this ref SequenceReader<byte> reader, int count, out ReadOnlySequence<byte> sequence)
    {
        if (reader.Remaining < count)
        {
            sequence = default;
            return false;
        }

        var startPosition = reader.Position;
        reader.Advance(count);
        sequence = reader.Sequence.Slice(startPosition, count);
        return true;
    }

    public static bool TryReadVariableInteger(ref this SequenceReader<byte> reader, out int value)
    {
        var numbersRead = 0;
        var result = 0;

        byte read;

        do
        {
            if (!reader.TryRead(out read))
            {
                value = default;
                return false;
            }

            var temporaryValue = read & 0b01111111;
            result |= temporaryValue << 7 * numbersRead;

            numbersRead++;

            if (numbersRead <= 5)
            {
                continue;
            }

            value = default;
            return false;
        } while ((read & 0b10000000) != 0);

        value = result;
        return true;
    }
}

internal static class VariableInteger
{
    public static int GetBytesCount(int value)
    {
        return (LeadingZeroCount((uint) value | 1) - 38) * -1171 >> 13;
    }

    private static int LeadingZeroCount(uint n)
    {
        if (n == 0) return 32;
        int count = 0;
        while ((n & 0x80000000) == 0)
        {
            n <<= 1;
            count++;
        }
        return count;
    }
}
