# ADR 0005 — Revisão Unificada por Baixa Confiança e Atualização Supervisionada de Embeddings

- Status: Aceito
- Data: 2026-07-19
- Entrada: `docs/requisitos/qualidade-biometrica.md` (item 03.1)
- Item de backlog: 03.2

## Contexto
Já existe uma infraestrutura de revisão manual (item 02.8), hoje acionada só por `Matches.Count > 1`. O item 03.1 decidiu estender essa mesma infraestrutura para cobrir também fotos de um único rosto com similaridade na faixa 0.30–0.42 ("incerto"), em vez de criar um mecanismo paralelo.

## Decisão 1 — Trigger de revisão unificado, sem barramento de eventos
Mesma linha do ADR 0002: não introduzir `PresencaValidada`/`PresencaRevisaoPendente` como eventos de um barramento real (RabbitMQ é módulo 05, ainda não existe). `RegistrarPresencaEtapa` passa a decidir "vai para revisão" com base em **duas condições independentes, unidas por OR**: `Matches.Count > 1` (já existente) **ou** exatamente 1 rosto com similaridade no intervalo `[0.30, 0.42)`. Ambos os caminhos chamam o mesmo `RevisaoPresencaRepository.CriarAsync` — nenhuma tabela nova para o "motivo" da revisão; o motivo é inferível a partir dos dados já presentes (quantidade de itens e confiança de cada um).

## Decisão 2 — Atualização de embedding: blend ponderado, não substituição
Quando uma revisão é confirmada com decisão `aprovada` e resolve para uma pessoa existente (`pessoa_final_id`), o embedding da pessoa é atualizado por **média ponderada** (70% embedding atual + 30% embedding da foto confirmada), não substituição direta. Justificativa: uma única confirmação humana pode ainda assim estar sujeita a erro de julgamento ou foto atípica (ângulo ruim, iluminação); blend ponderado deixa o embedding evoluir gradualmente em vez de saltar para um único ponto de dados novo. Histórico do embedding anterior é mantido (tabela `pessoa_embeddings_historico`) para permitir auditoria e reversão manual se necessário — não há reversão automática nesta v1.

### Consequência de schema
Nova tabela `pessoa_embeddings_historico` (pessoa_id, embedding_anterior, substituido_em, revisao_id, motivo). Sem mudança em `pessoas` além do próprio campo `embedding` já existente (que passa a ser atualizado, não só lido).

## Decisão 3 — Quando NÃO atualizar embedding
Não atualiza quando: decisão é `rejeitada` (óbvio); decisão é `reatribuida` para pessoa diferente da sugerida originalmente (o embedding da pessoa sugerida errada não deve aprender com uma foto que não é dela); confiança original já era muito alta (`>= 0.42`, ou seja, já teria sido auto-aceita se fosse foto de rosto único — nesse caso a revisão só existe por causa de `Matches.Count > 1`, e o ganho de reforçar um embedding que já está bom é baixo comparado ao risco). Atualiza especificamente quando o item confirmado tinha confiança abaixo de `0.42` e decisão final aponta pra mesma pessoa sugerida (ou uma reatribuição para a pessoa correta, quando reconhecível) — é o caso de real ganho de aprendizado supervisionado.

## Itens de backlog impactados
- 03.3 (implementação) cobre a extensão do trigger de revisão.
- 03.4 (painel) não muda — mesma tela do item 02.8 já exibe confiança por item, suficiente para o revisor decidir nos dois motivos.
- 03.5 (embeddings) implementa a Decisão 2/3 deste ADR.
