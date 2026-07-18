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
- Status: Não iniciado — 0%
- Skills recomendadas: [[arquiteto-software]]
- Depende de: 02.1
- Critério de aceite: ADR decidindo se as regras configuráveis por tenant ficam em tabela de configuração (parametrizável) ou motor de regras dedicado, e como isso se integra ao pipeline de eventos (`PresencaValidada`, `PresencaRevisaoPendente`).
- Retomada: —

---

### 02.3 — Idempotência: chave por pessoa+aula+janela
- Prioridade: Alta
- Dificuldade: Média
- Status: Não iniciado — 0%
- Skills recomendadas: [[dev-backend]]
- Depende de: 01.5 (repositórios com tenant), 02.2, 02.7 (turma/janela precisam existir como entidade)
- Critério de aceite: Constraint única no banco (não só validação em memória) impede duas presenças válidas para a mesma pessoa na mesma aula/janela. Nova foto da mesma pessoa/aula só atualiza confiança, não cria presença nova.
- Retomada: —

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
- Status: Não iniciado — 0%
- Skills recomendadas: [[dev-backend]]
- Depende de: 02.2
- Item descoberto durante o levantamento de requisitos 02.1 — não estava no desenho original do módulo. Sem isso, 02.3/02.4 não têm turma/janela real para consultar (hoje `turma` é só texto livre do chat).
- Critério de aceite: Novas tabelas `turmas` (tenant_id, nome, ativa), `turma_janelas` (turma_id, dias_semana, hora_inicio, hora_fim, tolerancia_atraso_minutos, corte_presenca_parcial_percentual) e `turma_chat_telegram` (turma_id, chat_id — mesmo padrão de `tenant_chat_telegram` do módulo 01). Comando `/turma CODIGO` no bot vincula o chat atual a uma Turma cadastrada. `presencas` passa a referenciar `turma_id`/`turma_janela_id` em vez de só o texto livre. Cadastro de Turma + janelas (painel ou comando administrativo) fica a critério do ADR 02.2 decidir onde entra.
- Retomada: —
