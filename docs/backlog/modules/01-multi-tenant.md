# Módulo 01 — Multi-Tenant e Fundação de Dados

> Prioridade do módulo: **Alta** (horizonte 30 dias). Bloqueia praticamente todos os outros módulos — schema sem `tenant_id` contamina qualquer feature construída em cima dele.

---

### 01.1 — Requisitos de multi-tenant
- Prioridade: Alta
- Dificuldade: Média
- Status: Concluído — 100%
- Skills recomendadas: [[analista-requisitos]]
- Depende de: —
- Critério de aceite: Documento em `docs/requisitos/multi-tenant.md` cobrindo — modelo de isolamento (coluna `tenant_id` + RLS vs. schema por tenant vs. banco por tenant), como o tenant é resolvido em cada canal de entrada (upload web, API, futuro mobile/WhatsApp), o que acontece com dados já existentes (migração de dados legados para um tenant "default").
- Retomada: Concluído em 2026-07-16. Investigação de código (Explore) achou 4 pontos críticos: (1) `pessoas.nome` único globalmente hoje, (2) `configuracoes` é singleton global (token/connection string), sugerindo que hoje cada cliente já roda implantação própria, (3) **`BotController.cs` não tem nenhuma autenticação** — bloqueador de segurança, não só de multi-tenant, (4) estado de cadastro é indexado só por `chatId` do Telegram sem tenant. Decisões confirmadas com o usuário: instância única compartilhada (isolamento lógico via `tenant_id`), resolução de tenant no bot via código de vinculação (`/vincular CODIGO` + tabela `tenant_chat_telegram`), nome único por `(tenant_id, nome)`, dados atuais migram para tenant piloto/cliente 0. Documento completo com requisitos derivados em `docs/requisitos/multi-tenant.md`. Próximo passo: iniciar item 01.2 (ADR de arquitetura) com a skill `arquiteto-software`, usando este documento como entrada — incluir a decisão de autenticação (mecanismo ainda em aberto) como parte do ADR.

---

### 01.2 — ADR de estratégia de isolamento multi-tenant
- Prioridade: Alta
- Dificuldade: Alta
- Status: Concluído — 100%
- Skills recomendadas: [[arquiteto-software]]
- Depende de: 01.1
- Critério de aceite: ADR em `docs/adr/` decidindo estratégia (recomendação: coluna `tenant_id` + Row Level Security no Postgres, mais simples de operar em monólito modular do que schema-per-tenant) e definindo o padrão de índice composto (`tenant_id, turma_id, aula_id, pessoa_id, data`).
- Retomada: Concluído em 2026-07-16 — `docs/adr/0001-multi-tenant-isolamento.md`. Levantamento técnico (Explore) achou que toda a DI é `Singleton` com connection string mutável em runtime (`DbConnectionFactory.SetConnectionString`, hoje chamado por `BotController`) e nenhum pacote/middleware de auth existe. Decisões do ADR: (1) `tenant_id` + filtro obrigatório na aplicação + RLS do Postgres como defesa adicional (não schema/banco por tenant); `configuracoes` continua global (infra do processo), só `pessoas`/`presencas` ganham `tenant_id`; `DbConnectionFactory` para de expor `SetConnectionString` via HTTP — connection string fixa por config/deploy. (2) Autenticação mínima via API key (header `X-Admin-Api-Key`) só para rotas administrativas do `BotController`, explicitamente temporária até o RBAC completo do módulo 04. Isso gerou o item novo 01.9 abaixo. Próximo passo: iniciar 01.3 (schema) e 01.9 (API key) com a skill `dev-backend` — 01.3 primeiro, pois 01.5 e outros dependem dele.

---

### 01.3 — Adicionar `tenant_id` ao schema (init.sql)
- Prioridade: Alta
- Dificuldade: Alta
- Status: Não iniciado — 0%
- Skills recomendadas: [[dev-backend]]
- Depende de: 01.2
- Critério de aceite: Todas as tabelas de negócio em `init.sql` (pessoas, presenças, configurações, turmas/aulas se existirem) possuem `tenant_id NOT NULL` com FK para tabela `tenants`, e migração cobre dados existentes atribuindo-os a um tenant default.
- Retomada: —

---

### 01.4 — Middleware/contexto de resolução de tenant
- Prioridade: Alta
- Dificuldade: Média
- Status: Não iniciado — 0%
- Skills recomendadas: [[dev-backend]]
- Depende de: 01.2
- Critério de aceite: Existe um mecanismo central (middleware/`ITenantContext`) que resolve o tenant da requisição (token, subdomínio ou header, conforme decidido no ADR) e o disponibiliza para os repositórios sem precisar passar `tenant_id` manualmente em cada chamada.
- Retomada: —

---

### 01.5 — Atualizar repositórios para filtrar por tenant
- Prioridade: Alta
- Dificuldade: Alta
- Status: Não iniciado — 0%
- Skills recomendadas: [[dev-backend]]
- Depende de: 01.3, 01.4
- Critério de aceite: `PessoaRepository.cs` e `PresencaRepository.cs` (e qualquer outro repositório) filtram por `tenant_id` em toda query — sem exceção. Nenhuma query nova pode ser escrita sem o filtro.
- Retomada: —

---

### 01.6 — Índices compostos por tenant
- Prioridade: Média
- Dificuldade: Baixa
- Status: Não iniciado — 0%
- Skills recomendadas: [[dev-backend]], [[devops-sre]] (revisão de performance)
- Depende de: 01.3
- Critério de aceite: Índices compostos criados conforme definido no ADR 01.2 (`tenant_id, turma, aula, pessoa, data`), validados com `EXPLAIN ANALYZE` nas queries mais frequentes do pipeline.
- Retomada: —

---

### 01.7 — Testes de isolamento entre tenants
- Prioridade: Alta
- Dificuldade: Média
- Status: Não iniciado — 0%
- Skills recomendadas: [[qa-testes]]
- Depende de: 01.5
- Critério de aceite: Teste automatizado prova que uma consulta feita no contexto do Tenant A nunca retorna dado do Tenant B, mesmo em cenários de erro/exceção no meio do pipeline.
- Retomada: —

---

### 01.8 — Painel: contexto de tenant no admin
- Prioridade: Média
- Dificuldade: Média
- Status: Não iniciado — 0%
- Skills recomendadas: [[dev-frontend]]
- Depende de: 01.4
- Critério de aceite: Painel web (`BotController.cs` e sua UI) reflete o tenant autenticado e nunca mistura configuração/métrica de tenants diferentes na mesma sessão.
- Retomada: —

---

### 01.9 — Autenticação mínima (API key) para rotas administrativas
- Prioridade: Alta
- Dificuldade: Baixa
- Status: Não iniciado — 0%
- Skills recomendadas: [[dev-backend]]
- Depende de: 01.2 (ADR 0001)
- Item descoberto durante o ADR 01.2 — não estava no desenho original do módulo, virou pré-requisito bloqueante ao constatar que `BotController.cs` não tem nenhuma autenticação hoje.
- Critério de aceite: Middleware exige header `X-Admin-Api-Key` em toda rota de `BotController` (iniciar/parar bot, configurações), validado contra segredo de ambiente (não em `configuracoes`/banco). Rotas do fluxo do bot no Telegram (`/vincular`, cadastro, consulta) não usam essa API key. Explicitamente marcado como solução temporária, a ser substituída (não estendida) pelo RBAC completo do módulo 04.
- Retomada: —
