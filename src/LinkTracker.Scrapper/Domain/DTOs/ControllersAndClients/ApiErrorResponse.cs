namespace LinkTracker.Scrapper.Domain.DTOs.ControllersAndClients;

public class ApiErrorResponse
{
    public string Description { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? ExceptionName { get; set; }
    public string? ExceptionMessage { get; set; }
    public List<string>? Stacktrace { get; set; }
}