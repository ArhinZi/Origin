using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Resources
{
    public class Material
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public string Type { get; set; }
        public double Value { get; set; }

        public override string ToString()
        {
            return ID;
        }
    }
}