using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Terp.Diagnostics;

namespace Terp.Tests.Diagnostics;

public sealed class TerpHttpMetricsHandlerTests
{
    private const string Duration = "http.client.request.duration";

    private sealed class SingleMeterFactory(Meter meter) : IMeterFactory
    {
        public Meter Create(MeterOptions options)
        {
            return meter;
        }

        public void Dispose()
        {
        }
    }

    private sealed class LoopbackServer : IDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _cts = new();

        public LoopbackServer()
        {
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _ = Task.Run(AcceptLoopAsync);
        }

        public int Port { get; }

        public void Dispose()
        {
            _cts.Cancel();
            _listener.Stop();
            _cts.Dispose();
        }

        private static async Task HandleAsync(TcpClient client)
        {
            using (client)
            {
                var stream = client.GetStream();
                var buffer = new byte[4096];
                var read = await stream.ReadAsync(buffer);
                var request = Encoding.ASCII.GetString(buffer, 0, read);
                var status = request.Contains("/fail", StringComparison.Ordinal) ? "500 Internal Server Error" : "200 OK";
                var response = $"HTTP/1.1 {status}\r\nContent-Length: 2\r\nConnection: close\r\n\r\nhi";
                await stream.WriteAsync(Encoding.ASCII.GetBytes(response));
            }
        }

        private async Task AcceptLoopAsync()
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                    _ = Task.Run(() => HandleAsync(client));
                }
            }
            catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException or SocketException)
            {
            }
        }
    }

    private static HttpClient CreateClient(Meter meter, string? api)
    {
        var inner = new SocketsHttpHandler { MeterFactory = new SingleMeterFactory(meter) };
        if (api is null)
        {
            return new HttpClient(inner);
        }

        return new HttpClient(new TerpHttpMetricsHandler(api) { InnerHandler = inner });
    }

    [Fact]
    public async Task GetAsync_SuccessResponse_CarriesApiTag()
    {
        using var meter = new Meter("test.http.ok");
        using var collector = new MetricCollector(meter);
        using var server = new LoopbackServer();
        using var client = CreateClient(meter, "tmdb");

        using var response = await client.GetAsync(new Uri($"http://127.0.0.1:{server.Port}/ok"), TestContext.Current.CancellationToken);

        var m = Assert.Single(collector.For(Duration));
        Assert.Multiple(
            () => Assert.Equal("tmdb", m.Tags[TerpHttpMetricsHandler.ApiTagName]),
            () => Assert.Equal(200, m.Tags["http.response.status_code"]),
            () => Assert.False(m.Tags.ContainsKey("error.type")));
    }

    [Fact]
    public async Task GetAsync_ServerError_CarriesApiTagAndErrorType()
    {
        using var meter = new Meter("test.http.500");
        using var collector = new MetricCollector(meter);
        using var server = new LoopbackServer();
        using var client = CreateClient(meter, "tmdb");

        using var response = await client.GetAsync(new Uri($"http://127.0.0.1:{server.Port}/fail"), TestContext.Current.CancellationToken);

        var m = Assert.Single(collector.For(Duration));
        Assert.Multiple(
            () => Assert.Equal("tmdb", m.Tags[TerpHttpMetricsHandler.ApiTagName]),
            () => Assert.Equal(500, m.Tags["http.response.status_code"]),
            () => Assert.Equal("500", m.Tags["error.type"]));
    }

    [Fact]
    public async Task GetAsync_ConnectionRefused_CarriesApiTagWithoutStatusCode()
    {
        using var meter = new Meter("test.http.conn");
        using var collector = new MetricCollector(meter);
        var closed = new TcpListener(IPAddress.Loopback, 0);
        closed.Start();
        var port = ((IPEndPoint)closed.LocalEndpoint).Port;
        closed.Stop();
        using var client = CreateClient(meter, "tmdb");

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.GetAsync(new Uri($"http://127.0.0.1:{port}/x"), TestContext.Current.CancellationToken));

        var m = Assert.Single(collector.For(Duration));
        Assert.Multiple(
            () => Assert.NotNull(ex),
            () => Assert.Equal("tmdb", m.Tags[TerpHttpMetricsHandler.ApiTagName]),
            () => Assert.Equal("connection_error", m.Tags["error.type"]),
            () => Assert.False(m.Tags.ContainsKey("http.response.status_code")));
    }

    [Fact]
    public void Send_SyncRequest_CarriesApiTag()
    {
        using var meter = new Meter("test.http.sync");
        using var collector = new MetricCollector(meter);
        using var server = new LoopbackServer();
        using var client = CreateClient(meter, "sync-api");
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"http://127.0.0.1:{server.Port}/ok"));

        using var response = client.Send(request, TestContext.Current.CancellationToken);

        Assert.Equal("sync-api", Assert.Single(collector.For(Duration)).Tags[TerpHttpMetricsHandler.ApiTagName]);
    }

    [Fact]
    public async Task GetAsync_WithoutHandler_HasNoApiTag()
    {
        using var meter = new Meter("test.http.plain");
        using var collector = new MetricCollector(meter);
        using var server = new LoopbackServer();
        using var client = CreateClient(meter, null);

        using var response = await client.GetAsync(new Uri($"http://127.0.0.1:{server.Port}/ok"), TestContext.Current.CancellationToken);

        Assert.False(Assert.Single(collector.For(Duration)).Tags.ContainsKey(TerpHttpMetricsHandler.ApiTagName));
    }

    [Fact]
    public void ApiTagName_Constant_IsApi()
    {
        Assert.Equal("api", TerpHttpMetricsHandler.ApiTagName);
    }

    [Fact]
    public void StatusClassTagName_Constant_IsStatusClass()
    {
        Assert.Equal("status_class", TerpHttpMetricsHandler.StatusClassTagName);
    }

    [Fact]
    public void Constructor_NullApiName_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new TerpHttpMetricsHandler(null!));

        Assert.Equal("apiName", ex.ParamName);
    }

    [Fact]
    public void Constructor_EmptyApiName_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => new TerpHttpMetricsHandler(""));

        Assert.Equal("apiName", ex.ParamName);
    }
}
