using Microsoft.Xna.Framework.Graphics;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Resources.Converters
{
    public class ResourceTexture2DConverter : JsonConverter<Texture2D>
    {
        public override bool CanRead => true;
        public override bool CanWrite => false;

        public override Texture2D ReadJson(JsonReader reader, Type objectType, Texture2D existingValue,
                                        bool hasExistingValue, JsonSerializer serializer)
        {
            var texId = reader.Value.ToString();

            return GlobalResources.GetResourceBy(GlobalResources.Textures, "Name", texId);

            throw new JsonException($"Texture2D with ID '{texId}' not found in resource dictionary.");
        }

        public override void WriteJson(JsonWriter writer, Texture2D value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}