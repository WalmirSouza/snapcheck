# Módulo 07 — Painel Gerencial e Relatórios

> Prioridade do módulo: **Média** (horizonte 60 dias). Considerado estratégico pelo negócio — é o que justifica o valor percebido do SaaS além da chamada automática.

---

### 07.1 — Requisitos de indicadores gerenciais
- Prioridade: Alta
- Dificuldade: Baixa
- Status: Não iniciado — 0%
- Skills recomendadas: [[analista-requisitos]]
- Depende de: —
- Critério de aceite: Documento definindo os indicadores (frequência, evasão, ranking, taxa de presença, horários críticos) com fórmula de cálculo de cada um e granularidade (por turma, por período, por tenant).
- Retomada: —

---

### 07.2 — ADR de API de relatórios
- Prioridade: Média
- Dificuldade: Média
- Status: Não iniciado — 0%
- Skills recomendadas: [[arquiteto-software]]
- Depende de: 07.1, 01.5
- Critério de aceite: ADR decidindo se os indicadores são calculados on-demand (query agregada) ou pré-agregados (tabela de fatos/materialized view), considerando o volume alvo (300 mil fotos/mês).
- Retomada: —

---

### 07.3 — Implementar endpoints de indicadores
- Prioridade: Alta
- Dificuldade: Média
- Status: Não iniciado — 0%
- Skills recomendadas: [[dev-backend]]
- Depende de: 07.2
- Critério de aceite: Endpoints retornam frequência, evasão, ranking, taxa de presença e horários críticos, sempre escopados por tenant (depende do módulo 01).
- Retomada: —

---

### 07.4 — Dashboard gerencial no painel
- Prioridade: Alta
- Dificuldade: Média
- Status: Não iniciado — 0%
- Skills recomendadas: [[dev-frontend]] (aciona a skill `dataviz`)
- Depende de: 07.3
- Critério de aceite: Tela de dashboard consumindo os endpoints de 07.3, com gráficos consistentes (paleta e forma definidas pela skill `dataviz`).
- Retomada: —

---

### 07.5 — Exportação para Excel e PDF
- Prioridade: Média
- Dificuldade: Baixa
- Status: Não iniciado — 0%
- Skills recomendadas: [[dev-backend]], [[dev-frontend]]
- Depende de: 07.3
- Critério de aceite: Usuário exporta qualquer relatório do dashboard em Excel e PDF, respeitando o RBAC (só exporta o que tem permissão de ver) — integra com auditoria (módulo 04, exportação é operação sensível).
- Retomada: —

---

### 07.6 — Testes de indicadores gerenciais
- Prioridade: Média
- Dificuldade: Baixa
- Status: Não iniciado — 0%
- Skills recomendadas: [[qa-testes]]
- Depende de: 07.3
- Critério de aceite: Testes validam que os números de frequência/evasão/ranking batem com dados de presença conhecidos (massa de teste controlada), evitando indicador gerencial errado.
- Retomada: —
