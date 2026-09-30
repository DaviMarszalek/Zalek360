"use client";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { ArrowRight, CheckCircle2, Clock, FileText, Package, Plus, UserPlus } from "lucide-react";
import { BadgeOrcamento, Botao, CabecalhoPagina, Cartao, ErroCarregamento, Esqueleto, EstadoVazio } from "@/components/ui";
import { dashboardService } from "@/services";
import { useUsuarioAtual } from "@/hooks";
import { data, moeda, numeroDocumento, relativo } from "@/utils/formatos";
import { erroDaApi } from "@/lib/api";

function saudacao() {
  const h = Number(new Intl.DateTimeFormat("pt-BR", { timeZone: "America/Sao_Paulo", hour: "numeric", hour12: false }).format(new Date()));
  return h < 12 ? "Bom dia" : h < 18 ? "Boa tarde" : "Boa noite";
}

function Indicador({ titulo, valor, detalhe, icone, href }: { titulo: string; valor: React.ReactNode; detalhe: React.ReactNode; icone: React.ReactNode; href: string }) {
  return (
    <Link href={href} className="cartao group block p-5 transition-colors hover:border-primaria-borda">
      <div className="flex items-center justify-between text-texto-fraco"><p className="text-[13px] font-medium">{titulo}</p>{icone}</div>
      <p className="numero mt-3 text-[28px] font-semibold leading-none text-texto">{valor}</p>
      <p className="mt-2 text-xs text-texto-fraco">{detalhe}</p>
    </Link>
  );
}

export function Dashboard() {
  const { data: usuario } = useUsuarioAtual();
  const q = useQuery({ queryKey: ["dashboard"], queryFn: dashboardService.obter, refetchInterval: 60_000 });
  const d = q.data;
  const hoje = new Intl.DateTimeFormat("pt-BR", { timeZone: "America/Sao_Paulo", weekday: "long", day: "numeric", month: "long", year: "numeric" }).format(new Date());

  return (
    <>
      <CabecalhoPagina titulo={`${saudacao()}${usuario ? `, ${usuario.nome.split(" ")[0]}` : ""}`} subtitulo={<span className="capitalize">{hoje}</span>}
        acoes={<>
          <Link href="/clientes/novo"><Botao variante="secundaria" icone={<UserPlus className="h-4 w-4" />}>Novo cliente</Botao></Link>
          <Link href="/orcamentos/novo"><Botao icone={<Plus className="h-4 w-4" />}>Novo orçamento</Botao></Link>
        </>} />
      {q.isError && <ErroCarregamento mensagem={erroDaApi(q.error).message} aoTentar={() => q.refetch()} />}
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {!d ? Array.from({ length: 4 }).map((_, i) => <Esqueleto key={i} className="h-[124px]" />) : <>
          <Indicador titulo="Orçamentos em elaboração" valor={d.indicadores.orcamentosEmElaboracao} icone={<FileText className="h-4 w-4" />} href="/orcamentos?status=EmElaboracao"
            detalhe={`${d.indicadores.orcamentosCriadosHoje} ${d.indicadores.orcamentosCriadosHoje === 1 ? "criado" : "criados"} hoje`} />
          <Indicador titulo="Aguardando retorno" valor={d.indicadores.aguardandoRetorno} icone={<Clock className="h-4 w-4" />} href="/orcamentos?status=AguardandoRetorno"
            detalhe={d.indicadores.aguardandoVencemEm3Dias > 0 ? <span className="text-amber-700">{d.indicadores.aguardandoVencemEm3Dias} {d.indicadores.aguardandoVencemEm3Dias === 1 ? "vence" : "vencem"} em até 3 dias</span> : "Nenhum vencendo nos próximos 3 dias"} />
          <Indicador titulo={`Aprovados em ${d.indicadores.mesReferencia}`} valor={d.indicadores.aprovadosNoMes} icone={<CheckCircle2 className="h-4 w-4" />} href="/orcamentos?status=Aprovado&periodo=mes"
            detalhe={`${moeda(d.indicadores.valorAprovadoNoMes)} aprovados`} />
          <Indicador titulo="Pedidos em andamento" valor={d.indicadores.pedidosEmAndamento} icone={<Package className="h-4 w-4" />} href="/pedidos"
            detalhe={`${d.indicadores.pedidosConcluidosNoMes} ${d.indicadores.pedidosConcluidosNoMes === 1 ? "entregue" : "entregues"} no mês`} />
        </>}
      </div>

      <div className="mt-6 grid gap-6 xl:grid-cols-[1fr_380px]">
        <Cartao titulo="Orçamentos recentes" semPadding acao={<Link href="/orcamentos" className="text-[13px] font-medium text-primaria hover:underline">Ver todos</Link>}>
          {!d ? <div className="space-y-2 p-5">{Array.from({ length: 5 }).map((_, i) => <Esqueleto key={i} className="h-9" />)}</div>
            : d.orcamentosRecentes.length === 0 ? <EstadoVazio titulo="Nenhum orçamento ainda" descricao="Crie o primeiro orçamento para um cliente cadastrado." />
              : <div className="overflow-x-auto"><table className="tabela w-full min-w-[560px] text-sm">
                <thead><tr><th>Número</th><th>Cliente</th><th>Data</th><th className="text-right">Valor</th><th>Status</th></tr></thead>
                <tbody>{d.orcamentosRecentes.map((o) => (
                  <tr key={o.id} className="hover:bg-slate-50/60">
                    <td><Link href={`/orcamentos/${o.id}`} className="numero font-medium text-primaria hover:underline">{numeroDocumento(o.numero)}</Link></td>
                    <td className="max-w-[220px] truncate">{o.cliente}</td>
                    <td className="numero text-texto-suave">{data(o.data)}</td>
                    <td className="numero text-right">{moeda(o.valorTotal)}</td>
                    <td><BadgeOrcamento situacao={o.situacao} /></td>
                  </tr>))}</tbody></table></div>}
        </Cartao>

        <Cartao titulo={<span>Aguardando retorno {d && <span className="numero ml-1 text-texto-fraco">{d.totalAguardandoRetorno}</span>}</span>} semPadding
          acao={<Link href="/orcamentos?status=AguardandoRetorno" className="text-[13px] font-medium text-primaria hover:underline">Ver todos</Link>}>
          {!d ? <div className="space-y-2 p-5">{Array.from({ length: 3 }).map((_, i) => <Esqueleto key={i} className="h-14" />)}</div>
            : d.aguardandoRetorno.length === 0 ? <p className="px-5 py-8 text-center text-sm text-texto-fraco">Nenhuma proposta aguardando retorno.</p>
              : <ul className="divide-y divide-borda">{d.aguardandoRetorno.map((a) => (
                <li key={a.id}><Link href={`/orcamentos/${a.id}`} className="flex items-center gap-3 px-5 py-3.5 hover:bg-slate-50/60">
                  <div className="min-w-0 flex-1">
                    <p className="truncate text-sm font-medium"><span className="numero text-primaria">{numeroDocumento(a.numero)}</span> · {a.cliente}</p>
                    <p className="mt-0.5 text-xs text-texto-fraco">{a.apresentadoEm ? `Apresentado ${relativo(a.apresentadoEm)}` : "Apresentado"} · <span className={a.diasParaVencer <= 1 ? "font-medium text-red-600" : a.diasParaVencer <= 3 ? "text-amber-700" : ""}>
                      {a.diasParaVencer < 0 ? "vencido" : a.diasParaVencer === 0 ? "vence hoje" : a.diasParaVencer === 1 ? "vence amanhã" : `vence em ${a.diasParaVencer} dias`}</span></p>
                  </div>
                  <span className="numero text-sm">{moeda(a.valorTotal)}</span><ArrowRight className="h-4 w-4 text-texto-fraco" />
                </Link></li>))}</ul>}
        </Cartao>
      </div>
    </>
  );
}
