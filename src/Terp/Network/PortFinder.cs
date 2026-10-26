using System.Net;
using System.Net.Sockets;

namespace Terp.Network;

/// <summary>
/// Helpers for finding free local TCP ports.
/// </summary>
public static class PortFinder
{
    /// <summary>
    /// Returns an unused local TCP port on the current host (IPv4).
    /// </summary>
    /// <remarks>
    /// The port is only reserved while this method runs: the probe socket is closed before the port
    /// is returned. Another process (or another call to this method) can claim the port before the
    /// caller binds it, so a bind can still fail with an address-in-use error. Callers that can bind
    /// directly should bind to port 0 and read the assigned port instead; otherwise retry on failure.
    /// </remarks>
    /// <returns>The port number.</returns>
    /// <exception cref="SocketException">No socket could be bound.</exception>
    public static int FindFreeLocalPort()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Any, 0));
        return ((IPEndPoint)socket.LocalEndPoint!).Port;
    }
}
