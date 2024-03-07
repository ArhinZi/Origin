using Origin.Source.Render.GpuAcceleratedSpriteSystem;

using System.Collections.Generic;

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