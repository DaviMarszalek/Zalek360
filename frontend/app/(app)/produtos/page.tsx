import type { Metadata } from "next";
import { Produtos } from "@/features/produtos/Produtos";
export const metadata: Metadata = { title: "Produtos" };
export default function PaginaProdutos() { return <Produtos />; }
