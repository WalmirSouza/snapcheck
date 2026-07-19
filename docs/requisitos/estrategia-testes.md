# Requisitos — Estratégia de Testes (item de backlog 10.1)

## Contexto
Não existe nenhum projeto de teste no repositório hoje. Isso bloqueia dois itens já prontos para fechar: 01.7 (isolamento multi-tenant) e 02.6 (antifraude/duplicidade). Toda a verificação até aqui foi feita manualmente contra o Postgres real do `docker-compose` (válido, mas não repetível/automatizado).

## Decisões (pragmáticas, escopo mínimo para desbloquear 01.7/02.6)

1. **Framework**: xUnit — padrão de fato em .NET, já é o que `dotnet new` e o tooling da comunidade .NET 8 assumem.
2. **Dois níveis de teste, não uma pirâmide completa por enquanto**:
   - **Unitário**: lógica pura sem I/O (ex.: cálculo de janela/status em `ValidarJanelaPresencaEtapa`), usando fakes simples para as dependências (`ITurmaRepository` fake em memória), não mocks de biblioteca — evita adicionar dependência nova (Moq/NSubstitute) para um projeto pequeno.
   - **Integração**: contra o Postgres real do `docker-compose` (mesmo ambiente que venho usando para validar manualmente), não Testcontainers — evita mais uma dependência/infra nova agora; testes de integração exigem o `docker-compose` rodando (`docker compose up -d postgres`), documentado no README do projeto de teste.
3. **Isolamento de dados nos testes de integração**: cada teste cria seu próprio tenant com código único (GUID) e limpa os dados no `Dispose`/`IAsyncLifetime`, mesmo padrão que venho usando manualmente (criar → validar → limpar). Não usa transação+rollback compartilhada porque os repositórios abrem conexão própria por chamada (não há transação ambiente entre chamadas).
4. **Escopo desta rodada**: cobrir os critérios de aceite já escritos nos itens 01.7 e 02.6 — não perseguir 80% de cobertura geral agora (isso é 10.6, esforço contínuo, não uma tarefa única).
5. **CI/CD**: fica para o item 10.2/10.3 (ADR + pipeline) — decisão de qual provedor de CI (GitHub Actions é o candidato óbvio, dado que o repo já está no GitHub) fica para confirmação explícita antes de criar qualquer arquivo de workflow, já que mexer em CI/CD é uma mudança de maior impacto.

## Fora de escopo (deste item)
- Testes de contrato e E2E completos (módulo 10 mais amplo).
- Cobertura de 80% (item 10.6, contínuo).
- Testcontainers ou infraestrutura de teste isolada — decisão consciente de usar o `docker-compose` já existente.
- Pipeline de CI em si (item 10.2/10.3).

## Status
Concluído — 100%.
