# Módulo 03 — Qualidade Biométrica e IA

> Prioridade do módulo: **Alta** (horizonte 30-60 dias). Meta de produto: falso positivo <0,1%, falso negativo <2%, latência de reconhecimento <5s. Prioriza nunca marcar a pessoa errada — prefere revisão manual a erro.

---

### 03.1 — Requisitos de fila de revisão manual
- Prioridade: Alta
- Dificuldade: Média
- Status: Não iniciado — 0%
- Skills recomendadas: [[analista-requisitos]]
- Depende de: —
- Critério de aceite: Documento definindo o limiar de confiança configurável por tenant, o que acontece com uma identificação abaixo do limiar (vai para fila, quem revisa, SLA de revisão), e o formato da fila (prioridade, prazo, escalonamento se não revisado).
- Retomada: —

---

### 03.2 — ADR de arquitetura de revisão manual e atualização de embeddings
- Prioridade: Alta
- Dificuldade: Alta
- Status: Não iniciado — 0%
- Skills recomendadas: [[arquiteto-software]]
- Depende de: 03.1
- Critério de aceite: ADR cobrindo o evento `PresencaRevisaoPendente`, o módulo de Revisão Manual, e a estratégia de atualização supervisionada de embeddings ao longo do tempo (quando reprocessar, como versionar o embedding antigo vs. novo, quem aprova).
- Retomada: —

---

### 03.3 — Implementar fila de revisão manual
- Prioridade: Alta
- Dificuldade: Alta
- Status: Não iniciado — 0%
- Skills recomendadas: [[dev-backend]]
- Depende de: 03.2, 01.5
- Critério de aceite: Identificações abaixo do limiar não geram presença automática — entram em fila persistida (não em memória), visível no painel para o perfil responsável revisar.
- Retomada: —

---

### 03.4 — Painel de revisão manual
- Prioridade: Alta
- Dificuldade: Média
- Status: Não iniciado — 0%
- Skills recomendadas: [[dev-frontend]]
- Depende de: 03.3
- Critério de aceite: Tela onde o revisor vê a foto, o(s) candidato(s) sugerido(s) pela IA, e aprova/rejeita/reatribui a identificação.
- Retomada: —

---

### 03.5 — Atualização supervisionada de embeddings
- Prioridade: Média
- Dificuldade: Alta
- Status: Não iniciado — 0%
- Skills recomendadas: [[dev-backend]]
- Depende de: 03.2
- Critério de aceite: Quando uma revisão manual confirma uma identificação com boa confiança em foto recente, o sistema atualiza o embedding da pessoa (com aprovação, não automaticamente sem supervisão), mantendo histórico do embedding anterior.
- Retomada: —

---

### 03.6 — Métricas de acurácia (falso positivo/negativo)
- Prioridade: Alta
- Dificuldade: Média
- Status: Não iniciado — 0%
- Skills recomendadas: [[devops-sre]], [[qa-testes]]
- Depende de: 03.3
- Critério de aceite: Métrica contínua de taxa de falso positivo/negativo por tenant, alimentando o módulo 06 (observabilidade) com alarme quando a acurácia degradar abaixo da meta (falso positivo >0,1%, falso negativo >2%).
- Retomada: —

---

### 03.7 — Testes de qualidade biométrica em condições adversas
- Prioridade: Média
- Dificuldade: Alta
- Status: Não iniciado — 0%
- Skills recomendadas: [[qa-testes]]
- Depende de: 03.3
- Critério de aceite: Casos de teste com fotos de baixa iluminação, ângulos diferentes, óculos, oclusão parcial e diferentes distâncias — validando que caem em revisão manual em vez de errar silenciosamente.
- Retomada: —
