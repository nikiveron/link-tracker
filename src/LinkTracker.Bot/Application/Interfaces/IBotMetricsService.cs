namespace LinkTracker.Bot.Application.Interfaces;

public interface IBotMetricsService
{
    void TrackCommandRequest(string command);
    void TrackCommandDuration(string scope, string scopeType, double durationMs);
    void TrackSentNotification();
}
