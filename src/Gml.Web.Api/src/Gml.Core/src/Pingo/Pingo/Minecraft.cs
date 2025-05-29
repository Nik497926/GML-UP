using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DnsClient;
using Pingo.Helpers;
using Pingo.Networking.Bedrock;
using Pingo.Networking.Java;
using Pingo.Status;

namespace Pingo;

/// <summary>
/// A helper static class that provides methods for pinging a Minecraft server.
/// </summary>
public static class Minecraft
{
    /// <summary>
    /// Attempts to ping a Minecraft server.
    /// </summary>
    /// <param name="options">Options for the asynchronous ping operation.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous ping operation, which wraps the status of the server.</returns>
    /// <exception cref="InvalidOperationException">Unknown protocol type.</exception>
    public static async Task<StatusBase?> PingAsync(MinecraftPingOptions options,
        CancellationToken cancellationToken = default)
    {
        using var timeOutSource = new CancellationTokenSource();
        timeOutSource.CancelAfter(options.TimeOut);

        using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeOutSource.Token);

        var endPoint = await ParseEndpointAsync(options, source.Token);

        Socket? socket;

        try
        {
            socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            await ConnectWithCancellationAsync(socket, endPoint, source.Token);
        }
        catch (SocketException)
        {
            socket = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            await ConnectWithCancellationAsync(socket, endPoint, source.Token);
        }

        switch (socket.ProtocolType)
        {
            case ProtocolType.Tcp:
            {
                await using var factory = new SocketConnectionContextFactory();

                await using var java = new JavaClient(factory.Create(socket));
                var status = await java.PingAsync(options.Address, options.Port, source.Token);

                return status is not null ? new JavaStatus(status) : null;
            }

            case ProtocolType.Udp:
            {
                using var bedrock = new BedrockClient(socket);
                return await bedrock.PingAsync(cancellationToken);
            }

            default:
                throw new InvalidOperationException("Unknown protocol type.");
        }
    }

    private static async Task ConnectWithCancellationAsync(Socket socket, EndPoint endPoint, CancellationToken token)
    {
        await using var registration = token.Register(socket.Close);
        try
        {
            await socket.ConnectAsync(endPoint);
        }
        catch (ObjectDisposedException) when (token.IsCancellationRequested)
        {
            throw new SocketException();
        }
    }

    private static async Task<IPEndPoint> ParseEndpointAsync(MinecraftPingOptions options, CancellationToken token)
    {
        if (!IPAddress.TryParse(options.Address, out var ipAddress))
        {
            var addresses = await Dns.GetHostAddressesAsync(options.Address);
            if (addresses.Length == 0)
            {
                throw new InvalidOperationException("Unable to resolve domain to an IP address.");
            }

            ipAddress = addresses[0];

            if (options.Port == 0)
            {
                var lookup = new LookupClient();
                var result = await lookup.QueryAsync($"_minecraft._tcp.{options.Address}", QueryType.SRV, cancellationToken: token);
                var srvRecord = result.Answers.SrvRecords().FirstOrDefault();
                if (srvRecord != null)
                {
                    options.Port = srvRecord.Port;
                }
                else
                {
                    throw new InvalidOperationException("Unable to resolve SRV record to get the port.");
                }
            }
        }

        var endPoint = new IPEndPoint(ipAddress, options.Port);

        return endPoint;
    }
}

/// <summary>
/// Stores options to ping a Minecraft server.
/// </summary>
public sealed class MinecraftPingOptions
{
    /// <summary>
    /// The server's address.
    /// </summary>
    public string Address { get; set; }

    /// <summary>
    /// The server's port.
    /// </summary>
    /// <remarks>
    /// 19132 is usually for Bedrock servers, and 25565 is usually for Java servers.
    /// </remarks>
    public ushort Port { get; set; }

    /// <summary>
    /// Specifies when should the asynchronous ping operation time out.
    /// </summary>
    public TimeSpan TimeOut { get; set; } = TimeSpan.FromSeconds(5);
}
