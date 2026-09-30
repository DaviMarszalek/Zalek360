using Zalek360.Domain.Common;

namespace Zalek360.Domain.Entities;

/// <summary>Observação interna sobre o cliente (visível apenas para a equipe), com autor e data.</summary>
public class ObservacaoCliente : Entidade
{
    public Guid ClienteId { get; private set; }
    public string Texto { get; private set; } = string.Empty;
    public DateTime CriadoEm { get; private set; }
    public Guid AutorId { get; private set; }
    public Usuario? Autor { get; private set; }

    private ObservacaoCliente() { }

    public static ObservacaoCliente Criar(Guid clienteId, string? texto, DateTime agoraUtc, Guid autorId)
    {
        var limpo = TextoOpcional.Limpar(texto);
        if (limpo is null)
            throw new DomainException(CodigosErro.ObservacaoInvalida, "Escreva a observação antes de adicionar.", CategoriaErro.Validacao,
                new Dictionary<string, string[]> { ["texto"] = new[] { "Escreva a observação antes de adicionar." } });
        if (limpo.Length > 2000)
            throw new DomainException(CodigosErro.ObservacaoInvalida, "Use no máximo 2.000 caracteres.", CategoriaErro.Validacao,
                new Dictionary<string, string[]> { ["texto"] = new[] { "Use no máximo 2.000 caracteres." } });
        return new ObservacaoCliente { ClienteId = clienteId, Texto = limpo, CriadoEm = agoraUtc, AutorId = autorId };
    }
}
