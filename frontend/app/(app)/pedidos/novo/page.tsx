import { Suspense } from "react";
import type { Metadata } from "next";
import { Carregando } from "@/components/ui";
import { PedidoManualBloqueado } from "@/features/pedidos/DetalhePedido";

export const metadata: Metadata = { title: "Novo pedido" };

export default function Pagina() {
  return <Suspense fallback={<Carregando />}><PedidoManualBloqueado /></Suspense>;
}
