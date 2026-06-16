# Roteiro — 05: Editar cliente

**Módulo:** Clientes  
**Arquivo de teste:** `automation/playwright/tests/clientes/05-editar-cliente.spec.ts`  
**Duração estimada do vídeo:** 1 a 1,5 minutos  
**Nível:** Intermediário

---

## Objetivo do vídeo

Mostrar como editar os dados de um cliente já cadastrado:
acessar a ficha, clicar em editar, alterar o telefone e salvar.

---

## Caminho de navegação

```
/app/atendimento/clientes → ficha do cliente → editar → salvar
```

---

## Dados usados no vídeo

| Campo | Valor |
|---|---|
| Nome | Carlos Eduardo Lima |
| Telefone (original) | gerado dinamicamente |
| Telefone (editado) | gerado dinamicamente |

---

## Roteiro narrado (cena a cena)

### Cena 1 — Ficha do cliente (0:00 – 0:20)
**Legenda:** "Abra a ficha de um cliente para ver seus dados e histórico."

### Cena 2 — Botão Editar (0:20 – 0:35)
**Legenda:** "Clique em Editar para alterar os dados do cadastro."

### Cena 3 — Alterando o telefone (0:35 – 0:55)
**Legenda:** "Altere o campo desejado e confirme.\nTodos os campos seguem a mesma máscara do cadastro."

### Cena 4 — Salvando (0:55 – 1:20)
**Legenda:** "Clique em Salvar para confirmar as alterações.\nO sistema atualiza a ficha imediatamente."

---

## Observações técnicas

- Um cliente de teste é criado via API no `beforeAll` e removido no `afterAll`
- Isso garante que o vídeo sempre mostra dados controlados

---

## Checklist
- [ ] Ficha do cliente carrega com nome no h1
- [ ] Botão de editar é visível e clicável
- [ ] Alteração do campo é visível no vídeo
- [ ] Confirmação de sucesso aparece após salvar
- [ ] Dado atualizado aparece na ficha após redirect/reload
