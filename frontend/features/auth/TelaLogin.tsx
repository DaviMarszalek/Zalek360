"use client";
import { useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { Eye, EyeOff, Lock, Mail } from "lucide-react";
import { Aviso, Botao, Campo, Entrada } from "@/components/ui";
import { Marca } from "@/components/layout/AppShell";
import { authService } from "@/services";
import { erroCampo, erroDaApi } from "@/lib/api";
import type { ErroApi } from "@/types/api";

export function TelaLogin() {
  const router = useRouter();
  const params = useSearchParams();
  const qc = useQueryClient();
  const [email, setEmail] = useState("");
  const [senha, setSenha] = useState("");
  const [manter, setManter] = useState(true);
  const [verSenha, setVerSenha] = useState(false);
  const [erro, setErro] = useState<ErroApi | null>(null);
  const [enviando, setEnviando] = useState(false);

  async function entrar(e: React.FormEvent) {
    e.preventDefault();
    setErro(null);
    setEnviando(true);
    try {
      const r = await authService.login(email, senha, manter);
      qc.setQueryData(["auth", "me"], r.usuario);
      const destino = params.get("redirect");
      router.replace(destino && destino.startsWith("/") && !destino.startsWith("//") ? destino : "/dashboard");
    } catch (ex) {
      setErro(erroDaApi(ex));
    } finally {
      setEnviando(false);
    }
  }

  return (
    <div className="grid min-h-screen lg:grid-cols-[1fr_480px]">
      <div className="relative hidden flex-col justify-between bg-lateral p-12 text-lateral-texto lg:flex">
        <Marca />
        <div className="max-w-md">
          <h1 className="text-3xl font-semibold leading-tight tracking-tight text-white">Do orçamento ao pedido, tudo registrado em um só lugar.</h1>
          <p className="mt-4 text-[15px] leading-relaxed">Cadastre clientes, monte orçamentos com personalização, registre cada contato feito pelo WhatsApp, telefone ou e-mail e gere pedidos automaticamente na aprovação.</p>
        </div>
        <p className="text-xs text-slate-500">Zalek Personalizados · Ambiente interno · uso exclusivo de colaboradores</p>
      </div>
      <div className="flex items-center justify-center px-6 py-12">
        <form onSubmit={entrar} className="w-full max-w-sm" noValidate>
          <div className="mb-8 lg:hidden"><Marca clara={false} /></div>
          <h2 className="text-xl font-semibold tracking-tight">Entrar no Zalek360</h2>
          <p className="mt-1 text-sm text-texto-fraco">Use seu e-mail corporativo.</p>
          {erro && !Object.keys(erro.errors).length && <Aviso tipo="erro" className="mt-6">{erro.message}</Aviso>}
          <div className="mt-6 space-y-4">
            <Campo rotulo="E-mail" htmlFor="email" erro={erroCampo(erro, "email")}>
              <div className="relative">
                <Mail className="pointer-events-none absolute left-3 top-2.5 h-4 w-4 text-texto-fraco" />
                <Entrada id="email" type="email" autoComplete="username" className="pl-9" placeholder="nome@zalekpersonalizados.com.br"
                  value={email} onChange={(e) => setEmail(e.target.value)} invalido={!!erroCampo(erro, "email")} autoFocus />
              </div>
            </Campo>
            <Campo rotulo="Senha" htmlFor="senha" erro={erroCampo(erro, "senha")}>
              <div className="relative">
                <Lock className="pointer-events-none absolute left-3 top-2.5 h-4 w-4 text-texto-fraco" />
                <Entrada id="senha" type={verSenha ? "text" : "password"} autoComplete="current-password" className="pl-9 pr-10"
                  value={senha} onChange={(e) => setSenha(e.target.value)} invalido={!!erroCampo(erro, "senha")} />
                <button type="button" className="absolute right-2.5 top-2.5 text-texto-fraco" onClick={() => setVerSenha((v) => !v)} aria-label={verSenha ? "Ocultar senha" : "Mostrar senha"}>
                  {verSenha ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>
            </Campo>
            <label className="flex items-center gap-2 text-sm text-texto-suave">
              <input type="checkbox" checked={manter} onChange={(e) => setManter(e.target.checked)} className="h-4 w-4 rounded border-borda accent-primaria" />
              Manter conectado neste dispositivo
            </label>
          </div>
          <Botao type="submit" className="mt-6 w-full" carregando={enviando}>Entrar</Botao>
          <p className="mt-6 text-center text-xs text-texto-fraco">Esqueceu a senha? Fale com o administrador do sistema.</p>
        </form>
      </div>
    </div>
  );
}
