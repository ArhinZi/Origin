using Microsoft.Xna.Framework.Graphics;

using System.Collections.Generic;
using System.IO;

namespace Origin.Source
{
    public static class Texture
    {
        public static readonly string MAIN_TEXTURE_NAME = "default2";

        public static Dictionary<string, Texture2D> textures { get; private set; } =
            new Dictionary<string, Texture2D>();

        public static void LoadTexture(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open))
            {
                Texture2D texture = Texture2D.FromStream(OriginGame.Instance.GraphicsDevice, stream);
                texture.Name = Path.GetFileNameWithoutExtension(path);
                textures.Add(texture.Name, texture);
            }
        }

        public static Texture2D GetTextureByName(string name)
        {
            foreach (var item in textures)
            {
                if (item.Value.Name == name)
                {
                    return item.Value;
                }
            }
            return null;
        }
    }
}