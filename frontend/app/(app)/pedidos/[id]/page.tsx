import { Suspense } from "react";
import type { Metadata } from "next";
import { Carregando } from "@/components/ui";
import { DetalhePedido } from "@/features/pedidos/DetalhePedido";

export const metadata: Metadata = { title: "Pedido" };

export default function Pagina() {
  return <Suspense fallback={<Carregando />}><DetalhePedido /></Suspense>;
}
