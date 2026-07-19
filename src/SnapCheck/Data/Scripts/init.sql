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

-- Turma como entidade (item 02.7) — vinculação de chat via comando /turma CODIGO,
-- mesmo padrão de tenant_chat_telegram (ADR 0001).
CREATE TABLE IF NOT EXISTS turmas (
    id SERIAL PRIMARY KEY,
    tenant_id INTEGER NOT NULL REFERENCES tenants(id),
    nome VARCHAR(200) NOT NULL,
    codigo_vinculacao VARCHAR(100) NOT NULL,
    ativa BOOLEAN NOT NULL DEFAULT TRUE,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (tenant_id, codigo_vinculacao)
);

-- Janelas de horário recorrentes por turma (ADR 0002 — janela simples, sem
-- calendário de aulas específicas). Uma turma pode ter várias janelas no
-- mesmo dia (ex.: academia manhã+noite, confirmado em docs/requisitos/regras-presenca.md).
CREATE TABLE IF NOT EXISTS turma_janelas (
    id SERIAL PRIMARY KEY,
    turma_id INTEGER NOT NULL REFERENCES turmas(id) ON DELETE CASCADE,
    dias_semana SMALLINT[] NOT NULL,
    hora_inicio TIME NOT NULL,
    hora_fim TIME NOT NULL,
    tolerancia_atraso_minutos INTEGER NOT NULL DEFAULT 0,
    corte_presenca_parcial_percentual SMALLINT NOT NULL DEFAULT 100,
    ativa BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE INDEX IF NOT EXISTS idx_turma_janelas_turma
    ON turma_janelas (turma_id)
    WHERE ativa = TRUE;

CREATE TABLE IF NOT EXISTS turma_chat_telegram (
    id SERIAL PRIMARY KEY,
    turma_id INTEGER NOT NULL REFERENCES turmas(id),
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
    turma VARCHAR(200),
    data_dia DATE,
    turma_normalizada VARCHAR(200),
    status_presenca VARCHAR(20) NOT NULL DEFAULT 'completa',
    responsavel_manual VARCHAR(200),
    motivo_manual VARCHAR(300)
);

-- Backfill de instalações existentes: presença herda o tenant da própria pessoa.
ALTER TABLE presencas ADD COLUMN IF NOT EXISTS tenant_id INTEGER;
ALTER TABLE presencas ADD COLUMN IF NOT EXISTS data_dia DATE;
ALTER TABLE presencas ADD COLUMN IF NOT EXISTS turma_normalizada VARCHAR(200);
ALTER TABLE presencas ADD COLUMN IF NOT EXISTS status_presenca VARCHAR(20) NOT NULL DEFAULT 'completa';
ALTER TABLE presencas ADD COLUMN IF NOT EXISTS responsavel_manual VARCHAR(200);
ALTER TABLE presencas ADD COLUMN IF NOT EXISTS motivo_manual VARCHAR(300);

UPDATE presencas p
SET tenant_id = pe.tenant_id
FROM pessoas pe
WHERE p.pessoa_id = pe.id
  AND p.tenant_id IS NULL;

UPDATE presencas
SET data_dia = data_hora::date
WHERE data_dia IS NULL;

UPDATE presencas
SET turma_normalizada = COALESCE(LOWER(TRIM(turma)), '')
WHERE turma_normalizada IS NULL;

ALTER TABLE presencas ALTER COLUMN tenant_id SET NOT NULL;
ALTER TABLE presencas ALTER COLUMN data_dia SET NOT NULL;
ALTER TABLE presencas ALTER COLUMN turma_normalizada SET NOT NULL;

DO $$
BEGIN
    ALTER TABLE presencas ADD CONSTRAINT fk_presencas_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id);
EXCEPTION
    WHEN duplicate_object THEN NULL;
END $$;

DROP INDEX IF EXISTS idx_presencas_pessoa_data;
DROP INDEX IF EXISTS idx_presencas_unq_tenant_pessoa_turma_dia;

CREATE INDEX IF NOT EXISTS idx_presencas_tenant_pessoa_data
    ON presencas (tenant_id, pessoa_id, data_hora DESC);

DELETE FROM presencas p
USING presencas d
WHERE p.id > d.id
  AND p.tenant_id = d.tenant_id
  AND p.pessoa_id = d.pessoa_id
  AND p.turma_normalizada = d.turma_normalizada
  AND p.data_dia = d.data_dia;

CREATE UNIQUE INDEX IF NOT EXISTS idx_presencas_unq_tenant_pessoa_turma_dia
    ON presencas (tenant_id, pessoa_id, turma_normalizada, data_dia);

CREATE TABLE IF NOT EXISTS revisoes_presenca_grupo (
    id SERIAL PRIMARY KEY,
    tenant_id INTEGER NOT NULL REFERENCES tenants(id),
    turma VARCHAR(200),
    chat_id BIGINT,
    status VARCHAR(20) NOT NULL DEFAULT 'pendente',
    expira_em TIMESTAMPTZ NOT NULL,
    criado_por VARCHAR(200) NOT NULL,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    confirmado_por VARCHAR(200),
    confirmado_em TIMESTAMPTZ,
    cancelado_por VARCHAR(200),
    cancelado_em TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS idx_revisoes_presenca_tenant_status
    ON revisoes_presenca_grupo (tenant_id, status, criado_em DESC);

CREATE TABLE IF NOT EXISTS revisao_faces_itens (
    id SERIAL PRIMARY KEY,
    revisao_id INTEGER NOT NULL REFERENCES revisoes_presenca_grupo(id) ON DELETE CASCADE,
    tenant_id INTEGER NOT NULL REFERENCES tenants(id),
    pessoa_sugerida_id INTEGER REFERENCES pessoas(id),
    nome_sugerido VARCHAR(200),
    confianca REAL,
    decisao VARCHAR(20) NOT NULL DEFAULT 'pendente',
    pessoa_final_id INTEGER REFERENCES pessoas(id),
    decidido_por VARCHAR(200),
    decidido_em TIMESTAMPTZ,
    motivo VARCHAR(300)
);

CREATE INDEX IF NOT EXISTS idx_revisao_faces_revisao
    ON revisao_faces_itens (revisao_id, id);

CREATE INDEX IF NOT EXISTS idx_revisao_faces_tenant
    ON revisao_faces_itens (tenant_id, decisao);

CREATE TABLE IF NOT EXISTS evidencias_foto_grupo (
    id SERIAL PRIMARY KEY,
    tenant_id INTEGER NOT NULL REFERENCES tenants(id),
    revisao_id INTEGER NOT NULL REFERENCES revisoes_presenca_grupo(id) ON DELETE CASCADE,
    origem VARCHAR(50) NOT NULL DEFAULT 'telegram',
    referencia_arquivo VARCHAR(500) NOT NULL,
    capturada_em TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expira_em TIMESTAMPTZ NOT NULL,
    apagada_em TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS idx_evidencias_foto_grupo_expira
    ON evidencias_foto_grupo (expira_em)
    WHERE apagada_em IS NULL;

CREATE TABLE IF NOT EXISTS revisao_presenca_auditoria (
    id SERIAL PRIMARY KEY,
    tenant_id INTEGER NOT NULL REFERENCES tenants(id),
    revisao_id INTEGER NOT NULL REFERENCES revisoes_presenca_grupo(id) ON DELETE CASCADE,
    evento VARCHAR(50) NOT NULL,
    detalhes TEXT,
    ator VARCHAR(200),
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_revisao_auditoria_revisao
    ON revisao_presenca_auditoria (revisao_id, criado_em DESC);

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
