# ADR 0001 — Isolamento Multi-Tenant e Autenticação Mínima

- Status: Aceito
- Data: 2026-07-16
- Entrada: `docs/requisitos/multi-tenant.md` (item de backlog 01.1)
- Item de backlog: 01.2

## Contexto

Levantamento técnico (via Explore) confirmou:
- Schema atual (`init.sql`): 3 tabelas (`pessoas`, `presencas`, `configuracoes`), sem `tenant_id`. `pessoas` tem índice único em `LOWER(nome) WHERE ativo` — global.
- **Toda a composição de serviços é `Singleton`** (`ServiceCollectionExtensions.cs`): `IDbConnectionFactory`, repositórios, `IBotManager`, `IBotClientProvider`. `DbConnectionFactory` guarda a connection string como campo mutável de instância, trocável em runtime via `SetConnectionString` — hoje chamado a partir de `BotController` (`POST /api/bot/configuracoes`).
- `IBotClientProvider` guarda um único `TelegramBotClient` (um bot ativo por processo); `IBotManager` guarda um único `_cts` (um "iniciar/parar" por processo).
- **Não existe nenhum pacote de Identity/JWT/Authentication** no `.csproj`, nenhum `AddAuthentication`/`UseAuthorization` no `Program.cs`, nenhuma seção de auth em `appsettings.json`. `BotController` está totalmente aberto.
- Decisão de produto já confirmada (`docs/requisitos/multi-tenant.md`): instância única compartilhada, resolução de tenant no Telegram via código de vinculação, nome único por `(tenant_id, nome)`, dados atuais migram para tenant piloto.

## Decisão 1 — Estratégia de isolamento de dados

**Escolhida: coluna `tenant_id` em todas as tabelas de negócio + filtro obrigatório na aplicação, com Row-Level Security (RLS) do PostgreSQL como camada de defesa adicional.**

### Alternativas consideradas

| Opção | Prós | Contras | Veredito |
|---|---|---|---|
| **A. `tenant_id` + RLS (escolhida)** | Um schema só, migração simples, RLS barra vazamento mesmo se uma query esquecer o filtro | Exige disciplina em toda query Dapper + configurar sessão RLS por conexão | Melhor equilíbrio para o volume alvo (10k fotos/dia) e para o estágio atual do produto |
| B. Schema por tenant | Isolamento forte, fácil de entender | Migração de schema por cliente novo é operacionalmente cara; não combina com "instância única compartilhada" já decidido | Rejeitada — o ganho de isolamento não compensa a complexidade operacional dado que já decidimos instância única |
| C. Banco por tenant | É essencialmente o que já existe hoje de fato | Contradiz a decisão de produto de consolidar em instância única; não escala para centenas de clientes pequenos | Rejeitada — já descartada na decisão de produto (docs/requisitos/multi-tenant.md) |

### Consequências técnicas
- Novas tabelas: `tenants` (id, nome, codigo_ativacao, status, criado_em) e `tenant_chat_telegram` (tenant_id FK, chat_id, vinculado_em) — usada pelo comando `/vincular CODIGO`.
- `pessoas`, `presencas` recebem `tenant_id NOT NULL` com FK para `tenants`. Índice único de `pessoas` muda de `LOWER(nome) WHERE ativo` para `(tenant_id, LOWER(nome)) WHERE ativo`.
- Índices compostos: `(tenant_id, pessoa_id, data_hora)` em `presencas` como base para os módulos 02 e 07.
- **`configuracoes` continua global, sem `tenant_id`** — guarda infraestrutura do processo inteiro (token único do bot compartilhado, connection string única do banco compartilhado), não configuração de negócio por cliente. Configuração de negócio por tenant (regras de presença, janela de aula etc.) é uma tabela nova, escopo do módulo 02, não deste ADR.
- RLS: cada policy filtra por uma variável de sessão (`current_setting('app.tenant_id')`); toda conexão aberta pelo `DbConnectionFactory` deve executar `SET app.tenant_id = @tenantId` logo após abrir, antes de qualquer query de negócio.
- **Mudança obrigatória de lifetime de DI**: `DbConnectionFactory` deixa de expor `SetConnectionString` como mutação livre em runtime. Com instância única compartilhada, a connection string do Postgres é fixa (vem de `appsettings`/secret, definida no deploy), não trocável via `BotController`. O endpoint `POST /api/bot/configuracoes` deixa de aceitar `postgres_connection_string` — isso deixa de fazer sentido no novo modelo e também elimina uma condição de corrida real que existe hoje (campo mutável em singleton lido concorrentemente).
- Repositórios (`PessoaRepository`, `PresencaRepository`) passam a exigir `tenantId` explícito em todo método público — é a segunda camada de defesa, RLS é a terceira, não substituem uma à outra.

## Decisão 2 — Autenticação mínima para rotas administrativas

**Escolhida: API key simples por enquanto (stopgap), não OAuth/Identity completo.**

### Justificativa
Não existe nenhuma infraestrutura de auth no projeto hoje (achado crítico do levantamento técnico). Construir RBAC completo com login é escopo do módulo 04 (item 04.2/04.3) e não deve ser antecipado aqui — mas deixar `BotController` sem nenhuma autenticação com múltiplos tenants reais na mesma instância é um risco de segurança inaceitável mesmo em uma primeira versão. A solução mínima:
- Middleware exige header `X-Admin-Api-Key` em toda rota de `BotController` (iniciar/parar bot, configurações), validado contra um segredo único definido em configuração de ambiente (não em `configuracoes`/banco).
- Isso é deliberadamente temporário — quando o módulo 04 implementar RBAC completo, este middleware é substituído, não estendido.
- O fluxo do bot no Telegram (`/vincular CODIGO`, cadastro, consulta) **não** usa essa API key — a identidade ali é resolvida pela tabela `tenant_chat_telegram`, população que só acontece após vinculação válida.

### Alternativa rejeitada
OAuth/Identity completo agora — descartado por escopo: exigiria adicionar pacotes, telas de login e não tem ganho real neste momento (só um punhado de rotas administrativas internas), além de duplicar trabalho que o módulo 04 já vai fazer de forma completa.

## Itens de backlog impactados
- Novo item necessário no módulo 01: implementar a API key mínima (ver item 01.9 adicionado abaixo).
- 01.3, 01.4, 01.5, 01.6 (schema, contexto de tenant, repositórios, índices) agora têm entrada clara para começar a implementação.
- Módulo 04 (RBAC) deve tratar este middleware como descartável, não como base a estender.
