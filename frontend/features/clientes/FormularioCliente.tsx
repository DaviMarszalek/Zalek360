"use client";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { AlertTriangle, Building2, CheckCircle2, Loader2, User } from "lucide-react";
import { Aviso, Botao, Campo, Cartao, Entrada, AreaTexto, Selecao } from "@/components/ui";
import { useToast } from "@/components/ui/toast";
import { clientesService } from "@/services";
import { erroCampo, erroDaApi } from "@/lib/api";
import { cn } from "@/lib/utils";
import { documento as fmtDocumento, mascaraCep, mascaraDocumento, mascaraTelefone, numeroDocumento, somenteDigitos, telefone } from "@/utils/formatos";
import type { ClienteDetalhe, ClienteExistente, ClienteRequisicao, ErroApi, TipoPessoa, VerificacaoDocumento } from "@/types/api";

const UFS = ["AC","AL","AP","AM","BA","CE","DF","ES","GO","MA","MT","MS","MG","PA","PB","PR","PE","PI","RJ","RN","RS","RO","RR","SC","SP","SE","TO"];

function inicial(c?: ClienteDetalhe, nome?: string | null, doc?: string | null): ClienteRequisicao {
  if (c) return {
    tipoPessoa: c.tipoPessoa, documento: mascaraDocumento(c.documento, c.tipoPessoa), nomeRazaoSocial: c.nomeRazaoSocial, nomeFantasia: c.nomeFantasia ?? "",
    contatoNome: c.contatoNome ?? "", contatoCargo: c.contatoCargo ?? "", telefone: telefone(c.telefone), whatsApp: telefone(c.whatsApp), email: c.email ?? "",
    cep: mascaraCep(c.endereco.cep ?? ""), logradouro: c.endereco.logradouro ?? "", numero: c.endereco.numero ?? "", complemento: c.endereco.complemento ?? "",
    bairro: c.endereco.bairro ?? "", cidade: c.endereco.cidade ?? "", uf: c.endereco.uf ?? "", versao: c.versao,
  };
  const digitos = somenteDigitos(doc);
  const tipo: TipoPessoa = digitos.length === 11 ? "PessoaFisica" : "PessoaJuridica";
  return { tipoPessoa: tipo, documento: digitos ? mascaraDocumento(digitos, tipo) : "", nomeRazaoSocial: nome ?? "", nomeFantasia: nome ?? "", contatoNome: "", contatoCargo: "",
    telefone: "", whatsApp: "", email: "", cep: "", logradouro: "", numero: "", complemento: "", bairro: "", cidade: "", uf: "", observacaoInicial: "" };
}

function CartaoDuplicado({ existente }: { existente: ClienteExistente }) {
  return (
    <div className="mt-3 rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm">
      <p className="flex items-center gap-2 font-medium text-amber-900"><AlertTriangle className="h-4 w-4" />Este {existente.tipoPessoa === "PessoaFisica" ? "CPF" : "CNPJ"} já está cadastrado</p>
      <div className="mt-3 rounded-md border border-amber-200 bg-white p-3">
        <p className="font-medium">{existente.nome}</p>
        <p className="numero mt-0.5 text-xs text-texto-fraco">{fmtDocumento(existente.documento)}{existente.cidade && ` · ${existente.cidade}`}{existente.ultimoOrcamentoNumero && ` · último orçamento ${numeroDocumento(existente.ultimoOrcamentoNumero)}`}</p>
      </div>
      <p className="mt-2 text-amber-900/80">Não é possível cadastrar o mesmo documento duas vezes. Use o cadastro existente.</p>
      <div className="mt-3 flex flex-wrap gap-2">
        <Link href={`/clientes/${existente.id}`}><Botao variante="secundaria" tamanho="sm">Abrir cliente</Botao></Link>
        <Link href={`/orcamentos/novo?clienteId=${existente.id}`}><Botao tamanho="sm">Criar orçamento para este cliente</Botao></Link>
      </div>
    </div>
  );
}

export function FormularioCliente({ cliente }: { cliente?: ClienteDetalhe }) {
  const router = useRouter();
  const params = useSearchParams();
  const qc = useQueryClient();
  const toast = useToast();
  const edicao = !!cliente;
  const [f, setF] = useState<ClienteRequisicao>(() => inicial(cliente, params.get("nome"), params.get("documento")));
  const [erro, setErro] = useState<ErroApi | null>(null);
  const [verificacao, setVerificacao] = useState<VerificacaoDocumento | null>(null);
  const [verificando, setVerificando] = useState(false);
  const [criarOrcamento, setCriarOrcamento] = useState(false);
  const pj = f.tipoPessoa === "PessoaJuridica";

  const set = <K extends keyof ClienteRequisicao>(campo: K, valor: ClienteRequisicao[K]) => {
    setF((v) => ({ ...v, [campo]: valor }));
    if (erro?.errors[campo as string]) setErro({ ...erro, errors: Object.fromEntries(Object.entries(erro.errors).filter(([k]) => k !== campo)) });
  };

  async function verificarDocumento() {
    const d = somenteDigitos(f.documento);
    if (!d || !f.tipoPessoa) { setVerificacao(null); return; }
    setVerificando(true);
    try { setVerificacao(await clientesService.verificarDocumento(d, f.tipoPessoa, cliente?.id)); } catch { setVerificacao(null); } finally { setVerificando(false); }
  }

  const salvar = useMutation({
    mutationFn: () => {
      const corpo: ClienteRequisicao = { ...f, documento: somenteDigitos(f.documento), telefone: somenteDigitos(f.telefone), whatsApp: somenteDigitos(f.whatsApp),
        cep: somenteDigitos(f.cep), nomeFantasia: pj ? f.nomeFantasia : "", contatoCargo: pj ? f.contatoCargo : "" };
      return edicao ? clientesService.atualizar(cliente!.id, corpo) : clientesService.criar(corpo);
    },
    onSuccess: (c) => {
      qc.invalidateQueries({ queryKey: ["clientes"] });
      qc.setQueryData(["cliente", c.id], c);
      toast("sucesso", edicao ? "Cadastro atualizado." : `Cliente ${c.nomeExibicao} cadastrado.`);
      router.push(criarOrcamento ? `/orcamentos/novo?clienteId=${c.id}` : `/clientes/${c.id}`);
    },
    onError: (e) => {
      const api = erroDaApi(e);
      setErro(api);
      const existente = (api.details as { clienteExistente?: ClienteExistente } | undefined)?.clienteExistente;
      if (existente) setVerificacao({ valido: true, disponivel: false, mensagem: api.message, clienteExistente: existente });
      window.scrollTo({ top: 0, behavior: "smooth" });
    },
  });

  const erroDoc = erroCampo(erro, "documento") ?? (verificacao && !verificacao.valido ? verificacao.mensagem ?? undefined : undefined);
  const e = (c: string) => erroCampo(erro, c);
  const qtdErros = erro ? Object.keys(erro.errors).length : 0;

  return (
    <form onSubmit={(ev) => { ev.preventDefault(); setCriarOrcamento(false); salvar.mutate(); }} noValidate className="space-y-5">
      {erro && qtdErros > 0 && !(verificacao?.clienteExistente) && <Aviso tipo="erro" titulo="Corrija os campos destacados para salvar o cliente.">{qtdErros} {qtdErros === 1 ? "campo precisa" : "campos precisam"} de atenção.</Aviso>}
      {erro && qtdErros === 0 && <Aviso tipo="erro">{erro.message}</Aviso>}

      <Cartao titulo="Identificação">
        {!edicao && (
          <div className="mb-5 grid max-w-md grid-cols-2 gap-2" role="radiogroup" aria-label="Tipo de pessoa">
            {(["PessoaJuridica", "PessoaFisica"] as TipoPessoa[]).map((t) => (
              <button key={t} type="button" role="radio" aria-checked={f.tipoPessoa === t}
                onClick={() => { set("tipoPessoa", t); set("documento", mascaraDocumento(f.documento, t)); setVerificacao(null); }}
                className={cn("flex items-center gap-2.5 rounded-lg border px-4 py-3 text-left text-sm transition-colors",
                  f.tipoPessoa === t ? "border-primaria bg-primaria-clara text-primaria" : "border-borda hover:bg-slate-50")}>
                {t === "PessoaJuridica" ? <Building2 className="h-4 w-4" /> : <User className="h-4 w-4" />}
                <span className="font-medium">{t === "PessoaJuridica" ? "Pessoa jurídica" : "Pessoa física"}</span>
              </button>))}
          </div>)}
        <div className="grid gap-4 md:grid-cols-2">
          <Campo rotulo={pj ? "CNPJ" : "CPF"} obrigatorio htmlFor="documento" erro={erroDoc}
            ajuda={verificando ? <span className="inline-flex items-center gap-1"><Loader2 className="h-3 w-3 animate-spin" />Verificando...</span>
              : verificacao?.valido && verificacao.disponivel ? <span className="inline-flex items-center gap-1 text-emerald-700"><CheckCircle2 className="h-3.5 w-3.5" />{verificacao.mensagem}</span> : "A validação é feita ao sair do campo."}>
            <Entrada id="documento" className="numero" inputMode="numeric" placeholder={pj ? "00.000.000/0000-00" : "000.000.000-00"} value={f.documento}
              invalido={!!erroDoc || (verificacao ? !verificacao.disponivel : false)}
              onChange={(ev) => { set("documento", mascaraDocumento(ev.target.value, f.tipoPessoa ?? "PessoaJuridica")); setVerificacao(null); }} onBlur={verificarDocumento} />
            {verificacao?.clienteExistente && <CartaoDuplicado existente={verificacao.clienteExistente} />}
          </Campo>
          <div className="hidden md:block" />
          <Campo rotulo={pj ? "Razão social" : "Nome completo"} obrigatorio htmlFor="nome" erro={e("nomeRazaoSocial")}>
            <Entrada id="nome" value={f.nomeRazaoSocial} onChange={(ev) => set("nomeRazaoSocial", ev.target.value)} invalido={!!e("nomeRazaoSocial")} maxLength={150} />
          </Campo>
          {pj && <Campo rotulo="Nome fantasia" htmlFor="fantasia" erro={e("nomeFantasia")} ajuda="Nome usado nas listas e no dia a dia.">
            <Entrada id="fantasia" value={f.nomeFantasia} onChange={(ev) => set("nomeFantasia", ev.target.value)} maxLength={100} />
          </Campo>}
        </div>
      </Cartao>

      <Cartao titulo="Contato">
        {e("contato") && <Aviso tipo="erro" className="mb-4">{e("contato")}</Aviso>}
        <div className="grid gap-4 md:grid-cols-2">
          {pj && <>
            <Campo rotulo="Nome do contato" htmlFor="contato" erro={e("contatoNome")}><Entrada id="contato" value={f.contatoNome} onChange={(ev) => set("contatoNome", ev.target.value)} maxLength={100} /></Campo>
            <Campo rotulo="Cargo do contato" htmlFor="cargo" erro={e("contatoCargo")}><Entrada id="cargo" placeholder="Ex.: Compras" value={f.contatoCargo} onChange={(ev) => set("contatoCargo", ev.target.value)} maxLength={60} /></Campo>
          </>}
          <Campo rotulo="WhatsApp" htmlFor="whatsapp" erro={e("whatsApp")}><Entrada id="whatsapp" className="numero" inputMode="tel" placeholder="(00) 00000-0000" value={f.whatsApp} onChange={(ev) => set("whatsApp", mascaraTelefone(ev.target.value))} invalido={!!e("whatsApp")} /></Campo>
          <Campo rotulo="Telefone" htmlFor="telefone" erro={e("telefone")}><Entrada id="telefone" className="numero" inputMode="tel" placeholder="(00) 0000-0000" value={f.telefone} onChange={(ev) => set("telefone", mascaraTelefone(ev.target.value))} invalido={!!e("telefone")} /></Campo>
          <Campo rotulo="E-mail" htmlFor="email" erro={e("email")} className="md:col-span-2"><Entrada id="email" type="email" value={f.email} onChange={(ev) => set("email", ev.target.value)} invalido={!!e("email")} maxLength={150} /></Campo>
        </div>
        <p className="mt-3 text-xs text-texto-fraco">Informe pelo menos um meio de contato (WhatsApp, telefone ou e-mail).</p>
      </Cartao>

      <Cartao titulo="Endereço">
        <div className="grid gap-4 md:grid-cols-6">
          <Campo rotulo="CEP" htmlFor="cep" erro={e("cep")} className="md:col-span-2"><Entrada id="cep" className="numero" inputMode="numeric" placeholder="00000-000" value={f.cep} onChange={(ev) => set("cep", mascaraCep(ev.target.value))} invalido={!!e("cep")} /></Campo>
          <Campo rotulo="Logradouro" htmlFor="logradouro" erro={e("logradouro")} className="md:col-span-4"><Entrada id="logradouro" value={f.logradouro} onChange={(ev) => set("logradouro", ev.target.value)} maxLength={150} /></Campo>
          <Campo rotulo="Número" htmlFor="numero" erro={e("numero")} className="md:col-span-1"><Entrada id="numero" value={f.numero} onChange={(ev) => set("numero", ev.target.value)} maxLength={20} /></Campo>
          <Campo rotulo="Complemento" htmlFor="complemento" erro={e("complemento")} className="md:col-span-2"><Entrada id="complemento" value={f.complemento} onChange={(ev) => set("complemento", ev.target.value)} maxLength={100} /></Campo>
          <Campo rotulo="Bairro" htmlFor="bairro" erro={e("bairro")} className="md:col-span-3"><Entrada id="bairro" value={f.bairro} onChange={(ev) => set("bairro", ev.target.value)} maxLength={100} /></Campo>
          <Campo rotulo="Cidade" htmlFor="cidade" erro={e("cidade")} className="md:col-span-4"><Entrada id="cidade" value={f.cidade} onChange={(ev) => set("cidade", ev.target.value)} maxLength={100} /></Campo>
          <Campo rotulo="UF" htmlFor="uf" erro={e("uf")} className="md:col-span-2">
            <Selecao id="uf" value={f.uf} onChange={(ev) => set("uf", ev.target.value)} invalido={!!e("uf")}><option value="">Selecione</option>{UFS.map((u) => <option key={u}>{u}</option>)}</Selecao>
          </Campo>
        </div>
      </Cartao>

      {!edicao && <Cartao titulo="Observações internas">
        <Campo erro={e("observacaoInicial")} ajuda="Visível apenas para a equipe. Novas observações podem ser adicionadas depois no cadastro.">
          <AreaTexto value={f.observacaoInicial} onChange={(ev) => set("observacaoInicial", ev.target.value)} maxLength={2000} placeholder="Preferências de contato, forma de pagamento habitual, cuidados com a arte..." />
        </Campo>
      </Cartao>}

      <div className="sticky bottom-0 -mx-4 flex flex-wrap justify-end gap-2 border-t border-borda bg-fundo/95 px-4 py-3 backdrop-blur sm:mx-0 sm:rounded-lg sm:border">
        <Botao variante="secundaria" onClick={() => router.back()}>Cancelar</Botao>
        {!edicao && <Botao variante="secundaria" carregando={salvar.isPending && criarOrcamento} disabled={salvar.isPending}
          onClick={() => { setCriarOrcamento(true); salvar.mutate(); }}>Salvar e criar orçamento</Botao>}
        <Botao type="submit" carregando={salvar.isPending && !criarOrcamento} disabled={salvar.isPending}>{edicao ? "Salvar alterações" : "Salvar cliente"}</Botao>
      </div>
    </form>
  );
}
