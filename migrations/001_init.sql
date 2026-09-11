CREATE TABLE IF NOT EXISTS chats (
    id BIGINT PRIMARY KEY,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS links (
    id BIGSERIAL PRIMARY KEY,
    url TEXT NOT NULL UNIQUE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_updated_at TIMESTAMP WITH TIME ZONE,
    last_notified_at TIMESTAMP WITH TIME ZONE,
    chat_id BIGINT NOT NULL,

    CONSTRAINT fk_links_chat FOREIGN KEY (chat_id) REFERENCES chats(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS tags (
    id BIGSERIAL PRIMARY KEY,
    name TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS link_tag (
    link_id BIGINT NOT NULL,
    tag_id BIGINT NOT NULL,

    PRIMARY KEY (link_id, tag_id),

    CONSTRAINT fk_linktag_link FOREIGN KEY (link_id) REFERENCES links(id) ON DELETE CASCADE,
    CONSTRAINT fk_linktag_tag FOREIGN KEY (tag_id) REFERENCES tags(id) ON DELETE CASCADE
);

-- Индексы для оптимизации
CREATE UNIQUE INDEX IF NOT EXISTS idx_chats_id ON chats(id);

CREATE UNIQUE INDEX IF NOT EXISTS idx_links_url ON links(url);

CREATE INDEX IF NOT EXISTS idx_links_chat_id ON links(chat_id);

-- Индексы для связующей таблицы
CREATE INDEX IF NOT EXISTS idx_linktag_link_id ON link_tag(link_id);
CREATE INDEX IF NOT EXISTS idx_linktag_tag_id ON link_tag(tag_id);

-- Индекс для поиска по имени тега
CREATE INDEX IF NOT EXISTS idx_tags_name ON tags(name);
