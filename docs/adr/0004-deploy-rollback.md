# ADR 0004 — Deploy e Rollback (preparação, sem ambiente real ainda)

- Status: Aceito
- Data: 2026-07-19
- Item de backlog: 10.5

## Contexto
Confirmado com o usuário em 2026-07-19: **não existe ambiente real hoje** (sem VPS, cloud provider ou cluster Kubernetes configurado). O item 10.5 pede "deploy automatizado... preparado para Kubernetes com rollback testado e documentado" — a decisão aqui é preparar os artefatos sem conectar a nada real, para não inventar credenciais/ambiente que não existem.

## Decisão 1 — Manifests Kubernetes preparatórios, sem workflow de CD real
Criados `k8s/deployment.yaml`, `k8s/service.yaml`, `k8s/configmap.yaml`, `k8s/secret.example.yaml` (modelo, sem valores reais — `k8s/secret.yaml` real fica no `.gitignore`). **Não** foi adicionado nenhum job de deploy ao `.github/workflows/ci.yml` — conectar a um cluster real exigiria secrets do GitHub (kubeconfig, registry) que não existem, e criar isso "no vazio" seria simular uma capacidade que o time ainda não tem.

## Decisão 2 — `replicas: 1` obrigatório, não escolha de capacidade
**Achado importante durante a preparação**: a aplicação hoje não é horizontalmente escalável. `IBotManager`, `ITenantContext`, `IMessageChannel` e `PipelineService` são singletons em memória por processo. Rodar 2+ réplicas significaria dois pollers do Telegram no mesmo token (a API do Telegram rejeita/conflita long-polling duplicado) e filas de pipeline não compartilhadas entre pods (mensagens enfileiradas em um pod nunca seriam vistas pelo outro). Por isso o `Deployment` usa `replicas: 1` e `strategy: Recreate` (não `RollingUpdate`, que rodaria o pod antigo e o novo simultaneamente por alguns segundos). Escalar de verdade depende do módulo 05 (RabbitMQ) substituir o `Channel` in-memory por fila externa — isso é um pré-requisito arquitetural para HA, não só uma configuração de réplicas.

## Decisão 3 — Rollback via reversão de imagem versionada, não `kubectl rollout undo` cego
Estratégia de rollback documentada no runbook (`docs/runbooks/deploy-rollback.md`): toda imagem publicada é tagueada com o SHA do commit (não só `latest`), e o rollback é reaplicar o `Deployment` apontando para a tag anterior conhecida-boa — mais rastreável do que confiar no histórico de revisão do Kubernetes (`kubectl rollout undo`), que também funciona mas não deixa explícito qual código estava rodando antes.

## Decisão 4 — Probe de saúde não usa rota autenticada
`readinessProbe` aponta para `/Index` (página do painel, sem autenticação) em vez de `/api/bot/status` (protegido pelo `AdminApiKeyMiddleware`, item 01.9) — usar uma rota protegida no probe exigiria vazar a API key no manifest, o que é pior do que só confirmar que o servidor web está de pé.

## Itens de backlog impactados
- 10.5 fica com os artefatos prontos, mas não testados contra um cluster real (não existe um para testar) — quando o ambiente existir, o teste real de rollback (matar uma versão ruim, confirmar que a anterior volta) é o critério de aceite que falta fechar.
- Módulo 05 (Resiliência e Mensageria) vira pré-requisito documentado para qualquer escala horizontal futura, não só recomendação — achado desta análise.
