# Módulo 00 — Núcleo MVP (baseline já entregue)

> Este módulo documenta o que já existe e funciona hoje. Serve de referência para os demais módulos não reimplementarem o que já está pronto. Não é backlog de trabalho — não há itens "a fazer" aqui, apenas baseline.

## O que já está funcional
- Cadastro de pessoa por foto — `CadastroHandler.cs`
- Pipeline assíncrono em etapas (download → comparação → registro → anotação → resposta) — `PipelineService.cs`
- Reconhecimento facial — `FaceService.cs`
- Registro de presença — `RegistrarPresencaEtapa.cs`
- Painel web básico (token, banco, iniciar/parar bot, métricas simples) — `BotController.cs`
- Persistência PostgreSQL via Dapper (pessoas, presenças, configurações) — `init.sql`, `PessoaRepository.cs`, `PresencaRepository.cs`
- Consulta de presença — `ConsultaHandler.cs`
- Injeção de dependência / composição do serviço — `ServiceCollectionExtensions.cs`

## Limitações conhecidas (motivo de existirem os módulos 01-10)
- Sem multi-tenant (`tenant_id` ausente em todas as tabelas) → módulo 01
- Presença registrada sem janela de aula, matrícula, deduplicação forte → módulo 02
- Sem revisão humana para baixa confiança, sem atualização de embeddings → módulo 03
- Sem consentimento, retenção, auditoria imutável, RBAC → módulo 04
- Fila em memória (`Channel`), sem retry/DLQ/circuit breaker → módulo 05
- Sem observabilidade distribuída, SLO, alarmes → módulo 06
- Painel sem dashboards gerenciais (frequência, evasão, exportações) → módulo 07
- Sem API pública, webhooks, integrações externas → módulo 08
- Sem app mobile → módulo 09
- Sem cobertura de teste formal nem pipeline de CI/CD → módulo 10

## Status
Concluído — 100% (é o ponto de partida, não avança mais por conta própria; qualquer mudança aqui passa a ser rastreada no módulo correspondente).
