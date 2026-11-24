import type { Metadata } from "next";
import { Configuracoes } from "@/features/configuracoes/Configuracoes";
export const metadata: Metadata = { title: "Configurações" };
export default function PaginaConfiguracoes() { return <Configuracoes />; }
