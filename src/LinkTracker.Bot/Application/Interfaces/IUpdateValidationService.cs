using LinkTracker.Contracts;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LinkTracker.Bot.Application.Interfaces;

public interface IUpdateValidationService
{
    bool IsValid(LinkUpdateDto update, out string errorMessage);
    bool IsModelStateValid(ModelStateDictionary modelState, out string errorMessage);
}
