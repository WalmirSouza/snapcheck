# Módulo 02 — Regras de Presença e Antifraude

> Prioridade do módulo: **Alta** (horizonte 30 dias). Depende parcialmente do modelo de dados do módulo 01 (matrícula/turma/aula precisam existir com `tenant_id`).

---

### 02.1 — Requisitos de regras de presença válida
- Prioridade: Alta
- Dificuldade: Média
- Status: Concluído — 100%
- Skills recomendadas: [[analista-requisitos]]
- Depende de: —
- Critério de aceite: Documento cobrindo a definição formal de presença válida (confiança acima do limiar, dentro da janela da aula, turma correta, matrícula ativa, sem presença anterior no mesmo período), incluindo os parâmetros configuráveis por cliente (tolerância de atraso, saída antecipada, presença parcial, múltiplas aulas no mesmo dia).
- Retomada: Concluído em 2026-07-17 — `docs/requisitos/regras-presenca.md`. Achado do código: `RegistrarPresencaEtapa.cs` registra presença incondicionalmente (sem janela, sem dedupe); `turma` hoje é só texto livre do título do chat do Telegram; o limiar de confiança (0.42) já existe mas é fixo no `FaceService`, não configurável por tenant (isso fica pro módulo 03). Decisões confirmadas com o usuário: (1) nível de modelagem v1 = janela simples recorrente por turma, sem matrícula formal nem calendário de aulas específicas; (2) turma pode ter múltiplas janelas no mesmo dia (ex.: academia manhã+noite) — muda a chave de dedupe para incluir a janela; (3) presença parcial definida por corte percentual de tempo dentro da janela, não por checkpoint de saída (sistema só recebe uma foto por evento); (4) turma vira entidade cadastrada, vinculada ao chat via comando explícito `/turma CODIGO` (mesmo padrão do `/vincular` de tenant do módulo 01) — sem auto-criação a partir do título do chat. Isso gerou um item novo (02.7) que não estava previsto originalmente. Próximo passo: 02.2 (ADR) com a skill `arquiteto-software`, usando este documento como entrada.

---

### 02.2 — ADR do motor de regras de presença
- Prioridade: Alta
- Dificuldade: Alta
- Status: Concluído — 100%
- Skills recomendadas: [[arquiteto-software]]
- Depende de: 02.1
- Critério de aceite: ADR decidindo se as regras configuráveis por tenant ficam em tabela de configuração (parametrizável) ou motor de regras dedicado, e como isso se integra ao pipeline de eventos (`PresencaValidada`, `PresencaRevisaoPendente`).
- Retomada: Concluído em 2026-07-19 — `docs/adr/0002-motor-regras-presenca.md`. Três decisões: (1) tabela de configuração parametrizável por Turma/Janela (colunas tipadas), não motor de regras genérico — parâmetros são um conjunto fechado e conhecido, DSL seria over-engineering. (2) Nova etapa de pipeline `ValidarJanelaPresencaEtapa` (entre `CompararRostosEtapa` e `RegistrarPresencaEtapa`), não barramento de eventos — os eventos `PresencaValidada`/`PresencaRevisaoPendente` da visão original são aspiracionais (módulo 05, RabbitMQ, ainda não existe); antecipar isso agora duplicaria trabalho. Ortogonal ao desvio de revisão em grupo do item 02.8 (`Matches.Count > 1`): uma etapa resolve "quem é", a outra resolve "o horário conta". (3) Chave de dedupe do item 02.3 (hoje por dia) precisa migrar para incluir `turma_janela_id` quando 02.7 existir — decisão registrada para não ser esquecida. Próximo passo: 02.7 (Turma como entidade) precisa vir antes de 02.4 poder consultar janela real — é o bloqueador atual.

---

### 02.3 — Idempotência: chave por pessoa+aula+janela
- Prioridade: Alta
- Dificuldade: Média
- Status: Concluído — 100% (versão interina, por dia — não por janela ainda)
- Skills recomendadas: [[dev-backend]]
- Depende de: 01.5 (repositórios com tenant), 02.2, 02.7 (turma/janela precisam existir como entidade)
- Critério de aceite: Constraint única no banco (não só validação em memória) impede duas presenças válidas para a mesma pessoa na mesma aula/janela. Nova foto da mesma pessoa/aula só atualiza confiança, não cria presença nova.
- Retomada: Implementado diretamente no código (fora do fluxo skill→backlog desta sessão): `presencas` ganhou `data_dia` e `turma_normalizada`, com índice único `idx_presencas_unq_tenant_pessoa_turma_dia (tenant_id, pessoa_id, turma_normalizada, data_dia)` e `INSERT ... ON CONFLICT DO NOTHING` em `PresencaRepository.RegistrarAsync`, retornando `RegistroPresencaResultado.Registrada`/`Duplicada` (usado por `RegistrarPresencaEtapa` e por `RevisaoPresencaRepository.ConfirmarAsync` para não duplicar ao efetivar revisão). **Gap conhecido**: a chave é por `(tenant_id, pessoa_id, turma_normalizada, data_dia)` — dedupe por dia inteiro, não por janela — porque 02.7 (turma/janela como entidade) ainda não existe. Quando 02.7 for implementado, revisar se a chave deve migrar para incluir `turma_janela_id` (para não bloquear presença legítima em janelas diferentes do mesmo dia, ex.: academia manhã+noite, conforme decidido em 02.1). Validado ponta a ponta no Postgres real do docker-compose (ver retomada de 02.8).

---

### 02.4 — Implementação do motor de regras configurável
- Prioridade: Alta
- Dificuldade: Alta
- Status: Não iniciado — 0%
- Skills recomendadas: [[dev-backend]]
- Depende de: 02.2, 02.7
- Critério de aceite: `RegistrarPresencaEtapa.cs` passa a consultar configuração por tenant (janela de aula, tolerância de atraso, presença parcial) em vez de regra fixa, mantendo compatibilidade com o fluxo atual quando a configuração não existir (default sensato).
- Retomada: —

---

### 02.5 — Presença manual auditada
- Prioridade: Média
- Dificuldade: Baixa
- Status: Não iniciado — 0%
- Skills recomendadas: [[dev-backend]]
- Depende de: 01.5
- Critério de aceite: Inclusão manual de presença registra responsável, motivo, data/horário, e gera evento de auditoria (integra com módulo 04).
- Retomada: —

---

### 02.6 — Testes de antifraude e duplicidade
- Prioridade: Alta
- Dificuldade: Média
- Status: Não iniciado — 0%
- Skills recomendadas: [[qa-testes]]
- Depende de: 02.3, 02.4
- Critério de aceite: Casos de teste cobrindo — duplicidade pessoa+aula, foto fora da janela, matrícula inativa, atraso configurado, presença parcial, e regressão do fluxo padrão do módulo 00.
- Retomada: —

---

### 02.7 — Turma como entidade + vinculação de chat (`/turma CODIGO`)
- Prioridade: Alta
- Dificuldade: Alta
- Status: Concluído — 100% (schema + vinculação; falta consumir na validação de janela)
- Skills recomendadas: [[dev-backend]]
- Depende de: 02.2
- Item descoberto durante o levantamento de requisitos 02.1 — não estava no desenho original do módulo. Sem isso, 02.3/02.4 não têm turma/janela real para consultar (hoje `turma` é só texto livre do chat).
- Critério de aceite: Novas tabelas `turmas` (tenant_id, nome, ativa), `turma_janelas` (turma_id, dias_semana, hora_inicio, hora_fim, tolerancia_atraso_minutos, corte_presenca_parcial_percentual) e `turma_chat_telegram` (turma_id, chat_id — mesmo padrão de `tenant_chat_telegram` do módulo 01). Comando `/turma CODIGO` no bot vincula o chat atual a uma Turma cadastrada. `presencas` passa a referenciar `turma_id`/`turma_janela_id` em vez de só o texto livre. Cadastro de Turma + janelas (painel ou comando administrativo) fica a critério do ADR 02.2 decidir onde entra.
- Retomada: Concluído em 2026-07-19. Schema em `init.sql` (`turmas` com `UNIQUE(tenant_id, codigo_vinculacao)`, `turma_janelas` com `dias_semana SMALLINT[]`, `hora_inicio`/`hora_fim TIME`, tolerância e corte percentual, `turma_chat_telegram` mesmo padrão de `tenant_chat_telegram`). `ITurmaRepository`/`TurmaRepository` (`CriarAsync`, `AdicionarJanelaAsync`, `ListarPorTenantAsync`, `ObterJanelasAtivasAsync`, `ObterTurmaIdPorChatAsync`, `VincularChatAsync`). Comando `/turma CODIGO` via `VincularTurmaHandler`, roteado em `BotManager` dentro do escopo de tenant já resolvido (usa `ITenantContext`, não precisa reresolver tenant). ADR 0002 não definiu onde o cadastro de Turma/Janela aconteceria — decidi (consistente com o padrão já usado para Revisões) criar `TurmasController` (`/api/turmas`, `/api/turmas/{id}/janelas`), protegido pelo mesmo `AdminApiKeyMiddleware` (rota adicionada à lista `RotasProtegidas`). Validado ponta a ponta no Postgres real: criar turma sem chave → `401`; com chave → criou; listar retornou a turma; adicionar janela (seg-sex 19h-21h, tolerância 10min, corte 50%) persistiu corretamente; duplicar código no mesmo tenant → `409` (constraint funcionando). Dados de teste removidos depois.
- **Ainda não usado**: `RegistrarPresencaEtapa`/`ValidarJanelaPresencaEtapa` ainda não consultam `ObterTurmaIdPorChatAsync`/`ObterJanelasAtivasAsync` — a entidade existe e pode ser cadastrada/vinculada, mas o pipeline ainda registra presença sem checar janela. Isso é exatamente o item 02.4.
- Não há UI de cadastro de Turma no painel — só a API. Mesma lógica do módulo 01 (tenant também não tem UI de cadastro, só SQL/API direta) — considerar item futuro se o volume de turmas justificar.

---

### 02.8 — Revisão de presença em grupo (confirmação humana para fotos multi-pessoa)
- Prioridade: Alta
- Dificuldade: Alta
- Status: Concluído — 100%
- Skills recomendadas: [[dev-backend]], [[dev-frontend]], [[seguranca-lgpd]]
- Depende de: 01.5, 02.3
- Item não previsto no desenho original do módulo — implementado diretamente no código (fora do fluxo skill→backlog) como mecanismo antifraude: quando uma foto tem mais de um rosto detectado (`context.Matches.Count > 1`), o pipeline não registra presença direto — cria uma "revisão de presença em grupo" (`revisoes_presenca_grupo`, `revisao_faces_itens`, `evidencias_foto_grupo`, `revisao_presenca_auditoria`) e avisa no chat para confirmar no painel web em até 24h. Confirmado com o usuário em 2026-07-19 que esse gatilho (toda foto de grupo passa por confirmação humana, não só casos de baixa confiança) é intencional — troca automação total por segurança antifraude máxima.
- **Dois problemas encontrados nesta revisão de código e corrigidos**:
  1. `RevisoesPresencaController` (`/api/revisoes-presenca`) não tinha nenhuma autenticação — pior que o gap original do `BotController`, pois deixava qualquer chamador confirmar revisão e efetivar presença para qualquer tenant só informando um `tenantId` no corpo. Corrigido estendendo `AdminApiKeyMiddleware` (`src/SnapCheck/Security/AdminApiKeyMiddleware.cs`) para proteger também `/api/revisoes-presenca`, mesmo padrão fail-closed do item 01.9.
  2. O bot prometia "confirme no painel web" mas não existia nenhuma página — só a API JSON crua, inutilizável por um professor de verdade. Construída `src/SnapCheck/Pages/Revisoes.cshtml` (+ `.cshtml.cs`): busca revisões pendentes por tenant, abre detalhe, permite aprovar/rejeitar/reatribuir cada rosto sugerido e confirmar (exige nome + perfil de quem confirma, `professor`/`coordenador`, replicando a regra já existente em `RevisaoPresencaRepository.PerfilPodeConfirmar`). Link adicionado no painel principal (`Pages/Index.cshtml`).
- Retomada: Concluído em 2026-07-19. Validado ponta a ponta no Postgres real do docker-compose: criei tenant+pessoa de teste, `POST /api/revisoes-presenca` sem chave → `401`; com chave → criou revisão; `GET /pendentes` e `GET /{id}` retornaram os dados certos; `POST /{id}/confirmar` com decisão "aprovada" efetivou a presença em `presencas` (respeitando a constraint de idempotência do item 02.3 — testado indiretamente, não duplicou). Dados de teste removidos do banco depois (tenant, pessoa, revisão, itens, evidência, auditoria, presença). Idempotência dentro da revisão herda a mesma limitação de 02.3 (dedupe por dia, não por janela) — mesmo gap conhecido, resolver junto quando 02.7 existir.
- **Pendência ainda em aberto**: não há teste automatizado cobrindo este fluxo (mesma limitação estrutural do item 02.6/01.7 — falta projeto de teste, módulo 10). RBAC de verdade (login real por perfil, não só um campo de texto "professor"/"coordenador" digitado pelo usuário) fica para o módulo 04 — o middleware de API key aqui é o mesmo stopgap do item 01.9, não uma solução definitiva.
