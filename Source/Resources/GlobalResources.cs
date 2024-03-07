using ImGuiNET;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Origin.Source.Resources.Converters;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Origin.Source.Resources
{
    public static class GlobalResources
    {
        private static JsonSerializerSettings settings;

        public static List<Texture2D> Textures = new();

        public static List<Sprite> Sprites = new();
        public static List<Material> Materials = new();
        public static List<Item> Items = new();
        public static List<Construction> Constructions = new();
        public static List<Vegetation> Vegetations = new();

        public static Settings Settings = new();

        private static ConcurrentDictionary<string, int> spritesMetaIDs = new();

        public static Sprite HIDDEN_WALL_SPRITE;
        public static Sprite HIDDEN_FLOOR_SPRITE;
        public static Color HIDDEN_COLOR;

        public static Dictionary<string, ImFontPtr> Fonts = new Dictionary<string, ImFontPtr>();

        public static void Init()
        {
            //wrapper = new DataWrapper();
            settings = new JsonSerializerSettings();
            settings.Converters.Add(new PointConverter());
            settings.Converters.Add(new ColorConverter());
            settings.Converters.Add(new SpriteConverter());

            var io = ImGui.GetIO();
            GlobalResources.Fonts.Add("Bold", io.Fonts.AddFontFromFileTTF(
                "Content\\Fonts\\Nunito-Black.ttf", Global.FontSize, null, io.Fonts.GetGlyphRangesCyrillic()));
            GlobalResources.Fonts.Add("Default", io.Fonts.AddFontFromFileTTF(
                "Content\\Fonts\\Nunito-Regular.ttf", Global.FontSize, null, io.Fonts.GetGlyphRangesCyrillic()));
            GlobalResources.Fonts.Add("DefaultTitle", io.Fonts.AddFontFromFileTTF(
                "Content\\Fonts\\Nunito-Regular.ttf", Global.FontSize * 2, null, io.Fonts.GetGlyphRangesCyrillic()));
            GlobalResources.Fonts.Add("BoldTitle", io.Fonts.AddFontFromFileTTF(
                "Content\\Fonts\\Nunito-Black.ttf", Global.FontSize * 2, null, io.Fonts.GetGlyphRangesCyrillic()));
            //ImGui.GetIO()  = Fonts["Default"];
        }

        public static void ReadFromJson(JObject obj)
        {
            var tok = obj.ToObject<Dictionary<string, JToken>>();

            Sprites = JsonConvert.DeserializeObject<List<Sprite>>(tok["Sprites"].ToString(), settings);

            Materials = JsonConvert.DeserializeObject<List<Material>>(tok["Materials"].ToString(), settings);
            Items = JsonConvert.DeserializeObject<List<Item>>(tok["Items"].ToString(), settings);
            Constructions = JsonConvert.DeserializeObject<List<Construction>>(tok["Constructions"].ToString(), settings);
            Vegetations = JsonConvert.DeserializeObject<List<Vegetation>>(tok["Vegetations"].ToString(), settings);

            Settings = JsonConvert.DeserializeObject<Settings>(tok["Settings"].ToString(), settings);

            HIDDEN_WALL_SPRITE = GetResourceBy(Sprites, "ID", Settings.HiddenWallSprite);
            HIDDEN_FLOOR_SPRITE = GetResourceBy(Sprites, "ID", Settings.HiddenFloorSprite);
            HIDDEN_COLOR = GetResourceBy(Materials, "ID", "HIDDEN").Color;

            Vegetation.InitCache(Vegetations);
            return;
        }

        public static void LoadTexture(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open))
            {
                Texture2D texture = Texture2D.FromStream(Global.GraphicsDevice, stream);
                texture.Name = Path.GetFileNameWithoutExtension(path);
                Textures.Add(texture);
            }
        }

        private static ConcurrentDictionary<(Type, string, string), object> ResourceByCache = new();

        //performance increase(~10 times) confirmed
        public static T GetResourceBy<T>(List<T> src, string propName, string value)
        {
            object obj;
            if (ResourceByCache.TryGetValue((typeof(T), propName, value), out obj))
                return (T)obj;

            T Tobj = src.FirstOrDefault(i => ((string)typeof(T).GetProperty(propName).GetValue(i, null)).ToUpper() == value.ToUpper());

            ResourceByCache.TryAdd((typeof(T), propName, value), Tobj);

            return Tobj;
        }

        public static int GetResourceMetaID<T>(List<T> src, string ID)
        {
            object obj;
            if (ResourceByCache.TryGetValue((typeof(T), "ID", ID), out obj))
            {
                return src.IndexOf((T)obj);
            }
            else
            {
                return src.IndexOf((T)GetResourceBy<T>(src, "ID", ID));
            }
        }

        public static int GetResourceMetaID<T>(List<T> src, object obj)
        {
            return src.IndexOf((T)obj);
        }

        public static T GetByMetaID<T>(List<T> src, int metaID)
        {
            return src[metaID];
        }
    }
}