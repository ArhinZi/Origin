using ImGuiNET;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Origin.Source.Resources.Converters;
using Origin.Source.Utils;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Origin.Source.Resources
{
    // TODO Start using Arch Resources
    public static class GlobalResources
    {
        private static JsonSerializerSettings settings;

        public static List<Texture2D> Textures = [];

        public static MarkedList<Sprite> Sprites = [];
        public static List<TextureInfo> TexturesInfo = [];

        public static MarkedList<Material> Materials = [];
        public static List<Item> Items = [];
        public static MarkedList<Construction> Constructions = [];
        public static MarkedList<Vegetation> Vegetations = [];

        public static Settings Settings = new();

        private static ConcurrentDictionary<string, int> spritesMetaIDs = new();

        public static Sprite HIDDEN_WALL_SPRITE;
        public static Sprite HIDDEN_FLOOR_SPRITE;
        public static Color HIDDEN_COLOR;

        public static Dictionary<string, ImFontPtr> Fonts = [];

        public static void Init()
        {
            //wrapper = new DataWrapper();

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
            settings = new JsonSerializerSettings();
            settings.Converters.Add(new PointConverter());
            settings.Converters.Add(new ColorConverter());
            settings.Converters.Add(new SpriteConverter());
            var tok = obj.ToObject<Dictionary<string, JToken>>();

            Sprites = new MarkedList<Sprite>(JsonConvert.DeserializeObject<List<Sprite>>(tok["Sprites"].ToString(), settings));
            Sprites.AddRange(JsonConvert.DeserializeObject<MarkedList<Sprite>>(tok["RotationSprites"].ToString(), settings));
            var selection = Sprites["Borders"];
            Sprites.Add(new Sprite(
                id: "RightBorder",
                GetResourceBy(Textures, "Name", "default"),
                new Rectangle()
                {
                    X = selection.RectPos.X + selection.RectPos.Width / 2,
                    Y = selection.RectPos.Y,
                    Width = selection.RectPos.Width / 2,
                    Height = selection.RectPos.Height / 4 + 1
                }));
            Sprites.Add(new Sprite(
                id: "LeftBorder",
                GetResourceBy(Textures, "Name", "default"),
                new Rectangle()
                {
                    X = selection.RectPos.X,
                    Y = selection.RectPos.Y,
                    Width = selection.RectPos.Width / 2,
                    Height = selection.RectPos.Height / 4 + 1
                }));

            settings.Converters.Clear();
            settings.Converters.Add(new PointConverter());
            settings.Converters.Add(new ColorConverter());
            settings.Converters.Add(new ResourceSpriteConverter());
            Materials = new MarkedList<Material>(JsonConvert.DeserializeObject<List<Material>>(tok["Materials"].ToString(), settings));
            Items = JsonConvert.DeserializeObject<List<Item>>(tok["Items"].ToString(), settings);
            Constructions = new MarkedList<Construction>(JsonConvert.DeserializeObject<List<Construction>>(tok["Constructions"].ToString(), settings));
            Vegetations = new MarkedList<Vegetation>(JsonConvert.DeserializeObject<List<Vegetation>>(tok["Vegetations"].ToString(), settings));
            TexturesInfo = JsonConvert.DeserializeObject<List<TextureInfo>>(tok["Textures"].ToString(), settings);

            Settings = JsonConvert.DeserializeObject<Settings>(tok["Settings"].ToString(), settings);

            HIDDEN_WALL_SPRITE = Sprites[Settings.HiddenWallSprite];
            HIDDEN_FLOOR_SPRITE = Sprites[Settings.HiddenFloorSprite];
            HIDDEN_COLOR = Materials["HIDDEN"].Color;

            Vegetation.InitCache(Vegetations);
            return;
        }

        public static void LoadTexture(string path)
        {
            using (FileStream stream = new(path, FileMode.Open))
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

        //public static int GetResourceMetaID<T>(List<T> src, string ID)
        //{
        //    object obj;
        //    if (ResourceByCache.TryGetValue((typeof(T), "ID", ID), out obj))
        //    {
        //        return src.IndexOf((T)obj);
        //    }
        //    else
        //    {
        //        return src.IndexOf((T)GetResourceBy<T>(src, "ID", ID));
        //    }
        //}

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