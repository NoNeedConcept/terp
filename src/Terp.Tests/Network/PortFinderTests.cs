using System.Net;
using System.Net.Sockets;
using Terp.Network;

namespace Terp.Tests.Network;

public sealed class PortFinderTests
{
    [Fact]
    public void FindFreeLocalPort_Called_ReturnsPortInValidRange()
    {
        // Act
        var port = PortFinder.FindFreeLocalPort();

        // Assert
        Assert.True(port is >= 1024 and <= 65535,
            $"Expected a valid port between 1024 and 65535, but got {port}");
    }

    [Fact]
    public void FindFreeLocalPort_Called_ReturnsBindablePort()
    {
        // Arrange
        var port = PortFinder.FindFreeLocalPort();

        // Act
        Exception? ex = null;
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var endpoint = new IPEndPoint(IPAddress.Loopback, port);
            socket.Bind(endpoint);
        }
        catch (Exception e)
        {
            ex = e;
        }

        // Assert
        Assert.Null(ex);
    }

    [Fact]
    public void FindFreeLocalPort_CalledTwice_ReturnsDifferentPorts()
    {
        // Act
        var port1 = PortFinder.FindFreeLocalPort();
        var port2 = PortFinder.FindFreeLocalPort();

        // Assert
        Assert.NotEqual(port1, port2);
    }

    [Fact]
    public void FindFreeLocalPort_PortReleased_CanBeBoundAgain()
    {
        var port = PortFinder.FindFreeLocalPort();

        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
        socket.Close();

        Exception? ex = null;
        try
        {
            using var testSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            testSocket.Bind(new IPEndPoint(IPAddress.Loopback, port));
        }
        catch (Exception e)
        {
            ex = e;
        }

        // Assert
        Assert.Null(ex);
    }
}
