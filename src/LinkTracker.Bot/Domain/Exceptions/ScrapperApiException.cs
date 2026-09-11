using LinkTracker.Scrapper.Domain.DTOs.ControllersAndClients;
using System.Net;

namespace LinkTracker.Bot.Domain.Exceptions;

public class ScrapperApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string Operation { get; }
    public ApiErrorResponse? ErrorResponse { get; }

    public ScrapperApiException(string message, HttpStatusCode statusCode, string operation, ApiErrorResponse? errorResponse = null)
        : base(message)
    {
        StatusCode = statusCode;
        Operation = operation;
        ErrorResponse = errorResponse;
    }
}

public class ScrapperBadRequestException : ScrapperApiException
{
    public ScrapperBadRequestException(string message, string operation, ApiErrorResponse? errorResponse = null)
        : base(message, HttpStatusCode.BadRequest, operation, errorResponse) { }
}

public class ScrapperNotFoundException : ScrapperApiException
{
    public ScrapperNotFoundException(string message, string operation, ApiErrorResponse? errorResponse = null)
        : base(message, HttpStatusCode.NotFound, operation, errorResponse) { }
}

public class ScrapperConflictException : ScrapperApiException
{
    public ScrapperConflictException(string message, string operation, ApiErrorResponse? errorResponse = null)
        : base(message, HttpStatusCode.Conflict, operation, errorResponse) { }
}