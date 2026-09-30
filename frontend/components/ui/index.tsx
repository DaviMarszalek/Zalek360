"use client";
import * as React from "react";
import * as DialogPrimitive from "@radix-ui/react-dialog";
import { AlertCircle, CheckCircle2, Info, Loader2, X } from "lucide-react";
import { cn } from "@/lib/utils";
import { estiloSituacaoOrcamento, estiloSituacaoPedido, rotuloSituacaoOrcamento, rotuloSituacaoPedido } from "@/lib/status";
import type { SituacaoOrcamento, SituacaoPedido } from "@/types/api";

type Variante = "primaria" | "secundaria" | "fantasma" | "perigo" | "sucesso";
const variantes: Record<Variante, string> = {
  primaria: "bg-primaria text-white hover:bg-primaria-escura border border-primaria",
  secundaria: "bg-white text-texto border border-borda hover:bg-slate-50",
  fantasma: "bg-transparent text-texto-suave hover:bg-slate-100 border border-transparent",
  perigo: "bg-red-600 text-white hover:bg-red-700 border border-red-600",
  sucesso: "bg-emerald-600 text-white hover:bg-emerald-700 border border-emerald-600",
};

export interface BotaoProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variante?: Variante; tamanho?: "sm" | "md"; carregando?: boolean; icone?: React.ReactNode;
}

export const Botao = React.forwardRef<HTMLButtonElement, BotaoProps>(function Botao(
  { variante = "primaria", tamanho = "md", carregando, icone, className, children, disabled, type = "button", ...props }, ref) {
  return (
    <button ref={ref} type={type} disabled={disabled || carregando}
      className={cn("inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-md font-medium transition-colors disabled:cursor-not-allowed disabled:opacity-60",
        tamanho === "sm" ? "h-8 px-3 text-[13px]" : "h-9 px-4 text-sm", variantes[variante], className)} {...props}>
      {carregando ? <Loader2 className="h-4 w-4 animate-spin" aria-hidden /> : icone}
      {children}
    </button>
  );
});

export const Entrada = React.forwardRef<HTMLInputElement, React.InputHTMLAttributes<HTMLInputElement> & { invalido?: boolean }>(
  function Entrada({ className, invalido, ...props }, ref) {
    return <input ref={ref} aria-invalid={invalido || undefined} className={cn("campo", invalido && "campo-erro", className)} {...props} />;
  });

export const AreaTexto = React.forwardRef<HTMLTextAreaElement, React.TextareaHTMLAttributes<HTMLTextAreaElement> & { invalido?: boolean }>(
  function AreaTexto({ className, invalido, ...props }, ref) {
    return <textarea ref={ref} aria-invalid={invalido || undefined} className={cn("campo h-auto min-h-[80px] py-2 leading-relaxed", invalido && "campo-erro", className)} {...props} />;
  });

export const Selecao = React.forwardRef<HTMLSelectElement, React.SelectHTMLAttributes<HTMLSelectElement> & { invalido?: boolean }>(
  function Selecao({ className, invalido, children, ...props }, ref) {
    return <select ref={ref} aria-invalid={invalido || undefined} className={cn("campo appearance-none bg-[length:16px] bg-[right_10px_center] bg-no-repeat pr-8", invalido && "campo-erro", className)}
      style={{ backgroundImage: "url(\"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='%2364748B' stroke-width='2'%3E%3Cpath d='m6 9 6 6 6-6'/%3E%3C/svg%3E\")" }} {...props}>{children}</select>;
  });

export function Campo({ rotulo, erro, ajuda, obrigatorio, children, className, htmlFor }: {
  rotulo?: string; erro?: string; ajuda?: React.ReactNode; obrigatorio?: boolean; children: React.ReactNode; className?: string; htmlFor?: string;
}) {
  return (
    <div className={className}>
      {rotulo && <label htmlFor={htmlFor} className="rotulo">{rotulo}{obrigatorio && <span className="ml-0.5 text-red-600">*</span>}</label>}
      {children}
      {erro ? <p className="mt-1 flex items-center gap-1 text-xs text-red-600" role="alert"><AlertCircle className="h-3.5 w-3.5 shrink-0" />{erro}</p>
        : ajuda ? <p className="mt-1 text-xs text-texto-fraco">{ajuda}</p> : null}
    </div>
  );
}

export function Cartao({ titulo, acao, children, className, semPadding }: { titulo?: React.ReactNode; acao?: React.ReactNode; children: React.ReactNode; className?: string; semPadding?: boolean }) {
  return (
    <section className={cn("cartao", className)}>
      {(titulo || acao) && (
        <header className="flex items-center justify-between gap-3 border-b border-borda px-5 py-3.5">
          <h2 className="text-sm font-semibold text-texto">{titulo}</h2>{acao}
        </header>
      )}
      <div className={semPadding ? "" : "p-5"}>{children}</div>
    </section>
  );
}

const baseBadge = "inline-flex items-center gap-1.5 whitespace-nowrap rounded-full border px-2.5 py-0.5 text-xs font-medium";
export const BadgeOrcamento = ({ situacao }: { situacao: SituacaoOrcamento }) =>
  <span className={cn(baseBadge, estiloSituacaoOrcamento[situacao])}><span className="h-1.5 w-1.5 rounded-full bg-current opacity-70" />{rotuloSituacaoOrcamento[situacao]}</span>;
export const BadgePedido = ({ situacao }: { situacao: SituacaoPedido }) =>
  <span className={cn(baseBadge, estiloSituacaoPedido[situacao])}><span className="h-1.5 w-1.5 rounded-full bg-current opacity-70" />{rotuloSituacaoPedido[situacao]}</span>;
export const Etiqueta = ({ children, className }: { children: React.ReactNode; className?: string }) =>
  <span className={cn("inline-flex items-center rounded border border-borda bg-slate-50 px-1.5 py-0.5 text-[11px] font-medium text-texto-suave", className)}>{children}</span>;

type TipoAviso = "info" | "sucesso" | "alerta" | "erro";
const estilosAviso: Record<TipoAviso, string> = {
  info: "border-blue-200 bg-blue-50 text-blue-900", sucesso: "border-emerald-200 bg-emerald-50 text-emerald-900",
  alerta: "border-amber-200 bg-amber-50 text-amber-900", erro: "border-red-200 bg-red-50 text-red-900",
};
export function Aviso({ tipo = "info", titulo, children, acao, className }: { tipo?: TipoAviso; titulo?: React.ReactNode; children?: React.ReactNode; acao?: React.ReactNode; className?: string }) {
  const Icone = tipo === "sucesso" ? CheckCircle2 : tipo === "info" ? Info : AlertCircle;
  return (
    <div role={tipo === "erro" ? "alert" : "status"} className={cn("flex items-start gap-3 rounded-lg border px-4 py-3 text-sm", estilosAviso[tipo], className)}>
      <Icone className="mt-0.5 h-4 w-4 shrink-0" />
      <div className="min-w-0 flex-1">{titulo && <p className="font-medium">{titulo}</p>}{children && <div className={cn(titulo && "mt-0.5", "opacity-90")}>{children}</div>}</div>
      {acao}
    </div>
  );
}

export function Esqueleto({ className }: { className?: string }) {
  return <div className={cn("animate-pulse rounded bg-slate-200/70", className)} />;
}

export function Carregando({ texto = "Carregando..." }: { texto?: string }) {
  return <div className="flex items-center justify-center gap-2 py-16 text-sm text-texto-fraco"><Loader2 className="h-4 w-4 animate-spin" />{texto}</div>;
}

export function EstadoVazio({ icone, titulo, descricao, acao }: { icone?: React.ReactNode; titulo: string; descricao?: React.ReactNode; acao?: React.ReactNode }) {
  return (
    <div className="flex flex-col items-center justify-center px-6 py-14 text-center">
      {icone && <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-slate-100 text-texto-fraco">{icone}</div>}
      <p className="text-[15px] font-semibold text-texto">{titulo}</p>
      {descricao && <p className="mt-1 max-w-md text-sm text-texto-fraco">{descricao}</p>}
      {acao && <div className="mt-5">{acao}</div>}
    </div>
  );
}

export function ErroCarregamento({ mensagem, aoTentar }: { mensagem?: string; aoTentar?: () => void }) {
  return (
    <Aviso tipo="erro" titulo="Não foi possível carregar os dados." acao={aoTentar && <Botao variante="secundaria" tamanho="sm" onClick={aoTentar}>Tentar novamente</Botao>}>
      {mensagem}
    </Aviso>
  );
}

export function Modal({ aberto, aoFechar, titulo, descricao, children, rodape, largura = "max-w-lg" }: {
  aberto: boolean; aoFechar: () => void; titulo: React.ReactNode; descricao?: React.ReactNode; children?: React.ReactNode; rodape?: React.ReactNode; largura?: string;
}) {
  return (
    <DialogPrimitive.Root open={aberto} onOpenChange={(v) => !v && aoFechar()}>
      <DialogPrimitive.Portal>
        <DialogPrimitive.Overlay className="fixed inset-0 z-50 bg-slate-950/45 backdrop-blur-[1px]" />
        <DialogPrimitive.Content className={cn("fixed left-1/2 top-1/2 z-50 flex max-h-[92vh] w-[calc(100vw-2rem)] -translate-x-1/2 -translate-y-1/2 flex-col rounded-xl border border-borda bg-white shadow-modal focus:outline-none", largura)}>
          <div className="flex items-start justify-between gap-4 border-b border-borda px-6 py-4">
            <div>
              <DialogPrimitive.Title className="text-base font-semibold text-texto">{titulo}</DialogPrimitive.Title>
              {descricao ? <DialogPrimitive.Description className="mt-1 text-sm text-texto-fraco">{descricao}</DialogPrimitive.Description>
                : <DialogPrimitive.Description className="sr-only">Janela de diálogo</DialogPrimitive.Description>}
            </div>
            <DialogPrimitive.Close className="rounded p-1 text-texto-fraco hover:bg-slate-100" aria-label="Fechar"><X className="h-4 w-4" /></DialogPrimitive.Close>
          </div>
          <div className="overflow-y-auto px-6 py-5">{children}</div>
          {rodape && <div className="flex flex-wrap justify-end gap-2 border-t border-borda bg-slate-50/60 px-6 py-3.5">{rodape}</div>}
        </DialogPrimitive.Content>
      </DialogPrimitive.Portal>
    </DialogPrimitive.Root>
  );
}

export function Abas<T extends string>({ abas, ativa, aoMudar }: { abas: { id: T; rotulo: string; contagem?: number }[]; ativa: T; aoMudar: (id: T) => void }) {
  return (
    <div className="flex gap-1 overflow-x-auto border-b border-borda" role="tablist">
      {abas.map((a) => (
        <button key={a.id} role="tab" aria-selected={a.id === ativa} onClick={() => aoMudar(a.id)}
          className={cn("-mb-px flex items-center gap-2 whitespace-nowrap border-b-2 px-3 py-2.5 text-sm font-medium transition-colors",
            a.id === ativa ? "border-primaria text-primaria" : "border-transparent text-texto-fraco hover:text-texto")}>
          {a.rotulo}
          {a.contagem !== undefined && <span className={cn("rounded-full px-1.5 text-[11px] numero", a.id === ativa ? "bg-primaria-clara text-primaria" : "bg-slate-100 text-texto-fraco")}>{a.contagem}</span>}
        </button>
      ))}
    </div>
  );
}

export function Paginacao({ pagina, totalPaginas, total, exibindo, nome = ["registro", "registros"], aoMudar }: {
  pagina: number; totalPaginas: number; total: number; exibindo?: number; nome?: [string, string]; aoMudar: (p: number) => void;
}) {
  if (total === 0) return null;
  const rotulo = total === 1 ? nome[0] : nome[1];
  return (
    <div className="flex flex-wrap items-center justify-between gap-2 border-t border-borda px-4 py-3 text-[13px] text-texto-fraco">
      <span>{exibindo !== undefined ? <>Mostrando <span className="numero">{exibindo}</span> de <span className="numero">{total}</span> {rotulo}</> : <><span className="numero">{total}</span> {rotulo}</>}</span>
      <div className="flex items-center gap-2">
        <Botao variante="secundaria" tamanho="sm" disabled={pagina <= 1} onClick={() => aoMudar(pagina - 1)}>Anterior</Botao>
        <span>Página <span className="numero">{pagina}</span> de <span className="numero">{totalPaginas}</span></span>
        <Botao variante="secundaria" tamanho="sm" disabled={pagina >= totalPaginas} onClick={() => aoMudar(pagina + 1)}>Próxima</Botao>
      </div>
    </div>
  );
}

export function CabecalhoPagina({ titulo, subtitulo, acoes, voltar }: { titulo: React.ReactNode; subtitulo?: React.ReactNode; acoes?: React.ReactNode; voltar?: React.ReactNode }) {
  return (
    <div className="mb-6">
      {voltar && <div className="mb-2 text-[13px]">{voltar}</div>}
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="min-w-0"><h1 className="text-xl font-semibold tracking-tight text-texto">{titulo}</h1>{subtitulo && <div className="mt-1 text-sm text-texto-fraco">{subtitulo}</div>}</div>
        {acoes && <div className="flex flex-wrap items-center gap-2">{acoes}</div>}
      </div>
    </div>
  );
}

export function Info2({ rotulo, children, className }: { rotulo: string; children: React.ReactNode; className?: string }) {
  return <div className={className}><dt className="text-xs text-texto-fraco">{rotulo}</dt><dd className="mt-0.5 text-sm text-texto">{children}</dd></div>;
}
