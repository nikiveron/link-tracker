namespace LinkTracker.Scrapper.Domain.Options;

public class CircuitBreakerOptions
{
    public double FailureRateThreshold { get; set; }
    public int MinimumThroughput { get; set; }
    public int SamplingDurationSeconds { get; set; }
    public int BreakDurationSeconds { get; set; }
    public int PermittedCallsInHalfOpenState { get; set; }
}
