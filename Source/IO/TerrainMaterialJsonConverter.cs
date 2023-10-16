using Microsoft.Xna.Framework;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;

namespace Origin.Source.IO
{
    public class TerrainMaterialJsonConverter : JsonConverter<TerrainMaterial>
    {
        public override bool CanRead => true;
        public override bool CanWrite => false;

        public override TerrainMaterial ReadJson(JsonReader reader, Type objectType, TerrainMaterial existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            try
            {
                var jsonObject = JObject.Load(reader);

                string id = jsonObject["ID"].Value<string>();

                string color = jsonObject["Color"].Value<string>();
                Color col = XMLoader.ParseColor(color);

                var spritesJson = jsonObject["Sprites"].ToObject<Dictionary<string, List<string>>>();

                // Create a dictionary to hold the actual Sprite objects
                var sprites = new Dictionary<string, List<Sprite>>();

                // Populate the sprites dictionary by looking up Sprites by name
                foreach (var categorySprites in spritesJson)
                {
                    var category = categorySprites.Key;
                    var spriteNames = categorySprites.Value;

                    var categorySpriteList = new List<Sprite>();
                    foreach (var spriteName in spriteNames)
                    {
                        if (GlobalResources.GetSpriteByID(spriteName) != default)
                        {
                            categorySpriteList.Add(GlobalResources.GetSpriteByID(spriteName));
                        }
                        else
                        {
                            // Handle the case where a Sprite with the given name is not found
                            Console.WriteLine($"Sprite not found for name: {spriteName}");
                        }
                    }

                    sprites.Add(category, categorySpriteList);
                }

                TerrainMaterial terramat = new TerrainMaterial(id, sprites, col);
                return terramat;
            }
            catch (Exception ex)
            {
                // Handle any exceptions or errors during deserialization as needed
                Console.WriteLine("Error during deserialization: " + ex.Message);
                return null;
            }
        }

        public override void WriteJson(JsonWriter writer, TerrainMaterial value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}