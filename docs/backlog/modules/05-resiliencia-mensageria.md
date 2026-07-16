# Módulo 05 — Resiliência e Mensageria

> Prioridade do módulo: **Média** (horizonte 60 dias). Meta: suportar até 10.000 fotos/dia e ~100.000 reconhecimentos/dia com crescimento horizontal, sem perder mensagem.

---

### 05.1 — Requisitos de volume e SLA de processamento
- Prioridade: Alta
- Dificuldade: Baixa
- Status: Não iniciado — 0%
- Skills recomendadas: [[analista-requisitos]]
- Depende de: —
- Critério de aceite: Documento consolidando os alvos já definidos pelo negócio — até 500 turmas/dia, 15-25 pessoas/foto (pico 100), 10.000 fotos/dia, reconhecimento <5s, API <1s, disponibilidade 99,5% — e traduzindo em requisitos técnicos de throughput para a fila.
- Retomada: —

---

### 05.2 — ADR de topologia RabbitMQ
- Prioridade: Alta
- Dificuldade: Alta
- Status: Não iniciado — 0%
- Skills recomendadas: [[devops-sre]], [[arquiteto-software]]
- Depende de: 05.1
- Critério de aceite: ADR definindo exchanges/filas por etapa do pipeline, política de retry/backoff, estratégia de DLQ, e critério de circuit breaker para dependências externas (serviço de reconhecimento facial, storage).
- Retomada: —

---

### 05.3 — Substituir Channel em memória por RabbitMQ
- Prioridade: Alta
- Dificuldade: Alta
- Status: Não iniciado — 0%
- Skills recomendadas: [[dev-backend]]
- Depende de: 05.2
- Critério de aceite: `PipelineService.cs` e `ServiceCollectionExtensions.cs` publicam/consomem via RabbitMQ em vez de `Channel` in-process; reinício do processo não perde mensagem em trânsito.
- Retomada: —

---

### 05.4 — Retry policy e DLQ
- Prioridade: Alta
- Dificuldade: Média
- Status: Não iniciado — 0%
- Skills recomendadas: [[dev-backend]]
- Depende de: 05.3
- Critério de aceite: Falha transitória em qualquer etapa do pipeline é reprocessada automaticamente com backoff; falha persistente vai para DLQ e gera alerta (integra com módulo 06).
- Retomada: —

---

### 05.5 — Circuit breaker para dependências externas
- Prioridade: Média
- Dificuldade: Média
- Status: Não iniciado — 0%
- Skills recomendadas: [[dev-backend]]
- Depende de: 05.2
- Critério de aceite: Chamadas a serviços externos (IA de reconhecimento, storage) têm circuit breaker configurado — falhas em sequência abrem o circuito e evitam sobrecarregar um serviço já degradado.
- Retomada: —

---

### 05.6 — Modo offline / sincronização posterior (desejável)
- Prioridade: Baixa
- Dificuldade: Alta
- Status: Não iniciado — 0%
- Skills recomendadas: [[arquiteto-software]], [[dev-backend]]
- Depende de: 05.3
- Critério de aceite: Quando não há conexão, fotos são armazenadas localmente (dispositivo/edge) e sincronizadas automaticamente quando a conexão voltar, sem duplicar presença já processada.
- Retomada: —

---

### 05.7 — Testes de carga e backpressure
- Prioridade: Média
- Dificuldade: Alta
- Status: Não iniciado — 0%
- Skills recomendadas: [[qa-testes]], [[devops-sre]]
- Depende de: 05.3, 05.4
- Critério de aceite: Teste de carga simulando pico de 100 pessoas por foto e volume diário alvo, validando que o sistema aplica backpressure em vez de cair ou perder mensagem.
- Retomada: —
