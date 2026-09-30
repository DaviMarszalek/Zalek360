"use client";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useState } from "react";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { ChevronRight, Plus, Search, SearchX, Users } from "lucide-react";
import { Botao, CabecalhoPagina, Cartao, Entrada, ErroCarregamento, Esqueleto, EstadoVazio, Etiqueta, Paginacao, Selecao } from "@/components/ui";
import { clientesService } from "@/services";
import { useDebounce } from "@/hooks";
import { erroDaApi } from "@/lib/api";
import { siglaTipoPessoa } from "@/lib/status";
import { data, documento, numeroDocumento, somenteDigitos, telefone } from "@/utils/formatos";
import type { TipoPessoa } from "@/types/api";

export function ListaClientes() {
  const router = useRouter();
  const params = useSearchParams();
  const [busca, setBusca] = useState(params.get("busca") ?? "");
  const [tipo, setTipo] = useState<TipoPessoa | "">("");
  const [cidade, setCidade] = useState("");
  const [pagina, setPagina] = useState(1);
  const termo = useDebounce(busca.trim());

  const q = useQuery({
    queryKey: ["clientes", termo, tipo, cidade, pagina],
    queryFn: () => clientesService.listar({ busca: termo, tipo, cidade, pagina }),
    placeholderData: keepPreviousData,
  });
  const cidades = useQuery({ queryKey: ["clientes", "cidades"], queryFn: clientesService.cidades, staleTime: 5 * 60_000 });
  const filtrando = !!(termo || tipo || cidade);
  const hrefNovo = termo ? `/clientes/novo?${somenteDigitos(termo).length >= 11 ? "documento" : "nome"}=${encodeURIComponent(termo)}` : "/clientes/novo";

  return (
    <>
      <CabecalhoPagina titulo="Clientes" subtitulo="Pessoas físicas e jurídicas atendidas pela Zalek Personalizados."
        acoes={<Link href="/clientes/novo"><Botao icone={<Plus className="h-4 w-4" />}>Novo cliente</Botao></Link>} />
      <Cartao semPadding>
        <div className="flex flex-wrap gap-3 border-b border-borda p-4">
          <div className="relative min-w-[260px] flex-1">
            <Search className="pointer-events-none absolute left-3 top-2.5 h-4 w-4 text-texto-fraco" />
            <Entrada className="pl-9" placeholder="Pesquisar por nome, razão social, CPF/CNPJ, telefone ou e-mail" value={busca}
              onChange={(e) => { setBusca(e.target.value); setPagina(1); }} aria-label="Pesquisar clientes" />
          </div>
          <Selecao className="w-auto min-w-[150px]" value={tipo} onChange={(e) => { setTipo(e.target.value as TipoPessoa | ""); setPagina(1); }} aria-label="Tipo de pessoa">
            <option value="">Todos os tipos</option><option value="PessoaJuridica">Pessoa jurídica</option><option value="PessoaFisica">Pessoa física</option>
          </Selecao>
          <Selecao className="w-auto min-w-[170px]" value={cidade} onChange={(e) => { setCidade(e.target.value); setPagina(1); }} aria-label="Cidade">
            <option value="">Todas as cidades</option>{cidades.data?.map((c) => <option key={c} value={c}>{c}</option>)}
          </Selecao>
          {filtrando && <Botao variante="fantasma" onClick={() => { setBusca(""); setTipo(""); setCidade(""); setPagina(1); }}>Limpar filtros</Botao>}
        </div>

        {q.isError ? <div className="p-4"><ErroCarregamento mensagem={erroDaApi(q.error).message} aoTentar={() => q.refetch()} /></div>
          : !q.data ? <div className="space-y-2 p-4">{Array.from({ length: 6 }).map((_, i) => <Esqueleto key={i} className="h-12" />)}</div>
            : q.data.itens.length === 0 ? (filtrando
              ? <EstadoVazio icone={<SearchX className="h-5 w-5" />} titulo={termo ? `Nenhum cliente encontrado para “${termo}”` : "Nenhum cliente com esses filtros"}
                  descricao="Confira a grafia ou pesquise pelo CPF/CNPJ. Se for um cliente novo, cadastre-o antes de criar o orçamento."
                  acao={<Botao icone={<Plus className="h-4 w-4" />} onClick={() => router.push(hrefNovo)}>Cadastrar novo cliente</Botao>} />
              : <EstadoVazio icone={<Users className="h-5 w-5" />} titulo="Nenhum cliente cadastrado" descricao="Cadastre o primeiro cliente para começar a criar orçamentos."
                  acao={<Link href="/clientes/novo"><Botao icone={<Plus className="h-4 w-4" />}>Cadastrar cliente</Botao></Link>} />)
              : <>
                <div className="overflow-x-auto"><table className="tabela w-full min-w-[820px] text-sm">
                  <thead><tr><th>Cliente</th><th>CPF/CNPJ</th><th>Contato</th><th>Cidade</th><th>Último orçamento</th><th className="w-8" /></tr></thead>
                  <tbody>{q.data.itens.map((c) => (
                    <tr key={c.id} className="cursor-pointer hover:bg-slate-50/60" onClick={() => router.push(`/clientes/${c.id}`)}>
                      <td><div className="flex items-center gap-2"><Link href={`/clientes/${c.id}`} className="font-medium text-texto hover:text-primaria" onClick={(e) => e.stopPropagation()}>{c.nome}</Link><Etiqueta>{siglaTipoPessoa[c.tipoPessoa]}</Etiqueta></div>
                        {c.razaoSocial && c.razaoSocial !== c.nome && <p className="mt-0.5 text-xs text-texto-fraco">{c.razaoSocial}</p>}</td>
                      <td className="numero text-texto-suave">{documento(c.documento)}</td>
                      <td className="text-texto-suave"><p className="numero">{telefone(c.whatsApp ?? c.telefone) || "—"}</p>{c.email && <p className="text-xs text-texto-fraco">{c.email}</p>}</td>
                      <td className="text-texto-suave">{c.cidade ?? "—"}</td>
                      <td>{c.ultimoOrcamento ? <><span className="numero text-texto">{numeroDocumento(c.ultimoOrcamento.numero)}</span><span className="numero ml-2 text-xs text-texto-fraco">{data(c.ultimoOrcamento.data)}</span></> : <span className="text-texto-fraco">Sem orçamentos</span>}</td>
                      <td><ChevronRight className="h-4 w-4 text-texto-fraco" /></td>
                    </tr>))}</tbody></table></div>
                <Paginacao pagina={q.data.pagina} totalPaginas={q.data.totalPaginas} total={q.data.total} exibindo={q.data.itens.length} nome={["cliente", "clientes"]} aoMudar={setPagina} />
              </>}
      </Cartao>
    </>
  );
}
