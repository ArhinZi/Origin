using Microsoft.Xna.Framework.Graphics;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Origin.Source.IO;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source
{
    public class VariableData<T>
    {
        public string VariableName { get; set; }
        public T Data { get; set; }
    }

    public class DataWrapper
    {
        public Dictionary<string, Sprite> Sprites { get; set; } = new();

        public DataWrapper()
        {
        }
    }

    public static class GlobalResources
    {
        private static JsonSerializerSettings settings;
        private static DataWrapper dataWrapper;

        public static Dictionary<string, Sprite> Sprites => dataWrapper.Sprites;

        public static Dictionary<string, Texture2D> Textures { get; private set; } = new();

        public static void Init()
        {
            //wrapper = new DataWrapper();
            settings = new JsonSerializerSettings();
            settings.Converters.Add(new SpriteJsonConverter());
        }

        public static void Read(JObject obj)
        {
            /*using StreamReader reader = new(path);
            var json = reader.ReadToEnd();*/
            List<string> sequence = new List<string>()
            {
                "Sprites",
                "Material"
            };
            var toks = obj.Children();
            foreach (var item in sequence)
            {
                foreach (var tok in toks)
                {
                    if (tok.Path == item)
                    {
                    }
                }
            }
            //spritesWrapper = obj.ToObject<SpritesWrapper>(new JsonSerializer(settings));
            dataWrapper = JsonConvert.DeserializeObject<DataWrapper>(obj.ToString(), settings);

            return;
        }

        public static void LoadTexture(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open))
            {
                Texture2D texture = Texture2D.FromStream(OriginGame.Instance.GraphicsDevice, stream);
                texture.Name = Path.GetFileNameWithoutExtension(path);
                Textures.Add(texture.Name, texture);
            }
        }
    }
}