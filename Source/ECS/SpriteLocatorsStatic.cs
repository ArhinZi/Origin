using Origin.Source.Render.GpuAcceleratedSpriteSystem;

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.ECS
{
    public struct SpriteLocatorsStatic
    {
        private List<SpriteLocator> _list = null;

        public List<SpriteLocator> list
        {
            get
            {
                if (_list == null) return _list = new List<SpriteLocator>();
                return _list;
            }
            set
            {
                _list = value;
            }
        }

        public SpriteLocatorsStatic()
        {
        }

        public void Clear()
        {
            list = null;
        }
    }
}