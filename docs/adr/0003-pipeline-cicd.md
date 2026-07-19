# ADR 0003 — Pipeline de CI/CD

- Status: Aceito
- Data: 2026-07-19
- Entrada: `docs/requisitos/estrategia-testes.md` (item 10.1)
- Item de backlog: 10.2

## Contexto
Repositório hospedado no GitHub (`origin` → `WalmirSouza/snapcheck`, `upstream` → `hudigitaldev/snapcheck`), sem nenhum workflow configurado ainda. Já existe `tests/SnapCheck.Tests` (xUnit, item 10.1/10.3 parcial) rodando localmente contra o Postgres do `docker-compose`.

## Decisão 1 — GitHub Actions como provedor de CI

**Escolhida: GitHub Actions.** É o candidato óbvio — repositório já está no GitHub, não exige conta/integração em serviço externo (CircleCI, Azure DevOps etc.), e tem suporte nativo a `services:` (containers efêmeros) para subir Postgres durante o job, cobrindo os testes de integração sem precisar de Testcontainers.

## Decisão 2 — Escopo do pipeline base (item 10.3)

**Escolhido: build + testes automatizados em todo push/PR para `main` e `release/*`, com Postgres como serviço do job.** Análise estática/segurança (item 10.4) e deploy/rollback (item 10.5) ficam para ADRs/itens seguintes — não antecipar agora.

### Consequência
- Workflow único `.github/workflows/ci.yml`:
  1. Checkout.
  2. Setup .NET 8.
  3. `dotnet restore` / `dotnet build --no-restore` na solution inteira (`SnapCheck.sln`).
  4. Serviço `postgres:16-alpine` no job (credenciais de teste, efêmero — não é o mesmo Postgres do `docker-compose` de desenvolvimento).
  5. `dotnet test tests/SnapCheck.Tests` com `SNAPCHECK_TEST_CONNECTION_STRING` apontando para o serviço do job.
- Falha em qualquer etapa bloqueia o workflow (comportamento padrão do GitHub Actions), o que já satisfaz "bloqueando merge se falhar" — **branch protection rule** exigindo o check passar antes de merge é configuração do repositório (Settings → Branches), fora do arquivo YAML; não faço isso automaticamente aqui porque mexe em configuração do repositório GitHub, não em código.

## Decisão 3 — Não replicar credenciais de produção no CI
O serviço Postgres do CI usa credenciais efêmeras próprias (`ci_test`/senha gerada), não as mesmas do `docker-compose` de desenvolvimento (`snapcheck`/`snapcheck`) nem qualquer segredo real. Evita qualquer confusão entre ambiente de CI e ambiente de dev/produção.

## Itens de backlog impactados
- 10.3 passa a ter o workflow real, fechando o critério de aceite (build + testes automáticos por push/PR).
- 10.4 (análise estática/segurança) e 10.5 (deploy/rollback) ficam como próximos itens do módulo, adicionando etapas a este mesmo workflow ou criando novos.
