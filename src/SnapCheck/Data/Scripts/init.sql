CREATE TABLE IF NOT EXISTS tenants (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(200) NOT NULL,
    codigo_ativacao VARCHAR(100) NOT NULL UNIQUE,
    status VARCHAR(20) NOT NULL DEFAULT 'ativo',
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

INSERT INTO tenants (nome, codigo_ativacao, status)
VALUES ('Tenant Piloto', 'PILOTO-MIGRACAO', 'ativo')
ON CONFLICT (codigo_ativacao) DO NOTHING;

CREATE TABLE IF NOT EXISTS tenant_chat_telegram (
    id SERIAL PRIMARY KEY,
    tenant_id INTEGER NOT NULL REFERENCES tenants(id),
    chat_id BIGINT NOT NULL UNIQUE,
    vinculado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS pessoas (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(200) NOT NULL,
    embedding BYTEA NOT NULL,
    data_cadastro TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    ativo BOOLEAN NOT NULL DEFAULT TRUE
);

-- Backfill de instalações existentes: adiciona tenant_id em pessoas já criadas
-- sem a coluna, associando ao tenant piloto (docs/requisitos/multi-tenant.md).
ALTER TABLE pessoas ADD COLUMN IF NOT EXISTS tenant_id INTEGER;

UPDATE pessoas
SET tenant_id = (SELECT id FROM tenants WHERE codigo_ativacao = 'PILOTO-MIGRACAO')
WHERE tenant_id IS NULL;

ALTER TABLE pessoas ALTER COLUMN tenant_id SET NOT NULL;

DO $$
BEGIN
    ALTER TABLE pessoas ADD CONSTRAINT fk_pessoas_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id);
EXCEPTION
    WHEN duplicate_object THEN NULL;
END $$;

-- Substitui o índice único global de nome por um único por tenant
-- (ADR 0001 — dois tenants podem ter pessoas com o mesmo nome).
DROP INDEX IF EXISTS idx_pessoas_nome_ativo;

CREATE UNIQUE INDEX IF NOT EXISTS idx_pessoas_tenant_nome_ativo
    ON pessoas (tenant_id, LOWER(nome))
    WHERE ativo = TRUE;

CREATE TABLE IF NOT EXISTS presencas (
    id SERIAL PRIMARY KEY,
    pessoa_id INTEGER NOT NULL REFERENCES pessoas(id),
    data_hora TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    turma VARCHAR(200)
);

-- Backfill de instalações existentes: presença herda o tenant da própria pessoa.
ALTER TABLE presencas ADD COLUMN IF NOT EXISTS tenant_id INTEGER;

UPDATE presencas p
SET tenant_id = pe.tenant_id
FROM pessoas pe
WHERE p.pessoa_id = pe.id
  AND p.tenant_id IS NULL;

ALTER TABLE presencas ALTER COLUMN tenant_id SET NOT NULL;

DO $$
BEGIN
    ALTER TABLE presencas ADD CONSTRAINT fk_presencas_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id);
EXCEPTION
    WHEN duplicate_object THEN NULL;
END $$;

DROP INDEX IF EXISTS idx_presencas_pessoa_data;

CREATE INDEX IF NOT EXISTS idx_presencas_tenant_pessoa_data
    ON presencas (tenant_id, pessoa_id, data_hora DESC);

-- configuracoes permanece global (infraestrutura do processo compartilhado:
-- token único do bot, connection string única do banco) — ver ADR 0001,
-- Decisão 1. Configuração de negócio por tenant é escopo do módulo 02.
-- 'postgres_connection_string' está marcada para remoção quando o item 01.9
-- fixar a connection string via configuração de deploy; mantida aqui por ora
-- porque Program.cs ainda depende dela como fallback em runtime.
CREATE TABLE IF NOT EXISTS configuracoes (
    id SERIAL PRIMARY KEY,
    chave VARCHAR(100) NOT NULL UNIQUE,
    valor TEXT NOT NULL
);

INSERT INTO configuracoes (chave, valor)
VALUES ('telegram_token', ''),
       ('postgres_connection_string', '')
ON CONFLICT (chave) DO NOTHING;
