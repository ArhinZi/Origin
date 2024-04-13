using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
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
                string textureName = jsonObject["TextureName"]?.Value<string>();
                string sourceRect = jsonObject["SourceRect"]?.Value<string>();

                Texture2D texture = textureName != null ? GlobalResources.GetResourceBy(GlobalResources.Textures, "Name", textureName) : null;
                //if (texture == null)
                //{
                //    Debug.WriteLine(string.Format("ERROR: No texture with name {}", textureName));
                //    return null;
                //}

                Rectangle rectangle = sourceRect != null ? Parser.RectangleFromString(sourceRect) : Rectangle.Empty;

                IsometricDirection direction = IsometricDirection.NONE;
                if (jsonObject["SpriteDir"] != null)
                {
                    direction = (IsometricDirection)Enum.Parse(typeof(IsometricDirection), jsonObject["SpriteDir"].Value<string>());
                }

                MySpriteEffect effect = MySpriteEffect.None;
                if (jsonObject["SpriteEffect"] != null)
                {
                    string[] flagNames = jsonObject["SpriteEffect"].Value<string>().Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    effect = flagNames.Select(flagName => Enum.Parse<MySpriteEffect>(flagName.Trim())).Aggregate((e1, e2) => e1 | e2);
                }

                Dictionary<string, List<Sprite>> rot = null;
                if (jsonObject["Rotations"] != null)
                {
                    var data = JObject.Parse(jsonObject["Rotations"].ToString());
                    rot = [];

                    foreach (var rotation in data)
                    {
                        string name = rotation.Key;
                        var l = new List<Sprite>();
                        foreach (var item in rotation.Value)
                        {
                            l.Add(GlobalResources.Sprites[item.Value<string>()]);
                        }
                        rot.Add(name, l);
                    }
                }

                var sprite = new Sprite(id, texture, rectangle, direction, effect, rot);

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