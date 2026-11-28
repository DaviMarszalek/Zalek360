namespace Zalek360.Application.Dtos;

/// <summary>Inscrição estadual encontrada em base pública.</summary>
public sealed record InscricaoEstadualDto(string Numero, string Uf, bool Ativa);

/// <summary>
/// Resultado da consulta de CNPJ em bases públicas. Falha, limite ou CNPJ não encontrado não são erro:
/// <c>Encontrado = false</c> e <c>Mensagem</c> orienta o cadastro manual.
/// </summary>
public sealed record ConsultaCnpjDto(
    bool Encontrado,
    string? Fonte,
    string? RazaoSocial,
    string? NomeFantasia,
    string? Telefone,
    string? Email,
    EnderecoDto? Endereco,
    IReadOnlyList<InscricaoEstadualDto> InscricoesEstaduais,
    string? Mensagem);
