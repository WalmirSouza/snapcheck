<div align="center">

# 📸 SnapCheck

*Snap the photo. Check the presence. Move on.*

[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)

</div>

---

## 💡 O Problema

Registrar presença ainda é um ritual analógico e falho:

- 📋 Listas de papel que molham, rasgam e somem
- ⏱️ Minutos preciosos perdidos fazendo chamada
- 😤 Alunos que esquecem de assinar
- 📊 Gestores sem dados sobre frequência real
- 💸 Sistemas caros e complexos demais pra pequenos negócios

## ✨ A Solução

**SnapCheck** transforma uma simples foto de grupo em presença registrada.

Um bot do Telegram que usa reconhecimento facial pra identificar pessoas, marcar presença e devolver a imagem com os nomes anotados. Em segundos.

📷 Foto do grupo → 🤖 Bot processa → ✅ Presença registrada

**Sem app extra. Sem hardware. Sem planilha. Sem atrito.**

---

## 🏗️ Arquitetura

```
Interface Web (ASP.NET)
        ↓
BackgroundService (Bot + Pipeline)
        ↓
Channel<Mensagem>
        ↓
Pipeline (etapas sequenciais)
        ↓
PostgreSQL (Dapper)
```

### Estrutura do projeto

```
src/SnapCheck/
├── Program.cs              # Startup + DI
├── Controllers/            # API do painel
├── Pages/                  # Interface web
├── Data/                   # PostgreSQL + Dapper
├── Bot/                    # Telegram + pipeline
├── Face/                   # Reconhecimento facial
└── Imaging/                # Anotação na foto
```

Um único projeto ASP.NET Core — sem referências entre assemblies.

- .NET 8 / C# / ASP.NET Core
- Telegram.Bot
- FaceONNX (detecção + embedding)
- SixLabors.ImageSharp (anotação)
- Dapper + PostgreSQL
- System.Threading.Channels + BackgroundService

---

## 🚀 Como Executar

### Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/) (opcional, para PostgreSQL)

### Opção 1 — Docker Compose (recomendado)

```bash
docker compose up --build
```

Acesse: **http://localhost:8080**

O PostgreSQL sobe automaticamente com:
- **Host:** `postgres` (interno) / `localhost:5432` (externo)
- **Database:** `snapcheck`
- **User/Password:** `snapcheck`

### Opção 2 — Desenvolvimento local

1. Suba apenas o PostgreSQL:

```bash
docker compose up postgres -d
```

2. Execute a aplicação:

```bash
dotnet run --project src/SnapCheck
```

3. Acesse: **http://localhost:5000**

### Configuração inicial

1. Abra o painel web
2. Informe o **Token do Bot** (obtido via [@BotFather](https://t.me/BotFather))
3. Informe a **Connection String** do PostgreSQL:
   ```
   Host=localhost;Port=5432;Database=snapcheck;Username=snapcheck;Password=snapcheck
   ```
4. Clique em **Salvar Configurações**
5. Clique em **Iniciar Bot**

---

## 🤖 Comandos do Bot

| Comando | Descrição |
|---------|-----------|
| `/start` | Mensagem de boas-vindas |
| `/cadastrar` | Fluxo de cadastro (nome + foto de rosto) |
| `/listar` | Lista pessoas cadastradas |
| `/frequencia Nome` | Consulta presença de uma pessoa |
| `/sumidos 7` | Quem não aparece há X dias |
| `/remover Nome` | Descadastra uma pessoa |
| *Enviar foto* | Registra presença e devolve imagem anotada |

---

## 🗄️ Banco de Dados

O script `src/SnapCheck/Data/Scripts/init.sql` cria automaticamente:

- `pessoas` — cadastro com embedding facial
- `presencas` — registros de presença
- `configuracoes` — token e connection string

---

## 📡 API do Painel

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| `GET` | `/api/bot/status` | Status do bot, banco e métricas |
| `POST` | `/api/bot/configuracoes` | Salva token e connection string |
| `POST` | `/api/bot/iniciar` | Inicia o bot |
| `POST` | `/api/bot/parar` | Para o bot |

---

## 🎯 Pra Quem É

| Segmento | Exemplo |
|----------|---------|
| 🥋 Academias de luta | Jiu-jitsu, boxe, muay thai, judô |
| 🏫 Educação | Escolas, cursinhos, reforço escolar |
| 🏢 Corporativo | Treinamentos, workshops, integração |
| 🎉 Eventos | Palestras, conferências, meetups |
| ⛪ Grupos | Religiosos, escoteiros, clubes, ONGs |

---

## 📄 Licença

Apache 2.0 — veja [LICENSE](LICENSE).
