using Microsoft.Xna.Framework;

using Newtonsoft.Json;

using System;

namespace Origin.Source.Resources.Converters
{
    public class Point3Converter : JsonConverter
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
                if (parts.Length == 3 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y) && int.TryParse(parts[2], out int z))
                {
                    return new Point3(x, y, z);
                }
            }

            throw new JsonSerializationException("Invalid point3 format");
        }

        public override bool CanRead => true;
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType) => objectType == typeof(Point3);
    }
}