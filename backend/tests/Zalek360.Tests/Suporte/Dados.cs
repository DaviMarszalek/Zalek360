using Zalek360.Application.Common;
using Zalek360.Domain.Entities;
using Zalek360.Domain.Enums;

namespace Zalek360.Tests.Suporte;

/// <summary>Relógio fixo: "hoje" = 24/09/2026 (mesma data dos protótipos), 10:00 em Brasília.</summary>
public sealed class RelogioFixo : RelogioComFusoHorario
{
    public DateTime Agora { get; set; } = new(2026, 9, 24, 13, 0, 0, DateTimeKind.Utc);
    public RelogioFixo() : base("America/Sao_Paulo") { }
    public override DateTime AgoraUtc => Agora;
    public void Avancar(TimeSpan tempo) => Agora = Agora.Add(tempo);
}

public static class Dados
{
    public static readonly Guid UsuarioId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly DateTime Agora = new(2026, 9, 24, 13, 0, 0, DateTimeKind.Utc);
    public static readonly DateOnly Hoje = new(2026, 9, 24);

    /// <summary>CNPJ da Academia Movimento com dígitos verificadores corrigidos (ver README).</summary>
    public const string CnpjAcademia = "38221907000153";
    public const string CnpjMetalurgica = "12345678000195";
    public const string CpfJoao = "31844296016";

    public static DadosCliente ClientePj(string cnpj = CnpjAcademia, string razao = "Academia Movimento Ltda.") => new(
        TipoPessoa.PessoaJuridica, cnpj, razao, "Academia Movimento", "Rafael Costa", "Gerente",
        "4730001122", "47999887766", "contato@academiamovimento.com.br", "89201000", "Rua das Palmeiras", "120", null,
        "Centro", "Joinville", "SC");

    public static DadosCliente ClientePf(string cpf = CpfJoao) => new(
        TipoPessoa.PessoaFisica, cpf, "João Martins", null, null, null,
        null, "47996547781", "joao.martins@gmail.com", null, null, null, null, null, "Joinville", "SC");

    public static DadosPersonalizacao Personalizacao() =>
        new("Bordado", "Azul marinho", "2 cores", "9 × 5 cm", "Peito esquerdo", "Logo oficial", "Conferir grade antes da produção");

    public static DadosItemOrcamento Item(Guid produtoId, int quantidade = 150, decimal unitario = 38m, decimal personalizacao = 8m,
        Guid? id = null, params ArquivoAnexo[] anexos) =>
        new(id, produtoId, "Camisa Polo", "Camisa polo piquet", quantidade, unitario, personalizacao, Personalizacao(), anexos);

    public static DadosComerciaisOrcamento Comercial(DateOnly? validade = null, decimal desconto = 0m, int prazo = 15) =>
        new(validade ?? Hoje.AddDays(15), prazo, "50% na aprovação + 50% na entrega (PIX)", "Kit comemorativo", desconto);

    public static Orcamento Orcamento(Guid? clienteId = null, params DadosItemOrcamento[] itens) =>
        Domain.Entities.Orcamento.Criar(124, clienteId ?? Guid.NewGuid(), UsuarioId, Hoje, Comercial(),
            itens.Length > 0 ? itens : new[] { Item(Guid.NewGuid()) }, Agora, UsuarioId);

    public static Orcamento OrcamentoAguardando(Guid? clienteId = null, params DadosItemOrcamento[] itens)
    {
        var o = Orcamento(clienteId, itens);
        o.MarcarAguardandoRetorno(MeioContato.WhatsApp, Hoje, Agora, UsuarioId);
        return o;
    }
}
