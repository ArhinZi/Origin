using Microsoft.Xna.Framework.Graphics;

using Arch.Persistence;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Origin.Source.Resources;
using Origin.Source.Utils;

namespace Origin.Source
{
    public class SaveGameEntity
    {
        public static List<SaveGameEntity> Saves = new List<SaveGameEntity>();

        public string Name { get; set; }
        public DateTime LastSaveTime { get; set; }
        public Texture2D Texture { get; set; }

        public string SavePath { get; set; }

        public SaveGameEntity()
        {
            Saves.Add(this);
        }

        public void Save(MainWorld world)
        {
            if (SavePath == null)
            {
                SavePath = Global.AppData;
                if (!Path.Exists(SavePath))
                    Directory.CreateDirectory(SavePath);

                SavePath = Path.Combine(SavePath, "Saves");
                if (!Path.Exists(SavePath))
                    Directory.CreateDirectory(SavePath);

                SavePath = Path.Combine(SavePath, $"{Name}");
                if (!Path.Exists(SavePath))
                    Directory.CreateDirectory(SavePath);
            }

            IniFile ini = new IniFile(Path.Combine(SavePath, "Info.ini"));
            ini.Write("Name", Name, "General");
            ini.Write("Time", LastSaveTime.ToString(), "General");
            ini.Write("Seed", world.Seed.ToString(), "General");

            Stream stream = File.Create(Path.Combine(SavePath, "ico.png"));
            Texture.SaveAsPng(stream, Texture.Width, Texture.Height);
            stream.Dispose();

            Arch.Persistence.ArchBinarySerializer abs = new Arch.Persistence.ArchBinarySerializer();
            byte[] b = abs.Serialize(world.ActiveSite.ArchWorld);
            File.WriteAllBytes(Path.Combine(SavePath, "arch.data"), b);

            /*ArchJsonSerializer ajs = new ArchJsonSerializer();
            string s = ajs.ToJson(world.ActiveSite.ArchWorld);
            File.WriteAllText(Path.Combine(SavePath, "arch.json"), s);*/
        }

        public void Load(MainWorld world)
        {
        }
    }
}