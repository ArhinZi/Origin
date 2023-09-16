using Arch.Core;
using Arch.Core.Extensions;

using Microsoft.Xna.Framework;

using Newtonsoft.Json.Linq;

using Origin.Source.Utils;

using SharpDX.MediaFoundation;

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using static System.Net.Mime.MediaTypeNames;

namespace Origin.Source.ECS
{
    public static class EntityFactory
    {
        private static Dictionary<string, List<Object>> Prefabs = new();
        private static Dictionary<string, List<Type>> PrefabsCompTypes = new();

        public static Entity CreateEntityByID(World world, string id)
        {
            Entity e = world.Create();
            for (int i = 0; i < Prefabs[id].Count(); i++)
            {
                var mi = typeof(EntityExtensions)
                    .GetMethods()
                    .FirstOrDefault(method =>
                    {
                        if (method.Name == "Add" && method.IsGenericMethod)
                        {
                            ParameterInfo[] parameters = method.GetParameters();
                            if (parameters.Length == 2 && parameters[1].IsOptional)
                            {
                                Type[] parameterTypes = method.GetGenericArguments();
                                if (parameterTypes.Length == 1)
                                    return true;
                            }
                        }
                        return false;
                    });
                MethodInfo method = mi.MakeGenericMethod(PrefabsCompTypes[id][i]);
                ParameterInfo[] methodParams = method.GetParameters();
                object[] parameters = new object[methodParams.Length];

                parameters[0] = e;
                parameters[1] = Prefabs[id][i];
                method.Invoke(e, parameters);
            }

            return e;
        }

        public static void LoadPrefabsFromXML(XElement root)
        {
            foreach (var pref in root.Elements("Prefab"))
            {
                string id = pref.Element("ID").Value;
                if (!Prefabs.ContainsKey(id)) Prefabs.Add(id, new List<object>());
                if (!PrefabsCompTypes.ContainsKey(id)) PrefabsCompTypes.Add(id, new List<Type>());
                foreach (var comp in pref.Elements("Component"))
                {
                    try
                    {
                        string name = comp.Attribute("name").Value;
                        Type type = Type.GetType("Origin.Source.ECS.Components." + name);

                        object c = Activator.CreateInstance(type);
                        foreach (var prop in comp.Elements())
                        {
                            string propName = prop.Name.LocalName;
                            if (prop.Attribute("method") != null &&
                                prop.Attribute("method").Value == "ByID")
                            {
                                object obj = GetObjById(propName, prop.Value);
                                FieldInfo field = type.GetField(propName);
                                field.SetValue(c, obj);
                            }
                            else
                                SetFieldValue(c, propName, prop.Value);
                        }
                        Prefabs[id].Add(c);
                        PrefabsCompTypes[id].Add(type);
                    }
                    catch
                    {
                        Debug.Print("Error when parsing Prefabs");
                    }
                }
            }
        }

        private static void SetFieldValue(object obj, string fieldName, string value)
        {
            Type type = obj.GetType();
            FieldInfo field = type.GetField(fieldName);

            if (field != null)
            {
                Type fieldType = field.FieldType;
                object convertedValue = Convert.ChangeType(value, fieldType);
                field.SetValue(obj, convertedValue);
            }
        }

        private static object GetObjById(string type, string id)
        {
            switch (type)
            {
                case "Material":
                    return (object)Material.Materials[id];

                case "Sprite":
                    return (object)Sprite.SpriteSet[id];

                default:
                    return null;
            }
        }
    }
}