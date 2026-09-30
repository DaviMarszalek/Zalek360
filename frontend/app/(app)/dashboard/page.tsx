import { Suspense } from "react";
import type { Metadata } from "next";
import { Carregando } from "@/components/ui";
import { Dashboard } from "@/features/dashboard/Dashboard";

export const metadata: Metadata = { title: "Dashboard" };

export default function Pagina() {
  return <Suspense fallback={<Carregando />}><Dashboard /></Suspense>;
}
