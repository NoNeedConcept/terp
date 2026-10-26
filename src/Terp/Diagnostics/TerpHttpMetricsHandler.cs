using System.Net.Http.Metrics;

namespace Terp.Diagnostics;

/// <summary>
/// Adds the tags <c>api=&lt;apiName&gt;</c> and <c>status_class</c> (<c>2xx</c>, <c>3xx</c>, …, or <c>error</c>)
/// to the built-in HttpClient metrics (meter <c>System.Net.Http</c>, e.g. <c>http.client.request.duration</c>)
/// of every request that passes through it. It records no instruments of its own.
/// </summary>
/// <remarks>
/// The tags are present on success, on non-success status codes and on connection failures. A request cancelled before
/// it was sent is not recorded by the BCL at all. The handler has no dependency on <c>IHttpClientBuilder</c>; the
/// application registers it as an HttpClient message handler and subscribes to the BCL meter:
/// <code>
/// services.AddHttpClient("tmdb").AddHttpMessageHandler(() => new TerpHttpMetricsHandler("tmdb"));
/// builder.Services.AddOpenTelemetry().WithMetrics(m => m.AddMeter("System.Net.Http"));
/// </code>
/// </remarks>
public sealed class TerpHttpMetricsHandler : DelegatingHandler
{
    /// <summary>The name of the API tag (<c>"api"</c>).</summary>
    public const string ApiTagName = "api";

    /// <summary>The name of the status class tag (<c>"status_class"</c>).</summary>
    public const string StatusClassTagName = "status_class";

    private readonly Action<HttpMetricsEnrichmentContext> _enrich;

    /// <summary>Initializes a new instance of the <see cref="TerpHttpMetricsHandler"/> class.</summary>
    /// <param name="apiName">The value of the <c>api</c> tag, e.g. <c>tmdb</c>. Keep it low-cardinality.</param>
    /// <exception cref="ArgumentException"><paramref name="apiName"/> is null, empty or whitespace.</exception>
    public TerpHttpMetricsHandler(string apiName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiName);
        _enrich = context =>
        {
            context.AddCustomTag(ApiTagName, apiName);
            var statusClass = context.Response is { } response
                ? $"{(int)response.StatusCode / 100}xx"
                : "error";
            context.AddCustomTag(StatusClassTagName, statusClass);
        };
    }

    /// <inheritdoc />
    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        HttpMetricsEnrichmentContext.AddCallback(request, _enrich);
        return base.Send(request, cancellationToken);
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        HttpMetricsEnrichmentContext.AddCallback(request, _enrich);
        return base.SendAsync(request, cancellationToken);
    }
}
