using Microsoft.Xna.Framework;

using Newtonsoft.Json;

using System;

namespace Origin.Source.Resources.Converters
{
    public class ColorConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                string value = (string)reader.Value;
                string[] parts = value.Split(' ');
                if (parts.Length == 4 &&
                    int.TryParse(parts[0], out int a) &&
                    int.TryParse(parts[1], out int b) &&
                    int.TryParse(parts[2], out int c) &&
                    int.TryParse(parts[3], out int d))
                {
                    return new Color(a, b, c, d);
                }
            }

            throw new JsonSerializationException("Invalid point format");
        }

        public override bool CanRead => true;
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType) => objectType == typeof(Color);
    }
}