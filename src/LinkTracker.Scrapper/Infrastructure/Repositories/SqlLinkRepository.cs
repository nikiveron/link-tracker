using LinkTracker.Scrapper.Application.Interfaces;
using LinkTracker.Scrapper.Domain.Repository;
using Npgsql;

public class SqlLinkRepository : ILinkRepository
{
    private readonly string _connectionString;

    public SqlLinkRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private const string _insertChatSql = "INSERT INTO chats (id) VALUES (@id) ON CONFLICT DO NOTHING";
    private const string _deleteChatSql = "DELETE FROM chats WHERE id = @id";
    private const string _chatExistsSql = "SELECT 1 FROM chats WHERE id = @id";

    private const string _insertLinkSql = @"
        INSERT INTO links (url, chat_id)
        VALUES (@url, @chatId)
        ON CONFLICT (url) DO NOTHING
        RETURNING id;";

    private const string _getLinkSql = @"
        SELECT l.id, l.url, l.created_at, l.last_updated_at, l.last_notified_at, l.chat_id
        FROM links l
        WHERE l.chat_id = @chatId AND l.url = @url";

    private const string _deleteLinkSql = @" 
        DELETE FROM links
        WHERE chat_id = @chatId AND url = @url";

    private const string _getAllLinksSql = "SELECT url, chat_id FROM links";
    private const string _getLinkIdSql = "SELECT id FROM links WHERE chat_id = @chatId AND url = @url";

    private const string _getLinksCountBaseSql = @"
        SELECT COUNT(*)
        FROM links l
        WHERE l.chat_id = @chatId";

    private const string _getLinksPaginationBaseSql = @"
        SELECT l.id, l.url, l.created_at, l.last_updated_at, l.last_notified_at, l.chat_id
        FROM links l
        WHERE l.chat_id = @chatId
        ORDER BY l.id
        LIMIT @limit OFFSET @offset";

    private const string _tagsFilterSql = @"
        AND l.id IN (
            SELECT lt.link_id
            FROM link_tag lt
            JOIN tags t ON t.id = lt.tag_id
            WHERE LOWER(t.name) = ANY(@tags)
            GROUP BY lt.link_id
            HAVING COUNT(DISTINCT LOWER(t.name)) = @tagCount
        )";

    private const string _getTagsByLinkIdSql = @"
        SELECT t.name
        FROM tags t
        JOIN link_tag lt ON lt.tag_id = t.id
        WHERE lt.link_id = @linkId";

    private const string _insertTagSql = @"
        INSERT INTO tags (name)
        VALUES (@name)
        ON CONFLICT (name) DO NOTHING;";

    private const string _getTagIdSql = "SELECT id FROM tags WHERE name = @name";

    private const string _insertLinkTagSql = @"
        INSERT INTO link_tag (link_id, tag_id)
        VALUES (@linkId, @tagId)
        ON CONFLICT DO NOTHING;";

    private const string _updateLinkSql = "UPDATE links SET {0} WHERE id = @linkId";

    private const string _getBatchSql = @"
        SELECT id, url, created_at, last_updated_at, last_notified_at, chat_id
        FROM links
        ORDER BY id
        LIMIT @limit OFFSET @offset";

    private const string _updateLinkByIdSql = "UPDATE links SET {0} WHERE id = @id";

    public bool AddChat(long chatId)
    {
        using var conn = OpenConnection();

        using var cmd = new NpgsqlCommand(_insertChatSql, conn);
        cmd.Parameters.AddWithValue("id", chatId);

        return cmd.ExecuteNonQuery() > 0;
    }

    public bool RemoveChat(long chatId)
    {
        using var conn = OpenConnection();

        using var cmd = new NpgsqlCommand(_deleteChatSql, conn);
        cmd.Parameters.AddWithValue("id", chatId);

        return cmd.ExecuteNonQuery() > 0;
    }

    public bool ChatExists(long chatId)
    {
        using var conn = OpenConnection();

        using var cmd = new NpgsqlCommand(_chatExistsSql, conn);
        cmd.Parameters.AddWithValue("id", chatId);

        return cmd.ExecuteScalar() != null;
    }

    public bool AddLink(LinkDto link)
    {
        using var conn = OpenConnection();
        using var tx = conn.BeginTransaction();

        var insertLink = new NpgsqlCommand(_insertLinkSql, conn, tx);
        insertLink.Parameters.AddWithValue("url", link.Url);
        insertLink.Parameters.AddWithValue("chatId", link.ChatId);

        var linkIdObj = insertLink.ExecuteScalar();

        if (linkIdObj == null)
        {
            tx.Rollback();
            return false;
        }

        long linkId = (long)linkIdObj;

        AddTagsToLink(conn, tx, linkId, link.Tags);

        tx.Commit();
        return true;
    }

    public LinkDto? GetLink(long chatId, string url)
    {
        using var conn = OpenConnection();

        using var cmd = new NpgsqlCommand(_getLinkSql, conn);
        cmd.Parameters.AddWithValue("chatId", chatId);
        cmd.Parameters.AddWithValue("url", url);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
        {
            return null;
        }

        var link = MapLink(reader);
        reader.Close();

        link.Tags = GetTagsByLinkId(conn, link.Id);

        return link;
    }

    public GetLinksDto GetLinks(long chatId, int page, int pageSize, List<string>? filterTags)
    {
        using var conn = OpenConnection();

        var offset = (page - 1) * pageSize;
        var hasFilter = filterTags != null && filterTags.Count > 0;

        int totalCount = GetLinksCount(conn, chatId, filterTags, hasFilter);
        if (totalCount == 0)
        {
            return new GetLinksDto { PagedLinks = [], TotalCount = 0 };
        }

        var links = GetLinksWithPagination(conn, chatId, pageSize, offset, filterTags, hasFilter);
        foreach (var link in links)
        {
            link.Tags = GetTagsByLinkId(conn, link.Id);
        }

        return new GetLinksDto
        {
            PagedLinks = links,
            TotalCount = totalCount
        };
    }

    private int GetLinksCount(NpgsqlConnection conn, long chatId, List<string>? filterTags, bool hasFilter)
    {
        string sql = _getLinksCountBaseSql;
        var normalizedTags = hasFilter ? filterTags!.Select(t => t.ToLower()).ToArray() : [];

        if (hasFilter)
        {
            sql += _tagsFilterSql;
        }

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("chatId", chatId);

        if (hasFilter)
        {
            cmd.Parameters.AddWithValue("tags", normalizedTags);
            cmd.Parameters.AddWithValue("tagCount", normalizedTags.Length);
        }

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private List<LinkDto> GetLinksWithPagination(NpgsqlConnection conn, long chatId, int pageSize, int offset, List<string>? filterTags, bool hasFilter)
    {
        string sql = _getLinksPaginationBaseSql;
        var normalizedTags = hasFilter ? filterTags!.Select(t => t.ToLower()).ToArray() : [];

        if (hasFilter)
        {
            sql = sql.Replace("WHERE l.chat_id = @chatId", $"WHERE l.chat_id = @chatId {_tagsFilterSql}");
        }

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("chatId", chatId);
        cmd.Parameters.AddWithValue("limit", pageSize);
        cmd.Parameters.AddWithValue("offset", offset);

        if (hasFilter)
        {
            cmd.Parameters.AddWithValue("tags", normalizedTags);
            cmd.Parameters.AddWithValue("tagCount", normalizedTags.Length);
        }

        var result = new List<LinkDto>();

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(MapLink(reader));
        }

        return result;
    }

    public bool UpdateLink(LinkDto linkDto)
    {
        using var conn = OpenConnection();
        using var tx = conn.BeginTransaction();

        var linkId = GetLinkId(conn, tx, linkDto.ChatId, linkDto.Url);
        if (linkId == null)
        {
            tx.Rollback();
            return false;
        }

        var updateFields = new List<string>();
        var parameters = new List<(string Name, object Value)>();

        if (linkDto.LastNotifiedAt != null)
        {
            updateFields.Add("last_notified_at = @lastNotifiedAt");
            parameters.Add(("@lastNotifiedAt", linkDto.LastNotifiedAt));
        }

        if (linkDto.LastUpdatedAt != null)
        {
            updateFields.Add("last_updated_at = @lastUpdatedAt");
            parameters.Add(("@lastUpdatedAt", linkDto.LastUpdatedAt));
        }

        if (updateFields.Count == 0)
        {
            tx.Commit();
            return true;
        }

        var updateSql = string.Format(_updateLinkSql, string.Join(", ", updateFields));
        using var updateCmd = new NpgsqlCommand(updateSql, conn, tx);
        updateCmd.Parameters.AddWithValue("linkId", linkId.Value);

        foreach (var (name, value) in parameters)
        {
            updateCmd.Parameters.AddWithValue(name, value);
        }

        updateCmd.ExecuteNonQuery();

        tx.Commit();
        return true;
    }

    public bool RemoveLink(long chatId, string url)
    {
        using var conn = OpenConnection();

        using var cmd = new NpgsqlCommand(_deleteLinkSql, conn);
        cmd.Parameters.AddWithValue("chatId", chatId);
        cmd.Parameters.AddWithValue("url", url);

        return cmd.ExecuteNonQuery() > 0;
    }

    public async Task<List<LinkDto>> GetBatch(int limit, int offset)
    {
        using var conn = OpenConnection();

        using var cmd = new NpgsqlCommand(_getBatchSql, conn);
        cmd.Parameters.AddWithValue("limit", limit);
        cmd.Parameters.AddWithValue("offset", offset);

        var result = new List<LinkDto>();

        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(new LinkDto
            {
                Id = reader.GetInt64(0),
                Url = reader.GetString(1),
                CreatedAt = reader.GetDateTime(2),
                LastUpdatedAt = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                LastNotifiedAt = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                ChatId = reader.GetInt64(5),
                Tags = []
            });
        }

        reader.Close();

        foreach (var link in result)
        {
            link.Tags = GetTagsByLinkId(conn, link.Id);
        }

        return result;
    }

    public async Task<bool> UpdateLinkAsync(LinkDto linkDto)
    {
        using var conn = OpenConnection();

        var updates = new List<string>();
        var parameters = new List<(string, object)>();

        if (linkDto.LastUpdatedAt != null)
        {
            updates.Add("last_updated_at = @lastUpdatedAt");
            parameters.Add(("lastUpdatedAt", linkDto.LastUpdatedAt));
        }

        if (linkDto.LastNotifiedAt != null)
        {
            updates.Add("last_notified_at = @lastNotifiedAt");
            parameters.Add(("lastNotifiedAt", linkDto.LastNotifiedAt));
        }

        if (updates.Count == 0)
        {
            return true;
        }

        var sql = string.Format(_updateLinkByIdSql, string.Join(", ", updates));

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", linkDto.Id);

        foreach (var (name, value) in parameters)
        {
            cmd.Parameters.AddWithValue(name, value);
        }

        var affected = await cmd.ExecuteNonQueryAsync();
        return affected > 0;
    }

    public async Task<int> GetCountBySource(string source)
    {
        using var conn = OpenConnection();

        const string sql = "SELECT COUNT(*) FROM links WHERE url LIKE @source";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("source", $"%{source}%");

        return await cmd.ExecuteScalarAsync() as int? ?? 0;
    }

    private NpgsqlConnection OpenConnection()
    {
        var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        return conn;
    }

    private static LinkDto MapLink(NpgsqlDataReader reader)
    {
        return new LinkDto
        {
            Id = reader.GetInt64(0),
            Url = reader.GetString(1),
            CreatedAt = reader.GetDateTime(2),
            LastUpdatedAt = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
            LastNotifiedAt = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
            ChatId = reader.GetInt64(5),
            Tags = []
        };
    }

    private static List<string> GetTagsByLinkId(NpgsqlConnection conn, long linkId)
    {
        var tags = new List<string>();

        using var cmd = new NpgsqlCommand(_getTagsByLinkIdSql, conn);
        cmd.Parameters.AddWithValue("linkId", linkId);

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            tags.Add(reader.GetString(0));
        }

        return tags;
    }

    private static long GetOrCreateTag(NpgsqlConnection conn, NpgsqlTransaction tx, string tag)
    {
        using (var insert = new NpgsqlCommand(_insertTagSql, conn, tx))
        {
            insert.Parameters.AddWithValue("name", tag);
            insert.ExecuteNonQuery();
        }

        using var select = new NpgsqlCommand(_getTagIdSql, conn, tx);
        select.Parameters.AddWithValue("name", tag);

        return (long)select.ExecuteScalar()!;
    }

    private static void AddTagsToLink(NpgsqlConnection conn, NpgsqlTransaction tx, long linkId, List<string> tags)
    {
        foreach (var tag in tags)
        {
            var tagId = GetOrCreateTag(conn, tx, tag);

            using var cmd = new NpgsqlCommand(_insertLinkTagSql, conn, tx);
            cmd.Parameters.AddWithValue("linkId", linkId);
            cmd.Parameters.AddWithValue("tagId", tagId);
            cmd.ExecuteNonQuery();
        }
    }

    private static long? GetLinkId(NpgsqlConnection conn, NpgsqlTransaction? tx, long chatId, string url)
    {
        using var cmd = tx == null ? new NpgsqlCommand(_getLinkIdSql, conn) : new NpgsqlCommand(_getLinkIdSql, conn, tx);

        cmd.Parameters.AddWithValue("chatId", chatId);
        cmd.Parameters.AddWithValue("url", url);

        return cmd.ExecuteScalar() as long?;
    }
}