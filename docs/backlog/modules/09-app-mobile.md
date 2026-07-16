# Módulo 09 — Aplicativo Móvel

> Prioridade do módulo: **Baixa** (horizonte 90 dias). Depende de API pública estável (módulo 08) e de multi-tenant/RBAC consolidados.

---

### 09.1 — Requisitos do app mobile
- Prioridade: Média
- Dificuldade: Média
- Status: Não iniciado — 0%
- Skills recomendadas: [[analista-requisitos]]
- Depende de: 08.3
- Critério de aceite: Documento definindo escopo do app (captura de foto para reconhecimento, consulta própria do aluno, notificações) e plataformas alvo (iOS/Android).
- Retomada: —

---

### 09.2 — ADR de arquitetura mobile
- Prioridade: Média
- Dificuldade: Alta
- Status: Não iniciado — 0%
- Skills recomendadas: [[arquiteto-software]]
- Depende de: 09.1
- Critério de aceite: ADR decidindo stack (nativo vs. híbrido), estratégia offline (integra com 05.6) e consumo da API pública (08.3).
- Retomada: —

---

### 09.3 — Implementar app mobile (captura e consulta)
- Prioridade: Baixa
- Dificuldade: Muito Alta
- Status: Não iniciado — 0%
- Skills recomendadas: [[dev-frontend]] (ou especialista mobile, fora do conjunto atual de skills — avaliar necessidade de skill dedicada quando este item for priorizado)
- Depende de: 09.2
- Critério de aceite: A definir no requisito 09.1 — este item só deve ser detalhado quando o negócio confirmar a entrada no roadmap de 90 dias.
- Retomada: —

---

### 09.4 — Testes do app mobile
- Prioridade: Baixa
- Dificuldade: Alta
- Status: Não iniciado — 0%
- Skills recomendadas: [[qa-testes]]
- Depende de: 09.3
- Critério de aceite: Cobertura de teste em dispositivos reais/emuladores para os fluxos de captura e consulta, incluindo cenário offline.
- Retomada: —
