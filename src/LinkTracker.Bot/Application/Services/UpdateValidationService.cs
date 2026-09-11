using LinkTracker.Bot.Application.Interfaces;
using LinkTracker.Contracts;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LinkTracker.Bot.Application.Services;

public class UpdateValidationService : IUpdateValidationService
{
    private readonly ILogger<UpdateValidationService> _logger;

    public UpdateValidationService(ILogger<UpdateValidationService> logger)
    {
        _logger = logger;
    }

    public bool IsModelStateValid(ModelStateDictionary modelState, out string errorMessage)
    {
        if (modelState.IsValid)
        {
            errorMessage = string.Empty;
            return true;
        }

        var errors = modelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

        errorMessage = string.Join("; ", errors);
        _logger.LogDebug("Валидация запроса завершилась с ошибкой: {Errors}", errorMessage);

        return false;
    }

    public bool IsValid(LinkUpdateDto update, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(update.Url))
        {
            errorMessage = "Parameter 'tgChatIds' cannot be empty";
            _logger.LogDebug("Валидация запроса завершилась с ошибкой: {ErrorMessage}", errorMessage);
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}
