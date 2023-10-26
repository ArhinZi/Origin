using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Resources
{
    public class Item
    {
        public string ID { get; set; }
        public string Category { get; set; }
        public List<string> AllowedMaterialTypes { get; set; }
        public List<object> AllowedMaterials { get; set; }

        public override string ToString()
        {
            return ID;
        }
    }
}