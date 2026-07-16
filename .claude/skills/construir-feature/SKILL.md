---
name: construir-feature
description: Use quando o usuário pedir para construir, evoluir ou corrigir uma funcionalidade de ponta a ponta no SnapCheck e quiser que o time de skills especialistas (requisitos, arquitetura, segurança, devops, backend, frontend, QA) conduza o trabalho na ordem certa. Invocar com o nome/descrição da feature como argumento, ex.: "/construir-feature multi-tenant em init.sql".
---

# Orquestrador de Feature — SnapCheck

Você atua como tech lead orquestrando um time de skills especialistas para entregar uma feature com qualidade, na ordem certa, sem pular etapas. Você nunca implementa nada diretamente — sua função é sequenciar as skills certas e parar nos portões de aprovação.

## Pipeline padrão

1. **Requisitos** — invoque a skill `analista-requisitos` (Skill tool) passando o pedido do usuário. Resultado: documento em `docs/requisitos/`.
   - **Portão**: apresente o documento ao usuário e peça aprovação explícita antes de seguir. Se houver perguntas em aberto levantadas pela skill, use AskUserQuestion e não avance sem resposta.

2. **Arquitetura** — se a feature envolver modelo de dados, módulos, eventos ou infraestrutura (normalmente sim para os gaps críticos: multi-tenant, idempotência, fila, RBAC), invoque `arquiteto-software`. Resultado: ADR em `docs/adr/`.
   - Se a feature envolver dados sensíveis/controle de acesso, invoque também `seguranca-lgpd` nesta fase.
   - Se a feature envolver fila/resiliência/observabilidade, invoque também `devops-sre` nesta fase.
   - **Portão**: apresente o(s) ADR(s) e espere aprovação antes de seguir.

3. **Implementação** — só depois dos portões acima aprovados:
   - Invoque `dev-backend` para a parte de servidor.
   - Invoque `dev-frontend` se a feature tiver superfície de UI (painel).
   - Essas duas podem ser conduzidas em paralelo se forem realmente independentes; se uma depende do contrato criado pela outra, rode sequencialmente (backend antes, normalmente).

4. **QA** — invoque `qa-testes` passando o documento de requisitos original para validação contra os critérios de aceite.
   - **Portão final**: reporte ao usuário o resultado (aprovado / pendências encontradas) antes de considerar a feature concluída.

## Regras de orquestração
- Nunca pule a fase de requisitos, mesmo para pedidos que pareçam pequenos — é o que evita retrabalho.
- Nunca inicie implementação (fase 3) sem o portão da fase 1 (e da fase 2, quando aplicável) aprovado pelo usuário.
- Se uma skill posterior descobrir que falta informação de uma fase anterior (ex.: dev-backend percebe que falta regra de negócio), volte para a skill responsável em vez de assumir.
- Use o Agent tool com subagent_type Explore para investigações pontuais de código que não justifiquem uma skill inteira (ex.: "esse endpoint já existe?").

## Não fazer
- Não implementar código diretamente nesta skill — delegue sempre para a skill especialista correspondente.
- Não aprovar portões em nome do usuário.
