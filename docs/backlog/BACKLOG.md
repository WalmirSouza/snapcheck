# SnapCheck — Backlog Principal

> Índice de navegação. Não coloque itens de trabalho aqui — cada módulo vive em `docs/backlog/modules/NN-nome.md` para caber no contexto de uma sessão. Este arquivo só tem visão geral, status agregado e o protocolo de como usar o sistema.

## Visão de produto
SnapCheck é uma plataforma SaaS de gestão inteligente de presença por reconhecimento facial (multi-tenant, antifraude, LGPD, gerencial), não apenas um sistema de chamada. Ver documento de visão estratégica completo (colado na conversa que originou este backlog) para contexto de negócio, métricas de sucesso e stack alvo (.NET 8, Angular 20, PostgreSQL, Redis, RabbitMQ, InsightFace/ONNX, S3).

## Protocolo de uso (leia antes de mexer em qualquer item)

1. **Cada item de backlog vive em um módulo** (`docs/backlog/modules/NN-nome.md`). Abra só o módulo que você vai trabalhar — não precisa carregar os outros.
2. **Todo item tem um template fixo**: Prioridade, Dificuldade, Status (%), Skills recomendadas, Depende de, Critério de aceite, Retomada.
3. **Ao começar um item**: mude o Status para `Em andamento — X%` e preencha **Retomada** com a data e o que você está prestes a fazer.
4. **Ao pausar ou parar um item** (mesmo que por falta de contexto): atualize **Retomada** com: o que já foi feito, arquivos tocados, e o próximo passo exato (não genérico — ex.: "falta adicionar `tenant_id` em `PresencaRepository.cs:GetByAlunoEAula`, query já ajustada em `PessoaRepository.cs`"). É esse campo que permite retomar sem reler a conversa inteira.
5. **Ao concluir um item**: Status = `Concluído — 100%`, mova a data para o campo Retomada como histórico ("concluído em 2026-07-20"), e marque as dependências liberadas nos itens que bloqueava.
6. **Sempre indique a skill recomendada antes de começar** — invoque-a em vez de improvisar o papel (ex.: item de arquitetura → `arquiteto-software`; implementação → `dev-backend`/`dev-frontend`; validação → `qa-testes`). Para itens que atravessam requisito→arquitetura→implementação→QA, use `construir-feature` como orquestrador.
7. **Não pule os portões**: um item de implementação não deve começar se o item de requisito/arquitetura do qual depende ("Depende de") ainda não estiver concluído.

## Status agregado por módulo

| Módulo | Arquivo | Prioridade | Status |
|---|---|---|---|
| 00 — Núcleo MVP (baseline já entregue) | [modules/00-nucleo-mvp.md](modules/00-nucleo-mvp.md) | — | Concluído — 100% |
| 01 — Multi-Tenant e Fundação de Dados | [modules/01-multi-tenant.md](modules/01-multi-tenant.md) | Alta (30d) | Concluído — 9/9 itens (100%) |
| 02 — Regras de Presença e Antifraude | [modules/02-presenca-antifraude.md](modules/02-presenca-antifraude.md) | Alta (30d) | Concluído — 8/8 itens (100%) |
| 03 — Qualidade Biométrica e IA | [modules/03-biometria-ia.md](modules/03-biometria-ia.md) | Alta (30-60d) | Não iniciado — 0% |
| 04 — Segurança e LGPD | [modules/04-seguranca-lgpd.md](modules/04-seguranca-lgpd.md) | Alta (30-60d) | Não iniciado — 0% |
| 05 — Resiliência e Mensageria | [modules/05-resiliencia-mensageria.md](modules/05-resiliencia-mensageria.md) | Média (60d) | Não iniciado — 0% |
| 06 — Observabilidade e Operação | [modules/06-observabilidade-operacao.md](modules/06-observabilidade-operacao.md) | Média (60d) | Não iniciado — 0% |
| 07 — Painel Gerencial e Relatórios | [modules/07-painel-gerencial.md](modules/07-painel-gerencial.md) | Média (60d) | Não iniciado — 0% |
| 08 — Integrações | [modules/08-integracoes.md](modules/08-integracoes.md) | Média-Baixa (60-90d) | Não iniciado — 0% |
| 09 — Aplicativo Móvel | [modules/09-app-mobile.md](modules/09-app-mobile.md) | Baixa (90d) | Não iniciado — 0% |
| 10 — Qualidade, Testes e CI/CD | [modules/10-qualidade-cicd.md](modules/10-qualidade-cicd.md) | Alta (transversal) | Em andamento — 3/6 itens (50%) |

## Ordem recomendada de ataque

1. **01 (Multi-Tenant)** bloqueia quase tudo — schema sem `tenant_id` contamina qualquer feature nova.
2. **02 (Presença/Antifraude)** e **10 (Qualidade/CI-CD)** correm em paralelo com 01, já que 10 é transversal e 02 depende parcialmente do modelo de dados de 01.
3. **04 (Segurança/LGPD)** e **03 (Biometria/IA)** entram assim que 01 estabilizar.
4. **05 (Resiliência)** e **06 (Observabilidade)** vêm juntas — RabbitMQ sem observabilidade é ficar cego.
5. **07 (Painel Gerencial)** consome dados de 01+02.
6. **08 (Integrações)** e **09 (Mobile)** ficam para o final — dependem de tudo acima estar estável.

## Métricas de sucesso do produto (para referência ao priorizar)
Acurácia >98%, falso positivo <0,1%, falso negativo <2%, latência de reconhecimento <5s, resposta de API <1s, disponibilidade >99,5%, redução de tempo de chamada >90%, redução de ajustes manuais >80%.
