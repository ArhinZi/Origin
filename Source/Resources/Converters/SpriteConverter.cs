using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Diagnostics;
using System.Linq;

using Parser = Origin.Source.Utils.Parser;

namespace Origin.Source.Resources.Converters
{
    public class SpriteConverter : JsonConverter<Sprite>
    {
        public override bool CanRead => true;
        public override bool CanWrite => false;

        public override Sprite ReadJson(JsonReader reader, Type objectType, Sprite existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            try
            {
                var jsonObject = JObject.Load(reader);

                string id = jsonObject["ID"].Value<string>();
                string textureName = jsonObject["TextureName"].Value<string>();
                string sourceRect = jsonObject["SourceRect"].Value<string>();

                Texture2D texture = GlobalResources.Textures.ContainsKey(textureName) ? GlobalResources.Textures[textureName] : null;
                if (texture == null)
                {
                    Debug.WriteLine(string.Format("ERROR: No texture with name {}", textureName));
                    return null;
                }

                Rectangle rectangle = Parser.RectangleFromString(sourceRect);

                // Create your Texture2D and Rectangle objects here based on textureName and sourceRect.
                // You'll need to implement this part according to your application's logic.

                IsometricDirection direction = IsometricDirection.NONE; // You can set a default value.
                if (jsonObject["SpriteDir"] != null)
                {
                    direction = (IsometricDirection)Enum.Parse(typeof(IsometricDirection), jsonObject["SpriteDir"].Value<string>());
                }

                MySpriteEffect effect = MySpriteEffect.None; // You can set a default value.
                if (jsonObject["SpriteEffect"] != null)
                {
                    string[] flagNames = jsonObject["SpriteEffect"].Value<string>().Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    effect = flagNames.Select(flagName => Enum.Parse<MySpriteEffect>(flagName.Trim())).Aggregate((e1, e2) => e1 | e2);
                }

                // Create and return a new Sprite object
                var sprite = new Sprite(id, texture, rectangle, direction, effect);

                // Add the sprite to your SpriteSet if needed
                //GlobalResources.Sprites[id] = sprite;

                return sprite;
            }
            catch (Exception ex)
            {
                // Handle any exceptions or errors during deserialization as needed
                Console.WriteLine("Error during deserialization: " + ex.Message);
                return null;
            }
        }

        public override void WriteJson(JsonWriter writer, Sprite value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}