using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WolvesvilleManager.Infrastructure.Wolvesville;

/// <summary>
/// Lê campos string de forma tolerante: a API do Wolvesville às vezes devolve um número
/// (ou booleano) onde o modelo espera texto — ex.: "status" e cores de perfil. Sem isso,
/// o System.Text.Json lança <see cref="JsonException"/> e a requisição vira um 500 opaco.
/// Números/booleanos são convertidos para texto; objetos/arrays são ignorados (viram null).
/// </summary>
public sealed class TolerantStringConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.String:
                return reader.GetString();
            case JsonTokenType.True:
                return "true";
            case JsonTokenType.False:
                return "false";
            case JsonTokenType.Number:
                return reader.TryGetInt64(out var l)
                    ? l.ToString(CultureInfo.InvariantCulture)
                    : reader.GetDouble().ToString(CultureInfo.InvariantCulture);
            default:
                // Objeto/array inesperado num campo de texto: descarta sem quebrar a leitura.
                reader.Skip();
                return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
