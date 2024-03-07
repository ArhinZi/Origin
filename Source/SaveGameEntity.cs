using Arch.Persistence;

using Microsoft.Xna.Framework.Graphics;

using Origin.Source.Model;

using Origin.Source.Resources;
using Origin.Source.Utils;

using System;
using System.IO;

namespace Origin.Source
{
    public class SaveGameEntity
    {
        public static MarkedDictionary<string, SaveGameEntity> Saves =
            new MarkedDictionary<string, SaveGameEntity>((obj) => { return obj.Name; });

        public static void ReadAllSaves()
        {
            var dirs = Directory.GetDirectories(Path.Combine(Global.AppData, "Saves"));
            foreach (var dir in dirs)
            {
                SaveGameEntity save = new SaveGameEntity(Path.GetFileName(dir));
                try
                {
                    IniFile ini = new IniFile(Path.Combine(dir, "Info.ini"));
                    //var Name = ini.Read("SaveName", "General");

                    var LastSaveTime = DateTime.Parse(ini.Read("Time", "General"));
                    var Texture = Texture2D.FromFile(Global.GraphicsDevice, Path.Combine(dir, "ico.png"));

                    save.LastSaveTime = LastSaveTime;
                    save.Texture = Texture;
                    save.SavePath = dir;
                }
                catch (Exception)
                {
                    save.Corrupted = true;
                }
                finally
                {
                }
            }
        }

        public string Name { get; set; }
        public string WorldName { get; set; }
        public DateTime LastSaveTime { get; set; }
        public Texture2D Texture { get; set; }

        public string SavePath { get; set; }

        public bool Corrupted { get; private set; } = false;

        public SaveGameEntity(string Name)
        {
            this.Name = Name;
            Saves.Add(this);
        }

        public void Save(World world)
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
            ini.Write("SaveName", Name, "General");
            ini.Write("Time", LastSaveTime.ToString(), "General");
            ini.Write("Seed", world.Seed.ToString(), "General");
            ini.Write("WorldName", world.Name, "General");

            Stream stream = File.Create(Path.Combine(SavePath, "ico.png"));
            Texture.SaveAsPng(stream, Texture.Width, Texture.Height);
            stream.Dispose();

            Arch.Persistence.ArchBinarySerializer abs = new Arch.Persistence.ArchBinarySerializer();
            byte[] b = abs.Serialize(world.ActiveSite.ArchWorld);
            File.WriteAllBytes(Path.Combine(SavePath, "arch.data"), b);

            ArchJsonSerializer ajs = new ArchJsonSerializer();
            string s = ajs.ToJson(world.ActiveSite.ArchWorld);
            File.WriteAllText(Path.Combine(SavePath, "arch.json"), s);
        }

        public void Load(World world)
        {
            /*if (File.Exists(Path.Combine(SavePath, "arch.data")))
            {
                world.ActiveSite.ArchWorld.Clear();

                *//*Arch.Persistence.ArchBinarySerializer abs = new Arch.Persistence.ArchBinarySerializer();
                byte[] b = File.ReadAllBytes(Path.Combine(SavePath, "arch.data"));
                abs.Deserialize(world.ActiveSite.ArchWorld, b);*//*

                ArchJsonSerializer ajs = new ArchJsonSerializer();
                string s = File.ReadAllText(Path.Combine(SavePath, "arch.json"));
                ajs.FromJson(world.ActiveSite.ArchWorld, s);

                world.ActiveSite.PostInit();
            }
            else
            {
                Debug.Write("Cant load save. No save file");
            }*/
        }
    }
}