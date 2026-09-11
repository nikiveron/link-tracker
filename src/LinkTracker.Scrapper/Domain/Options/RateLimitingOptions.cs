namespace LinkTracker.Scrapper.Domain.Options;

public class RateLimitingOptions
{
    public int PermitLimit { get; set; }
    public int WindowSeconds { get; set; }
}
