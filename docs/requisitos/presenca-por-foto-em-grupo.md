# Requisitos — Presença por Foto em Grupo com Confirmação do Professor

## Contexto (estado atual verificado no código)

- O fluxo atual recebe foto do Telegram (`FotoHandler.cs`), processa no pipeline (`PipelineService.cs`) e registra presença automaticamente para cada `match` reconhecido (`RegistrarPresencaEtapa.cs`).
- O reconhecimento compara com pessoas ativas do tenant (`PessoaRepository.cs`) e usa limiar fixo de similaridade (`0.42`) no `FaceService.cs`.
- A gravação de presença hoje é `INSERT` direto em `presencas` (`PresencaRepository.cs`), sem etapa de confirmação humana.
- Não existe entidade de revisão/aprovação, nem armazenamento estruturado de candidatos por face para o professor confirmar.
- Não há deduplicação no banco para impedir múltiplas presenças da mesma pessoa no mesmo contexto.

## Objetivo da funcionalidade

Permitir que o professor tire uma foto de grupo ao final da aula e confirme a presença de vários alunos de uma só vez, reduzindo tempo operacional e mantendo controle de qualidade (evitar marcação incorreta).

## Decisões de produto confirmadas com o usuário (2026-07-19)

1. A v1 **sempre exige confirmação do professor** antes de gravar qualquer presença.
2. As fotos de grupo devem ser armazenadas por **30 dias** para auditoria/revisão.
3. A base legal LGPD para biometria nesta funcionalidade será **consentimento explícito do aluno/responsável**.
4. A revisão da presença por foto em grupo na v1 ocorrerá no **painel web**.
5. Podem confirmar revisão na v1: **professor e coordenador**.
6. Revisões pendentes expiram em **24 horas**.

## User stories

1. Como professor, quero enviar foto de grupo da turma para marcar presença em lote, para reduzir tempo no fechamento da aula.
2. Como professor, quero revisar e confirmar os reconhecimentos sugeridos antes da gravação final, para evitar presenças incorretas.
3. Como coordenador/gestor, quero rastreabilidade de quem confirmou a presença e quando, para fins de auditoria.
4. Como DPO/compliance, quero controlar retenção e acesso às fotos de grupo, para cumprir LGPD.
5. Como aluno, quero ser informado quando eu já tiver presença registrada na turma, para não tentar marcar novamente sem necessidade.

## Critérios de aceite (Given/When/Then)

### CA-01 — Criação de revisão de presença por foto de grupo
- **Given** uma aula/sessão válida aberta para a turma e o professor autorizado
- **When** o professor envia uma foto de grupo para marcar presença
- **Then** o sistema cria uma revisão pendente com a lista de faces detectadas, candidatos reconhecidos e confiança por face
- **And** nenhuma presença é gravada neste momento.

### CA-02 — Confirmação obrigatória antes de registrar presença
- **Given** uma revisão pendente gerada a partir de foto de grupo
- **When** o professor confirma a revisão
- **Then** o sistema grava as presenças aprovadas em lote
- **And** registra auditoria com professor responsável, data/hora e origem "foto em grupo".

### CA-03 — Ajuste manual na revisão
- **Given** uma revisão pendente com faces não reconhecidas ou ambíguas
- **When** o professor corrige (aprova, rejeita ou reatribui) itens antes da confirmação
- **Then** somente os itens aprovados entram na gravação final
- **And** os itens rejeitados permanecem sem presença e com motivo rastreável.

### CA-04 — Deduplicação de presença na confirmação em lote
- **Given** uma pessoa já possui presença válida no mesmo contexto de aula/janela
- **When** a confirmação em lote tenta gravar nova presença para essa mesma pessoa
- **Then** o sistema não duplica presença
- **And** registra a duplicidade apenas para auditoria/telemetria interna, sem aviso ao professor.

### CA-05 — Evidência e retenção
- **Given** uma revisão de foto em grupo confirmada ou cancelada
- **When** o registro é finalizado
- **Then** a foto/evidência associada fica armazenada por 30 dias
- **And** após 30 dias os arquivos são removidos automaticamente, preservando apenas metadados de auditoria.

### CA-06 — Conformidade LGPD (consentimento)
- **Given** que o tratamento biométrico depende de consentimento explícito
- **When** o sistema processa uma foto em grupo
- **Then** apenas alunos com consentimento válido participam da marcação automática
- **And** alunos sem consentimento não são marcados automaticamente e aparecem para tratamento manual conforme política.

### CA-07 — Tentativa duplicada pelo aluno (fluxo individual)
- **Given** que o aluno já possui presença válida registrada para a mesma turma/janela/período
- **When** ele tenta fazer nova marcação de presença
- **Then** o sistema não cria nova presença
- **And** o aluno recebe mensagem explícita informando que a presença já foi registrada.

### CA-08 — Duplicidade na confirmação por foto em grupo
- **Given** que um aluno já possui presença válida na turma/janela/período antes da confirmação da foto em grupo
- **When** o professor confirma a revisão da foto em grupo contendo esse aluno
- **Then** o sistema não cria nova presença para esse aluno
- **And** o professor não recebe aviso de duplicidade desse aluno (descartar silenciosamente esse item).

## Regras de negócio explícitas

- A origem "foto em grupo" deve seguir o mesmo isolamento por `tenant_id` já existente.
- Presença por foto em grupo na v1 é sempre "pendente de confirmação" até ação explícita do professor.
- Uma revisão deve ter estados mínimos: `pendente`, `confirmada`, `cancelada`, `expirada`.
- Cada face detectada na revisão deve guardar: identificador da face, candidato sugerido, confiança, decisão final e usuário que decidiu.
- O resultado final da confirmação deve apresentar resumo de presenças efetivadas e pendências de revisão, sem alerta de duplicidade para o professor.
- A funcionalidade deve reaproveitar as regras de presença válida/deduplicação já definidas para o domínio de presença (módulo 02), não criar regra paralela.
- Regra de notificação para duplicidade:
  - no fluxo individual do aluno: bloquear nova presença e informar explicitamente "presença já registrada";
  - no fluxo de confirmação por foto em grupo do professor: bloquear nova presença e descartar duplicidade sem aviso ao professor.

## Prioridade no roadmap (30/60/90)

- Esta funcionalidade depende diretamente de fundamentos ainda em andamento:
  - 02.3 (idempotência/deduplicação em banco),
  - 03.1/03.2 (requisitos e ADR de revisão manual),
  - 04 (controles de segurança/LGPD operacionais).
- Prioridade sugerida: **horizonte 60 dias**, após fechamento dos bloqueadores de 30 dias (multi-tenant e idempotência).

## Perguntas em aberto

1. Qual limite de faces por foto e quantidade máxima de fotos por sessão na v1?
2. Em caso de baixa qualidade da foto (poucas detecções), a revisão é bloqueada ou segue com alerta?

## Fora de escopo (deste requisito)

- Implementação técnica (código, schema final, APIs, UI).
- Estratégia de infra (fila externa, storage definitivo, tuning de performance).
- Treinamento/atualização automática de embeddings com base nas confirmações.
- Definição jurídica detalhada de texto de consentimento (será tratada com jurídico/compliance).

## Status

Concluído — 100% para etapa de requisitos. Pronto para entrada no [[arquiteto-software]].
