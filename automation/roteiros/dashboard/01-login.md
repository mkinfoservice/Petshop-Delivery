# Roteiro — 01: Login no sistema

**Módulo:** Dashboard / Acesso  
**Arquivo de teste:** `automation/playwright/tests/dashboard/01-login.spec.ts`  
**Duração estimada do vídeo:** 45 s a 1 minuto  
**Nível:** Iniciante

---

## Objetivo do vídeo

Mostrar como acessar o vendApps pela primeira vez, preenchendo usuário e senha
na tela de login e chegando ao painel principal.

---

## Pré-requisitos

- Ter o endereço do sistema (ex: `suaempresa.vendapps.com.br`)
- Ter usuário e senha cadastrados por um administrador

---

## Caminho de navegação

```
/ (raiz) → /login → /app (painel principal)
```

---

## Roteiro narrado (cena a cena)

### Cena 1 — Tela de login (0:00 – 0:15)

**O que aparece na tela:**
Página de login do vendApps com campos de usuário e senha.

**Legenda exibida no vídeo:**
> "Para acessar o vendApps, informe seu usuário e senha."

**Narração sugerida:**
> "Para entrar no vendApps, acesse o endereço do seu sistema no navegador.
> Você verá a tela de login com os campos de usuário e senha."

---

### Cena 2 — Preencher usuário (0:15 – 0:25)

**O que aparece na tela:**
Cursor clica no campo de usuário; seta indicadora aparece; campo é preenchido.

**Legenda exibida no vídeo:**
> "Digite seu usuário de acesso no campo abaixo."

**Narração sugerida:**
> "Clique no campo usuário e digite seu login."

---

### Cena 3 — Preencher senha (0:25 – 0:35)

**O que aparece na tela:**
Cursor clica no campo de senha; campo exibe `••••••••` enquanto é preenchido.

**Legenda exibida no vídeo:**
> "Informe sua senha.\nOs caracteres são ocultados por segurança."

**Narração sugerida:**
> "No campo senha, os caracteres são ocultos automaticamente."

---

### Cena 4 — Confirmar login (0:35 – 0:45)

**O que aparece na tela:**
Cursor vai até o botão "Entrar"; seta e contorno roxo aparecem sobre o botão;
clique com ripple duplo.

**Legenda exibida no vídeo:**
> "Clique em Entrar para acessar o sistema."

**Narração sugerida:**
> "Com os dados preenchidos, clique em Entrar."

---

### Cena 5 — Painel principal (0:45 – 1:00)

**O que aparece na tela:**
Dashboard do vendApps carregado com menu lateral e indicadores visíveis.

**Legenda exibida no vídeo:**
> "Acesso realizado com sucesso!\nBem-vindo ao painel principal do vendApps."

**Narração sugerida:**
> "Pronto! Você está no painel principal do vendApps, onde tem acesso
> a todos os módulos do sistema."

---

## Resultado esperado

- URL final: `/app` ou `/admin`
- Painel principal visível com menu lateral

---

## Observações

- A senha nunca aparece em texto claro no vídeo (campo do tipo `password`)
- Use credenciais de uma conta de demonstração — nunca exponha dados reais
- Este vídeo usa sessão limpa (sem storageState) para mostrar o login real

---

## Checklist de validação antes de publicar

- [ ] Vídeo começa na tela de login (não no dashboard)
- [ ] Os dois campos são preenchidos com cursor visível
- [ ] O clique no botão Entrar mostra ripple + seta
- [ ] O redirecionamento para o dashboard é visível
- [ ] Duração total entre 45 s e 1 min 15 s

---

## Arquivos relacionados

| Item | Caminho |
|---|---|
| Teste E2E | `automation/playwright/tests/dashboard/01-login.spec.ts` |
| Vídeo gerado | `E:\videos-vendapps\...\video.webm` |
