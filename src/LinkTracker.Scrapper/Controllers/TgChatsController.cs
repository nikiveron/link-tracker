using LinkTracker.Scrapper.Application.Interfaces;
using LinkTracker.Scrapper.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace LinkTracker.Scrapper.Controllers;

[ApiController]
[Route("tg-chat")]
public class TgChatsController : ControllerBase
{
    private readonly ILogger<TgChatsController> _logger;
    private readonly ILinkRepository _linkRepository;
    private readonly IChatValidationService _validationService;

    public TgChatsController(ILogger<TgChatsController> logger, ILinkRepository linkRepository, IChatValidationService validationService)
    {
        _logger = logger;
        _linkRepository = linkRepository;
        _validationService = validationService;
    }

    /// <summary>
    /// Зарегистрировать чат
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpPost("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult RegistryChat([FromRoute] long id)
    {
        _logger.LogInformation("Регистрация чата {ChatId}", id);

        if (!_validationService.IsValidChatId(id, out var errorMessage))
        {
            return BadRequest(ApiErrorFactory.CreateBadRequest("Некорректные параметры запроса", "INVALID_CHAT_ID", errorMessage));
        }

        try
        {
            return _linkRepository.AddChat(id)
                ? HandleSuccess($"Чат {id} успешно зарегистрирован", "Чат зарегистрирован")
                : HandleConflict("Чат уже существует", "CHAT_ALREADY_EXISTS", "Chat already exists");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка регистрации чата {ChatId}", id);
            return StatusCode(500, ApiErrorFactory.CreateInternalError(ex, "Внутренняя ошибка сервера при регистрации чата"));
        }
    }

    /// <summary>
    /// Удалить чат
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeleteChat([FromRoute] long id)
    {
        _logger.LogInformation("Удаление чата {ChatId}", id);

        if (!_validationService.IsValidChatId(id, out var errorMessage))
        {
            return BadRequest(ApiErrorFactory.CreateBadRequest("Некорректные параметры запроса", "INVALID_CHAT_ID", errorMessage));
        }

        try
        {
            return _linkRepository.RemoveChat(id)
                ? HandleSuccess($"Чат {id} успешно удален", "Чат успешно удален")
                : HandleNotFound("Чат не существует", "CHAT_NOT_FOUND", "Chat not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка удаления чата {ChatId}", id);
            return StatusCode(500, ApiErrorFactory.CreateInternalError(ex, "Внутренняя ошибка сервера при удалении чата"));
        }
    }

    private OkObjectResult HandleSuccess(string logMessage, string responseMessage)
    {
        _logger.LogInformation(logMessage);
        return Ok(new { Description = responseMessage });
    }

    private ConflictObjectResult HandleConflict(string description, string code, string message)
    {
        return Conflict(ApiErrorFactory.CreateConflict(description, code, message));
    }

    private NotFoundObjectResult HandleNotFound(string description, string code, string message)
    {
        return NotFound(ApiErrorFactory.CreateNotFound(description, code, message));
    }
}