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

CREATE TABLE IF NOT EXISTS permissoes (
    id SERIAL PRIMARY KEY,
    codigo VARCHAR(100) NOT NULL UNIQUE,
    nome VARCHAR(200) NOT NULL,
    descricao VARCHAR(300),
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS papeis (
    id SERIAL PRIMARY KEY,
    tenant_id INTEGER REFERENCES tenants(id) ON DELETE CASCADE,
    codigo VARCHAR(100) NOT NULL,
    nome VARCHAR(200) NOT NULL,
    descricao VARCHAR(300),
    ativo BOOLEAN NOT NULL DEFAULT TRUE,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_papeis_tenant_codigo_ativo
    ON papeis (COALESCE(tenant_id, 0), LOWER(codigo))
    WHERE ativo = TRUE;

CREATE TABLE IF NOT EXISTS usuarios (
    id SERIAL PRIMARY KEY,
    tenant_id INTEGER REFERENCES tenants(id) ON DELETE CASCADE,
    nome VARCHAR(200) NOT NULL,
    email VARCHAR(200) NOT NULL,
    ativo BOOLEAN NOT NULL DEFAULT TRUE,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS senha_salt VARCHAR(100);
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS senha_hash VARCHAR(255);
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS auth_token_hash VARCHAR(255);
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS auth_token_expira_em TIMESTAMPTZ;
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS ultimo_login_em TIMESTAMPTZ;

CREATE UNIQUE INDEX IF NOT EXISTS idx_usuarios_tenant_email_ativo
    ON usuarios (COALESCE(tenant_id, 0), LOWER(email))
    WHERE ativo = TRUE;

CREATE TABLE IF NOT EXISTS papel_permissoes (
    id SERIAL PRIMARY KEY,
    papel_id INTEGER NOT NULL REFERENCES papeis(id) ON DELETE CASCADE,
    permissao_id INTEGER NOT NULL REFERENCES permissoes(id) ON DELETE CASCADE,
    concedido_em TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (papel_id, permissao_id)
);

CREATE TABLE IF NOT EXISTS usuario_papeis (
    id SERIAL PRIMARY KEY,
    usuario_id INTEGER NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
    papel_id INTEGER NOT NULL REFERENCES papeis(id) ON DELETE CASCADE,
    atribuido_em TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (usuario_id, papel_id)
);

INSERT INTO permissoes (codigo, nome, descricao)
VALUES
    ('tenant.read', 'Ler tenant', 'Visualizar tenants e seus metadados'),
    ('tenant.write', 'Editar tenant', 'Criar e atualizar tenants'),
    ('tenant.delete', 'Excluir tenant', 'Desativar ou excluir tenants'),
    ('usuario.read', 'Ler usuários', 'Visualizar usuários e seus vínculos'),
    ('usuario.write', 'Editar usuários', 'Criar e atualizar usuários'),
    ('usuario.delete', 'Excluir usuários', 'Desativar usuários'),
    ('papel.read', 'Ler papéis', 'Visualizar papéis e permissões'),
    ('papel.write', 'Editar papéis', 'Criar e atualizar papéis'),
    ('papel.delete', 'Excluir papéis', 'Desativar papéis'),
    ('turma.read', 'Ler turmas', 'Visualizar turmas'),
    ('turma.write', 'Editar turmas', 'Criar e atualizar turmas'),
    ('turma.delete', 'Excluir turmas', 'Desativar turmas'),
    ('turno.read', 'Ler turnos', 'Visualizar janelas e turnos'),
    ('turno.write', 'Editar turnos', 'Criar e atualizar janelas e turnos'),
    ('turno.delete', 'Excluir turnos', 'Desativar janelas e turnos'),
    ('professor.read', 'Ler professores', 'Visualizar professores vinculados'),
    ('professor.write', 'Editar professores', 'Criar e atualizar professores'),
    ('professor.delete', 'Excluir professores', 'Desativar professores'),
    ('aluno.read', 'Ler alunos', 'Visualizar alunos vinculados'),
    ('aluno.write', 'Editar alunos', 'Criar e atualizar alunos'),
    ('aluno.delete', 'Excluir alunos', 'Desativar alunos'),
    ('vinculo.turma.write', 'Vincular turma', 'Vincular alunos, professores ou chats a turmas'),
    ('vinculo.solicitar.write', 'Solicitar vínculo', 'Solicitar entrada em uma turma'),
    ('presenca.read', 'Ler presenças', 'Consultar presenças'),
    ('presenca.write', 'Registrar presença', 'Registrar presença por foto ou operação'),
    ('presenca.manual', 'Registrar presença manual', 'Registrar presença com justificativa'),
    ('presenca.propria.read', 'Ler presença própria', 'Consultar presença do próprio usuário'),
    ('presenca.propria.write', 'Registrar presença própria', 'Registrar presença do próprio usuário'),
    ('revisao.read', 'Ler revisões', 'Visualizar revisões de presença'),
    ('revisao.write', 'Editar revisões', 'Alterar decisões de revisão'),
    ('revisao.approve', 'Aprovar revisões', 'Confirmar revisões de presença'),
    ('relatorio.read', 'Ler relatórios', 'Visualizar relatórios e indicadores'),
    ('relatorio.export', 'Exportar relatórios', 'Exportar relatórios em PDF ou Excel'),
    ('pagamento.read', 'Ler pagamentos', 'Visualizar cobranças e status'),
    ('pagamento.write', 'Editar pagamentos', 'Criar ou atualizar cobranças'),
    ('pagamento.refund', 'Estornar pagamentos', 'Cancelar ou estornar cobranças'),
    ('auditoria.read', 'Ler auditoria', 'Visualizar trilha de auditoria')
ON CONFLICT (codigo) DO NOTHING;

INSERT INTO papeis (tenant_id, codigo, nome, descricao, ativo)
SELECT NULL, 'super_admin', 'Super Admin', 'Acesso global à plataforma', TRUE
WHERE NOT EXISTS (SELECT 1 FROM papeis WHERE tenant_id IS NULL AND codigo = 'super_admin');

INSERT INTO papeis (tenant_id, codigo, nome, descricao, ativo)
SELECT NULL, 'coordenador', 'Coordenador', 'Administração da instituição', TRUE
WHERE NOT EXISTS (SELECT 1 FROM papeis WHERE tenant_id IS NULL AND codigo = 'coordenador');

INSERT INTO papeis (tenant_id, codigo, nome, descricao, ativo)
SELECT NULL, 'professor', 'Professor', 'Operação de presença da turma', TRUE
WHERE NOT EXISTS (SELECT 1 FROM papeis WHERE tenant_id IS NULL AND codigo = 'professor');

INSERT INTO papeis (tenant_id, codigo, nome, descricao, ativo)
SELECT NULL, 'aluno', 'Aluno', 'Autoatendimento do aluno', TRUE
WHERE NOT EXISTS (SELECT 1 FROM papeis WHERE tenant_id IS NULL AND codigo = 'aluno');

INSERT INTO papeis (tenant_id, codigo, nome, descricao, ativo)
SELECT NULL, 'gestor', 'Gestor', 'Visão gerencial e relatórios', TRUE
WHERE NOT EXISTS (SELECT 1 FROM papeis WHERE tenant_id IS NULL AND codigo = 'gestor');

INSERT INTO papeis (tenant_id, codigo, nome, descricao, ativo)
SELECT NULL, 'auditor', 'Auditor', 'Leitura para auditoria e conformidade', TRUE
WHERE NOT EXISTS (SELECT 1 FROM papeis WHERE tenant_id IS NULL AND codigo = 'auditor');

INSERT INTO papeis (tenant_id, codigo, nome, descricao, ativo)
SELECT NULL, 'operador', 'Operador', 'Suporte operacional', TRUE
WHERE NOT EXISTS (SELECT 1 FROM papeis WHERE tenant_id IS NULL AND codigo = 'operador');

INSERT INTO papel_permissoes (papel_id, permissao_id)
SELECT p.id, pr.id
FROM papeis p
JOIN permissoes pr ON pr.codigo IN (
    'tenant.read', 'tenant.write', 'tenant.delete',
    'usuario.read', 'usuario.write', 'usuario.delete',
    'papel.read', 'papel.write', 'papel.delete',
    'turma.read', 'turma.write', 'turma.delete',
    'turno.read', 'turno.write', 'turno.delete',
    'professor.read', 'professor.write', 'professor.delete',
    'aluno.read', 'aluno.write', 'aluno.delete',
    'vinculo.turma.write', 'vinculo.solicitar.write',
    'presenca.read', 'presenca.write', 'presenca.manual', 'presenca.propria.read', 'presenca.propria.write',
    'revisao.read', 'revisao.write', 'revisao.approve',
    'relatorio.read', 'relatorio.export',
    'pagamento.read', 'pagamento.write', 'pagamento.refund',
    'auditoria.read'
)
WHERE p.tenant_id IS NULL AND p.codigo = 'super_admin'
ON CONFLICT (papel_id, permissao_id) DO NOTHING;

INSERT INTO papel_permissoes (papel_id, permissao_id)
SELECT p.id, pr.id
FROM papeis p
JOIN permissoes pr ON pr.codigo IN (
    'usuario.read', 'usuario.write',
    'papel.read', 'papel.write',
    'turma.read', 'turma.write',
    'turno.read', 'turno.write',
    'professor.read', 'professor.write',
    'aluno.read', 'aluno.write',
    'vinculo.turma.write',
    'presenca.read', 'presenca.write', 'presenca.manual',
    'revisao.read', 'revisao.approve',
    'relatorio.read', 'relatorio.export',
    'pagamento.read',
    'auditoria.read'
)
WHERE p.tenant_id IS NULL AND p.codigo = 'coordenador'
ON CONFLICT (papel_id, permissao_id) DO NOTHING;

INSERT INTO papel_permissoes (papel_id, permissao_id)
SELECT p.id, pr.id
FROM papeis p
JOIN permissoes pr ON pr.codigo IN (
    'turma.read',
    'aluno.read',
    'presenca.read', 'presenca.write',
    'presenca.propria.read',
    'revisao.read', 'revisao.approve',
    'relatorio.read'
)
WHERE p.tenant_id IS NULL AND p.codigo = 'professor'
ON CONFLICT (papel_id, permissao_id) DO NOTHING;

INSERT INTO papel_permissoes (papel_id, permissao_id)
SELECT p.id, pr.id
FROM papeis p
JOIN permissoes pr ON pr.codigo IN (
    'presenca.propria.read', 'presenca.propria.write',
    'vinculo.solicitar.write',
    'pagamento.read', 'pagamento.write'
)
WHERE p.tenant_id IS NULL AND p.codigo = 'aluno'
ON CONFLICT (papel_id, permissao_id) DO NOTHING;

INSERT INTO papel_permissoes (papel_id, permissao_id)
SELECT p.id, pr.id
FROM papeis p
JOIN permissoes pr ON pr.codigo IN (
    'relatorio.read', 'relatorio.export',
    'presenca.read', 'usuario.read', 'aluno.read', 'professor.read',
    'auditoria.read', 'pagamento.read'
)
WHERE p.tenant_id IS NULL AND p.codigo = 'gestor'
ON CONFLICT (papel_id, permissao_id) DO NOTHING;

INSERT INTO papel_permissoes (papel_id, permissao_id)
SELECT p.id, pr.id
FROM papeis p
JOIN permissoes pr ON pr.codigo IN (
    'auditoria.read', 'usuario.read', 'turma.read', 'presenca.read', 'revisao.read', 'pagamento.read', 'relatorio.read'
)
WHERE p.tenant_id IS NULL AND p.codigo = 'auditor'
ON CONFLICT (papel_id, permissao_id) DO NOTHING;

INSERT INTO papel_permissoes (papel_id, permissao_id)
SELECT p.id, pr.id
FROM papeis p
JOIN permissoes pr ON pr.codigo IN (
    'usuario.read', 'aluno.read', 'professor.read', 'turma.read', 'presenca.read'
)
WHERE p.tenant_id IS NULL AND p.codigo = 'operador'
ON CONFLICT (papel_id, permissao_id) DO NOTHING;

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
    embedding BYTEA,
    decisao VARCHAR(20) NOT NULL DEFAULT 'pendente',
    pessoa_final_id INTEGER REFERENCES pessoas(id),
    decidido_por VARCHAR(200),
    decidido_em TIMESTAMPTZ,
    motivo VARCHAR(300)
);

-- Backfill de instalações existentes: embedding do rosto detectado na foto,
-- necessário para a atualização supervisionada de embeddings (item 03.5).
ALTER TABLE revisao_faces_itens ADD COLUMN IF NOT EXISTS embedding BYTEA;

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

-- Histórico de embeddings (item 03.5, ADR 0005 Decisão 2) — guarda o embedding
-- anterior sempre que uma revisão confirmada atualiza o embedding de uma
-- pessoa por blend ponderado. Permite auditoria/reversão manual.
CREATE TABLE IF NOT EXISTS pessoa_embeddings_historico (
    id SERIAL PRIMARY KEY,
    pessoa_id INTEGER NOT NULL REFERENCES pessoas(id),
    embedding_anterior BYTEA NOT NULL,
    revisao_id INTEGER REFERENCES revisoes_presenca_grupo(id),
    motivo VARCHAR(100) NOT NULL DEFAULT 'revisao_confirmada',
    substituido_em TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_pessoa_embeddings_historico_pessoa
    ON pessoa_embeddings_historico (pessoa_id, substituido_em DESC);

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
