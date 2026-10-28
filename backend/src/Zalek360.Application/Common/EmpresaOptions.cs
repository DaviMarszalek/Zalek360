namespace Zalek360.Application.Common;

/// <summary>Identificação pública da empresa usada na proposta, configurada por Empresa__*.</summary>
public sealed class EmpresaOptions
{
    public const string Secao = "Empresa";
    public string NomeFantasia { get; set; } = "Zalek Personalizados";
    public string RazaoSocial { get; set; } = "";
    public string Cnpj { get; set; } = "";
    public string Telefone { get; set; } = "";
    public string WhatsApp { get; set; } = "";
    public string Email { get; set; } = "";
    public string Site { get; set; } = "";
    public string Endereco { get; set; } = "";
    public string? CaminhoLogo { get; set; }
    public string? TextoRodape { get; set; }
}
