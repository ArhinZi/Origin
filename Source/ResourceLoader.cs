using Origin.Source.ECS;
using Origin.Source.Generators;
using Origin.Source.Utils;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Origin.Source
{
    public static class ResourceLoader
    {
        public static void LoadResources()
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            List<string> resourceLine = new List<string>()
            {
                "Sprites", "Materials", "Entities"
            };
            foreach (var res in resourceLine)
            {
                foreach (var modeFolder in Directory.GetDirectories(Path.Combine(path, "Mods\\")))
                {
                    foreach (var file in GetAllFiles(modeFolder))
                    {
                        if (file.Split(".").Last() == "png")
                        {
                            Texture.LoadTexture(file);
                        }
                        else if (file.Split(".").Last() == "xml")
                        {
                            var xml = XDocument.Load(file);
                            if (xml.Root.Name == res)
                            {
                                if (xml.Root.Name == "Sprites")
                                    Sprite.LoadSpritesFromXML(file);
                                else if (xml.Root.Name == "Materials")
                                    Material.LoadMaterialsFromXML(xml.Root);
                                else if (xml.Root.Name == "Entities")
                                    EntityFactory.LoadPrefabsFromXML(xml.Root);
                            }
                        }
                    }
                }
            }
        }

        public static IEnumerable<string> GetAllFiles(string rootDirectory)
        {
            Queue<string> pending = new Queue<string>();
            pending.Enqueue(rootDirectory);

            while (pending.Count > 0)
            {
                string currentDirectory = pending.Dequeue();
                foreach (string file in Directory.GetFiles(currentDirectory))
                {
                    yield return file;
                }
                foreach (string subdir in Directory.GetDirectories(currentDirectory))
                {
                    pending.Enqueue(subdir);
                }
            }
        }
    }
}