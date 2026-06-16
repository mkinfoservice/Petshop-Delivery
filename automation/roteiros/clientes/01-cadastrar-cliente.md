# Roteiro — 01: Cadastrar novo cliente

**Módulo:** Clientes  
**Arquivo de teste:** `automation/playwright/tests/clientes/01-cadastrar-cliente.spec.ts`  
**Duração estimada do vídeo:** 1,5 a 2 minutos  
**Nível:** Iniciante  

---

## Objetivo do vídeo

Mostrar como cadastrar um novo cliente no vendApps a partir do dashboard,
usando o módulo de Atendimento.

---

## Roteiro narrado (cena a cena)

### Cena 1 — Dashboard (0:00 – 0:15)

**O que aparece na tela:**  
O painel principal do sistema com o menu lateral visível.

**Narração sugerida:**  
> "Este é o painel principal do vendApps. Para cadastrar um novo cliente,
> vamos acessar o módulo de Atendimento no menu lateral."

---

### Cena 2 — Módulo Atendimento (0:15 – 0:30)

**O que aparece na tela:**  
O hub de Atendimento com os atalhos disponíveis (Montar pedido, Clientes,
Novo cliente, Todos os pedidos).

**Narração sugerida:**  
> "Dentro do Atendimento, você tem acesso rápido às principais funções.
> Vamos clicar em **Novo cliente**."

---

### Cena 3 — Formulário de cadastro (0:30 – 1:00)

**O que aparece na tela:**  
Formulário com os campos: Nome *, Telefone e CPF.

**Narração sugerida:**  
> "O formulário é simples. Apenas o **Nome** é obrigatório.
> O telefone e o CPF são preenchidos automaticamente com máscara
> conforme você digita."

**Campos preenchidos no vídeo:**

| Campo | Valor demonstrado |
|---|---|
| Nome * | Ana Paula Rodrigues |
| Telefone | (21) 98765-4321 |
| CPF | 529.982.247-25 |

---

### Cena 4 — Confirmar cadastro (1:00 – 1:20)

**O que aparece na tela:**  
Formulário preenchido. O cursor vai até o botão "Cadastrar cliente".

**Narração sugerida:**  
> "Com os dados preenchidos, basta clicar em **Cadastrar cliente**.
> O sistema valida o CPF automaticamente e exibe um erro se o número
> for inválido."

---

### Cena 5 — Tela de detalhe do cliente (1:20 – 2:00)

**O que aparece na tela:**  
Tela de detalhe do cliente recém-criado com nome, telefone e CPF visíveis.

**Narração sugerida:**  
> "Pronto! O cliente foi cadastrado com sucesso. O sistema redireciona
> automaticamente para a ficha do cliente, onde você pode ver o histórico
> de pedidos, o saldo de fidelidade e editar os dados quando necessário."

---

## Checklist de validação antes de publicar

- [ ] Vídeo começa com o dashboard visível (não com tela em branco)
- [ ] Todos os campos são preenchidos com dados fictícios (não dados reais)
- [ ] A máscara de telefone e CPF aparece funcionando no vídeo
- [ ] O redirect para a tela de detalhe está visível
- [ ] O nome do cliente aparece confirmado na tela de detalhe
- [ ] Duração total está entre 1:30 e 2:30

---

## Notas técnicas

- CPF usado no vídeo (`529.982.247-25`) é um CPF de teste com dígitos válidos
- O cliente criado ficará na base de dados de `novaempresa` — apagar após gravação se necessário
- Para regravar apenas esta cena, execute:
  ```bash
  cd automation/playwright
  npx playwright test tests/clientes/01-cadastrar-cliente.spec.ts --headed
  ```
