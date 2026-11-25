"use client";
import { useId, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Botao, Entrada } from "@/components/ui";
import { clientesService } from "@/services";
import { useDebounce } from "@/hooks";
import { documento } from "@/utils/formatos";

export function FiltroCliente({ clienteId, aoMudar }: { clienteId: string; aoMudar: (id: string) => void }) {
  const id = useId();
  const [texto, setTexto] = useState("");
  const [aberto, setAberto] = useState(false);
  const [indice, setIndice] = useState(-1);
  const termo = useDebounce(texto.trim(), 250);
  const selecionado = useQuery({ queryKey: ["cliente", clienteId], queryFn: () => clientesService.obter(clienteId), enabled: !!clienteId });
  const q = useQuery({ queryKey: ["clientes", "filtro-pedidos", termo], queryFn: () => clientesService.listar({ busca: termo }), enabled: aberto && !clienteId });
  const aguardando = termo !== texto.trim();
  const opcoes = aguardando ? [] : q.data?.itens ?? [];
  function selecionar(valor: string) { aoMudar(valor); setAberto(false); setTexto(""); setIndice(-1); }
  return <div className="relative min-w-0 w-full sm:w-64">
    <div className="flex gap-1">
      <Entrada role="combobox" aria-label="Filtrar por cliente" aria-expanded={aberto && !clienteId} aria-controls={id} aria-autocomplete="list"
        aria-activedescendant={aberto && indice >= 0 && !!opcoes[indice] ? `${id}-${indice}` : undefined} placeholder="Todos os clientes" value={clienteId ? selecionado.data?.nomeExibicao ?? "Carregando cliente…" : texto}
        onChange={e => { if (clienteId) aoMudar(""); setTexto(e.target.value); setIndice(-1); setAberto(true); }} onFocus={() => setAberto(true)} onBlur={() => setTimeout(() => setAberto(false), 180)}
        onKeyDown={e => {
          if (e.key === "Escape") { setAberto(false); setIndice(-1); }
          if (e.key === "ArrowDown") { e.preventDefault(); setAberto(true); setIndice(i => Math.min(i + 1, opcoes.length - 1)); }
          if (e.key === "ArrowUp") { e.preventDefault(); setIndice(i => Math.max(0, i - 1)); }
          if (e.key === "Enter" && aberto && opcoes[indice]) { e.preventDefault(); selecionar(opcoes[indice].id); }
        }} />
      {(clienteId || texto) && <Botao variante="fantasma" aria-label="Limpar cliente" onClick={() => selecionar("")}>×</Botao>}
    </div>
    {selecionado.isError && clienteId && <p className="mt-1 text-xs text-red-700">Cliente indisponível. Limpe o filtro para selecionar outro.</p>}
    {aberto && !clienteId && <ul id={id} role="listbox" aria-label="Clientes encontrados" className="absolute z-20 mt-1 max-h-64 w-full overflow-y-auto rounded-md border border-borda bg-white shadow-modal">
      {q.isError ? <li className="p-3 text-sm"><button type="button" onMouseDown={e => e.preventDefault()} onClick={() => q.refetch()}>Não foi possível buscar. Tentar novamente</button></li> : aguardando || !q.data ? <li className="p-3 text-sm text-texto-fraco">Buscando…</li> : !opcoes.length ? <li className="p-3 text-sm text-texto-fraco">Nenhum cliente encontrado.</li> : opcoes.map((c, i) => <li key={c.id}>
        <button id={`${id}-${i}`} type="button" role="option" aria-selected={i === indice} className="w-full px-3 py-2 text-left text-sm hover:bg-slate-50 aria-selected:bg-slate-100" onMouseDown={e => e.preventDefault()} onClick={() => selecionar(c.id)}>{c.nome}<span className="numero block text-xs text-texto-fraco">{documento(c.documento)}</span></button>
      </li>)}
    </ul>}
  </div>;
}
