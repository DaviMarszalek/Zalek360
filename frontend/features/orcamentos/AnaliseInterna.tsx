"use client";
import { Cartao, ErroCarregamento } from "@/components/ui";
import { moeda } from "@/utils/formatos";
import type { AnaliseOrcamento, ItemAnalise } from "@/types/api";

const valor = (n: number | null | undefined) => n == null ? "Não informado" : moeda(n);
const quantidade = (n: number) => new Intl.NumberFormat("pt-BR", { maximumFractionDigits: 0 }).format(n);

export function ResumoAnaliseInterna({ dados, carregando, erro, aoTentar }: { dados?: AnaliseOrcamento; carregando: boolean; erro?: string; aoTentar: () => void }) {
  return <Cartao titulo="Análise interna">
    {erro ? <ErroCarregamento mensagem={erro} aoTentar={aoTentar} /> : carregando ? <p role="status" className="text-sm text-texto-fraco">Atualizando análise interna…</p> : <>
      <dl className="space-y-2 text-sm">
        <div className="flex justify-between gap-3"><dt>Custo dos produtos</dt><dd className="numero">{valor(dados?.custoTotal)}</dd></div>
        <div className="flex justify-between gap-3"><dt>Margem estimada</dt><dd className="numero">{valor(dados?.margemValor)}</dd></div>
        <div className="flex justify-between gap-3"><dt>Margem sobre a venda</dt><dd className="numero">{dados?.margemPercentual == null ? "Não calculada" : `${new Intl.NumberFormat("pt-BR", { maximumFractionDigits: 2 }).format(dados.margemPercentual)}%`}</dd></div>
      </dl>
      {!dados?.completa && <p className="mt-3 text-xs text-texto-fraco">Há custo não informado ou item incompleto. Custos históricos ausentes não são estimados pelo catálogo atual.</p>}
    </>}
    <p className="mt-3 text-xs text-texto-fraco">Total com personalização, após desconto, menos custo dos produtos. Não inclui despesas adicionais. Informação interna, fora do PDF e das mensagens ao cliente.</p>
  </Cartao>;
}

export function DisponibilidadeItem({ dados, carregando }: { dados?: ItemAnalise; carregando?: boolean }) {
  if (carregando) return <p role="status" className="mt-3 text-xs text-texto-fraco">Consultando custo e materiais…</p>;
  if (!dados?.produtoId) return null;
  return <section className="mt-4 space-y-2 rounded-lg border border-borda bg-slate-50/60 p-3" aria-label="Custo e disponibilidade do item">
    <p className="text-xs text-texto-suave">Custo unitário interno: <span className="numero">{valor(dados.custoUnitario)}</span></p>
    {dados.semFicha ? <p className="text-xs text-amber-800">Sem composição cadastrada; disponibilidade não calculada.</p> : <ul className="space-y-2 text-xs">{dados.materiais.map(m => <li key={m.id} className={m.falta > 0 || !m.ativo ? "text-amber-800" : "text-texto-suave"}>
      <span className="font-medium">{m.nome}{!m.ativo ? " (inativa)" : ""}</span><br />
      Necessário: <span className="numero">{quantidade(m.necessario)} {m.unidade}</span> · Disponível neste item: <span className="numero">{quantidade(m.disponivel)} {m.unidade}</span>
      {m.falta > 0 && <strong> · Falta: <span className="numero">{quantidade(m.falta)} {m.unidade}</span></strong>}
    </li>)}</ul>}
    <p className="text-xs text-texto-fraco">Simulação na ordem dos itens, sem reservar ou bloquear o orçamento. Inclui reservas do próprio pedido, quando houver.</p>
  </section>;
}
