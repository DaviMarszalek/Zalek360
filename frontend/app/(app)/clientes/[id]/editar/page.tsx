import { Suspense } from "react";
import type { Metadata } from "next";
import { Carregando } from "@/components/ui";
import { EditarCliente } from "@/features/clientes/EditarCliente";

export const metadata: Metadata = { title: "Editar cliente" };

export default function Pagina() {
  return <Suspense fallback={<Carregando />}><EditarCliente /></Suspense>;
}
