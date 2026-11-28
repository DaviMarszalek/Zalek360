using Zalek360.Application.Dtos;

namespace Zalek360.Application.Interfaces;

/// <summary>Consulta dados cadastrais e inscrições estaduais de um CNPJ em bases públicas.</summary>
public interface IConsultaCnpj
{
    /// <param name="cnpj">CNPJ já validado: exatamente 14 dígitos, sem máscara.</param>
    Task<ConsultaCnpjDto> ConsultarAsync(string cnpj, CancellationToken ct);
}
