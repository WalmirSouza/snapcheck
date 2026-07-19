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
- Status: Concluído — 100% (escopo base; análise estática/segurança e deploy ficam para 10.4/10.5)
- Skills recomendadas: [[devops-sre]], [[arquiteto-software]]
- Depende de: 10.1
- Critério de aceite: ADR definindo etapas do pipeline (build, testes, análise estática, segurança, deploy automático, rollback, versionamento) e a ferramenta de CI a usar.
- Retomada: Concluído em 2026-07-19 — `docs/adr/0003-pipeline-cicd.md`. GitHub Actions (repo já hospedado lá, suporte nativo a `services:` para Postgres efêmero no job, sem precisar de Testcontainers). Escopo do pipeline base: checkout → setup .NET 8 → restore → build → test (contra Postgres de serviço do job, credenciais efêmeras próprias, não as do `docker-compose` de dev). Análise estática/segurança (10.4) e deploy/rollback (10.5) ficam para depois, como etapas adicionais ao mesmo workflow. Branch protection (exigir o check antes de merge) é configuração do GitHub, não arquivo de código — não mexi nisso automaticamente.

---

### 10.3 — Implementar pipeline CI/CD base
- Prioridade: Alta
- Dificuldade: Média
- Status: Concluído — 100% (falta análise estática/lint, que é o item 10.4)
- Skills recomendadas: [[devops-sre]]
- Depende de: 10.2
- Critério de aceite: Pipeline executando build + testes + lint/análise estática automaticamente a cada PR, bloqueando merge se falhar.
- Retomada: Concluído em 2026-07-19. Criado `.github/workflows/ci.yml` (GitHub Actions, conforme ADR 0003): dispara em push/PR para `main` e `release/**`; sobe `postgres:16-alpine` como serviço do job (credenciais efêmeras `ci_test`); roda `dotnet restore` → `dotnet build --configuration Release` → `dotnet test` com `SNAPCHECK_TEST_CONNECTION_STRING` apontando pro serviço. Validei a sequência de comandos localmente em modo Release contra o Postgres do docker-compose (porta 5439) antes de considerar pronto — 12/12 passando, mesmo resultado que em Debug. **Ainda não verificado rodando de verdade no GitHub** (isso só acontece quando o commit for enviado ao repositório remoto — ainda não fiz push, aguardando decisão do usuário). Branch protection exigindo o check `build-and-test` antes de merge é configuração do GitHub (Settings → Branches), não faço isso automaticamente. Falta análise estática/lint (item 10.4) como próxima etapa deste mesmo workflow.

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
