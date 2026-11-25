# Redefinir senha de usuário

Ferramenta administrativa para trocar a senha de uma conta existente pelo e-mail, sem endpoint nem tela. Ela roda dentro do container da API:

```powershell
powershell -ExecutionPolicy Bypass -File backend/tools/redefinir-senha/redefinir-senha.ps1 -Email admin@zalekpersonalizados.com.br
```

- A senha nova é pedida **duas vezes, sem eco**. Mínimo de 12 caracteres e até 72 bytes, a mesma política da criação de contas.
- Ela **nunca** é aceita como argumento de linha de comando, que ficaria no histórico do terminal. Também não é gravada em arquivo nem registrada em log.
- Usa o mesmo hash BCrypt e a mesma conexão (`ConnectionStrings__Zalek360`) da aplicação.
- Se o e-mail não existir, ou se as duas digitações não conferirem, nada é alterado.
- Sessões já abertas com a senha antiga continuam válidas até expirar. Para derrubar todas as sessões, troque a `JWT_CHAVE` (docs/11).

Parâmetros:

| Parâmetro | Padrão | Uso |
|---|---|---|
| `-Email` | obrigatório | Conta a redefinir |
| `-Projeto` | `zalek360` | Projeto Compose (revisão: `zalek360-revisao`) |
| `-ArquivoEnv` | `.env` | Arquivo de ambiente (revisão: `.env.revisao`) |

Uso direto, equivalente:

```powershell
docker compose exec backend dotnet Zalek360.Api.dll redefinir-senha admin@zalekpersonalizados.com.br
```

Códigos de saída: `0` redefinida; `2` uso incorreto ou entrada encerrada; `3` e-mail inexistente; `4` senha fora da política ou digitações diferentes.

Com a entrada redirecionada (pipe), a ferramenta lê duas linhas da entrada padrão, sem exibi-las. Use isso só em automação controlada: nunca grave a senha num arquivo.
