# Módulo 08 — Integrações

> Prioridade do módulo: **Média-Baixa** (horizonte 60-90 dias). Cada integração é tratada como um item independente porque tem complexidade e prioridade comercial próprias.

---

### 08.1 — Requisitos e priorização de integrações
- Prioridade: Alta
- Dificuldade: Baixa
- Status: Não iniciado — 0%
- Skills recomendadas: [[analista-requisitos]]
- Depende de: —
- Critério de aceite: Documento priorizando qual integração entra primeiro (decisão de negócio pendente do usuário — qual gera venda mais rápido), cobrindo Upload Web, App Mobile, WhatsApp, Telegram, Instagram, API pública, Webhooks, e depois LMS/ERP/CRM/controle de acesso/folha de pagamento.
- Retomada: —

---

### 08.2 — ADR de API pública e autenticação de integrações
- Prioridade: Alta
- Dificuldade: Alta
- Status: Não iniciado — 0%
- Skills recomendadas: [[arquiteto-software]], [[seguranca-lgpd]]
- Depende de: 08.1, 04.2 (RBAC)
- Critério de aceite: ADR definindo contrato da API pública (versionamento, autenticação por tenant, rate limit) e do mecanismo de webhooks (eventos, retry, assinatura de payload).
- Retomada: —

---

### 08.3 — Implementar API pública v1
- Prioridade: Média
- Dificuldade: Alta
- Status: Não iniciado — 0%
- Skills recomendadas: [[dev-backend]]
- Depende de: 08.2
- Critério de aceite: Endpoints públicos documentados (cadastro, consulta de presença, indicadores) autenticados por tenant, com testes de contrato.
- Retomada: —

---

### 08.4 — Implementar Webhooks
- Prioridade: Média
- Dificuldade: Média
- Status: Não iniciado — 0%
- Skills recomendadas: [[dev-backend]]
- Depende de: 08.2, 05.4 (retry policy)
- Critério de aceite: Cliente cadastra URL de webhook por tenant e recebe eventos (ex.: `PresencaValidada`) com retry e assinatura verificável.
- Retomada: —

---

### 08.5 — Integração WhatsApp
- Prioridade: Baixa
- Dificuldade: Alta
- Status: Não iniciado — 0%
- Skills recomendadas: [[analista-requisitos]], [[dev-backend]]
- Depende de: 08.1
- Critério de aceite: Definido apenas quando 08.1 priorizar este canal — critério de aceite específico a ser detalhado no requisito daquele momento.
- Retomada: —

---

### 08.6 — Integração Telegram / Instagram
- Prioridade: Baixa
- Dificuldade: Alta
- Status: Não iniciado — 0%
- Skills recomendadas: [[analista-requisitos]], [[dev-backend]]
- Depende de: 08.1
- Critério de aceite: Definido apenas quando 08.1 priorizar este canal.
- Retomada: —

---

### 08.7 — Integrações LMS/ERP/CRM/controle de acesso/folha
- Prioridade: Baixa
- Dificuldade: Muito Alta
- Status: Não iniciado — 0%
- Skills recomendadas: [[analista-requisitos]], [[arquiteto-software]]
- Depende de: 08.3
- Critério de aceite: Tratado como iniciativa própria por sistema-alvo, só se inicia após a API pública (08.3) estar estável — cada integração externa vira seu próprio conjunto de itens quando priorizada.
- Retomada: —
