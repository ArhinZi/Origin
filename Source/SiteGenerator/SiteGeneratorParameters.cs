using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.SiteGenerator
{
    public class SiteGeneratorParameters
    {
        private Dictionary<string, Dictionary<string, IMetaParameter>> parameters = new Dictionary<string, Dictionary<string, IMetaParameter>>();

        public Parameter<T> Get<T>(string type, string name) where T : IComparable<T>
        {
            if (parameters.ContainsKey(type))
                if (parameters[type].ContainsKey(name))
                    return parameters[type][name] as Parameter<T>;
            return null;
        }

        public void Set(string type, string name, IMetaParameter parameter)
        {
            if (parameters == null)
                parameters = new Dictionary<string, Dictionary<string, IMetaParameter>>();
            if (!parameters.ContainsKey(type))
                parameters.Add(type, new Dictionary<string, IMetaParameter>());
            if (!parameters[type].ContainsKey(name))
                parameters[type].Add(name, parameter);
        }

        public SiteGeneratorParameters Clone()
        {
            SiteGeneratorParameters clone = new SiteGeneratorParameters();
            foreach (var key in parameters.Keys)
            {
                foreach (var name in parameters[key].Keys)
                {
                    clone.Set(key, name, parameters[key][name]);
                }
            }
            return clone;
        }
    }
}