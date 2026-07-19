# Módulo 03 — Qualidade Biométrica e IA

> Prioridade do módulo: **Alta** (horizonte 30-60 dias). Meta de produto: falso positivo <0,1%, falso negativo <2%, latência de reconhecimento <5s. Prioriza nunca marcar a pessoa errada — prefere revisão manual a erro.

---

### 03.1 — Requisitos de fila de revisão manual
- Prioridade: Alta
- Dificuldade: Média
- Status: Concluído — 100% (limiar global na v1, não por tenant — decisão consciente)
- Skills recomendadas: [[analista-requisitos]]
- Depende de: —
- Critério de aceite: Documento definindo o limiar de confiança configurável por tenant, o que acontece com uma identificação abaixo do limiar (vai para fila, quem revisa, SLA de revisão), e o formato da fila (prioridade, prazo, escalonamento se não revisado).
- Retomada: Concluído em 2026-07-19 — `docs/requisitos/qualidade-biometrica.md`. Achado do código: `FaceService` já calculava a similaridade de todo rosto, mesmo abaixo do limiar de auto-aceite (0.42), mas o dado era descartado (rosto só virava "não reconhecido"). Decisões confirmadas: (1) faixa de incerteza 0.30–0.42 vira revisão em vez de descarte; abaixo de 0.30 continua desconhecido; (2) reaproveitar a infraestrutura de revisão já construída no item 02.8 (mesma tabela, painel, regra de confirmação por perfil, prazo de 24h) em vez de mecanismo paralelo; (3) limiares ficam globais na v1, configuração por tenant é item futuro sem demanda real ainda. Próximo passo: ADR 03.2 com `arquiteto-software`.

---

### 03.2 — ADR de arquitetura de revisão manual e atualização de embeddings
- Prioridade: Alta
- Dificuldade: Alta
- Status: Concluído — 100%
- Skills recomendadas: [[arquiteto-software]]
- Depende de: 03.1
- Critério de aceite: ADR cobrindo o evento `PresencaRevisaoPendente`, o módulo de Revisão Manual, e a estratégia de atualização supervisionada de embeddings ao longo do tempo (quando reprocessar, como versionar o embedding antigo vs. novo, quem aprova).
- Retomada: Concluído em 2026-07-19 — `docs/adr/0005-revisao-unificada-e-embeddings.md`. Decisão 1: trigger de revisão unificado (`Matches.Count > 1` OU exatamente 1 rosto com similaridade em [0.30, 0.42)), sem barramento de eventos (mesma linha do ADR 0002 — `PresencaValidada`/`PresencaRevisaoPendente` da visão original ficam mapeados pro módulo 05, que não existe). Decisão 2: atualização de embedding por blend ponderado (70% atual + 30% novo), não substituição — histórico do embedding anterior mantido em tabela nova para auditoria/reversão manual. Decisão 3: NÃO atualiza quando rejeitada, quando reatribuída pra pessoa diferente da sugerida, ou quando a confiança original já era >= 0.42 (revisão só existiu por causa de múltiplos rostos, não por incerteza real).

---

### 03.3 — Implementar fila de revisão manual
- Prioridade: Alta
- Dificuldade: Alta
- Status: Concluído — 100%
- Skills recomendadas: [[dev-backend]]
- Depende de: 03.2, 01.5
- Critério de aceite: Identificações abaixo do limiar não geram presença automática — entram em fila persistida (não em memória), visível no painel para o perfil responsável revisar.
- Retomada: Concluído em 2026-07-19. `RegistrarPresencaEtapa` ganhou constante `LimiarInferiorRevisao = 0.30f` e a condição de revisão virou `revisaoPorMultiplosRostos || revisaoPorBaixaConfianca` (antes só multi-rosto). `RevisaoCriacaoItemInput` ganhou campo `Embedding` (necessário pro item 03.5), populado a partir de `match.Face.Embedding` via `EmbeddingHelper.ToBytes`. Schema: coluna `embedding BYTEA` em `revisao_faces_itens` (migração idempotente). Testado com fakes (`RegistrarPresencaEtapaTests.cs`, 4 casos: incerto cria revisão, muito baixo não cria nada, múltiplos rostos cria independente de confiança, reconhecido com alta confiança registra direto) — 19/19 testes passando no total do projeto.
- **Nota de escopo**: fila "persistida, não em memória" já era verdade desde o item 02.8 (tabelas reais no Postgres) — este item apenas estende o gatilho, não muda a persistência em si.

---

### 03.4 — Painel de revisão manual
- Prioridade: Alta
- Dificuldade: Média
- Status: Concluído — 100% (reaproveitado do item 02.8, sem mudança necessária)
- Skills recomendadas: [[dev-frontend]]
- Depende de: 03.3
- Critério de aceite: Tela onde o revisor vê a foto, o(s) candidato(s) sugerido(s) pela IA, e aprova/rejeita/reatribui a identificação.
- Retomada: Concluído em 2026-07-19 — sem trabalho novo. `Pages/Revisoes.cshtml` (item 02.8) já lista revisões pendentes, mostra confiança por item e permite aprovar/rejeitar/reatribuir, independente do motivo da revisão ter sido múltiplos rostos ou baixa confiança (o dado exibido é o mesmo: nome sugerido + confiança).

---

### 03.5 — Atualização supervisionada de embeddings
- Prioridade: Média
- Dificuldade: Alta
- Status: Concluído — 100%
- Skills recomendadas: [[dev-backend]]
- Depende de: 03.2
- Critério de aceite: Quando uma revisão manual confirma uma identificação com boa confiança em foto recente, o sistema atualiza o embedding da pessoa (com aprovação, não automaticamente sem supervisão), mantendo histórico do embedding anterior.
- Retomada: Concluído em 2026-07-19. Nova tabela `pessoa_embeddings_historico` (pessoa_id, embedding_anterior, revisao_id, motivo, substituido_em). `RevisaoPresencaRepository.ConfirmarAsync` ganhou `AtualizarEmbeddingSupervisionadoAsync`: blend ponderado 70/30 (constantes `PesoEmbeddingAtual`/`PesoEmbeddingNovo`), só executa quando `confianca < 0.42` (`LimiarAutoAceite`, mantido em sync manual com o default de `FaceService` — risco de duplicação anotado no código) e `PessoaFinalId` é nulo ou igual ao `PessoaSugeridaId` (não atualiza reatribuição pra pessoa diferente). Tudo dentro da mesma transação da confirmação. Validado com 3 testes de integração reais (`RevisaoEmbeddingTests.cs`) contra o Postgres do docker-compose: blend correto (0.7\*1.0 + 0.3\*0.0 = 0.7, conferido com precisão de 4 casas), histórico gravado com o embedding anterior certo, alta confiança não atualiza, reatribuição pra outra pessoa não mexe no embedding da sugerida errada. 19/19 testes do projeto passando.
- **Dívida técnica anotada, não resolvida agora**: o limiar `0.42` está duplicado em dois lugares (`FaceService.CompararComCadastro` e `RevisaoPresencaRepository`) — candidato a centralizar numa configuração/constante compartilhada quando o módulo tocar nisso de novo.

---

### 03.6 — Métricas de acurácia (falso positivo/negativo)
- Prioridade: Alta
- Dificuldade: Média
- Status: Não iniciado — 0%
- Skills recomendadas: [[devops-sre]], [[qa-testes]]
- Depende de: 03.3
- Critério de aceite: Métrica contínua de taxa de falso positivo/negativo por tenant, alimentando o módulo 06 (observabilidade) com alarme quando a acurácia degradar abaixo da meta (falso positivo >0,1%, falso negativo >2%).
- Retomada: —

---

### 03.7 — Testes de qualidade biométrica em condições adversas
- Prioridade: Média
- Dificuldade: Alta
- Status: Não iniciado — 0%
- Skills recomendadas: [[qa-testes]]
- Depende de: 03.3
- Critério de aceite: Casos de teste com fotos de baixa iluminação, ângulos diferentes, óculos, oclusão parcial e diferentes distâncias — validando que caem em revisão manual em vez de errar silenciosamente.
- Retomada: —
