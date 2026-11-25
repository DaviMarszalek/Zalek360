namespace Zalek360.Infrastructure.Options;

public sealed class EmailOptions
{
    public string Host { get; set; } = "";
    public int Porta { get; set; } = 587;
    public string Seguranca { get; set; } = "StartTls";
    public string Usuario { get; set; } = "";
    public string Senha { get; set; } = "";
    public string Remetente { get; set; } = "";
    public string NomeRemetente { get; set; } = "Zalek Personalizados";
}
