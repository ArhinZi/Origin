using Microsoft.Xna.Framework;

using Newtonsoft.Json;

using Origin.Source.Resources.Converters;
using Origin.Source.Utils;

namespace Origin.Source.Resources
{
    public class Material : IDKeeper
    {
        public string Name { get; set; }

        [JsonConverter(typeof(ColorConverter))]
        public Color Color { get; set; }

        public string Type { get; set; }
        public float Value { get; set; }

        public override string ToString()
        {
            return ID;
        }
    }
}