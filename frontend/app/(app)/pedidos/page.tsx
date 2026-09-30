import { Suspense } from "react";
import type { Metadata } from "next";
import { Carregando } from "@/components/ui";
import { ListaPedidos } from "@/features/pedidos/ListaPedidos";

export const metadata: Metadata = { title: "Pedidos" };

export default function Pagina() {
  return <Suspense fallback={<Carregando />}><ListaPedidos /></Suspense>;
}
