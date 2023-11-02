using Microsoft.Xna.Framework;

using Newtonsoft.Json;

using System;

namespace Origin.Source.Resources.Converters
{
    public class PointConverter : JsonConverter
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
                if (parts.Length == 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
                {
                    return new Point(x, y);
                }
            }

            throw new JsonSerializationException("Invalid point format");
        }

        public override bool CanRead => true;
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType) => objectType == typeof(Point);
    }
}