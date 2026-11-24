using Zalek360.Application.Common;
using Zalek360.Application.Dtos;
using Zalek360.Application.Interfaces;
using Zalek360.Domain.Common;
using Zalek360.Domain.Entities;

namespace Zalek360.Application.Services;

public sealed class ConfiguracaoCatalogoService(IGestaoCatalogoRepository repo, IUnitOfWork uow)
{
    public async Task<IReadOnlyList<OpcaoCatalogoDto>> ListarAsync(bool somenteAtivas, CancellationToken ct) =>
        (await repo.OpcoesAsync(somenteAtivas, ct)).Select(Mapear).ToList();

    public async Task<IReadOnlyList<string>> NomesAsync(TipoOpcaoCatalogo tipo, CancellationToken ct) =>
        (await repo.OpcoesAsync(true, ct)).Where(x => x.Tipo == tipo).Select(x => x.Nome).ToList();

    public async Task<OpcaoCatalogoDto> SalvarAsync(Guid? id, OpcaoCatalogoRequest r, CancellationToken ct)
    {
        var atual = id.HasValue ? await repo.OpcaoAsync(id.Value, ct) ?? throw new NaoEncontradoException("Opção") : null;
        if (atual is not null) Concorrencia.VerificarVersao(atual.Versao, r.Versao);
        var e = new Dictionary<string, string[]>();
        if (!Enum.IsDefined(r.Tipo) || atual is not null && atual.Tipo != r.Tipo) e["tipo"] = ["Selecione um tipo válido; o tipo de uma opção existente não pode ser alterado."];
        var limite = r.Tipo == TipoOpcaoCatalogo.CategoriaProduto ? 50 : 100;
        if (string.IsNullOrWhiteSpace(r.Nome) || r.Nome.Trim().Length > limite) e["nome"] = [$"Informe um nome com até {limite} caracteres."];
        if (r.Ordem < 0 || r.Ordem > 9999) e["ordem"] = ["Informe uma ordem entre 0 e 9999."];
        if (e.Count > 0) throw new ValidacaoException(CodigosErro.Validacao, "Revise a opção.", e);
        var nome = r.Nome!.Trim();
        if ((await repo.OpcoesAsync(false, ct)).Any(x => x.Id != id && x.Tipo == r.Tipo && x.NomeNormalizado == nome.ToUpperInvariant()))
            throw new ConflitoException(CodigosErro.RegistroDuplicado, "Já existe uma opção desse tipo com o mesmo nome.");
        var opcao = atual ?? OpcaoCatalogo.Criar(r.Tipo, nome, r.Ordem);
        if (atual is null) repo.Adicionar(opcao);
        opcao.Atualizar(nome, r.Ativo, r.Ordem);
        await uow.SaveChangesAsync(ct);
        return Mapear(opcao);
    }
    private static OpcaoCatalogoDto Mapear(OpcaoCatalogo x) => new(x.Id, x.Tipo, x.Nome, x.Ativo, x.Ordem, x.Versao);
}
