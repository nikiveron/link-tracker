using LinkTracker.Bot.Application.Interfaces;
using LinkTracker.Bot.Application.Services;
using LinkTracker.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace LinkTracker.Bot.Controllers;

[ApiController]
[Route("updates")]
public class UpdatesController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IUpdateValidationService _validationService;
    private readonly ILogger<UpdatesController> _logger;

    public UpdatesController(INotificationService notificationService, IUpdateValidationService validationService, ILogger<UpdatesController> logger)
    {
        _notificationService = notificationService;
        _validationService = validationService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReceiveUpdate([FromBody] LinkUpdateDto update, CancellationToken ct)
    {
        _logger.LogInformation("Получено обновление для ссылки {Url} (Id: {Id})", update.Url, update.ChatId);

        if (!_validationService.IsModelStateValid(ModelState, out var modelErrors))
        {
            return BadRequest(ApiErrorFactory.CreateValidationError(modelErrors));
        }

        if (!_validationService.IsValid(update, out var errorMessage))
        {
            return BadRequest(ApiErrorFactory.CreateBadRequest("Список чатов для уведомления не может быть пустым", "EMPTY_CHAT_LIST", errorMessage));
        }

        try
        {
            await _notificationService.NotifyUsersAsync(update, ct);
            return Ok(new { status = "Обновление обработано" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки обновления {Id}", update.ChatId);
            return StatusCode(500, ApiErrorFactory.CreateInternalError(ex));
        }
    }
}