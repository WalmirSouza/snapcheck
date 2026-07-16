# Módulo 02 — Regras de Presença e Antifraude

> Prioridade do módulo: **Alta** (horizonte 30 dias). Depende parcialmente do modelo de dados do módulo 01 (matrícula/turma/aula precisam existir com `tenant_id`).

---

### 02.1 — Requisitos de regras de presença válida
- Prioridade: Alta
- Dificuldade: Média
- Status: Não iniciado — 0%
- Skills recomendadas: [[analista-requisitos]]
- Depende de: —
- Critério de aceite: Documento cobrindo a definição formal de presença válida (confiança acima do limiar, dentro da janela da aula, turma correta, matrícula ativa, sem presença anterior no mesmo período), incluindo os parâmetros configuráveis por cliente (tolerância de atraso, saída antecipada, presença parcial, múltiplas aulas no mesmo dia).
- Retomada: —

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
- Depende de: 01.5 (repositórios com tenant), 02.2
- Critério de aceite: Constraint única no banco (não só validação em memória) impede duas presenças válidas para a mesma pessoa na mesma aula/janela. Nova foto da mesma pessoa/aula só atualiza confiança, não cria presença nova.
- Retomada: —

---

### 02.4 — Implementação do motor de regras configurável
- Prioridade: Alta
- Dificuldade: Alta
- Status: Não iniciado — 0%
- Skills recomendadas: [[dev-backend]]
- Depende de: 02.2
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
