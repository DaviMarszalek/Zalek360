using System.Text;
using Zalek360.Api.Configuracao;
using Zalek360.Application.Common;
using Zalek360.Application.Dtos;
using Zalek360.Application.Services;
using Zalek360.Infrastructure;

namespace Zalek360.Api.Ferramentas;

/// <summary>
/// Redefine a senha de um usuário pelo e-mail, dentro do container da API:
/// <c>dotnet Zalek360.Api.dll redefinir-senha &lt;e-mail&gt;</c>.
/// A senha é lida da entrada sem eco (duas vezes) e nunca aceita como argumento; nada é registrado em log.
/// Usa a mesma configuração (ConnectionStrings__Zalek360) e o mesmo hash BCrypt da aplicação. Não inicia o servidor web.
/// </summary>
public static class FerramentaRedefinirSenha
{
    public const string Comando = "redefinir-senha";

    public static bool Solicitada(string[] args) => args.Length > 0 && args[0] == Comando;

    public static async Task<int> ExecutarAsync(string[] args)
    {
        // Valida os argumentos antes de montar a infraestrutura.
        if (ArgumentosInvalidos(args, Console.Out)) return 2;

        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
        builder.Logging.ClearProviders();
        builder.Services.AddInfrastructure(builder.Configuration).AddZalekApplication();
        await using var app = builder.Build();
        await using var escopo = app.Services.CreateAsyncScope();
        var usuarios = escopo.ServiceProvider.GetRequiredService<UsuarioService>();
        return await ExecutarAsync(args, LerSenhaDoConsole, Console.Out, usuarios.RedefinirSenhaAsync, CancellationToken.None);
    }

    /// <summary>Fluxo da ferramenta com dependências injetáveis (testável).</summary>
    public static async Task<int> ExecutarAsync(string[] args, Func<string, string?> lerSenha, TextWriter saida,
        Func<string, string, CancellationToken, Task<UsuarioAcessoDto>> redefinir, CancellationToken ct)
    {
        if (ArgumentosInvalidos(args, saida)) return 2;
        var email = args[1].Trim();

        var senha = lerSenha("Nova senha (não aparece na tela): ");
        var confirmacao = lerSenha("Repita a nova senha: ");
        if (senha is null || confirmacao is null)
        {
            saida.WriteLine("Entrada encerrada sem a senha. Nada foi alterado.");
            return 2;
        }
        if (!string.Equals(senha, confirmacao, StringComparison.Ordinal))
        {
            saida.WriteLine("As senhas digitadas não conferem. Nada foi alterado.");
            return 4;
        }

        try
        {
            var usuario = await redefinir(email, senha, ct);
            saida.WriteLine($"Senha redefinida para {usuario.Email} (perfil {usuario.Perfil}{(usuario.Ativo ? "" : ", conta inativa")}).");
            return 0;
        }
        catch (NaoEncontradoException)
        {
            saida.WriteLine("Nenhum usuário com esse e-mail. Nada foi alterado.");
            return 3;
        }
        catch (ValidacaoException ex)
        {
            saida.WriteLine($"{ex.Message} Nada foi alterado.");
            return 4;
        }
    }

    private static bool ArgumentosInvalidos(string[] args, TextWriter saida)
    {
        if (args.Length > 2)
        {
            saida.WriteLine("Não informe a senha na linha de comando: ela fica no histórico do terminal. Informe só o e-mail; a senha é pedida em seguida.");
            return true;
        }
        if (args.Length < 2 || string.IsNullOrWhiteSpace(args[1]))
        {
            saida.WriteLine($"Uso: dotnet Zalek360.Api.dll {Comando} <e-mail>");
            return true;
        }
        return false;
    }

    /// <summary>Lê uma linha sem eco no terminal; com entrada redirecionada (pipe), lê a linha sem exibi-la.</summary>
    private static string? LerSenhaDoConsole(string pergunta)
    {
        Console.Write(pergunta);
        if (Console.IsInputRedirected)
        {
            var linha = Console.In.ReadLine();
            Console.WriteLine();
            return linha;
        }
        var texto = new StringBuilder();
        while (true)
        {
            var tecla = Console.ReadKey(intercept: true);
            if (tecla.Key == ConsoleKey.Enter) break;
            if (tecla.Key == ConsoleKey.Backspace) { if (texto.Length > 0) texto.Length--; continue; }
            if (!char.IsControl(tecla.KeyChar)) texto.Append(tecla.KeyChar);
        }
        Console.WriteLine();
        return texto.ToString();
    }
}
