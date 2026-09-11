using LinkTracker.Scrapper.Application.Interfaces;
using Prometheus;

namespace LinkTracker.Scrapper.Application.Services;

public class MetricsService : IMetricsService
{
    public static readonly Gauge LinksOnTrackTotal = Metrics.CreateGauge(
        "links_on_track_total",
        "Количество ссылок в БД, поставленных на мониторинг",
        new GaugeConfiguration
        {
            LabelNames = ["tracked_source", "app"]
        });

    public static readonly Histogram RequestDurationHistogram = Metrics.CreateHistogram(
        "request_duration_ms_total",
        "Длительность одной операции в миллисекундах",
        new HistogramConfiguration
        {
            LabelNames = ["scope", "scope_type", "app"],
            Buckets = [0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1.0, 2.5, 5.0, 10.0]
        });

    public static readonly Counter ApiRequestsTotal = Metrics.CreateCounter(
        "api_requests_total",
        "Счетчик пришедших к API запросов",
        new CounterConfiguration
        {
            LabelNames = ["source", "app"]
        });

    public void TrackLinksOnTrack(string source, int count)
    {
        LinksOnTrackTotal.WithLabels(source, "scrapper").Set(count);
    }

    public void TrackRequestDuration(string scope, string scopeType, double durationMs)
    {
        RequestDurationHistogram.WithLabels(scope, scopeType, "scrapper").Observe(durationMs / 1000.0);
    }

    public void TrackApiRequest(string source)
    {
        ApiRequestsTotal.WithLabels(source, "scrapper").Inc();
    }
}
