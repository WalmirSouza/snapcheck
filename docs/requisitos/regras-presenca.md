# Requisitos — Regras de Presença e Antifraude (item de backlog 02.1)

## Contexto (estado atual verificado no código)

- `RegistrarPresencaEtapa.cs`: para cada `match` reconhecido pelo `FaceService`, chama `presencaRepository.RegistrarAsync(tenantId, pessoaId, turma, ct)` **sem nenhuma verificação de horário, matrícula ou duplicidade**. Toda foto reconhecida gera uma presença nova.
- `FotoHandler.cs`: `turma` é `message.Chat.Title ?? Chat.Username ?? Chat.Id.ToString()` — texto livre derivado do chat do Telegram, não é uma entidade cadastrada.
- `FaceService.cs`: já existe um limiar de confiança (`limiarSimilaridade = 0.42f`, hoje hardcoded, não configurável por tenant) aplicado no momento do match — abaixo disso, `PessoaId` fica `null` e `Reconhecido` é `false`. Ou seja, o "não marcar errado" parcialmente já existe na camada de reconhecimento; falta a fila de revisão manual para os casos abaixo do limiar (isso é módulo 03, não este).
- Não existe conceito de Turma cadastrada, Aula (evento com horário) nem Matrícula (vínculo pessoa-turma) em nenhuma tabela.

## Decisões de produto (confirmadas pelo usuário em 2026-07-16/17)

1. **Nível de modelagem para v1**: janela simples por turma. Turma vira entidade com um ou mais horários recorrentes configurados pelo cliente (ex.: seg-sex 19h-21h). **Sem** calendário de aulas específicas nem matrícula formal nesta versão — qualquer pessoa ativa do tenant pode ser reconhecida em qualquer turma do mesmo tenant.
2. **Múltiplas janelas por turma por dia**: sim. Uma turma pode ter mais de um horário no mesmo dia (ex.: academia com a mesma modalidade de manhã e à noite).
3. **Presença parcial**: definida por corte de tempo dentro da janela (percentual configurável do tempo decorrido), não por checkpoint de entrada+saída — o sistema só recebe uma foto por evento, não há segunda foto de "saída" na v1.
4. **Vinculação de turma ao chat**: comando de vinculação explícito, análogo ao `/vincular` de tenant. O administrador cadastra a Turma (nome, janelas de horário) no painel/API e vincula o chat do Telegram a ela via comando (ex.: `/turma CODIGO`). Não há auto-criação de Turma a partir do título do chat — evita turma duplicada por erro de digitação e mantém o mesmo padrão de vinculação já usado para tenant.

## Regras de negócio explícitas

- Uma presença só é válida se: a pessoa for reconhecida acima do limiar de confiança (já existe, hoje fixo — tornar configurável por tenant é candidato ao módulo 03) **e** o chat estiver vinculado a uma Turma **e** o horário do registro cair dentro de alguma janela daquela Turma (considerando tolerância de atraso) **e** não existir presença anterior válida para a mesma pessoa + turma + janela + dia.
- Fora de qualquer janela (ou chat sem Turma vinculada): a foto não gera presença — resposta ao usuário deve deixar claro o motivo (fora do horário / turma não vinculada), não apenas silenciar.
- Dentro da tolerância de atraso configurada: presença completa.
- Depois da tolerância mas ainda dentro da janela: presença "atrasado".
- Depois do corte percentual configurado da janela: presença "parcial" em vez de completa.
- Presença manual (fora do fluxo de foto) sempre é permitida, mas exige registrar responsável, motivo e data/hora, e gera evento de auditoria (integra com módulo 04 — já é o item 02.5 do backlog).
- Deduplicação: uma única presença válida por `(tenant_id, pessoa_id, turma_id, janela_id, data)`. Nova foto da mesma pessoa na mesma janela/dia não cria presença nova — no máximo atualiza a confiança do reconhecimento (detalhe de implementação do item 02.3/02.4).

## Requisitos derivados (para o ADR do item 02.2)

- Novas entidades: `turmas` (tenant_id, nome, ativa), `turma_janelas` (turma_id, dias_semana, hora_inicio, hora_fim, tolerancia_atraso_minutos, corte_presenca_parcial_percentual), `turma_chat_telegram` (turma_id, chat_id — mesmo padrão de `tenant_chat_telegram`, com comando `/turma CODIGO`).
- `presencas` precisa referenciar `turma_id` e `turma_janela_id` (hoje só tem `turma` como texto livre) para a deduplicação e para saber qual janela validou o registro.
- Estado da presença passa a ter um status (`completa` / `atrasado` / `parcial`), não só existir/não existir.
- `RegistrarPresencaEtapa` precisa consultar a Turma vinculada ao chat e a janela vigente antes de decidir se registra, e com qual status — atualmente registra incondicionalmente.
- Fluxo do bot precisa de um novo comando `/turma CODIGO` (mesmo padrão do `/vincular` de tenant) e de uma forma de cadastrar Turma + janelas (painel ou comando administrativo — a definir no ADR/dev-frontend).

## Fora de escopo (deste item)

- Matrícula formal (vínculo obrigatório pessoa-turma) — fica para uma v2 se o negócio pedir; v1 permite qualquer pessoa ativa do tenant em qualquer turma do tenant.
- Checkpoint de saída (segunda foto) para presença parcial "de verdade" — fica para quando houver essa demanda.
- Tornar o limiar de confiança do `FaceService` configurável por tenant — candidato ao módulo 03 (Qualidade Biométrica e IA), não a este.
- Fila de revisão manual para reconhecimento abaixo do limiar — módulo 03, item 03.3.

## Status
Concluído — 100%. Decisões de produto confirmadas em 2026-07-17. Pronto para virar entrada do ADR 02.2 (arquiteto-software).
