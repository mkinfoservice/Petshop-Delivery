# Roteiro — 03: Dashboard — Indicadores

**Módulo:** Dashboard  
**Arquivo de teste:** `automation/playwright/tests/dashboard/03-indicadores.spec.ts`  
**Duração estimada do vídeo:** 1 a 1,5 minutos  
**Nível:** Iniciante

---

## Objetivo do vídeo

Apresentar os indicadores operacionais do painel principal: pedidos por status,
rotas e entregadores — mostrando como acompanhar a operação em tempo real.

---

## Pré-requisitos

- Usuário autenticado no sistema

---

## Caminho de navegação

```
/app (painel principal)
  ├── Seção Pedidos     — 6 contadores (Recebidos → Cancelados)
  ├── Seção Rotas       — 5 contadores (Criadas → Canceladas)
  └── Seção Entregadores — 3 contadores (Total, Ativos, Com rota ativa)
```

---

## Roteiro narrado (cena a cena)

### Cena 1 — Painel carregado (0:00 – 0:15)

**O que aparece na tela:**
Dashboard completo com todas as seções visíveis.

**Legenda exibida no vídeo:**
> "O painel principal exibe os indicadores operacionais em tempo real.\nAcompanhe pedidos, rotas e entregadores de uma só tela."

---

### Cena 2 — Seção Pedidos (0:15 – 0:40)

**O que aparece na tela:**
Destaque na grade de 6 cards de pedidos.

**Legenda exibida no vídeo:**
> "A seção Pedidos mostra o volume por status:\nRecebidos, Em preparo, Prontos, Saiu p/ entrega, Entregues e Cancelados."

---

### Cena 3 — Seção Rotas (0:40 – 1:00)

**O que aparece na tela:**
Scroll suave até a seção Rotas.

**Legenda exibida no vídeo:**
> "A seção Rotas mostra o andamento das entregas:\nCriadas, Atribuídas, Em andamento, Concluídas e Canceladas."

---

### Cena 4 — Seção Entregadores (1:00 – 1:20)

**O que aparece na tela:**
Cards de entregadores (Total, Ativos, Com rota ativa).

**Legenda exibida no vídeo:**
> "A seção Entregadores mostra o time disponível:\nTotal cadastrado, ativos no momento e quantos estão com rota ativa."

---

## Resultado esperado

- Viewer entende o significado de cada seção do dashboard
- Demonstração visual de todos os indicadores disponíveis

---

## Checklist de validação antes de publicar

- [ ] Dashboard carrega com dados reais (não erro)
- [ ] Cards de Pedidos, Rotas e Entregadores visíveis
- [ ] Scroll suave entre seções está visível no vídeo
- [ ] Duração entre 1 min e 1 min 30 s

---

## Arquivos relacionados

| Item | Caminho |
|---|---|
| Teste E2E | `automation/playwright/tests/dashboard/03-indicadores.spec.ts` |
| Vídeo gerado | `E:\videos-vendapps\...\video.webm` |
