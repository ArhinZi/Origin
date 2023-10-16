using Microsoft.Xna.Framework.Graphics;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Origin.Source.IO;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;

namespace Origin.Source
{
    public static class GlobalResources
    {
        private static JsonSerializerSettings settings;
        private static HashSet<Sprite> _sprites { get; set; } = new();
        private static HashSet<TerrainMaterial> _terraMats { get; set; } = new();

        public static HashSet<Sprite> Sprites => _sprites;

        public static HashSet<TerrainMaterial> TerrainMaterials => _terraMats;

        public static Dictionary<string, Texture2D> Textures { get; private set; } = new();

        public static void Init()
        {
            //wrapper = new DataWrapper();
            settings = new JsonSerializerSettings();
            settings.Converters.Add(new SpriteJsonConverter());
            settings.Converters.Add(new TerrainMaterialJsonConverter());
        }

        public static void Read(JObject obj)
        {
            /*using StreamReader reader = new(path);
            var json = reader.ReadToEnd();*/
            List<string> sequence = new List<string>()
            {
                "Sprites",
                "TerrainMaterials"
            };

            var tok = obj.ToObject<Dictionary<string, JToken>>();
            _sprites = JsonConvert.DeserializeObject<HashSet<Sprite>>(tok["Sprites"].ToString(), settings);
            _terraMats = JsonConvert.DeserializeObject<HashSet<TerrainMaterial>>(tok["TerrainMaterials"].ToString(), settings);

            //spriteDataWrapper = JsonConvert.DeserializeObject<DataWrapper>(obj.ToString(), settings);

            return;
        }

        public static void LoadTexture(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open))
            {
                Texture2D texture = Texture2D.FromStream(OriginGame.Instance.GraphicsDevice, stream);
                texture.Name = Path.GetFileNameWithoutExtension(path);
                Textures[texture.Name] = texture;
            }
        }

        public static T GetHashSetResourceBy<T>(HashSet<T> src, string propName, string value)
        {
            T obj = src.FirstOrDefault(i => (string)(typeof(T).GetProperty(propName).GetValue(i, null)) == value);

            return obj;
        }

        public static Sprite GetSpriteByID(string ID)
        {
            // Do we have the result in the cache?
            var result = MemoryCache.Default.Get(ID) as Sprite;
            if (result != null)
                // Yay, we have it!
                return result;

            result = GetHashSetResourceBy(Sprites, "ID", ID);

            // Stores the result in the cache so that we yay the next time!
            MemoryCache.Default.Set(ID, result,
              DateTimeOffset.Now.Add(new TimeSpan(0, 0, 30, 0)));

            return result;
        }

        public static TerrainMaterial GetTerrainMaterialByID(string ID)
        {
            // Do we have the result in the cache?
            var result = MemoryCache.Default.Get(ID) as TerrainMaterial;
            if (result != null)
                // Yay, we have it!
                return result;

            result = GetHashSetResourceBy(TerrainMaterials, "ID", ID);

            // Stores the result in the cache so that we yay the next time!
            MemoryCache.Default.Set(ID, result,
              DateTimeOffset.Now.Add(new TimeSpan(0, 0, 30, 0)));

            return result;
        }
    }
}