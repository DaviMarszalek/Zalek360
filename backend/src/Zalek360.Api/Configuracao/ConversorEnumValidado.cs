using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zalek360.Api.Configuracao;

/// <summary>
/// Enums como texto (igual ao JsonStringEnumConverter), mas recusando na leitura valores fora da lista
/// (ex.: "resultado": 99). Sem isso, o valor inválido chegava ao domínio e virava erro 500;
/// agora o binding falha e a API responde 400 no envelope padrão.
/// </summary>
public sealed class ConversorEnumValidado : JsonConverterFactory
{
    private readonly JsonStringEnumConverter _interno = new();

    public override bool CanConvert(Type tipo) => _interno.CanConvert(tipo);

    public override JsonConverter CreateConverter(Type tipo, JsonSerializerOptions opcoes) =>
        (JsonConverter)Activator.CreateInstance(typeof(Conversor<>).MakeGenericType(tipo), _interno.CreateConverter(tipo, opcoes))!;

    private sealed class Conversor<T> : JsonConverter<T> where T : struct, Enum
    {
        private readonly JsonConverter<T> _base;
        public Conversor(JsonConverter interno) => _base = (JsonConverter<T>)interno;

        public override T Read(ref Utf8JsonReader leitor, Type tipo, JsonSerializerOptions opcoes)
        {
            var valor = _base.Read(ref leitor, tipo, opcoes);
            if (!Enum.IsDefined(valor)) throw new JsonException($"Valor fora da lista permitida para {typeof(T).Name}.");
            return valor;
        }

        public override void Write(Utf8JsonWriter escritor, T valor, JsonSerializerOptions opcoes) => _base.Write(escritor, valor, opcoes);
    }
}
