using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace Pingo.Helpers;

internal class SocketConnectionContext : ConnectionContext, IAsyncDisposable
{
    private readonly Socket socket;

    public SocketConnectionContext(Socket socket)
    {
        this.socket = socket;
        Transport = new SocketTransport(socket);
    }

    public override IDuplexPipe Transport { get; set; }

    public async ValueTask DisposeAsync()
    {
        try
        {
            socket.Shutdown(SocketShutdown.Both);
        }
        catch (SocketException ex)
        {

        }
        socket.Close();
        socket.Dispose();
        await Task.CompletedTask;
    }

    public override string ConnectionId { get; set; }
    public override IFeatureCollection Features { get; }
    public override IDictionary<object, object?> Items { get; set; }
}
