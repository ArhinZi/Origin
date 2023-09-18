using Newtonsoft.Json.Linq;

using Origin.Source.Generators;

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
            GlobalResources.Init();
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            JObject jobj = new JObject();
            foreach (var modeFolder in Directory.GetDirectories(Path.Combine(path, "Mods\\")))
            {
                foreach (var file in GetAllFiles(modeFolder))
                {
                    if (file.Split(".").Last() == "png")
                    {
                        GlobalResources.LoadTexture(file);
                    }
                    else if (file.Split(".").Last() == "json")
                    {
                        using StreamReader reader = new(file);
                        var json = reader.ReadToEnd();
                        JObject f = JObject.Parse(json);
                        jobj.Merge(f);
                        //GlobalJsonResources.Read(file);
                    }
                }
            }
            GlobalResources.Read(jobj);
            foreach (var modeFolder in Directory.GetDirectories(Path.Combine(path, "Mods\\")))
            {
                foreach (var file in GetAllFiles(modeFolder))
                {
                    if (file.Split(".").Last() == "xml")
                    {
                        var xml = XDocument.Load(file);
                        /*
                        if (xml.Root.Name == "Sprites")
                            XMLoader.LoadSprites(file);
                        else
                        if (xml.Root.Name == "TerraMats")
                            XMLoader.LoadTerraMats(file);
                        else if (xml.Root.Name == "SitePasses")
                            SiteBlocksMaker.ReadPasses(xml.Root);
                        */
                        if (xml.Root.Name == "Materials")
                            Material.LoadMaterialsFromXML(xml.Root);
                        else if (xml.Root.Name == "TerraMats")
                            XMLoader.LoadTerraMats(file);
                        else if (xml.Root.Name == "SiteGenParameters")
                            SiteBlocksMaker.ReadParameters(xml.Root);
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