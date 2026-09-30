"use client";
import Link from "next/link";
import { ArrowLeft, Bot, FileText, Mail, MapPin, MessageCircle, Package, Phone, RefreshCw, Users } from "lucide-react";
import { Selecao } from "@/components/ui";
import { cn } from "@/lib/utils";
import { rotuloMeio, siglaTipoPessoa } from "@/lib/status";
import { dataHora, documento, hojeIso, numeroDocumento, somarDias, telefone } from "@/utils/formatos";
import type { ClienteCard, HistoricoEvento, MeioContato } from "@/types/api";

export const Voltar = ({ href, children }: { href: string; children: React.ReactNode }) =>
  <Link href={href} className="inline-flex items-center gap-1 text-texto-fraco hover:text-texto"><ArrowLeft className="h-3.5 w-3.5" />{children}</Link>;

export type Periodo = "7" | "30" | "90" | "mes" | "todos";
export const opcoesPeriodo: { id: Periodo; rotulo: string }[] = [
  { id: "7", rotulo: "Últimos 7 dias" }, { id: "30", rotulo: "Últimos 30 dias" }, { id: "90", rotulo: "Últimos 90 dias" },
  { id: "mes", rotulo: "Este mês" }, { id: "todos", rotulo: "Todo o período" },
];

export function intervaloDoPeriodo(p: Periodo): { de?: string; ate?: string } {
  const hoje = hojeIso();
  if (p === "todos") return {};
  if (p === "mes") return { de: hoje.slice(0, 8) + "01", ate: hoje };
  return { de: somarDias(hoje, -Number(p) + 1), ate: hoje };
}

export function FiltroPeriodo({ valor, aoMudar, className }: { valor: Periodo; aoMudar: (p: Periodo) => void; className?: string }) {
  return (
    <Selecao aria-label="Período" value={valor} onChange={(e) => aoMudar(e.target.value as Periodo)} className={cn("w-auto min-w-[170px]", className)}>
      {opcoesPeriodo.map((o) => <option key={o.id} value={o.id}>{o.rotulo}</option>)}
    </Selecao>
  );
}

export const IconeMeio = ({ meio, className }: { meio: MeioContato | null; className?: string }) => {
  const c = cn("h-3.5 w-3.5", className);
  if (meio === "WhatsApp") return <MessageCircle className={c} />;
  if (meio === "Ligacao") return <Phone className={c} />;
  if (meio === "Email") return <Mail className={c} />;
  if (meio === "Presencial") return <Users className={c} />;
  return <RefreshCw className={c} />;
};

const corCategoria: Record<string, string> = {
  alteracao: "bg-slate-100 text-slate-600 ring-slate-200",
  contato: "bg-blue-50 text-blue-700 ring-blue-200",
  decisao: "bg-amber-50 text-amber-700 ring-amber-200",
  pedido: "bg-emerald-50 text-emerald-700 ring-emerald-200",
};

/** Linha do tempo única para orçamento, pedido e cliente (UC05). */
export function LinhaDoTempo({ eventos, mostrarDocumento, vazio = "Nenhum evento registrado." }: { eventos: HistoricoEvento[]; mostrarDocumento?: boolean; vazio?: string }) {
  if (eventos.length === 0) return <p className="py-6 text-center text-sm text-texto-fraco">{vazio}</p>;
  return (
    <ol className="relative space-y-5 before:absolute before:bottom-2 before:left-[13px] before:top-2 before:w-px before:bg-borda">
      {eventos.map((e) => (
        <li key={e.id} className="relative flex gap-3.5">
          <span className={cn("relative z-10 mt-0.5 flex h-7 w-7 shrink-0 items-center justify-center rounded-full ring-4 ring-white", corCategoria[e.categoria])}>
            {e.automatico ? <Bot className="h-3.5 w-3.5" /> : e.categoria === "contato" || e.categoria === "decisao" ? <IconeMeio meio={e.meioContato} /> : e.categoria === "pedido" ? <Package className="h-3.5 w-3.5" /> : <FileText className="h-3.5 w-3.5" />}
          </span>
          <div className="min-w-0 flex-1">
            <div className="flex flex-wrap items-baseline gap-x-2">
              <p className="text-sm font-medium text-texto">{e.titulo}</p>
              {mostrarDocumento && e.orcamentoId && e.orcamentoNumero && (
                <Link href={`/orcamentos/${e.orcamentoId}`} className="numero text-xs text-primaria hover:underline">Orç. {numeroDocumento(e.orcamentoNumero)}</Link>)}
              {mostrarDocumento && e.pedidoId && e.pedidoNumero && (
                <Link href={`/pedidos/${e.pedidoId}`} className="numero text-xs text-primaria hover:underline">Ped. {numeroDocumento(e.pedidoNumero)}</Link>)}
            </div>
            {e.descricao && <p className="mt-0.5 whitespace-pre-line text-[13px] text-texto-suave">{e.descricao}</p>}
            <p className="mt-1 text-xs text-texto-fraco">{dataHora(e.ocorridoEm)} · {e.automatico ? "Sistema (automático)" : e.autor}{e.meioContato && ` · ${rotuloMeio[e.meioContato]}`}</p>
          </div>
        </li>
      ))}
    </ol>
  );
}

export function CartaoCliente({ cliente }: { cliente: ClienteCard }) {
  return (
    <div className="space-y-3 text-sm">
      <div>
        <Link href={`/clientes/${cliente.id}`} className="font-semibold text-texto hover:text-primaria">{cliente.nome}</Link>
        <p className="numero mt-0.5 text-xs text-texto-fraco">{siglaTipoPessoa[cliente.tipoPessoa]} · {documento(cliente.documento)}</p>
      </div>
      <dl className="space-y-1.5 text-[13px] text-texto-suave">
        {cliente.contatoNome && <div className="flex items-center gap-2"><Users className="h-3.5 w-3.5 text-texto-fraco" />{cliente.contatoNome}{cliente.contatoCargo && ` · ${cliente.contatoCargo}`}</div>}
        {cliente.whatsApp && <div className="flex items-center gap-2"><MessageCircle className="h-3.5 w-3.5 text-texto-fraco" /><span className="numero">{telefone(cliente.whatsApp)}</span></div>}
        {cliente.telefone && <div className="flex items-center gap-2"><Phone className="h-3.5 w-3.5 text-texto-fraco" /><span className="numero">{telefone(cliente.telefone)}</span></div>}
        {cliente.email && <div className="flex items-center gap-2"><Mail className="h-3.5 w-3.5 text-texto-fraco" /><span className="truncate">{cliente.email}</span></div>}
        {cliente.cidade && <div className="flex items-center gap-2"><MapPin className="h-3.5 w-3.5 text-texto-fraco" />{cliente.cidade}</div>}
      </dl>
    </div>
  );
}
