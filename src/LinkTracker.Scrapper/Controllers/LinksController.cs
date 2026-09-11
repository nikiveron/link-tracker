using LinkTracker.Scrapper.Application.Interfaces;
using LinkTracker.Scrapper.Application.Services;
using LinkTracker.Scrapper.Domain.DTOs.ControllersAndClients;
using LinkTracker.Scrapper.Domain.Repository;
using Microsoft.AspNetCore.Mvc;

namespace LinkTracker.Scrapper.Controllers;

[ApiController]
[Route("links")]
public class LinksController : ControllerBase
{
    private readonly ILogger<LinksController> _logger;
    private readonly ILinkRepository _repository;
    private readonly ILinkRequestValidator _validator;
    private readonly LinksControllerHelperService _linksControllerHelperService;
    private readonly ICacheService _cacheService;
    private readonly IConfiguration _configuration;

    public LinksController(ILogger<LinksController> logger, ILinkRepository linkRepository, ILinkRequestValidator validator, ICacheService cacheService, IConfiguration configuration)
    {
        _logger = logger;
        _repository = linkRepository;
        _validator = validator;
        _linksControllerHelperService = new LinksControllerHelperService();
        _cacheService = cacheService;
        _configuration = configuration;
    }

    /// <summary>
    /// Получить все отслеживаемые ссылки
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLinks([FromHeader(Name = "Tg-Chat-Id")] long tgChatId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? tags = null)
    {
        _logger.LogInformation("Получение ссылок для чата {ChatId}, страница {page}, кол-во строк {pageSize}, фильтры {tags}", tgChatId, page, pageSize, tags);

        if (!_validator.ValidateChatId(tgChatId, out var chatIdError))
        {
            return BadRequest(chatIdError);
        }
        if (!_validator.ValidatePage(page, out var pageError))
        {
            return BadRequest(pageError);
        }
        if (!_validator.ValidatePageSize(pageSize, out var pageSizeError))
        {
            return BadRequest(pageSizeError);
        }
        if (!_validator.ValidateChatExists(tgChatId, _repository, out var chatExistsError))
        {
            return NotFound(chatExistsError);
        }

        try
        {
            var cacheEnabled = _configuration.GetValue<bool>("Cache:Enabled");
            var ttlSeconds = _configuration.GetValue<int>("Cache:TtlSeconds");

            var safeTags = string.IsNullOrEmpty(tags) ? "none" : tags;
            var cacheKey = $"links:{tgChatId}:page{page}:size{pageSize}:tags{safeTags}";

            _logger.LogInformation("Проверка кэша cacheEnabled {cacheEnabled} с ключом {CacheKey}", cacheEnabled, cacheKey);

            if (cacheEnabled)
            {
                var cached = await _cacheService.GetAsync<ListLinksResponse>(cacheKey);
                if (cached != null)
                {
                    _logger.LogInformation("Результат найден в кэше по ключу {CacheKey}", cacheKey);
                    return Ok(cached);
                }
                else
                {
                    _logger.LogInformation("Warning! Результат  не найден в кэше по ключу {CacheKey}", cacheKey);
                }
            }

            var filterTags = _linksControllerHelperService.ParseFilterTags(tags);
            var linksDto = _repository.GetLinks(tgChatId, page, pageSize, filterTags);
            var links = linksDto.PagedLinks;

            var response = _linksControllerHelperService.BuildLinksResponse(linksDto, page, pageSize);

            if (cacheEnabled)
            {
                await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromSeconds(ttlSeconds));
                _logger.LogInformation("Результат  сохранен в кэше с ключом {CacheKey} на {TtlSeconds} секунд", cacheKey, ttlSeconds);
            }

            LogGetLinksResult(tgChatId, page, response, filterTags);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return HandleException(ex, "получении ссылок чата", tgChatId);
        }
    }

    /// <summary>
    /// Добавить отслеживание ссылки
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddLink([FromHeader(Name = "Tg-Chat-Id")] long tgChatId, [FromBody] AddLinkRequest request)
    {
        _logger.LogInformation("Добавление ссылки для чата {ChatId}: {Url}", tgChatId, request.Uri);

        if (!_validator.ValidateChatId(tgChatId, out var chatIdError))
        {
            return BadRequest(chatIdError);
        }
        if (!_validator.ValidateLink(request.Uri, out var linkError))
        {
            return BadRequest(linkError);
        }
        if (!_validator.ValidateChatExists(tgChatId, _repository, out var chatExistsError))
        {
            return NotFound(chatExistsError);
        }

        try
        {
            var link = new LinkDto
            {
                Url = request.Uri,
                Tags = request.Tags ?? [],
                ChatId = tgChatId,
                CreatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Создание ссылки для чата {ChatId}: {Link}", tgChatId, link.ToString());
            var added = _repository.AddLink(link);

            if (!added)
            {
                _logger.LogWarning("Ссылка {Url} уже отслеживается чатом {ChatId}", request.Uri, tgChatId);
                return Conflict(ApiErrorFactory.CreateConflict("Ссылка уже отслеживается", "LINK_ALREADY_EXISTS", "Link already exists for this chat"));
            }

            await _cacheService.DeleteByPrefixAsync($"links:{tgChatId}:");
            _logger.LogInformation("Ссылка {Link} добавлена для чата {ChatId} с id ссылки {LinkId}", request.Uri, tgChatId, link.Id);

            return Ok(new LinkResponse
            {
                Id = link.Id,
                Url = link.Url,
                Tags = link.Tags
            });
        }
        catch (Exception ex)
        {
            return HandleException(ex, "добавлении ссылки", tgChatId);
        }
    }

    /// <summary>
    /// Убрать отслеживание ссылки
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveLink([FromHeader(Name = "Tg-Chat-Id")] long tgChatId, [FromBody] RemoveLinkRequest request)
    {
        _logger.LogInformation("Удаление ссылки для чата {ChatId}: {Link}", tgChatId, request.Link);

        if (!_validator.ValidateChatId(tgChatId, out var chatIdError))
        {
            return BadRequest(chatIdError);
        }

        if (!_validator.ValidateLink(request.Link, out var linkError))
        {
            return BadRequest(linkError);
        }

        if (!_validator.ValidateChatExists(tgChatId, _repository, out var chatExistsError))
        {
            return NotFound(chatExistsError);
        }

        try
        {
            var link = _repository.GetLink(tgChatId, request.Link);
            var removed = _repository.RemoveLink(tgChatId, request.Link);

            if (!removed || link == null)
            {
                _logger.LogWarning("Ссылка {Link} не найдена для чата {ChatId}", request.Link, tgChatId);
                return NotFound(ApiErrorFactory.CreateNotFound("Ссылка не найдена", "LINK_NOT_FOUND", "Link not found for this chat"));
            }

            await _cacheService.DeleteByPrefixAsync($"links:{tgChatId}:");
            _logger.LogInformation("Ссылка {Link} удалена для чата {ChatId}", request.Link, tgChatId);

            return Ok(new LinkResponse
            {
                Id = link!.Id,
                Url = link.Url,
                Tags = link.Tags
            });
        }
        catch (Exception ex)
        {
            return HandleException(ex, "удалении ссылки", tgChatId);
        }
    }

    private ObjectResult HandleException(Exception ex, string operation, long chatId)
    {
        _logger.LogError(ex, "Ошибка при {Operation} для чата {ChatId}", operation, chatId);
        return StatusCode(500, ApiErrorFactory.CreateInternalError(ex, $"Внутренняя ошибка сервера при {operation}"));
    }

    private void LogGetLinksResult(long chatId, int page, ListLinksResponse response, List<string>? filterTags)
    {
        var uniqueTags = response.Links.SelectMany(l => l.Tags).Distinct().Count();

        _logger.LogInformation("Найдено {TotalCount} ссылок для чата {ChatId} (страница {Page}/{TotalPages}), показано {Displayed} ссылок, уникальных тегов: {UniqueTags}",
            response.TotalCount, chatId, page, response.TotalPages, response.Size, uniqueTags);

        if (filterTags != null && filterTags.Count != 0)
        {
            _logger.LogInformation("Применен фильтр по тегам: {FilterTags}", string.Join(", ", filterTags));
        }
    }
}