# ADR 0002 — Presença por Foto em Grupo com Confirmação no Painel

- Status: Aceito
- Data: 2026-07-19
- Entrada: `docs/requisitos/presenca-por-foto-em-grupo.md`
- Itens de backlog: 03.2 (principal), 02.3 (idempotência), 04.x (RBAC/LGPD)

## Contexto

Levantamento técnico no código atual:
- Pipeline assíncrono interno executa `Download -> DetectarRostos -> CompararRostos -> RegistrarPresenca -> AnotarImagem -> EnviarResposta` (`ServiceCollectionExtensions.cs`).
- `RegistrarPresencaEtapa.cs` registra presença automaticamente para cada match reconhecido.
- `PresencaRepository.cs` faz `INSERT` direto em `presencas`, sem etapa de revisão humana e sem chave de deduplicação por contexto de aula/janela.
- `FaceService.cs` usa limiar fixo (0.42) e retorna o melhor candidato reconhecido.

Requisito aprovado:
- Confirmação humana obrigatória antes de gravar presença.
- Evidência (foto de grupo) com retenção de 30 dias.
- Base legal: consentimento explícito.
- Canal da revisão na v1: **painel web**.
- Perfis que podem confirmar na v1: **professor e coordenador**.
- Expiração de revisão pendente na v1: **24h**.
- Regra de duplicidade aprovada:
  - no fluxo individual do aluno, tentativa duplicada deve ser bloqueada com mensagem explícita de "presença já registrada";
  - na confirmação da foto em grupo, duplicidade deve ser bloqueada silenciosamente para o professor (sem aviso de duplicidade na UI).

## Decisão

Adotar arquitetura **review-first assíncrona com confirmação no painel web**:

1. O pipeline deixa de gravar presença diretamente para foto em grupo.
2. Após reconhecimento facial, o sistema persiste uma **revisão pendente** (com itens por face e candidatos).
3. Professor/coordenador confirma no painel web.
4. A confirmação dispara processamento transacional em lote para gravar presenças aprovadas com idempotência.
5. Resultado da confirmação é auditado e publicado como evento de domínio.
6. Duplicidades detectadas na confirmação em lote são registradas apenas para auditoria/telemetria interna, sem notificação ao professor.

## Alternativas consideradas

| Opção | Prós | Contras | Veredito |
|---|---|---|---|
| **A. Revisão e confirmação no Telegram** | Menor esforço inicial no ecossistema atual do bot | UX ruim para turmas grandes (muitos rostos), pouca rastreabilidade visual, maior risco operacional em ajustes manuais | Rejeitada |
| **B. Revisão no painel web + pipeline assíncrono (escolhida)** | Melhor UX para revisão em lote, trilha de auditoria forte, separa reconhecimento de confirmação, facilita RBAC e compliance | Exige API/painel e novo fluxo persistente | Escolhida |

## Desenho arquitetural (alto nível)

### Limites de módulo
- **Cadastro Biométrico/Reconhecimento**: detecta faces e classifica candidatos.
- **Revisão Manual**: mantém revisões, itens por face, decisões e expiração.
- **Presença**: efetiva presença aprovada com deduplicação/idempotência.
- **Auditoria**: trilha imutável de confirmação/rejeição.

### Modelo de dados (proposto)
- `revisoes_presenca_grupo`  
  - `id`, `tenant_id`, `turma_id`, `sessao_id`, `status` (`pendente|confirmada|cancelada|expirada`), `expira_em`, `criado_por`, `criado_em`, `confirmado_por`, `confirmado_em`.
- `revisao_faces_itens`  
  - `id`, `revisao_id`, `tenant_id`, `face_index`, `pessoa_sugerida_id`, `confianca`, `decisao` (`aprovada|rejeitada|reatribuida|pendente`), `pessoa_final_id`, `decidido_por`, `decidido_em`, `motivo`.
- `evidencias_foto_grupo`  
  - `id`, `tenant_id`, `revisao_id`, `storage_key`, `hash`, `capturada_em`, `expira_em`, `apagada_em`.
- `presenca_lote_resultado` (opcional, para resposta resumida)  
  - totais: `aprovadas`, `duplicadas`, `rejeitadas`, `nao_reconhecidas`.

Observações:
- Todas as tabelas com `tenant_id` e índices compostos por tenant.
- Chave de idempotência de presença no banco (via módulo 02.3) deve ser aplicada no momento da confirmação em lote.

### Contratos de evento (domínio)
- `FotoGrupoRecebida` (entrada da foto e contexto de turma/sessão).
- `RevisaoPresencaCriada` (revisão pendente pronta para painel).
- `RevisaoPresencaConfirmada` (decisão humana concluída).
- `PresencaLoteEfetivada` (resultado consolidado: aprovadas/duplicadas/rejeitadas).
- `RevisaoPresencaExpirada` (pendência não confirmada em 24h).
- `PresencaDuplicidadeIgnorada` (evento interno opcional para observabilidade, sem efeito de notificação ao professor).

## Resiliência e operação

- **Idempotência**: confirmação em lote deve suportar retry sem duplicar presença.
- **Política de feedback por canal**:
  - fluxo individual do aluno: duplicidade bloqueada + mensagem explícita;
  - fluxo de foto em grupo do professor: duplicidade bloqueada sem aviso ao professor.
- **Consistência**: uso de transação por confirmação; falha parcial não pode deixar revisão confirmada sem persistir auditoria/presenças.
- **Expiração**: job de varredura marca revisões pendentes como expiradas após 24h.
- **Backpressure**: criação de revisão desacopla pico de fotos da etapa de decisão humana.
- **Observabilidade**: métricas mínimas por tenant:
  - tempo médio até confirmação,
  - taxa de expiração,
  - taxa de duplicidade evitada,
  - taxa de reatribuição manual.

## Impacto multi-tenant e segurança/LGPD

- Isolamento obrigatório por `tenant_id` em leitura e escrita de revisões/evidências.
- Acesso de confirmação restrito a perfis **professor** e **coordenador** (integra com módulo RBAC).
- Filtragem por consentimento explícito: aluno sem consentimento não entra em aprovação automática.
- Evidência com retenção de 30 dias e expurgo automático, preservando metadados de auditoria.

## Dependências explícitas

- **[[seguranca-lgpd]]**: RBAC efetivo para confirmação, política de retenção/expurgo, trilha de auditoria imutável, consentimento versionado.
- **[[devops-sre]]**: estratégia de storage de evidências, políticas de retry/DLQ para eventos, métricas e alertas operacionais.

## Consequências

### Positivas
- Remove risco de marcação automática indevida em foto de grupo.
- Melhora auditabilidade e conformidade para biometria.
- Escala melhor operacionalmente que revisão em chat.

### Custos/Trade-offs
- Aumenta escopo inicial (API/painel/worker de expiração).
- Introduz latência entre envio da foto e presença efetivada (intencional por segurança).

## Fora de escopo desta ADR

- Implementação de código (handlers, endpoints, UI, migrations).
- Definição de UX detalhada da tela de revisão.
- Política jurídica de texto de consentimento e termos (além da diretriz já aprovada).
- Troca de infraestrutura para RabbitMQ/Redis/S3 neste momento (esta ADR define contratos e limites; a implantação da infra segue roadmap de DevOps/SRE).
