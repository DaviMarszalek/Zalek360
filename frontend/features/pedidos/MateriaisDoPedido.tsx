"use client";
import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AlertTriangle, Check, Pencil, Plus, Trash2 } from "lucide-react";
import { Botao, Campo, Cartao, Entrada, ErroCarregamento, Selecao } from "@/components/ui";
import { useToast } from "@/components/ui/toast";
import { erroDaApi } from "@/lib/api";
import { estoqueService } from "@/services";
import type { PedidoDetalhe } from "@/types/api";

type Linha = { materiaPrimaId: string; quantidade: number };
const fmt = (n: number) => new Intl.NumberFormat("pt-BR", { maximumFractionDigits: 3 }).format(n);

export function MateriaisDoPedido({ pedido }: { pedido: PedidoDetalhe }) {
  const qc = useQueryClient();
  const toast = useToast();
  const [editando, setEditando] = useState(false);
  const [linhas, setLinhas] = useState<Linha[]>([]);
  const [erro, setErro] = useState("");
  const reservas = useQuery({ queryKey: ["pedido", pedido.id, "materiais"], queryFn: () => estoqueService.reservasPedido(pedido.id) });
  const materiais = useQuery({ queryKey: ["estoque", "materias"], queryFn: estoqueService.materias });
  useEffect(() => {
    if (reservas.data) setLinhas(reservas.data.filter((x) => x.situacao === "Reservada").map((x) => ({ materiaPrimaId: x.materiaPrimaId, quantidade: x.quantidade })));
  }, [reservas.data]);
  const ativos = (materiais.data ?? []).filter((m) => m.ativo);
  const aplicarFicha = useMutation({
    mutationFn: () => estoqueService.aplicarFichaPedido(pedido.id),
    onSuccess: async (data) => { qc.setQueryData(["pedido", pedido.id, "materiais"], data); await qc.invalidateQueries({ queryKey: ["estoque", "materias"] }); setErro(""); toast("sucesso", "Consumo padrão da ficha técnica aplicado."); },
    onError: (e) => setErro(erroDaApi(e).message),
  });
  const salvar = useMutation({
    mutationFn: () => estoqueService.atualizarMateriaisPedido(pedido.id, linhas),
    onSuccess: async (data) => {
      qc.setQueryData(["pedido", pedido.id, "materiais"], data);
      await qc.invalidateQueries({ queryKey: ["estoque", "materias"] });
      setEditando(false); setErro(""); toast("sucesso", "Materiais do pedido atualizados.");
    },
    onError: (e) => setErro(erroDaApi(e).message),
  });
  const editavel = pedido.situacao === "Aberto";
  const registros = reservas.data ?? [];

  return <Cartao titulo="Materiais deste pedido" acao={editavel && !editando ? <Botao variante="secundaria" tamanho="sm" icone={<Pencil className="h-3.5 w-3.5" />} onClick={() => { setLinhas(registros.filter((x) => x.situacao === "Reservada").map((x) => ({ materiaPrimaId: x.materiaPrimaId, quantidade: x.quantidade }))); setErro(""); setEditando(true); }}>Ajustar consumo</Botao> : undefined}>
    {reservas.isError ? <ErroCarregamento mensagem={erroDaApi(reservas.error).message} aoTentar={() => reservas.refetch()} />
      : reservas.isLoading || materiais.isLoading ? <p className="text-sm text-texto-fraco">Carregando materiais…</p>
        : <>
          {!registros.some((x) => x.situacao === "Reservada" || x.situacao === "Consumida") && <div className="mb-3 flex flex-wrap items-center gap-2 rounded-md border border-amber-200 bg-amber-50 p-3 text-sm text-amber-900"><AlertTriangle className="h-4 w-4 shrink-0" /><p className="flex-1">Este pedido ainda não tem materiais vinculados. Aplique a ficha técnica ou informe o consumo aqui antes de iniciar a produção.</p>{editavel && <Botao variante="secundaria" tamanho="sm" carregando={aplicarFicha.isPending} onClick={() => { setErro(""); aplicarFicha.mutate(); }}>Carregar ficha técnica</Botao>}</div>}
          {erro && !editando && <p role="alert" className="mb-3 rounded-md bg-red-50 p-3 text-sm text-red-700">{erro}</p>}
          {editando ? <div className="space-y-3">
            {erro && <p role="alert" className="rounded-md bg-red-50 p-3 text-sm text-red-700">{erro}</p>}
            {linhas.map((linha, i) => <div key={i} className="grid grid-cols-[1fr_130px_36px] items-end gap-2"><Campo rotulo={i === 0 ? "Matéria-prima" : undefined}><Selecao required value={linha.materiaPrimaId} onChange={(e) => setLinhas(linhas.map((x, j) => j === i ? { ...x, materiaPrimaId: e.target.value } : x))}><option value="">Selecione</option>{ativos.map((m) => <option key={m.id} value={m.id}>{m.nome} ({m.unidade})</option>)}</Selecao></Campo><Campo rotulo={i === 0 ? "Quantidade" : undefined}><Entrada required type="number" min="0.001" step="0.001" value={linha.quantidade} onChange={(e) => setLinhas(linhas.map((x, j) => j === i ? { ...x, quantidade: Number(e.target.value) } : x))} /></Campo><Botao variante="fantasma" tamanho="sm" aria-label="Remover material" onClick={() => setLinhas(linhas.filter((_, j) => j !== i))} icone={<Trash2 className="h-4 w-4" />} /></div>)}
            <div className="flex flex-wrap justify-between gap-2"><Botao variante="secundaria" tamanho="sm" icone={<Plus className="h-3.5 w-3.5" />} onClick={() => setLinhas([...linhas, { materiaPrimaId: "", quantidade: 1 }])}>Adicionar material</Botao><div className="flex gap-2"><Botao variante="secundaria" tamanho="sm" onClick={() => setEditando(false)}>Cancelar</Botao><Botao tamanho="sm" carregando={salvar.isPending} icone={<Check className="h-3.5 w-3.5" />} onClick={() => { if (linhas.every((x) => x.materiaPrimaId && x.quantidade > 0) && new Set(linhas.map((x) => x.materiaPrimaId)).size === linhas.length) salvar.mutate(); else setErro("Escolha materiais sem repetir e informe quantidades maiores que zero."); }}>Salvar consumo</Botao></div></div>
          </div> : registros.length === 0 ? <p className="text-sm text-texto-fraco">Nenhum consumo registrado.</p> : <div className="space-y-2">
            {registros.map((x) => <div key={x.id} className="flex flex-wrap items-center justify-between gap-2 border-b border-borda pb-2 text-sm last:border-0 last:pb-0"><span className="font-medium">{x.materiaPrimaNome}</span><span className="numero text-texto-suave">{fmt(x.quantidade)} {x.unidade}</span><span className={`rounded-full px-2 py-1 text-xs ${x.situacao === "Consumida" ? "bg-blue-50 text-blue-700" : x.situacao === "Reservada" ? "bg-amber-50 text-amber-800" : "bg-slate-100 text-slate-600"}`}>{x.situacao === "Consumida" ? "Baixada do estoque" : x.situacao === "Reservada" ? "Reservada" : "Reserva liberada"}</span></div>)}
          </div>}
          {editavel && !editando && registros.some((x) => x.situacao === "Reservada") && <p className="mt-3 text-xs text-texto-fraco">Ao iniciar a produção, as quantidades reservadas serão baixadas do estoque físico.</p>}
        </>}
  </Cartao>;
}
