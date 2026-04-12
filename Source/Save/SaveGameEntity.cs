using Arch.Core.Utils;
using Arch.Persistence;

using MessagePack;

using Microsoft.Xna.Framework.Graphics;

using Origin.Source.Model;
using Origin.Source.Model.Map;
using Origin.Source.Resources;
using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Origin.Source.Save
{
    public class SaveGameEntity
    {
        public static MarkedDictionary<string, SaveGameEntity> Saves =
            new((obj) => { return obj.Name; });

        public static void ReadAllSaves()
        {
            var dirs = Directory.GetDirectories(Path.Combine(Global.AppData, "Saves"));
            foreach (var dir in dirs)
            {
                SaveGameEntity save = new(Path.GetFileName(dir));
                try
                {
                    IniFile ini = new(Path.Combine(dir, "Info.ini"));
                    var Name = ini.Read("SaveName", "General");

                    var LastSaveTime = DateTime.Parse(ini.Read("Time", "General"));
                    Texture2D Texture = new Texture2D(Global.GraphicsDevice, 128, 128);
                    //using (FileStream stream = new(Path.Combine(dir, "ico.png"), FileMode.Open))
                    //{
                    //    Texture = Texture2D.FromStream(Global.GraphicsDevice, stream);
                    //}

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
        public DateTime LastSaveTime { get; set; }
        public Texture2D Texture { get; set; }

        public string SavePath { get; set; }

        public bool Corrupted { get; private set; } = false;

        public SaveGameEntity(string Name)
        {
            this.Name = Name;
            Saves.Add(this);
        }

        /*
                public string GetSavePath()
                {
                    if (SavePath == null)
                        SavePath = Global.AppData;

                    if (!Path.Exists(SavePath))
                        Directory.CreateDirectory(SavePath);

                    if (!Path.Exists(SavePath))
                    {
                        SavePath = Path.Combine(SavePath, "Saves");
                        Directory.CreateDirectory(SavePath);
                    }

                    if (!Path.Exists(SavePath))
                    {
                        SavePath = Path.Combine(SavePath, $"{Name}");
                        Directory.CreateDirectory(SavePath);
                    }

                    return SavePath;
                }*/

        public void Save(World world)
        {
            SavePath = Path.Combine(Global.AppData, "Saves", world.Name);
            if (!Path.Exists(SavePath))
            {
                Directory.CreateDirectory(SavePath);
            }

            IniFile ini = new(Path.Combine(SavePath, "Info.ini"));
            ini.Write("SaveName", Name, "General");
            ini.Write("Time", LastSaveTime.ToString(), "General");
            ini.Write("Seed", world.Seed.ToString(), "General");
            ini.Write("Tick", world.TimeManager.Ticks.ToString(), "PRIVATE");

            //using (Stream stream = File.Create(Path.Combine(SavePath, "ico.png")))
            //{
            //    Texture.SaveAsPng(stream, Texture.Width, Texture.Height);
            //}

            var types = ComponentRegistry.TypeToComponentType.ToArray();
            SaveComponentRegistry[] saveObjs = new SaveComponentRegistry[types.Length];
            for (int i = 0; i < saveObjs.Length; i++)
            {
                saveObjs[i] = new SaveComponentRegistry()
                {
                    Id = types[i].Value.Id,
                    ByteSize = types[i].Value.ByteSize,
                    Type = types[i].Key.ToString()
                };
            }
            byte[] br = MessagePackSerializer.Serialize(typeof(SaveComponentRegistry[]), saveObjs);
            File.WriteAllBytes(Path.Combine(SavePath, $"arch.reg"), br);

            foreach (var item in world.Sites)
            {
                ArchBinarySerializer abs = new();
                byte[] b = abs.Serialize(item.ArchWorld);
                File.WriteAllBytes(Path.Combine(SavePath, $"{item.ID}.data"), b);

                var dump = item.Dump();
                byte[] bd = MessagePackSerializer.Serialize(typeof(SaveSiteDump), dump);
                File.WriteAllBytes(Path.Combine(SavePath, $"{item.ID}.dump"), bd);
            }

            //MessagePackSerializer.Serialize()
        }

        public World Load()
        {
            SavePath = Path.Combine(Global.AppData, "Saves", Name);
            IniFile ini = new(Path.Combine(SavePath, "Info.ini"));
            var name = ini.Read("SaveName", "General");
            var time = ini.Read("Time", "General");
            var seed = int.Parse(ini.Read("Seed", "General"));
            var ticks = ini.Read("Tick", "PRIVATE");

            // Clear ComponentRegistry before restore
            var typesOld = ComponentRegistry.TypeToComponentType;
            foreach (var item in typesOld)
            {
                ComponentRegistry.Remove(item.Key);
            }

            byte[] br = File.ReadAllBytes(Path.Combine(SavePath, $"arch.reg"));
            var typesNew = (SaveComponentRegistry[])MessagePackSerializer.Deserialize(typeof(SaveComponentRegistry[]), br);
            foreach (var nnew in typesNew)
            {
                Type type = Type.GetType(nnew.Type);
                ComponentType cmp = new ComponentType(id: nnew.Id, byteSize: nnew.ByteSize);
                ComponentRegistry.Add(type, cmp);
            }

            var dirs = Directory.GetFiles(SavePath, "*.data");
            List<Site> sites = new List<Site>();
            var world = new World();
            World.Load(world, name, seed, this, sites);
            foreach (var item in dirs)
            {
                int ID = int.Parse(Path.GetFileNameWithoutExtension(item));
                ArchBinarySerializer abs = new();
                byte[] b = File.ReadAllBytes(Path.Combine(SavePath, $"{ID}.data"));
                var arch = abs.Deserialize(b);
                byte[] bd = File.ReadAllBytes(Path.Combine(SavePath, $"{ID}.dump"));
                var dump = (SaveSiteDump)MessagePackSerializer.Deserialize(typeof(SaveSiteDump), bd);
                sites.Add(new Site(world, dump, arch));
            }
            Global.World = world;

            world.PostInitialize(true);
            world.TimeManager.Ticks = ulong.Parse(ticks);
            return world;
        }
    }
}