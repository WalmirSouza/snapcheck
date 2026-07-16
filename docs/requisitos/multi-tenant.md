# Requisitos — Multi-Tenant (item de backlog 01.1)

## Contexto (estado atual verificado no código)

Levantamento feito em `init.sql`, `PessoaRepository.cs`, `PresencaRepository.cs`, `ConfiguracaoRepository.cs`, `BotController.cs`, `CadastroHandler.cs`, `ConsultaHandler.cs`:

- **Schema hoje**: 3 tabelas — `pessoas` (id, nome, embedding, data_cadastro, ativo), `presencas` (id, pessoa_id FK, data_hora, turma como texto livre, não FK), `configuracoes` (chave/valor singleton global). Nenhuma coluna ou tabela de tenant/cliente/organização existe.
- **Achado crítico #1**: `pessoas` tem índice único parcial em `LOWER(nome) WHERE ativo = TRUE` — nome de pessoa ativa é **único globalmente**. Isso quebra no primeiro cliente que cadastrar alguém com nome igual ao de outro cliente.
- **Achado crítico #2**: `configuracoes` guarda `telegram_token` e `postgres_connection_string` como valores únicos globais, setados via `POST /api/bot/configuracoes` e aplicados a um `DbConnectionFactory` singleton mutável em runtime. Isso indica que a arquitetura atual já pressupõe **uma implantação inteira por cliente** (um bot, um banco, uma connection string) — não é um sistema single-instance servindo vários tenants hoje.
- **Achado crítico #3 (bloqueador de segurança, não só de multi-tenant)**: não existe nenhuma autenticação/autorização em `BotController.cs` (sem `[Authorize]`, sem API key, sem nada). Qualquer chamador pode trocar token e connection string ou iniciar/parar o bot. Multi-tenant sem autenticação não isola nada — é pré-requisito, não opcional.
- **Achado crítico #4**: o estado de conversa do cadastro é indexado por `chatId` do Telegram (`ConcurrentDictionary<long, CadastroState>`), sem qualquer contexto de tenant associado ao chat.

## Regras de negócio explícitas (a confirmar)
- Uma pessoa só pode ter presença registrada dentro do escopo do seu próprio tenant.
- Nome de pessoa deixa de ser único globalmente e passa a ser único apenas dentro do tenant.
- Toda configuração (token, connection string ou equivalente) passa a ser por tenant, nunca global.
- Toda rota administrativa (trocar configuração, iniciar/parar bot) exige autenticação e sabe identificar a qual tenant a chamada pertence.

## User stories (rascunho — depende das respostas às perguntas em aberto abaixo)
- Como Super Admin, quero cadastrar um novo tenant (cliente) para poder provisionar seu acesso ao SnapCheck.
- Como Administrador da instituição, quero que meus dados (pessoas, presenças, configurações) nunca sejam visíveis para outro tenant.
- Como pessoa cadastrada, quero poder ter o mesmo nome que uma pessoa de outra instituição sem conflito.

## Decisões de produto (confirmadas pelo usuário em 2026-07-16)

1. **Modelo de implantação**: instância única compartilhada. Uma implantação atende todos os clientes; isolamento é lógico via `tenant_id`, não físico por banco/bot.
2. **Resolução de tenant no bot do Telegram**: código de vinculação. Cada tenant recebe um código/token de ativação único; o administrador do cliente vincula o chat/grupo ao tenant uma única vez (ex.: comando `/vincular CODIGO`). A partir daí o `chatId` fica mapeado ao `tenant_id` em uma tabela de vínculo — um único bot do Telegram atende todos os tenants.
3. **Unicidade de nome**: passa a ser único por tenant, não mais globalmente. Índice único parcial de `pessoas` deve mudar de `LOWER(nome) WHERE ativo` para `(tenant_id, LOWER(nome)) WHERE ativo`.
4. **Migração de dados existentes**: os dados atuais do banco (pessoas/presenças já cadastradas) viram o tenant piloto/cliente 0 — não são descartados.

## Requisitos derivados destas decisões
- Nova tabela `tenants` (id, nome, código de ativação, status, data de criação).
- Nova tabela de vínculo `tenant_chat_telegram` (tenant_id, chat_id, data de vinculação) para resolver tenant a partir do `chatId` recebido pelo bot.
- Comando de bot `/vincular CODIGO` que, dado um código de ativação válido e ainda não usado, associa o `chatId` atual ao tenant correspondente.
- Toda operação do bot (cadastro, consulta, configuração) passa a exigir um `chatId` já vinculado a um tenant; chat não vinculado deve receber instrução para rodar `/vincular` em vez de ser atendido.
- **Autenticação passa a ser pré-requisito bloqueante deste módulo** (achado crítico #3): rotas administrativas de `BotController.cs` (configurações, iniciar/parar bot) não podem continuar sem nenhuma autenticação quando servirem múltiplos tenants simultaneamente — decisão de mecanismo (API key, login, OAuth) fica para o ADR 01.2, mas a exigência em si não é opcional.
- Migração de dados: script que cria o tenant piloto e associa todas as linhas existentes de `pessoas`/`presencas`/`configuracoes` a ele.

## Fora de escopo (deste item)
- RBAC detalhado por perfil (fica no módulo 04, item 04.2).
- Mecanismo específico de autenticação (API key vs. login vs. OAuth) — decisão do arquiteto-software no ADR 01.2.
- Geração/distribuição do código de ativação do tenant (fluxo comercial/onboarding) — a definir quando o módulo de Billing/onboarding for priorizado.

## Status
Concluído — 100%. Decisões de produto confirmadas em 2026-07-16. Pronto para virar entrada do ADR 01.2 (arquiteto-software).
