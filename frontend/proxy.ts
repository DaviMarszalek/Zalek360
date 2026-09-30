import { NextResponse, type NextRequest } from "next/server";

// Proteção de rotas: sem o cookie de sessão, redireciona para o login.
// A validade do token é conferida pelo backend em cada chamada (401 → login).
export function proxy(request: NextRequest) {
  if (request.cookies.get("zalek360_token")?.value) return NextResponse.next();
  const url = request.nextUrl.clone();
  const destino = request.nextUrl.pathname + request.nextUrl.search;
  url.pathname = "/login";
  url.search = destino && destino !== "/" ? `?redirect=${encodeURIComponent(destino)}` : "";
  return NextResponse.redirect(url);
}

export const config = {
  matcher: ["/((?!api|_next/static|_next/image|favicon.ico|manifest.webmanifest|icons|login).*)"],
};
