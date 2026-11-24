using Zalek360.Domain.Common;

namespace Zalek360.Domain.Entities;

public enum TipoOpcaoCatalogo { TipoPersonalizacao, CondicaoPagamento, MotivoDecisao, CategoriaProduto }

public sealed class OpcaoCatalogo : Entidade
{
    public TipoOpcaoCatalogo Tipo { get; private set; }
    public string Nome { get; private set; } = "";
    public string NomeNormalizado { get; private set; } = "";
    public bool Ativo { get; private set; } = true;
    public int Ordem { get; private set; }
    public int Versao { get; private set; } = 1;
    private OpcaoCatalogo() { }
    public static OpcaoCatalogo Criar(TipoOpcaoCatalogo tipo, string nome, int ordem, Guid? id = null) => new()
    { Id = id ?? Guid.NewGuid(), Tipo = tipo, Nome = nome.Trim(), NomeNormalizado = nome.Trim().ToUpperInvariant(), Ordem = ordem };
    public void Atualizar(string nome, bool ativo, int ordem)
    { Nome = nome.Trim(); NomeNormalizado = Nome.ToUpperInvariant(); Ativo = ativo; Ordem = ordem; Versao++; }
}
