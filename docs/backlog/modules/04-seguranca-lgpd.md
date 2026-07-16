# Módulo 04 — Segurança e LGPD

> Prioridade do módulo: **Alta** (horizonte 30-60 dias). Dado biométrico é dado sensível (LGPD art. 11) — este módulo não pode ser adiado para depois do lançamento comercial.

---

### 04.1 — Requisitos de consentimento e retenção
- Prioridade: Alta
- Dificuldade: Média
- Status: Não iniciado — 0%
- Skills recomendadas: [[analista-requisitos]], [[seguranca-lgpd]]
- Depende de: —
- Critério de aceite: Documento cobrindo — consentimento versionado por tenant, retenção de fotos (30 dias configurável), retenção de embeddings (enquanto houver vínculo contratual), retenção de histórico (mínimo 5 anos configurável). Perguntas de base legal por segmento ficam explicitamente marcadas como "decisão jurídica pendente do usuário".
- Retomada: —

---

### 04.2 — RBAC: definição de perfis e permissões
- Prioridade: Alta
- Dificuldade: Alta
- Status: Não iniciado — 0%
- Skills recomendadas: [[seguranca-lgpd]], [[arquiteto-software]]
- Depende de: 01.4 (contexto de tenant)
- Critério de aceite: Matriz de permissões para os perfis Super Admin, Cliente, Gestor, Coordenador, Instrutor, Operador, Auditor, Aluno (consulta própria) — o que cada um pode ver/fazer, sempre escopado por tenant.
- Retomada: —

---

### 04.3 — Implementar RBAC
- Prioridade: Alta
- Dificuldade: Alta
- Status: Não iniciado — 0%
- Skills recomendadas: [[dev-backend]]
- Depende de: 04.2
- Critério de aceite: Autorização por perfil aplicada em toda rota/handler sensível (`BotController.cs`, `ConsultaHandler.cs`, `CadastroHandler.cs`), com teste que prova que um perfil sem permissão recebe 403, não apenas esconde botão na UI.
- Retomada: —

---

### 04.4 — Criptografia em repouso (embeddings e fotos)
- Prioridade: Alta
- Dificuldade: Média
- Status: Não iniciado — 0%
- Skills recomendadas: [[seguranca-lgpd]], [[dev-backend]]
- Depende de: —
- Critério de aceite: Embeddings faciais e fotos armazenadas criptografadas em repouso; chaves geridas fora do código-fonte (secret manager/KMS conforme ambiente de deploy).
- Retomada: —

---

### 04.5 — Trilha de auditoria imutável
- Prioridade: Alta
- Dificuldade: Alta
- Status: Não iniciado — 0%
- Skills recomendadas: [[seguranca-lgpd]], [[dev-backend]]
- Depende de: 01.4
- Critério de aceite: Toda operação sensível (login, alteração, inclusão, exclusão, revisão manual, exportação, consulta sensível) gera registro de auditoria append-only (sem UPDATE/DELETE possível pela aplicação), com tenant, ator, timestamp e payload relevante.
- Retomada: —

---

### 04.6 — Retenção e expurgo automatizado
- Prioridade: Média
- Dificuldade: Alta
- Status: Não iniciado — 0%
- Skills recomendadas: [[dev-backend]], [[devops-sre]]
- Depende de: 04.1
- Critério de aceite: Job automatizado expurga fotos após o prazo configurado por tenant e reporta o que foi expurgado na trilha de auditoria (o próprio expurgo é auditável).
- Retomada: —

---

### 04.7 — Consentimento versionado no cadastro
- Prioridade: Alta
- Dificuldade: Média
- Status: Não iniciado — 0%
- Skills recomendadas: [[dev-backend]], [[dev-frontend]]
- Depende de: 04.1
- Critério de aceite: Cadastro de pessoa (`CadastroHandler.cs`) registra a versão do texto de consentimento aceito, por tenant, com data/hora.
- Retomada: —

---

### 04.8 — Revisão de segurança do código implementado
- Prioridade: Alta
- Dificuldade: Média
- Status: Não iniciado — 0%
- Skills recomendadas: [[seguranca-lgpd]] (aciona a skill `security-review`)
- Depende de: 04.3, 04.4, 04.5
- Critério de aceite: Revisão de segurança formal (via skill `security-review`) sobre RBAC, criptografia e auditoria antes de liberar para produção.
- Retomada: —
