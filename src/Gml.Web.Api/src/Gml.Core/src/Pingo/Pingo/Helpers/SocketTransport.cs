using System;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Pingo.Helpers;

internal class SocketTransport : IDuplexPipe
{
    private readonly Socket socket;
    private readonly Pipe inputPipe;
    private readonly Pipe outputPipe;
    private readonly CancellationTokenSource cancellationTokenSource;
    private readonly TaskCompletionSource<object> completionSource;

    public SocketTransport(Socket socket)
    {
        this.socket = socket;
        inputPipe = new Pipe();
        outputPipe = new Pipe();
        cancellationTokenSource = new CancellationTokenSource();
        completionSource = new TaskCompletionSource<object>();

        // Start reading from the socket
        _ = FillPipeAsync();

        // Start writing to the socket
        _ = ReadPipeAsync();
    }

    public PipeReader Input => inputPipe.Reader;
    public PipeWriter Output => outputPipe.Writer;

    private async Task FillPipeAsync()
    {
        var writer = inputPipe.Writer;

        try
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                Memory<byte> memory = writer.GetMemory(4096);
                int bytesRead = await socket.ReceiveAsync(memory, SocketFlags.None, cancellationTokenSource.Token);

                if (bytesRead == 0)
                {
                    break;
                }

                writer.Advance(bytesRead);
                var result = await writer.FlushAsync(cancellationTokenSource.Token);

                if (result.IsCompleted)
                {
                    break;
                }
            }

            writer.Complete();
        }
        catch (Exception ex)
        {
            writer.Complete(ex);
        }
    }

    private async Task ReadPipeAsync()
    {
        var reader = outputPipe.Reader;

        try
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                var result = await reader.ReadAsync(cancellationTokenSource.Token);
                var buffer = result.Buffer;

                if (buffer.IsEmpty && result.IsCompleted)
                {
                    break;
                }

                foreach (var segment in buffer)
                {
                    await socket.SendAsync(segment, SocketFlags.None, cancellationTokenSource.Token);
                }

                reader.AdvanceTo(buffer.End);
            }

            reader.Complete();
        }
        catch (Exception ex)
        {
            reader.Complete(ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        cancellationTokenSource.Cancel();

        try
        {
            socket.Shutdown(SocketShutdown.Both);
        }
        catch (SocketException) { }

        socket.Close();
        socket.Dispose();

        inputPipe.Writer.Complete();
        outputPipe.Reader.Complete();

        await completionSource.Task;
    }
}
