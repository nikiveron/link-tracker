using LinkTracker.Bot.Domain.DTOs;

namespace LinkTracker.Bot.Application.Services;

public static class ApiErrorFactory
{
    public static ApiErrorResponse CreateBadRequest(string description, string code, string message)
    {
        return new ApiErrorResponse
        {
            Description = description,
            Code = code,
            ExceptionName = "ArgumentException",
            ExceptionMessage = message,
            Stacktrace = null
        };
    }

    public static ApiErrorResponse CreateValidationError(string errors)
    {
        return new ApiErrorResponse
        {
            Description = "Ошибка валидации входных данных",
            Code = "VALIDATION_ERROR",
            ExceptionName = "ValidationException",
            ExceptionMessage = errors,
            Stacktrace = null
        };
    }

    public static ApiErrorResponse CreateInternalError(Exception ex, string description = "Внутренняя ошибка сервера")
    {
        var stackTrace = ex.StackTrace?.Split('\n')
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        return new ApiErrorResponse
        {
            Description = description,
            Code = "INTERNAL_SERVER_ERROR",
            ExceptionName = ex.GetType().FullName,
            ExceptionMessage = ex.Message,
            Stacktrace = stackTrace
        };
    }
}