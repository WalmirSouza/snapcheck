# Módulo 06 — Observabilidade e Operação

> Prioridade do módulo: **Média** (horizonte 60 dias). Anda junto com o módulo 05 — mensageria nova sem observabilidade é ficar cego em produção.

---

### 06.1 — Requisitos de SLO e alarmes
- Prioridade: Alta
- Dificuldade: Baixa
- Status: Não iniciado — 0%
- Skills recomendadas: [[analista-requisitos]], [[devops-sre]]
- Depende de: —
- Critério de aceite: Documento traduzindo as metas de negócio (reconhecimento <5s, API <1s, disponibilidade 99,5%, acurácia >98%) em SLOs mensuráveis por etapa do pipeline (ingestão, reconhecimento, gravação, resposta) e definindo o limiar de cada alarme.
- Retomada: —

---

### 06.2 — ADR de stack de observabilidade
- Prioridade: Alta
- Dificuldade: Média
- Status: Não iniciado — 0%
- Skills recomendadas: [[devops-sre]]
- Depende de: 06.1
- Critério de aceite: ADR definindo instrumentação (OpenTelemetry), onde ficam métricas/traces/logs, e como são segmentados por tenant.
- Retomada: —

---

### 06.3 — Instrumentar pipeline com OpenTelemetry
- Prioridade: Alta
- Dificuldade: Alta
- Status: Não iniciado — 0%
- Skills recomendadas: [[dev-backend]]
- Depende de: 06.2
- Critério de aceite: Cada etapa do pipeline (`PipelineService.cs`, `RegistrarPresencaEtapa.cs`, `FaceService.cs`) emite trace/métrica correlacionável de ponta a ponta, com tenant como dimensão.
- Retomada: —

---

### 06.4 — Dashboards operacionais
- Prioridade: Média
- Dificuldade: Média
- Status: Não iniciado — 0%
- Skills recomendadas: [[devops-sre]], [[dev-frontend]]
- Depende de: 06.3
- Critério de aceite: Dashboard mostrando latência por etapa, tamanho da fila, taxa de erro, e acurácia (falso positivo/negativo) do módulo 03 — usa a skill `dataviz` se for tela nova no painel.
- Retomada: —

---

### 06.5 — Alarmes de fila acumulada e acurácia degradada
- Prioridade: Alta
- Dificuldade: Média
- Status: Não iniciado — 0%
- Skills recomendadas: [[devops-sre]]
- Depende de: 06.3, 05.4
- Critério de aceite: Alarme automático dispara quando a fila DLQ cresce acima do limiar ou quando a taxa de falso positivo/negativo do módulo 03 ultrapassa a meta.
- Retomada: —

---

### 06.6 — Logs estruturados e correlação
- Prioridade: Média
- Dificuldade: Baixa
- Status: Não iniciado — 0%
- Skills recomendadas: [[dev-backend]]
- Depende de: 06.2
- Critério de aceite: Logs em formato estruturado (JSON) com tenant_id e ID de correlação do pipeline em toda etapa, substituindo o log operacional em memória atual (`BotController.cs`, `ConsultaHandler.cs`).
- Retomada: —
