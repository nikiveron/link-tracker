using LinkTracker.Bot.Application.Interfaces;
using Prometheus;

namespace LinkTracker.Bot.Application.Services;

public class BotMetricsService : IBotMetricsService
{
    private static readonly Counter CommandRequestsTotal = Metrics.CreateCounter(
        "command_requests_total",
        "Счетчик обработанных команд в боте",
        new CounterConfiguration
        {
            LabelNames = ["command", "app"]
        });

    private static readonly Histogram CommandDurationHistogram = Metrics.CreateHistogram(
        "command_duration_ms_total",
        "Длительность одной операции в миллисекундах",
        new HistogramConfiguration
        {
            LabelNames = ["scope", "scope_type", "app"],
            Buckets = [0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1.0, 2.5, 5.0, 10.0]
        });

    private static readonly Counter SentNotificationTotal = Metrics.CreateCounter(
        "sent_notification_total",
        "Счетчик отправленных нотификаций",
        new CounterConfiguration
        {
            LabelNames = ["app"]
        });

    public void TrackCommandRequest(string command)
    {
        CommandRequestsTotal.WithLabels(command, "bot").Inc();
    }

    public void TrackCommandDuration(string scope, string scopeType, double durationMs)
    {
        CommandDurationHistogram.WithLabels(scope, scopeType, "bot").Observe(durationMs / 1000.0);
    }

    public void TrackSentNotification()
    {
        SentNotificationTotal.WithLabels("bot").Inc();
    }
}
