# Requisitos — Qualidade Biométrica e Revisão por Baixa Confiança (item de backlog 03.1)

## Contexto (estado atual verificado no código)

- `FaceService.CompararComCadastro` já calcula a similaridade de **todo** rosto detectado, mesmo quando fica abaixo do limiar de auto-aceite (`0.42f`, hoje hardcoded, não configurável por tenant). Quando abaixo do limiar, o `FaceMatch` fica com `PessoaId = null` (`Reconhecido = false`) mas `Similaridade` já carrega o score real — o dado já existe, só não é aproveitado.
- Hoje, qualquer rosto não reconhecido (independente do quão perto do limiar) é reportado como "não reconhecido" (`EnviarRespostaEtapa`) e descartado — sem fila de revisão, sem histórico.
- Já existe uma infraestrutura de revisão manual completa (item 02.8): `revisoes_presenca_grupo`, `revisao_faces_itens`, `evidencias_foto_grupo`, `revisao_presenca_auditoria`, `RevisaoPresencaRepository`, `RevisoesPresencaController` (`/api/revisoes-presenca`), painel web (`Pages/Revisoes.cshtml`). Hoje só é acionada quando `Matches.Count > 1` (mais de um rosto na foto) — não é acionada por baixa confiança em foto de um único rosto.

## Decisões de produto (confirmadas pelo usuário em 2026-07-19)

1. **Faixa de incerteza**: rostos com similaridade entre **0.30 e 0.42** passam a ser tratados como candidato incerto (em vez de simplesmente "não reconhecido") e vão para revisão humana. Abaixo de 0.30 continua sendo tratado como desconhecido — baixo demais para valer o esforço de revisar, mesmo comportamento de hoje (relatado como "não reconhecido", oferece cadastrar).
2. **Reaproveitar a infraestrutura de revisão existente** (item 02.8) em vez de criar um mecanismo paralelo — mesma tabela, mesmo painel, mesma regra de confirmação por perfil (`professor`/`coordenador`), mesmo prazo de expiração (24h). Um único conceito de "revisão pendente" cobre os dois motivos (múltiplos rostos E baixa confiança), diferenciados só pelo dado que cada item carrega.
3. **Limiares globais na v1**: `0.42` (auto-aceite) e `0.30` (piso da faixa de revisão) ficam como configuração global da aplicação, não por tenant — configuração por tenant é item futuro, sem demanda real ainda.

## Regras de negócio explícitas

- Similaridade `>= 0.42`: presença automática (comportamento já existente, sem mudança).
- Similaridade `>= 0.30 e < 0.42`: vira item de revisão pendente (mesmo mecanismo do item 02.8) — revisor decide aprovar (efetiva presença), rejeitar, ou reatribuir para outra pessoa.
- Similaridade `< 0.30`: continua sendo tratado como desconhecido, sem revisão — reportado como hoje ("rosto não reconhecido", oferece cadastrar).
- Foto com múltiplos rostos onde pelo menos um está na faixa de incerteza: já cai em revisão de qualquer forma (regra do item 02.8, `Matches.Count > 1`); a mudança deste item afeta especificamente **fotos de um único rosto** na faixa 0.30–0.42, que hoje não geram revisão nenhuma.

## Requisitos derivados (para dev-backend, item 03.3)

- `RegistrarPresencaEtapa` (ou uma nova etapa dedicada) passa a verificar, para fotos com exatamente 1 rosto: se a similaridade cai na faixa 0.30–0.42, cria revisão via `RevisaoPresencaRepository.CriarAsync` (mesmo caminho já usado para `Matches.Count > 1`), em vez de tratar como não reconhecido.
- Consolidar a condição de "vai para revisão" para cobrir ambos os motivos (múltiplos rostos OU pelo menos um rosto na faixa de incerteza), não só `Matches.Count > 1`.
- Sem mudança de schema — a infraestrutura de revisão já suporta o que é necessário (`confianca` já é campo de `revisao_faces_itens`).

## Fora de escopo (deste item)
- Atualização supervisionada de embeddings pós-confirmação (item 03.5, próximo).
- Métricas de acurácia/alarme (item 03.6, depende de observabilidade do módulo 06).
- Limiares configuráveis por tenant (adiado conscientemente, ver decisão 3).
- Testes de condições adversas (item 03.7).

## Status
Concluído — 100%. Decisões de produto confirmadas em 2026-07-19. Pronto para virar entrada do ADR 03.2 (arquiteto-software) e implementação (03.3).
