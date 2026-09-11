namespace LinkTracker.Scrapper.Domain.Options;

public class ResilienceOptions
{
    public int TimeoutSeconds { get; set; }
    public int RetryCount { get; set; }
    public int RetryDelayMilliseconds { get; set; }
    public List<int> RetryableStatusCodes { get; set; } = [];
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new CircuitBreakerOptions();

}
