using System.Net.Http.Metrics;

namespace Terp.Diagnostics;

/// <summary>
/// Adds the tag <c>api=&lt;apiName&gt;</c> to the built-in HttpClient metrics (meter <c>System.Net.Http</c>, e.g.
/// <c>http.client.request.duration</c>) of every request that passes through it. It records no instruments of its own.
/// </summary>
/// <remarks>
/// The tag is present on success, on non-success status codes and on connection failures. A request cancelled before it
/// was sent is not recorded by the BCL at all. The handler has no dependency on <c>IHttpClientBuilder</c>; the application
/// registers it as an HttpClient message handler and subscribes to the BCL meter:
/// <code>
/// services.AddHttpClient("tmdb").AddHttpMessageHandler(() => new TerpHttpMetricsHandler("tmdb"));
/// builder.Services.AddOpenTelemetry().WithMetrics(m => m.AddMeter("System.Net.Http"));
/// </code>
/// </remarks>
public sealed class TerpHttpMetricsHandler : DelegatingHandler
{
    /// <summary>The name of the added tag (<c>"api"</c>).</summary>
    public const string TagName = "api";

    private readonly Action<HttpMetricsEnrichmentContext> _enrich;

    /// <summary>Initializes a new instance of the <see cref="TerpHttpMetricsHandler"/> class.</summary>
    /// <param name="apiName">The value of the <c>api</c> tag, e.g. <c>tmdb</c>. Keep it low-cardinality.</param>
    /// <exception cref="ArgumentException"><paramref name="apiName"/> is null, empty or whitespace.</exception>
    public TerpHttpMetricsHandler(string apiName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiName);
        _enrich = context => context.AddCustomTag(TagName, apiName);
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
