CREATE TABLE IF NOT EXISTS pessoas (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(200) NOT NULL,
    embedding BYTEA NOT NULL,
    data_cadastro TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    ativo BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_pessoas_nome_ativo
    ON pessoas (LOWER(nome))
    WHERE ativo = TRUE;

CREATE TABLE IF NOT EXISTS presencas (
    id SERIAL PRIMARY KEY,
    pessoa_id INTEGER NOT NULL REFERENCES pessoas(id),
    data_hora TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    turma VARCHAR(200)
);

CREATE INDEX IF NOT EXISTS idx_presencas_pessoa_data
    ON presencas (pessoa_id, data_hora DESC);

CREATE TABLE IF NOT EXISTS configuracoes (
    id SERIAL PRIMARY KEY,
    chave VARCHAR(100) NOT NULL UNIQUE,
    valor TEXT NOT NULL
);

INSERT INTO configuracoes (chave, valor)
VALUES ('telegram_token', ''),
       ('postgres_connection_string', '')
ON CONFLICT (chave) DO NOTHING;
