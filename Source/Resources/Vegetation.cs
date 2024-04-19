using Origin.Source.Utils;

using System.Collections.Generic;

namespace Origin.Source.Resources
{
    public class VegetationShape
    {
        public string Name { get; set; }
        public List<Sprite> Sprites { get; set; }
    }

    public class Drawing
    {
        public List<string> OnConstructions { get; set; }
        public List<string> OnConstructionsCategories { get; set; }
        public List<Sprite> Sprites { get; set; }

        public Dictionary<string, VegetationShape> Shapes { get; set; }
    }

    public class Vegetation : IDKeeper
    {
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
                        VegetationDrawingByConstruction.Add((veg, constr), draw);
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

        public static Dictionary<string, Vegetation> VegetationByConstruction = [];
        public static Dictionary<string, Vegetation> VegetationByConstrCategory = [];
        public static Dictionary<(Vegetation, string), List<Sprite>> VegetationSpritesByConstruction = [];
        public static Dictionary<(Vegetation, string), Drawing> VegetationDrawingByConstruction = [];
        public static Dictionary<(Vegetation, string), List<Sprite>> VegetationSpritesByConstrCategory = [];
    }
}