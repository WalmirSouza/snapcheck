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
- Status: Concluído — 100%
- Skills recomendadas: [[dev-backend]]
- Depende de: 01.2
- Critério de aceite: Todas as tabelas de negócio em `init.sql` (pessoas, presenças, configurações, turmas/aulas se existirem) possuem `tenant_id NOT NULL` com FK para tabela `tenants`, e migração cobre dados existentes atribuindo-os a um tenant default.
- Retomada: Concluído em 2026-07-16 em `src/SnapCheck/Data/Scripts/init.sql`. Adicionadas tabelas `tenants` (com seed do tenant piloto `codigo_ativacao='PILOTO-MIGRACAO'`) e `tenant_chat_telegram` (chat_id único → tenant, suporte ao `/vincular`). `pessoas` e `presencas` ganharam `tenant_id` via `ALTER TABLE ADD COLUMN IF NOT EXISTS` + backfill (`presencas` herda o tenant da própria pessoa via JOIN) + `SET NOT NULL` + FK adicionada em bloco `DO $$ ... EXCEPTION WHEN duplicate_object` (idempotente, já que `init.sql` roda a cada start via `DatabaseInitializer.cs`). Índice único de nome trocou de global (`idx_pessoas_nome_ativo`) para `(tenant_id, LOWER(nome))` (`idx_pessoas_tenant_nome_ativo`); índice de presença virou `(tenant_id, pessoa_id, data_hora DESC)`. `configuracoes` ficou intencionalmente sem `tenant_id` (decisão do ADR 0001) — mantive a seed de `postgres_connection_string` porque `Program.cs` ainda lê essa chave como fallback; a remoção definitiva é do item 01.9, não deste. `dotnet build SnapCheck.sln` passou (só avisos pré-existentes de vulnerabilidade do ImageSharp). Próximo passo: 01.4 (middleware/contexto de resolução de tenant) com `dev-backend`, que agora tem `tenant_id` disponível para se apoiar; 01.9 (API key) pode ser feito em paralelo por não depender de 01.4.

---

### 01.4 — Middleware/contexto de resolução de tenant
- Prioridade: Alta
- Dificuldade: Média
- Status: Concluído — 100%
- Skills recomendadas: [[dev-backend]]
- Depende de: 01.2
- Critério de aceite: Existe um mecanismo central (middleware/`ITenantContext`) que resolve o tenant da requisição (token, subdomínio ou header, conforme decidido no ADR) e o disponibiliza para os repositórios sem precisar passar `tenant_id` manualmente em cada chamada.
- Retomada: Concluído em 2026-07-16. Investigação prévia (Explore) mostrou que não existe `IUpdateHandler`/DI scope por update — tudo é singleton e o roteamento acontece direto em `BotManager.HandleUpdateAsync`, com fotos desacopladas via `IMessageChannel` (fila in-memory) consumida depois por `PipelineService` em outro loop. Isso descartou `AsyncLocal` puro como suficiente (não atravessa a fila) — decisão: `ITenantContext` (AsyncLocal, `src/SnapCheck/Data/Tenancy/ITenantContext.cs`) cobre o trecho síncrono dentro do mesmo update (Cadastro/Consulta/Start), e `MensagemProcessamento.TenantId` (`required int`, `src/SnapCheck/Bot/Models/MensagemProcessamento.cs`) carrega o valor explicitamente através da fila para o pipeline. Criados: `ITenantRepository`/`TenantRepository` (resolve tenant por `chat_id` e implementa a vinculação — `src/SnapCheck/Data/Repositories/TenantRepository.cs`), `VincularHandler` (comando `/vincular CODIGO` — `src/SnapCheck/Bot/Handlers/VincularHandler.cs`). `BotManager.HandleUpdateAsync` foi reestruturado: resolve tenant por `chatId` antes de qualquer roteamento; chat não vinculado só pode rodar `/vincular`, qualquer outra coisa recebe aviso; roteamento antigo foi extraído para `ProcessarUpdateAsync` sem mudar o comportamento, só movido para dentro do `using tenantContext.BeginScope(...)`. `FotoHandler` agora injeta `ITenantContext` e preenche `TenantId` ao enfileirar. DI atualizado em `ServiceCollectionExtensions.cs`. `dotnet build SnapCheck.sln` passou limpo (só avisos pré-existentes do ImageSharp). Não há projeto de testes ainda para rodar (módulo 10). Próximo passo: 01.5 (repositórios `PessoaRepository`/`PresencaRepository` passam a exigir `tenantId` e a usar `ctx.Mensagem.TenantId`/`tenantContext.TenantId` nas chamadas) — é o item que efetivamente fecha o ciclo de isolamento; 01.9 (API key) pode ser feito em paralelo.

---

### 01.5 — Atualizar repositórios para filtrar por tenant
- Prioridade: Alta
- Dificuldade: Alta
- Status: Concluído — 100%
- Skills recomendadas: [[dev-backend]]
- Depende de: 01.3, 01.4
- Critério de aceite: `PessoaRepository.cs` e `PresencaRepository.cs` (e qualquer outro repositório) filtram por `tenant_id` em toda query — sem exceção. Nenhuma query nova pode ser escrita sem o filtro.
- Retomada: Concluído em 2026-07-16. **Achado crítico durante a implementação**: `CompararRostosEtapa` (em `DetectarRostosEtapa.cs`) chamava `pessoaRepository.ListarAtivasAsync` sem filtro nenhum — uma foto de qualquer tenant era comparada contra as pessoas cadastradas de **todos** os tenants (vazamento/match cruzado real, não só teórico). Corrigido usando `context.Mensagem.TenantId`. Todos os métodos de `IPessoaRepository`/`IPresencaRepository` que retornam ou gravam dado de negócio agora exigem `tenantId` como primeiro parâmetro (`ListarAtivasAsync`, `ObterPorNomeAsync`, `InserirAsync`, `RemoverPorNomeAsync`, `ListarSumidosAsync`, `RegistrarAsync`, `ListarPorPessoaAsync`), com filtro `tenant_id = @tenantId` na query (defesa em profundidade mesmo onde já havia JOIN com `pessoas` filtrada). **Exceção deliberada e documentada** (XML doc em ambas interfaces): `ContarAtivasAsync`/`ContarHojeAsync` permanecem agregados cross-tenant porque só alimentam a métrica operacional do `BotController` (que ainda não tem contexto de tenant — isso é o item 01.8) e não expõem nome/dado de nenhuma pessoa. Chamadores atualizados: `CadastroHandler`/`ConsultaHandler` passaram a injetar `ITenantContext` e ler `tenantContext.TenantId!.Value` (rodam dentro do escopo aberto por `BotManager`); `RegistrarPresencaEtapa`/`DetectarRostosEtapa` usam `context.Mensagem.TenantId` (rodam no pipeline, fora da cadeia async do update — AsyncLocal não serve aqui, decisão já tomada no item 01.4). `dotnet build SnapCheck.sln` passou limpo. Próximo passo: 01.6 (índices compostos — já cobertos em boa parte pelo `init.sql` do item 01.3, revisar com `EXPLAIN ANALYZE` quando houver dado real) ou 01.7 (testes de isolamento, hoje sem projeto de teste — depende do módulo 10); 01.9 (API key) segue independente.

---

### 01.6 — Índices compostos por tenant
- Prioridade: Média
- Dificuldade: Baixa
- Status: Concluído — 100%
- Skills recomendadas: [[dev-backend]], [[devops-sre]] (revisão de performance)
- Depende de: 01.3
- Critério de aceite: Índices compostos criados conforme definido no ADR 01.2 (`tenant_id, turma, aula, pessoa, data`), validados com `EXPLAIN ANALYZE` nas queries mais frequentes do pipeline.
- Retomada: Concluído em 2026-07-16, validado no Postgres real do `docker-compose` local (não só teoria). Reconstruí a imagem (`docker compose up -d --build snapcheck`) para aplicar o `init.sql` novo — log confirmou "Banco de dados inicializado com sucesso" e `\d` mostrou `tenants`, `tenant_chat_telegram`, `tenant_id`/FK/índices novos em `pessoas`/`presencas` exatamente como esperado. Rodei uma transação de teste (1000 pessoas + 3000 presenças sintéticas em 2 tenants, `ROLLBACK` no final para não deixar resíduo): `EXPLAIN ANALYZE` confirmou `Index Scan using idx_pessoas_tenant_nome_ativo` para `ObterPorNomeAsync` e `Index Scan using idx_presencas_tenant_pessoa_data` para `ListarPorPessoaAsync` — os índices são realmente usados pelo planner, não é só suposição. De brinde, validei o isolamento na prática: mesmo nome (`Joao Silva`) em dois tenants diferentes funcionou (2 linhas), e duplicar o mesmo nome dentro do mesmo tenant foi corretamente rejeitado pela constraint (`duplicate key value violates unique constraint "idx_pessoas_tenant_nome_ativo"`). Confirmei que o `ROLLBACK` não deixou dado nenhum (`SELECT COUNT(*) FROM pessoas` voltou a 0). Próximo passo: 01.8 (painel com contexto de tenant) ou 01.7 (segue bloqueado até existir projeto de teste no módulo 10).

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
- Status: Concluído — 100% (redefinido)
- Skills recomendadas: [[dev-frontend]]
- Depende de: 01.4
- Critério de aceite original: Painel web (`BotController.cs` e sua UI) reflete o tenant autenticado e nunca mistura configuração/métrica de tenants diferentes na mesma sessão.
- **Redefinição consciente (confirmada com o usuário em 2026-07-16)**: o critério original supunha um painel por tenant, mas o ADR 0001 e o item 01.9 já haviam decidido que o painel/`BotController` é um console da instância inteira, protegido só por API key (sem identidade de tenant em requisições HTTP — só o chat do Telegram tem isso, via `/vincular`). Construir seleção de tenant agora seria antecipar uma feature de admin multi-tenant que ninguém pediu. Escopo revisado: deixar explícito que é um painel de instância + indicador somente-leitura "tenants ativos". Painel por tenant de verdade fica para quando o RBAC (módulo 04) ou um portal do cliente existir.
- Retomada: Concluído em 2026-07-16. Adicionado `ITenantRepository.ContarAtivosAsync` (`SELECT COUNT(*) FROM tenants WHERE status='ativo'`), exposto em `StatusResponse.TotalTenants` via `BotController`, e card "Tenants ativos" no painel (`Pages/Index.cshtml`) com um aviso no cabeçalho deixando explícito que é um console de instância compartilhada. Validado ponta a ponta no `docker-compose` real: adicionei `Admin__ApiKey: dev-local-only` ao `docker-compose.yml` (só ambiente local, mesmo padrão das credenciais de Postgres já hardcoded ali) e reconstruí o container — `curl` sem header → `401`; com `X-Admin-Api-Key` → `200` retornando `{"totalTenants":1,"bancoConectado":true,...}` (o tenant piloto criado pela migração do item 01.3). Módulo 01 fica em 8/9 itens (89%). Único item restante é **01.7 — testes de isolamento**, que está genuinamente bloqueado: não existe projeto de teste no repositório (isso é o módulo 10, ainda não iniciado). Próximo passo natural: iniciar módulo 10 (item 10.1/10.3, criar o projeto de teste) para poder fechar 01.7, ou seguir para outro módulo (02 é o próximo mais lógico, já que 01 está praticamente fechado).

---

### 01.9 — Autenticação mínima (API key) para rotas administrativas
- Prioridade: Alta
- Dificuldade: Baixa
- Status: Concluído — 100%
- Skills recomendadas: [[dev-backend]]
- Depende de: 01.2 (ADR 0001)
- Item descoberto durante o ADR 01.2 — não estava no desenho original do módulo, virou pré-requisito bloqueante ao constatar que `BotController.cs` não tem nenhuma autenticação hoje.
- Critério de aceite: Middleware exige header `X-Admin-Api-Key` em toda rota de `BotController` (iniciar/parar bot, configurações), validado contra segredo de ambiente (não em `configuracoes`/banco). Rotas do fluxo do bot no Telegram (`/vincular`, cadastro, consulta) não usam essa API key. Explicitamente marcado como solução temporária, a ser substituída (não estendida) pelo RBAC completo do módulo 04.
- Retomada: Concluído em 2026-07-16. Criado `AdminApiKeyMiddleware` (`src/SnapCheck/Security/AdminApiKeyMiddleware.cs`), fail-closed: sem `Admin:ApiKey` configurado retorna 503 (não deixa passar); com chave configurada e header `X-Admin-Api-Key` ausente/errado retorna 401. Registrado em `Program.cs` logo após `UseRouting()`, antes de `MapControllers()`. `appsettings.json` ganhou `Admin:ApiKey` vazio (prod fica bloqueado até configurar via ambiente/secret); `appsettings.Development.json` ganhou `dev-local-only` só para não travar o `dotnet run` local. **Efeito colateral necessário**: o painel web (`Pages/Index.cshtml`) fazia `fetch` direto nos endpoins — sem ajuste, o middleware quebraria o painel inteiro. Adicionei `apiFetch()` no JS da página, que pede a chave uma vez via `prompt()`, guarda em `localStorage` e anexa o header em toda chamada; em 401 limpa o storage e avisa para recarregar. Validado com smoke test real: subi a aplicação (`dotnet run`), `curl` sem header → `401`; `curl -H "X-Admin-Api-Key: dev-local-only"` → `200`. Processo de teste encerrado depois. Módulo 01 fica em 6/9 itens (67%) — restam 01.6 (índices, baixa dificuldade, pode ser rápido), 01.7 (testes de isolamento — bloqueado até o módulo 10 existir um projeto de teste) e 01.8 (painel com contexto de tenant, dev-frontend).
