using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Origin.Source.IO;
using Origin.Source.Resources.Converters;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;

namespace Origin.Source.Resources
{
    public static class GlobalResources
    {
        private static JsonSerializerSettings settings;

        public static Dictionary<string, Texture2D> Textures { get; private set; } = new();

        public static List<Sprite> Sprites = new();
        public static List<Material> Materials = new();
        public static List<Item> Items = new();
        public static List<Construction> Constructions = new();

        public static Settings Settings = new();

        public static Sprite HIDDEN_WALL_SPRITE;
        public static Sprite HIDDEN_FLOOR_SPRITE;
        public static Color HIDDEN_COLOR;

        public static void Init()
        {
            //wrapper = new DataWrapper();
            settings = new JsonSerializerSettings();
            settings.Converters.Add(new PointConverter());
            settings.Converters.Add(new ColorConverter());
            settings.Converters.Add(new SpriteConverter());
        }

        public static void ReadFromJson(JObject obj)
        {
            var tok = obj.ToObject<Dictionary<string, JToken>>();

            Sprites = JsonConvert.DeserializeObject<List<Sprite>>(tok["Sprites"].ToString(), settings);

            Materials = JsonConvert.DeserializeObject<List<Material>>(tok["Materials"].ToString(), settings);
            Items = JsonConvert.DeserializeObject<List<Item>>(tok["Items"].ToString(), settings);
            Constructions = JsonConvert.DeserializeObject<List<Construction>>(tok["Constructions"].ToString(), settings);

            Settings = JsonConvert.DeserializeObject<Settings>(tok["Settings"].ToString(), settings);

            HIDDEN_WALL_SPRITE = GetResourceBy(Sprites, "ID", Settings.HiddenWallSprite);
            HIDDEN_FLOOR_SPRITE = GetResourceBy(Sprites, "ID", Settings.HiddenFloorSprite);
            HIDDEN_COLOR = GetResourceBy(Materials, "ID", "HIDDEN").Color;

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

        private static ConcurrentDictionary<(Type, string, string), object> ResourceByCache = new();

        //performance increase(~10 times) confirmed
        public static T GetResourceBy<T>(List<T> src, string propName, string value)
        {
            if (ResourceByCache.ContainsKey((typeof(T), propName, value)))
                return (T)ResourceByCache[(typeof(T), propName, value)];

            T obj = src.FirstOrDefault(i => ((string)typeof(T).GetProperty(propName).GetValue(i, null)).ToUpper() == value.ToUpper());

            ResourceByCache.TryAdd((typeof(T), propName, value), obj);

            return obj;
        }
    }
}