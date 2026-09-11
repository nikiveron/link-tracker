namespace LinkTracker.Scrapper.Application.Interfaces;

public interface IMetricsService
{
    void TrackLinksOnTrack(string source, int count);
    void TrackRequestDuration(string scope, string scopeType, double durationMs);
    void TrackApiRequest(string source);
}
