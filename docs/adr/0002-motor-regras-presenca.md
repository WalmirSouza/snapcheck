# ADR 0002 — Motor de Regras de Presença (Janela, Tolerância, Presença Parcial)

- Status: Aceito
- Data: 2026-07-19
- Entrada: `docs/requisitos/regras-presenca.md` (item de backlog 02.1)
- Item de backlog: 02.2

## Contexto

Estado real do código nesta data:
- `RegistrarPresencaEtapa` já desvia fotos com mais de um rosto detectado para uma revisão humana (`revisoes_presenca_grupo` — item 02.8, ADR posterior a este documento cronologicamente mas já implementado). Isso resolve ambiguidade de *identificação*, não de *horário* — hoje não existe nenhuma verificação de janela, tolerância de atraso ou presença parcial.
- Idempotência (item 02.3) já existe via `UNIQUE (tenant_id, pessoa_id, turma_normalizada, data_dia)` + `ON CONFLICT DO NOTHING`. É uma dedupe **por dia**, não por janela — porque Turma/Janela (item 02.7) ainda não existe como entidade.
- O pipeline é uma sequência de `IPipelineEtapa` executadas in-process (`PipelineService`, `DownloadFotoEtapa` → `DetectarRostosEtapa`/`CompararRostosEtapa` → `RegistrarPresencaEtapa` → `AnotarImagemEtapa` → `EnviarRespostaEtapa`), **não** um barramento de eventos. Os eventos de domínio citados na visão estratégica original (`PresencaValidada`, `PresencaRevisaoPendente`) são aspiracionais — pertencem à arquitetura de mensageria do módulo 05 (RabbitMQ), que ainda não existe.

## Decisão 1 — Configuração parametrizável, não motor de regras genérico

**Escolhida: tabela de configuração parametrizável por Turma/Janela, não um motor de regras (rule engine) com DSL genérico.**

Os parâmetros confirmados no item 02.1 são um conjunto fechado e conhecido: horário de início/fim, dias da semana, tolerância de atraso em minutos, corte percentual de presença parcial. Não há indicação de que o cliente vá precisar expressar regras arbitrárias (ex.: "se está chovendo, tolerância dobra") — isso justificaria um motor de regras genérico (ex.: biblioteca de rules engine, DSL em JSON). Introduzir esse nível de abstração agora seria over-engineering para o escopo confirmado.

### Consequência
`turma_janelas` (item 02.7) carrega os parâmetros diretamente como colunas tipadas: `dias_semana`, `hora_inicio`, `hora_fim`, `tolerancia_atraso_minutos`, `corte_presenca_parcial_percentual`. A lógica de avaliação é código C# direto (comparação de horário), não um interpretador de regras.

## Decisão 2 — Nova etapa de pipeline, não barramento de eventos

**Escolhida: nova etapa `ValidarJanelaPresencaEtapa`, executada entre `CompararRostosEtapa` e `RegistrarPresencaEtapa`, mantendo o padrão in-process já existente.**

Não introduzir um barramento de eventos (RabbitMQ) só para este módulo — isso é escopo do módulo 05 e antecipar essa infraestrutura aqui duplicaria trabalho e adiaria a entrega de regras de presença, que é prioridade de 30 dias. Quando o módulo 05 existir, esta etapa continua funcionando como está (uma etapa a mais na sequência), só migrando o transporte entre etapas de in-process para fila — não muda a lógica de negócio.

### Consequência
- `ValidarJanelaPresencaEtapa` consulta a Turma vinculada ao chat (via `turma_chat_telegram`, item 02.7) e a(s) `turma_janelas` vigente(s) no momento do recebimento da foto.
- Calcula, por foto: `SemTurmaVinculada` | `ForaDaJanela` | `Completa` | `Atrasado` | `Parcial` (usando tolerância e corte percentual).
- `SemTurmaVinculada` e `ForaDaJanela` **não chamam** `RegistrarPresencaEtapa` — a resposta ao usuário explica o motivo (conforme regra de negócio do item 02.1: "não apenas silenciar").
- Para os demais casos, o status calculado passa a acompanhar o registro de presença (`RegistroPresencaResultado` ou o novo tipo que o substituir passa a incluir o status, não só Registrada/Duplicada).
- A etapa **não interfere** no desvio para revisão em grupo do item 02.8 (`Matches.Count > 1`) — continuam ortogonais: revisão resolve "quem é", esta etapa resolve "o horário conta como presença". Ordem de execução: `CompararRostos` → `ValidarJanelaPresenca` (calcula status) → `RegistrarPresenca` (decide registrar direto ou criar revisão, carregando o status calculado).

## Decisão 3 — Evolução da chave de idempotência

A chave atual (`tenant_id, pessoa_id, turma_normalizada, data_dia`, item 02.3) é uma aproximação por dia. Quando `turma_janelas` (item 02.7) existir, a chave deve migrar para `(tenant_id, pessoa_id, turma_janela_id, data_dia)`, permitindo duas presenças válidas no mesmo dia se forem em janelas diferentes da mesma turma (ex.: academia manhã+noite, confirmado no item 02.1). Esta migração fica registrada como parte do critério de aceite do item 02.7, não deste ADR — mas o ADR deixa a decisão explícita para não ser esquecida.

## Itens de backlog impactados
- 02.7 (Turma como entidade) precisa nascer antes de 02.4 poder consultar janela real — dependência já registrada.
- 02.4 (implementação do motor) passa a ser, na prática, "implementar `ValidarJanelaPresencaEtapa` + consulta a `turma_janelas`", não um motor de regras separado.
- 02.3 ganha uma nota de acompanhamento: revisar a chave de dedupe quando 02.7 for implementado.
