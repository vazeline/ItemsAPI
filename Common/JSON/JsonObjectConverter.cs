using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.JSON
{
    public class JsonObjectConverter : JsonConverter<object>
    {
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString();
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt64(out long l))
                {
                    return l;
                }

                return reader.GetDouble();
            }

            if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
            {
                return reader.GetBoolean();
            }

            if (reader.TokenType == JsonTokenType.Null || reader.TokenType == JsonTokenType.None)
            {
                return null;
            }

            // use JsonElement as fallback
            // deserialize it according to its real type
            using (JsonDocument document = JsonDocument.ParseValue(ref reader))
            {
                return document.RootElement.Clone();
            }
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case string s:
                    writer.WriteStringValue(s);
                    break;
                case int i:
                    writer.WriteNumberValue(i);
                    break;
                case long l:
                    writer.WriteNumberValue(l);
                    break;
                case double d:
                    writer.WriteNumberValue(d);
                    break;
                case decimal dec:
                    writer.WriteNumberValue(dec);
                    break;
                case float f:
                    writer.WriteNumberValue(f);
                    break;
                case bool b:
                    writer.WriteBooleanValue(b);
                    break;
                case DateTime d:
                    writer.WriteStringValue(d.ToString("o"));
                    break;
                case null:
                    writer.WriteNullValue();
                    break;
                default:
                    if (value.GetType().IsEnum)
                    {
                        writer.WriteNumberValue((int)value);
                    }
                    else if (value is JsonElement element)
                    {
                        element.WriteTo(writer);
                    }
                    else
                    {
                        throw new ArgumentException($"Unexpected value type: {value.GetType()}");
                    }

                    break;
            }
        }
    }
}
