# Runbook — Deploy e Rollback (SnapCheck)

> Ver `docs/adr/0004-deploy-rollback.md` para o contexto e as decisões por trás deste runbook. Este documento assume que já existe um cluster Kubernetes real e acesso via `kubectl` — nenhum passo aqui foi testado contra um ambiente real ainda, porque ele não existe (confirmado em 2026-07-19).

## Pré-requisitos
- Acesso a um registry de imagens (ex.: GHCR, Docker Hub, ECR — a definir quando o ambiente existir).
- `kubectl` configurado apontando para o cluster/namespace corretos.
- `k8s/secret.yaml` preenchido a partir de `k8s/secret.example.yaml` (nunca commitar com valores reais — já está no `.gitignore`).

## Deploy

1. Buildar e taguear a imagem com o SHA do commit (não usar só `latest` — rollback rastreável depende disso):
   ```
   docker build -t <registry>/snapcheck:<git-sha> .
   docker push <registry>/snapcheck:<git-sha>
   ```
2. Atualizar `k8s/deployment.yaml` (campo `image:`) para `<registry>/snapcheck:<git-sha>`.
3. Aplicar os manifests (na ordem — ConfigMap/Secret antes do Deployment):
   ```
   kubectl apply -f k8s/configmap.yaml
   kubectl apply -f k8s/secret.yaml
   kubectl apply -f k8s/deployment.yaml
   kubectl apply -f k8s/service.yaml
   ```
4. Confirmar que o pod novo subiu e passou no `readinessProbe`:
   ```
   kubectl rollout status deployment/snapcheck
   ```
5. **Importante**: por causa do `strategy: Recreate` (ver ADR 0004), o pod antigo é derrubado antes do novo subir — espera-se uma janela curta de indisponibilidade a cada deploy. Isso é intencional (evita dois pods rodando o bot ao mesmo tempo), não um bug do manifest.

## Rollback

1. Identificar a última tag de imagem conhecida-boa (git SHA do commit anterior ao problema).
2. Reaplicar o `Deployment` apontando para essa tag:
   ```
   kubectl set image deployment/snapcheck snapcheck=<registry>/snapcheck:<git-sha-anterior>
   ```
3. Confirmar:
   ```
   kubectl rollout status deployment/snapcheck
   ```
4. Alternativa (se a tag anterior não estiver à mão): `kubectl rollout undo deployment/snapcheck` reverte para a revisão anterior do histórico do Kubernetes — funciona, mas não deixa explícito qual código estava rodando (por isso a Decisão 3 do ADR 0004 prefere a reversão por tag quando possível).

## O que ainda falta (pendência conhecida)
- Nenhum passo deste runbook foi executado contra um cluster real — não existe um ainda. Quando existir, o critério de aceite do item 10.5 só fecha de verdade depois de um teste de rollback real (deploy de uma versão quebrada de propósito, confirmar que o rollback recupera o serviço).
- Escala horizontal (mais de 1 réplica) não é suportada hoje — ver ADR 0004, Decisão 2. Não tente aumentar `replicas` sem antes resolver o módulo 05 (fila externa).
