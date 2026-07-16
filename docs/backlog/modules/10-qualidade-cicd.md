# Módulo 10 — Qualidade, Testes e CI/CD

> Prioridade do módulo: **Alta** (transversal — corre em paralelo com todos os outros, não é uma fase única). Meta: cobertura mínima de 80% (unitários, integração, contrato, E2E).

---

### 10.1 — Requisitos de estratégia de testes
- Prioridade: Alta
- Dificuldade: Baixa
- Status: Não iniciado — 0%
- Skills recomendadas: [[analista-requisitos]], [[qa-testes]]
- Depende de: —
- Critério de aceite: Documento definindo a pirâmide de testes do projeto (o que é unitário, integração, contrato, E2E), meta de cobertura por camada, e ferramentas a usar no stack .NET/Angular.
- Retomada: —

---

### 10.2 — ADR de pipeline CI/CD
- Prioridade: Alta
- Dificuldade: Média
- Status: Não iniciado — 0%
- Skills recomendadas: [[devops-sre]], [[arquiteto-software]]
- Depende de: 10.1
- Critério de aceite: ADR definindo etapas do pipeline (build, testes, análise estática, segurança, deploy automático, rollback, versionamento) e a ferramenta de CI a usar.
- Retomada: —

---

### 10.3 — Implementar pipeline CI/CD base
- Prioridade: Alta
- Dificuldade: Média
- Status: Não iniciado — 0%
- Skills recomendadas: [[devops-sre]]
- Depende de: 10.2
- Critério de aceite: Pipeline executando build + testes + lint/análise estática automaticamente a cada PR, bloqueando merge se falhar.
- Retomada: —

---

### 10.4 — Análise estática e segurança no pipeline
- Prioridade: Média
- Dificuldade: Baixa
- Status: Não iniciado — 0%
- Skills recomendadas: [[devops-sre]], [[seguranca-lgpd]]
- Depende de: 10.3
- Critério de aceite: Pipeline roda análise estática e scan de segurança (dependências vulneráveis) a cada build, com falha bloqueante para vulnerabilidades críticas.
- Retomada: —

---

### 10.5 — Deploy automático com rollback
- Prioridade: Média
- Dificuldade: Alta
- Status: Não iniciado — 0%
- Skills recomendadas: [[devops-sre]]
- Depende de: 10.3
- Critério de aceite: Deploy automatizado para o ambiente alvo (preparado para Kubernetes) com rollback testado e documentado.
- Retomada: —

---

### 10.6 — Elevar cobertura de testes para 80%
- Prioridade: Alta
- Dificuldade: Alta
- Status: Não iniciado — 0%
- Skills recomendadas: [[qa-testes]]
- Depende de: 10.1
- Critério de aceite: Cobertura medida no pipeline atinge 80% incluindo unitários, integração, contrato e E2E — aplicado incrementalmente a cada módulo (01-09) conforme é implementado, não como esforço isolado no final.
- Retomada: —
