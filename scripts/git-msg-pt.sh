#!/bin/sh

msg="$(cat)"

case "$GIT_COMMIT" in
  4708a4e*)
    echo "feat(whatsapp): enviar mensagem complementar de fidelidade apos entrega com deduplicacao segura"
    ;;
  ea64240*)
    echo "fix(charset): corrigir dupla codificacao UTF-8 em PdvController e AdminProductsController"
    ;;
  96c4ea4*)
    echo "fix(fidelidade): exibir codigo do cupom apos resgate com botao de copiar"
    ;;
  6a45701*)
    echo "feat(fidelidade,pdv): recompensas resgataveis com modal de cupom no PDV"
    ;;
  b3efb67*)
    echo "feat(pdv): barra lateral em 2 colunas e rolagem independente no grid de produtos"
    ;;
  8309580*)
    echo "feat(ui): refinar rotulos da barra lateral de categorias e expandir para atendimento"
    ;;
  752e7b1*)
    echo "feat(ui): substituir chips de categorias por barra lateral com icones no PDV e catalogo"
    ;;
  db8520b*)
    echo "fix(ui): ocultar codigos de cupom antes do resgate e corrigir codificacao de texto no PDV"
    ;;
  43c8c85*)
    echo "feat(fidelidade,pdv): portal de recompensas por tenant com UI premium e pontos confirmados por CPF"
    ;;
  81933b3*)
    echo "feat(clientes): reforcar mascaramento de CPF LGPD com visibilidade por perfil"
    ;;
  4f144c9*)
    echo "feat(fidelidade): adicionar portal publico de pontos, resgate seguro e CTA no WhatsApp"
    ;;
  749b7db*)
    echo "feat(seguranca): conformidade LGPD, cupom no checkout e seletor de cliente no PDV"
    ;;
  f6f6db6*)
    echo "seguranca: reforco de arquitetura"
    ;;
  a2dd9a5*)
    echo "feat(promocoes): ampliar cupom/desconto no admin e aplicar no PDV por itens"
    ;;
  af2a6d9*)
    echo "fix(compilacao): remover import getCupom nao utilizado"
    ;;
  *)
    printf "%s" "$msg"
    ;;
esac
