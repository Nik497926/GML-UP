using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace Pingo.Helpers;

internal class SocketConnectionContextFactory : IAsyncDisposable, IDisposable
{
    public ConnectionContext Create(Socket socket)
    {
        return new SocketConnectionContext(socket);
    }

    public async ValueTask DisposeAsync()
    {
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }
}
