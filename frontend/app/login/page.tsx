import { Suspense } from "react";
import type { Metadata } from "next";
import { TelaLogin } from "@/features/auth/TelaLogin";
export const metadata: Metadata = { title: "Entrar" };
export default function PaginaLogin() { return <Suspense><TelaLogin /></Suspense>; }
