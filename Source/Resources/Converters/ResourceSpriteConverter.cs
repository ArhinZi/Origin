using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Resources.Converters
{
    public class ResourceSpriteConverter : JsonConverter<Sprite>
    {
        public override bool CanRead => true;
        public override bool CanWrite => false;

        public override Sprite ReadJson(JsonReader reader, Type objectType, Sprite existingValue,
                                        bool hasExistingValue, JsonSerializer serializer)
        {
            var spriteId = reader.Value.ToString();

            return GlobalResources.Sprites[spriteId];

            throw new JsonException($"Sprite with ID '{spriteId}' not found in resource dictionary.");
        }

        public override void WriteJson(JsonWriter writer, Sprite value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}