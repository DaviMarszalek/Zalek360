using Zalek360.Domain.Common;

namespace Zalek360.Application.Common;

/// <summary>Erro de aplicação com status HTTP, código estável e (opcionalmente) erros por campo e detalhes.</summary>
public class AppException : Exception
{
    public string Codigo { get; }
    public int StatusCode { get; }
    public IReadOnlyDictionary<string, string[]> Erros { get; }
    public object? Detalhes { get; }

    public AppException(string codigo, string mensagem, int statusCode,
        IReadOnlyDictionary<string, string[]>? erros = null, object? detalhes = null) : base(mensagem)
    {
        Codigo = codigo;
        StatusCode = statusCode;
        Erros = erros ?? new Dictionary<string, string[]>();
        Detalhes = detalhes;
    }
}

public sealed class NaoEncontradoException : AppException
{
    public NaoEncontradoException(string recurso)
        : base(CodigosErro.NaoEncontrado, $"{recurso} não encontrado.", 404) { }
}

public sealed class ConflitoException : AppException
{
    public ConflitoException(string codigo, string mensagem, IReadOnlyDictionary<string, string[]>? erros = null, object? detalhes = null)
        : base(codigo, mensagem, 409, erros, detalhes) { }
}

public sealed class NaoAutenticadoException : AppException
{
    public NaoAutenticadoException(string codigo, string mensagem) : base(codigo, mensagem, 401) { }
}

public sealed class ValidacaoException : AppException
{
    public ValidacaoException(string codigo, string mensagem, IReadOnlyDictionary<string, string[]> erros)
        : base(codigo, mensagem, 400, erros) { }
}

public static class Concorrencia
{
    public const string Mensagem = "Este registro foi alterado por outra pessoa. Recarregue a página para ver a versão atual.";

    /// <summary>Concorrência otimista explícita: o cliente envia a versão que está editando.</summary>
    public static void VerificarVersao(int versaoAtual, int? versaoInformada)
    {
        if (versaoInformada.HasValue && versaoInformada.Value != versaoAtual)
            throw new ConflitoException(CodigosErro.ConflitoConcorrencia, Mensagem);
    }
}
