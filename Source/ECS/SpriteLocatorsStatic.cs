using MessagePack;

using Origin.Source.Render;

using System.Collections.Generic;

namespace Origin.Source.ECS
{
    [MessagePackObject]
    public struct SpriteLocatorsStatic
    {
        [IgnoreMember]
        private List<SpriteLocator> _list = null;

        [IgnoreMember]
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