using Zalek360.Domain.Entities;

namespace Zalek360.Application.Interfaces;

/// <summary>Relógio do sistema com fuso de negócio (America/Sao_Paulo). Permite testes determinísticos.</summary>
public interface IClock
{
    DateTime AgoraUtc { get; }
    DateOnly HojeLocal { get; }
    DateOnly ParaDataLocal(DateTime utc);
    DateTime ParaHoraLocal(DateTime utc);
    DateTime InicioDoDiaLocalEmUtc(DateOnly data);
}

public interface IUsuarioAtual
{
    bool Autenticado { get; }
    /// <summary>Id do usuário autenticado. Lança NaoAutenticadoException se não houver usuário.</summary>
    Guid Id { get; }
}

public interface IPasswordHasher
{
    string Gerar(string senha);
    bool Verificar(string senha, string hash);
}

public sealed record TokenGerado(string Token, DateTime ExpiraEm);

public interface ITokenService
{
    TokenGerado Gerar(Usuario usuario, bool manterConectado);
}

/// <summary>Abstração de armazenamento: local no MVP 1, substituível por S3/Azure Blob sem mudar a aplicação.</summary>
public interface IFileStorage
{
    Task<string> SalvarAsync(Stream conteudo, string extensao, CancellationToken ct);
    Task<Stream> AbrirLeituraAsync(string chave, CancellationToken ct);
    Task ExcluirAsync(string chave, CancellationToken ct);
}

/// <summary>Numeração sequencial exibida ao usuário (#000124, #000057).</summary>
public interface INumeradorDocumentos
{
    Task<int> ProximoNumeroOrcamentoAsync(CancellationToken ct);
    Task<int> ProximoNumeroPedidoAsync(CancellationToken ct);
}

public interface ITransacao : IAsyncDisposable
{
    Task ConfirmarAsync(CancellationToken ct);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
    Task<ITransacao> IniciarTransacaoAsync(CancellationToken ct);
}
