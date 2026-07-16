<div align="center">

# 📸 SnapCheck

*Snap the photo. Check the presence. Move on.*

[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![Status](https://img.shields.io/badge/status-em%20desenvolvimento-yellow)]()

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

## 🎯 Pra Quem É

| Segmento | Exemplo |
|----------|---------|
| 🥋 Academias de luta | Jiu-jitsu, boxe, muay thai, judô |
| 🏫 Educação | Escolas, cursinhos, reforço escolar |
| 🏢 Corporativo | Treinamentos, workshops, integração |
| 🎉 Eventos | Palestras, conferências, meetups |
| ⛪ Grupos | Religiosos, escoteiros, clubes, ONGs |

> **Qualquer lugar onde alguém reúne pessoas e precisa registrar quem estava lá.**

---

## 🧠 Fluxo Completo

### Cadastro (faz uma vez)

/comando + nome da pessoa + foto de rosto → cadastrado


### Presença (faz todo dia)

Foto da turma → reconhecimento → presença registrada → foto devolvida com nomes

### Consulta (quando quiser)

/frequencia Nome → histórico de presença
/sumidos 7 → quem não aparece há 7 dias

### Na prática

1. Professor reúne a turma
2. Puxa o celular, abre o Telegram
3. Tira uma foto do grupo e envia pro bot
4. Em segundos recebe a foto de volta com:
   - Nome sobre cada rosto reconhecido
   - Alerta de rostos não reconhecidos (cadastra na hora se quiser)
   - Presença registrada automaticamente
   - Aula segue. Zero burocracia.


---

## 🚀 Funcionalidades

### ✅ MVP — O Essencial
- [ ] Cadastro de pessoas (nome/apelido + foto de rosto)
- [ ] Reconhecimento facial em fotos de grupo
- [ ] Registro automático de presença com data e hora
- [ ] Foto processada com nomes anotados nos rostos
- [ ] Alerta para rostos não cadastrados
- [ ] Comando `/frequencia` — consulta individual
- [ ] Comando `/sumidos` — alerta de abandono

### 🔜 Versão 1.0
- [ ] Múltiplos grupos/turmas
- [ ] Relatórios automáticos (diário, semanal, mensal)
- [ ] Estatísticas e gráficos
- [ ] Exportação CSV e PDF
- [ ] Múltiplos administradores por grupo
- [ ] Dashboard web complementar

### 💡 Ideias Futuras
- [ ] Ranking de frequência
- [ ] Metas personalizadas
- [ ] Integração com sistemas de mensalidade
- [ ] Notificações automáticas (ex: "Fulano faltou 3x seguidas")
- [ ] Modo evento (check-in único)
- [ ] Suporte multilíngue

---

