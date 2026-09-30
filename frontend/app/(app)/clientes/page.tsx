import { Suspense } from "react";
import type { Metadata } from "next";
import { Carregando } from "@/components/ui";
import { ListaClientes } from "@/features/clientes/ListaClientes";

export const metadata: Metadata = { title: "Clientes" };

export default function Pagina() {
  return <Suspense fallback={<Carregando />}><ListaClientes /></Suspense>;
}
