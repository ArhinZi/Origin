using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Resources
{
    public class Drawing
    {
        public List<string> OnConstructions { get; set; }
        public List<string> OnConstructionsCategories { get; set; }
        public List<string> Sprites { get; set; }
    }

    public class Vegetation
    {
        public string ID { get; set; }
        public List<Drawing> Drawing { get; set; }

        public override string ToString()
        {
            return ID;
        }

        public static void InitCache(List<Vegetation> vegetations)
        {
            foreach (var veg in vegetations)
            {
                foreach (var draw in veg.Drawing)
                {
                    foreach (var constr in draw.OnConstructions)
                    {
                        VegetationByConstruction.Add(constr, veg);
                        VegetationSpritesByConstruction.Add((veg, constr), draw.Sprites);
                    }
                    foreach (var cat in draw.OnConstructionsCategories)
                    {
                        VegetationByConstrCategory.Add(cat, veg);
                        VegetationSpritesByConstrCategory.Add((veg, cat), draw.Sprites);
                    }
                }
            }
        }

        public static Dictionary<string, Vegetation> VegetationByConstruction = new();
        public static Dictionary<string, Vegetation> VegetationByConstrCategory = new();
        public static Dictionary<(Vegetation, string), List<string>> VegetationSpritesByConstruction = new();
        public static Dictionary<(Vegetation, string), List<string>> VegetationSpritesByConstrCategory = new();
    }
}