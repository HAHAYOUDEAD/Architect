namespace Architect
{
    public class Jsoning
    {
        public static JsonSerializerOptions GetDefaultOptions()
        {
            var options = new JsonSerializerOptions
            {
                IncludeFields = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                //UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement // .NET 8+
            };

            options.Converters.Add(new Vector3Converter());
            options.Converters.Add(new QuaternionConverter());
            options.Converters.Add(new ColorConverter());

            return options;
        }
    }

    public class Vector3Converter : JsonConverter<Vector3> // GPT
    {
        public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            float x = 0, y = 0, z = 0;
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                reader.Read(); x = reader.GetSingle();
                reader.Read(); y = reader.GetSingle();
                reader.Read(); z = reader.GetSingle();
                reader.Read(); // EndArray
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    string name = reader.GetString();
                    reader.Read();
                    switch (name)
                    {
                        case "x": x = reader.GetSingle(); break;
                        case "y": y = reader.GetSingle(); break;
                        case "z": z = reader.GetSingle(); break;
                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
            return new Vector3(x, y, z);
        }

        public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.x);
            writer.WriteNumberValue(value.y);
            writer.WriteNumberValue(value.z);
            writer.WriteEndArray();
        }
    }

    public class QuaternionConverter : JsonConverter<Quaternion> // GPT
    {
        public override Quaternion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            float x = 0, y = 0, z = 0, w = 1;
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                reader.Read(); x = reader.GetSingle();
                reader.Read(); y = reader.GetSingle();
                reader.Read(); z = reader.GetSingle();
                reader.Read(); w = reader.GetSingle();
                reader.Read();
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    string name = reader.GetString();
                    reader.Read();
                    switch (name)
                    {
                        case "x": x = reader.GetSingle(); break;
                        case "y": y = reader.GetSingle(); break;
                        case "z": z = reader.GetSingle(); break;
                        case "w": w = reader.GetSingle(); break;
                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
            return new Quaternion(x, y, z, w);
        }

        public override void Write(Utf8JsonWriter writer, Quaternion value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.x);
            writer.WriteNumberValue(value.y);
            writer.WriteNumberValue(value.z);
            writer.WriteNumberValue(value.w);
            writer.WriteEndArray();
        }
    }

    public class ColorConverter : JsonConverter<Color> // GPT
    {
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            float r = 0, g = 0, b = 0, a = 1;
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                reader.Read(); r = reader.GetSingle();
                reader.Read(); g = reader.GetSingle();
                reader.Read(); b = reader.GetSingle();
                if (reader.Read() && reader.TokenType == JsonTokenType.Number)
                {
                    a = reader.GetSingle();
                    reader.Read();
                }
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    string name = reader.GetString();
                    reader.Read();
                    switch (name)
                    {
                        case "r": r = reader.GetSingle(); break;
                        case "g": g = reader.GetSingle(); break;
                        case "b": b = reader.GetSingle(); break;
                        case "a": a = reader.GetSingle(); break;
                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
            return new Color(r, g, b, a);
        }

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.r);
            writer.WriteNumberValue(value.g);
            writer.WriteNumberValue(value.b);
            writer.WriteNumberValue(value.a);
            writer.WriteEndArray();
        }
    }
}
