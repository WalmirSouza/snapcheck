# Módulo 10 — Qualidade, Testes e CI/CD

> Prioridade do módulo: **Alta** (transversal — corre em paralelo com todos os outros, não é uma fase única). Meta: cobertura mínima de 80% (unitários, integração, contrato, E2E).

---

### 10.1 — Requisitos de estratégia de testes
- Prioridade: Alta
- Dificuldade: Baixa
- Status: Concluído — 100%
- Skills recomendadas: [[analista-requisitos]], [[qa-testes]]
- Depende de: —
- Critério de aceite: Documento definindo a pirâmide de testes do projeto (o que é unitário, integração, contrato, E2E), meta de cobertura por camada, e ferramentas a usar no stack .NET/Angular.
- Retomada: Concluído em 2026-07-19 — `docs/requisitos/estrategia-testes.md`. Escopo pragmático: xUnit, dois níveis (unitário com fakes simples, integração contra o Postgres real do `docker-compose` — sem Testcontainers/Moq para não adicionar infra/dependência nova), foco em desbloquear 01.7/02.6 primeiro, 80% de cobertura fica para 10.6 (contínuo). CI/CD decidido separadamente no item 10.2 antes de criar qualquer workflow (mudança de maior impacto, exige confirmação explícita).

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
- Status: Em andamento — projeto de teste pronto e rodando localmente; falta o CI de verdade (workflow automatizado por PR)
- Skills recomendadas: [[devops-sre]]
- Depende de: 10.2
- Critério de aceite: Pipeline executando build + testes + lint/análise estática automaticamente a cada PR, bloqueando merge se falhar.
- Retomada: Parcial em 2026-07-19. Criado `tests/SnapCheck.Tests` (xUnit, referenciando `src/SnapCheck/SnapCheck.csproj`, adicionado à `SnapCheck.sln`). `dotnet build`/`dotnet test` funcionam localmente e passam (12/12). **Achado de infraestrutura, não de código**: a porta 5432 do host já está ocupada por um Postgres nativo do Windows, e 5433/5434 por outros projetos em docker-compose na mesma máquina — `docker-compose.yml` teve o mapeamento de porta do serviço `postgres` trocado de `5432:5432` para `5439:5432` (só a porta do host; a rede interna do compose continua em `postgres:5432`, sem impacto no app). Testes de integração assumem Postgres acessível em `localhost:5439` (`SNAPCHECK_TEST_CONNECTION_STRING` para sobrescrever). **Ainda falta**: nenhum workflow de CI real (GitHub Actions ou outro) rodando isso automaticamente por PR — isso exige confirmação explícita do usuário antes de criar (mexer em CI/CD é mudança de maior impacto, ver diretriz do item 10.1). Próximo passo: perguntar ao usuário se quer GitHub Actions agora (é o candidato óbvio, repo já está lá) antes de criar o workflow.

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
