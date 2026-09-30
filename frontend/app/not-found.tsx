import Link from "next/link";

export default function NaoEncontrado() {
  return (
    <main className="flex min-h-screen items-center justify-center bg-fundo p-6">
      <div className="max-w-md text-center">
        <p className="numero text-sm font-medium text-primaria">404</p>
        <h1 className="mt-2 text-xl font-semibold text-texto">Página não encontrada</h1>
        <p className="mt-2 text-sm text-texto-suave">O endereço acessado não existe no Zalek360 ou foi removido.</p>
        <Link href="/dashboard" className="mt-6 inline-flex h-9 items-center rounded-md bg-primaria px-4 text-sm font-medium text-white hover:bg-primaria-escura">Ir para o dashboard</Link>
      </div>
    </main>
  );
}
