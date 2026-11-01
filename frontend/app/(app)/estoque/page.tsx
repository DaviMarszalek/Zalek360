import type { Metadata } from "next";
import { Estoque } from "@/features/estoque/Estoque";

export const metadata: Metadata = { title: "Estoque" };

export default function PaginaEstoque() {
  return <Estoque />;
}
