import { useState, useEffect } from "react";
import { motion } from "framer-motion";
import { X, Minus, Plus, ShoppingCart } from "lucide-react";
import { useProduct } from "@/features/catalog/queries";
import { useCart } from "@/features/cart/cart";
import { useToast } from "@/components/Toast";
import { ProductAddonStepper } from "./ProductAddonStepper";
import type { Product } from "./api";

function formatBRL(cents: number) {
  return (cents / 100).toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
}

interface Props {
  productId: string;
  onClose: () => void;
}

export function ProductQuickViewModal({ productId, onClose }: Props) {
  const { data: product, isLoading } = useProduct(productId);
  const cart = useCart();
  const { showToast } = useToast();
  const [qty, setQty] = useState(1);

  useEffect(() => {
    document.body.style.overflow = "hidden";
    return () => { document.body.style.overflow = ""; };
  }, []);

  useEffect(() => {
    const handler = (e: KeyboardEvent) => { if (e.key === "Escape") onClose(); };
    document.addEventListener("keydown", handler);
    return () => document.removeEventListener("keydown", handler);
  }, [onClose]);

  function handleSimpleAdd() {
    if (!product) return;
    for (let i = 0; i < qty; i++) cart.add(product as any);
    showToast(qty > 1 ? `${qty}x ${product.name} adicionados!` : `${product.name} adicionado!`);
    onClose();
  }

  function handleStepperConfirm(synthetic: Product, confirmedQty: number) {
    for (let i = 0; i < confirmedQty; i++) cart.add(synthetic as any);
    showToast(
      confirmedQty > 1
        ? `${confirmedQty}x ${synthetic.name} adicionados!`
        : `${synthetic.name} adicionado!`
    );
    onClose();
  }

  const img = product?.imageUrl || "https://picsum.photos/seed/pet/800/600";
  const hasStepper = (product?.addonGroups?.length ?? 0) > 0 || (product?.variants?.length ?? 0) > 0;

  return (
    /* Mobile: alinha na base (bottom sheet). sm+: centralizado */
    <div className="fixed inset-0 z-50 flex items-end sm:items-center justify-center sm:p-6">
      {/* Overlay */}
      <motion.div
        className="absolute inset-0 bg-black/50"
        style={{ backdropFilter: "blur(2px)" }}
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        exit={{ opacity: 0 }}
        transition={{ duration: 0.2 }}
        onClick={onClose}
      />

      {/* Modal
          Mobile  → bottom sheet: largura total, cantos arredondados só em cima, desliza de baixo
          sm+     → modal centralizado 720px, todos os cantos arredondados */}
      <motion.div
        className="relative bg-white rounded-t-3xl sm:rounded-3xl shadow-2xl flex flex-col sm:flex-row overflow-hidden w-full sm:w-auto sm:max-w-[720px]"
        style={{ maxHeight: "92dvh" }}
        initial={{ opacity: 0, y: 48 }}
        animate={{ opacity: 1, y: 0 }}
        exit={{ opacity: 0, y: 48 }}
        transition={{ duration: 0.28, ease: [0.25, 0.46, 0.45, 0.94] }}
        onClick={(e) => e.stopPropagation()}
      >
        {/* Botão fechar */}
        <button
          type="button"
          onClick={onClose}
          className="absolute top-3 right-3 z-10 w-8 h-8 rounded-full bg-white/90 shadow-sm flex items-center justify-center hover:bg-gray-100 active:scale-95 transition-all"
          aria-label="Fechar"
        >
          <X className="w-4 h-4 text-gray-600" />
        </button>

        {/* Skeleton */}
        {isLoading && (
          <div className="flex flex-col sm:flex-row w-full animate-pulse" style={{ minHeight: "300px" }}>
            <div className="w-full h-52 sm:w-[280px] sm:h-auto shrink-0 bg-gray-200" />
            <div className="flex-1 p-6 space-y-4">
              <div className="h-6 bg-gray-200 rounded w-3/4 mt-8" />
              <div className="h-4 bg-gray-200 rounded w-1/2" />
              <div className="mt-4 h-8 bg-gray-200 rounded w-28" />
              <div className="mt-6 space-y-3">
                <div className="h-px bg-gray-200" />
                <div className="h-12 bg-gray-200 rounded-2xl" />
                <div className="h-12 bg-gray-200 rounded-2xl" />
              </div>
            </div>
          </div>
        )}

        {/* Not found */}
        {!isLoading && !product && (
          <div className="flex-1 flex items-center justify-center p-12">
            <p className="text-gray-400 font-semibold">Produto não encontrado.</p>
          </div>
        )}

        {/* Conteúdo */}
        {!isLoading && product && (
          <>
            {/* Imagem
                Mobile  → largura total, altura fixa 220px (acima do dobra)
                sm+     → coluna lateral 280px, altura 100% */}
            <div className="w-full h-[220px] sm:w-[280px] sm:h-auto shrink-0 bg-gray-100 overflow-hidden">
              <img src={img} alt={product.name} className="w-full h-full object-cover" />
            </div>

            {/* Painel de conteúdo */}
            <div className="flex-1 flex flex-col min-w-0 overflow-hidden">
              {hasStepper ? (
                <>
                  {/* Cabeçalho fixo com nome + preço base */}
                  <div className="px-5 pt-4 pb-3 border-b border-gray-100 shrink-0">
                    <h2 className="font-bold text-lg text-gray-900 leading-tight pr-10">
                      {product.name}
                    </h2>
                    <p className="text-xs text-gray-400 mt-0.5">
                      a partir de{" "}
                      <span className="font-bold" style={{ color: "var(--brand)" }}>
                        {formatBRL(product.priceCents)}
                      </span>
                    </p>
                  </div>
                  <ProductAddonStepper
                    product={product}
                    onConfirm={handleStepperConfirm}
                    onCancel={onClose}
                  />
                </>
              ) : (
                <>
                  <div className="flex-1 overflow-y-auto p-6 pb-4">
                    <h2 className="font-bold text-xl text-gray-900 leading-tight pr-10">
                      {product.name}
                    </h2>

                    <div className="mt-5 flex items-end justify-between gap-4">
                      <div>
                        <p className="text-xs text-gray-400 mb-1">Preço</p>
                        <span className="text-2xl font-black tabular-nums" style={{ color: "var(--brand)" }}>
                          {formatBRL(product.priceCents)}
                        </span>
                      </div>

                      <div className="shrink-0">
                        <p className="text-xs text-gray-400 mb-1 text-right">Quantidade</p>
                        <div className="flex items-center gap-2">
                          <button
                            type="button"
                            onClick={() => setQty((q) => Math.max(1, q - 1))}
                            className="w-8 h-8 rounded-full bg-gray-100 hover:bg-gray-200 flex items-center justify-center active:scale-95 transition-all"
                            aria-label="Diminuir"
                          >
                            <Minus className="w-3.5 h-3.5 text-gray-600" />
                          </button>
                          <span className="w-6 text-center text-sm font-bold text-gray-900 tabular-nums select-none">
                            {qty}
                          </span>
                          <button
                            type="button"
                            onClick={() => setQty((q) => q + 1)}
                            className="w-8 h-8 rounded-full text-white flex items-center justify-center hover:brightness-110 active:scale-95 transition-all"
                            style={{ background: "var(--brand)" }}
                            aria-label="Aumentar"
                          >
                            <Plus className="w-3.5 h-3.5" />
                          </button>
                        </div>
                      </div>
                    </div>

                    <div className="mt-5 divide-y divide-gray-100 border-t border-gray-100">
                      <div className="py-3 flex items-center justify-between text-sm">
                        <span className="text-gray-400">Categoria</span>
                        <span className="font-semibold text-gray-900">
                          {product.category?.name ?? "—"}
                        </span>
                      </div>
                      <div className="py-3 flex items-center justify-between text-sm">
                        <span className="text-gray-400">Entrega</span>
                        <span className="font-semibold text-green-500">Entrega rápida disponível</span>
                      </div>
                    </div>
                  </div>

                  <div className="px-6 py-4 border-t border-gray-100 bg-white shrink-0">
                    <button
                      type="button"
                      onClick={handleSimpleAdd}
                      className="w-full h-12 rounded-2xl font-black text-base text-white flex items-center justify-center gap-2 hover:brightness-110 active:scale-[0.99] transition"
                      style={{ background: "var(--brand)" }}
                    >
                      <ShoppingCart className="w-5 h-5" />
                      Adicionar ao Carrinho
                    </button>
                  </div>
                </>
              )}
            </div>
          </>
        )}
      </motion.div>
    </div>
  );
}
