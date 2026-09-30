"use client";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { ClipboardList, FileText, LayoutDashboard, LogOut, Menu, Package, Users, X } from "lucide-react";
import { cn } from "@/lib/utils";
import { useUsuarioAtual } from "@/hooks";
import { authService } from "@/services";
import { Carregando } from "@/components/ui";

const menu = [
  { href: "/dashboard", rotulo: "Dashboard", icone: LayoutDashboard },
  { href: "/clientes", rotulo: "Clientes", icone: Users },
  { href: "/orcamentos", rotulo: "Orçamentos", icone: FileText },
  { href: "/pedidos", rotulo: "Pedidos", icone: Package },
];

export function Marca({ clara = true }: { clara?: boolean }) {
  return (
    <div className="flex items-center gap-2.5">
      <div className="flex h-8 w-8 items-center justify-center rounded-md bg-primaria text-sm font-bold text-white">Z</div>
      <div className="leading-tight">
        <p className={cn("text-[15px] font-semibold tracking-tight", clara ? "text-white" : "text-texto")}>Zalek360</p>
        <p className={cn("text-[11px]", clara ? "text-lateral-texto" : "text-texto-fraco")}>Gestão comercial</p>
      </div>
    </div>
  );
}

export function AppShell({ children }: { children: React.ReactNode }) {
  const caminho = usePathname();
  const router = useRouter();
  const qc = useQueryClient();
  const { data: usuario, isLoading, isError } = useUsuarioAtual();
  const [menuAberto, setMenuAberto] = useState(false);

  async function sair() {
    try { await authService.logout(); } finally { qc.clear(); router.replace("/login"); }
  }

  if (isError) {
    if (typeof window !== "undefined") router.replace(`/login?redirect=${encodeURIComponent(caminho)}`);
    return <Carregando texto="Redirecionando para o login..." />;
  }

  const lateral = (
    <aside className="flex h-full w-60 flex-col bg-lateral text-lateral-texto">
      <div className="flex h-16 items-center justify-between px-5"><Marca />
        <button className="lg:hidden" onClick={() => setMenuAberto(false)} aria-label="Fechar menu"><X className="h-5 w-5" /></button>
      </div>
      <nav className="mt-2 flex-1 space-y-0.5 px-3" aria-label="Menu principal">
        {menu.map(({ href, rotulo, icone: Icone }) => {
          const ativo = caminho === href || caminho.startsWith(href + "/");
          return (
            <Link key={href} href={href} onClick={() => setMenuAberto(false)}
              className={cn("relative flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors",
                ativo ? "bg-lateral-ativo text-white" : "hover:bg-lateral-ativo/60 hover:text-white")}>
              {ativo && <span className="absolute left-0 top-1.5 h-[calc(100%-12px)] w-[3px] rounded-r bg-primaria" />}
              <Icone className="h-4 w-4" />{rotulo}
            </Link>
          );
        })}
      </nav>
      <div className="border-t border-lateral-borda p-3">
        {usuario && (
          <div className="mb-2 flex items-center gap-2.5 px-2 py-1.5">
            <div className="flex h-8 w-8 items-center justify-center rounded-full bg-lateral-ativo text-xs font-semibold text-white">{usuario.iniciais}</div>
            <div className="min-w-0 leading-tight"><p className="truncate text-[13px] font-medium text-white">{usuario.nome}</p><p className="text-[11px]">{usuario.perfil}</p></div>
          </div>
        )}
        <button onClick={sair} className="flex w-full items-center gap-3 rounded-md px-3 py-2 text-sm hover:bg-lateral-ativo hover:text-white">
          <LogOut className="h-4 w-4" />Sair
        </button>
        <p className="mt-3 px-3 text-[11px] leading-snug text-slate-500">Zalek Personalizados · Ambiente interno · colaboradores</p>
      </div>
    </aside>
  );

  return (
    <div className="flex min-h-screen">
      <div className="sticky top-0 hidden h-screen shrink-0 lg:block">{lateral}</div>
      {menuAberto && (
        <div className="fixed inset-0 z-40 lg:hidden">
          <div className="absolute inset-0 bg-slate-950/50" onClick={() => setMenuAberto(false)} />
          <div className="relative h-full w-60">{lateral}</div>
        </div>
      )}
      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-30 flex h-14 items-center gap-3 border-b border-borda bg-white px-4 lg:hidden">
          <button onClick={() => setMenuAberto(true)} aria-label="Abrir menu"><Menu className="h-5 w-5" /></button>
          <Marca clara={false} />
        </header>
        <main className="mx-auto w-full max-w-[1280px] flex-1 px-4 py-6 sm:px-6 lg:px-8 lg:py-8">
          {isLoading ? <Carregando /> : children}
        </main>
      </div>
    </div>
  );
}

export { ClipboardList };
