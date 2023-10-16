using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Origin.Source.Generators
{
    public static class SiteBlocksMaker
    {
        private static SortedList<int, AbstractPass> PassList = new SortedList<int, AbstractPass>();

        private static SiteGeneratorParameters Parameters = new SiteGeneratorParameters();

        public static SiteGeneratorParameters GetDefaultParameters()
        {
            return Parameters.Clone();
        }

        public static void GenerateSite(
            Site site,
            SiteGeneratorParameters parameters,
            int seed)
        {
            PassList.Add(0, new TerrainPass());
            foreach (var item in PassList.Values)
            {
                item.Run(site, site.Size, parameters, seed);
            }
        }

        public static void ReadPasses(XElement root)
        {
            if (root.Name != "SitePasses")
                throw new Exception("Not valid root element for ReadPasses");

            PassList.Clear();
            foreach (var pass in root.Elements("Pass"))
            {
                string tmpClass = pass.Element("Class").Value;
                int order = int.Parse(pass.Element("Order").Value);
                PassList.Add(order, (AbstractPass)Activator.CreateInstanceFrom(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name, tmpClass).Unwrap());
            }
        }

        public static void ReadParameters(XElement root)
        {
            if (root.Name != "SiteGenParameters")
                throw new Exception("Not valid root element for ReadParameters");

            foreach (var param in root.Elements("Parameter"))
            {
                string tmpType = param.Attribute("Type").Value;
                string tmpName = param.Element("Name").Value;
                string tmpMin = param.Element("Min").Value;
                string tmpMax = param.Element("Max").Value;
                string tmpDefault = param.Element("DefaultValue").Value;

                if (tmpType == "Int")
                {
                    Parameters.Set(tmpType, tmpName, new Parameter<int>(
                        int.Parse(tmpDefault),
                        int.Parse(tmpMin),
                        int.Parse(tmpMax),
                        tmpName));
                }
                else if (tmpType == "Float")
                {
                    Parameters.Set(tmpType, tmpName, new Parameter<float>(
                        float.Parse(tmpDefault),
                        float.Parse(tmpMin),
                        float.Parse(tmpMax),
                        tmpName));
                }
            }
        }
    }
}