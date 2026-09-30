namespace Zalek360.Infrastructure.Options;

public sealed class JwtOptions
{
    public const string Secao = "Jwt";
    public string Emissor { get; set; } = "Zalek360";
    public string Audiencia { get; set; } = "Zalek360.Web";
    /// <summary>Chave HMAC-SHA256 com pelo menos 32 caracteres. Nunca versionar a chave real.</summary>
    public string Chave { get; set; } = string.Empty;
    public int MinutosExpiracao { get; set; } = 600;
    public int DiasManterConectado { get; set; } = 7;
}

public sealed class ArmazenamentoOptions
{
    public const string Secao = "Armazenamento";
    public string Provedor { get; set; } = "Local";
    public string CaminhoLocal { get; set; } = "./data/uploads";
}

public sealed class AplicacaoOptions
{
    public const string Secao = "Aplicacao";
    public string FusoHorario { get; set; } = "America/Sao_Paulo";
}

public sealed class BancoOptions
{
    public const string Secao = "Banco";
    public bool AplicarMigrationsNaInicializacao { get; set; } = true;
    public bool ExecutarSeed { get; set; } = true;
    public bool VerificarEsquema { get; set; } = true;
    public string SenhaUsuariosDemo { get; set; } = "Zalek@2026";
}

public sealed class RotinasOptions
{
    public const string Secao = "Rotinas";
    public bool ExpiracaoHabilitada { get; set; } = true;
    public int ExpiracaoIntervaloMinutos { get; set; } = 60;
    public bool LimpezaAnexosHabilitada { get; set; } = true;
    public int LimpezaAnexosIntervaloHoras { get; set; } = 6;
    public int AnexoPendenteIdadeHoras { get; set; } = 24;
}
