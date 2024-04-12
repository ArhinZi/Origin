using MessagePack;

using Origin.Source.Render;

using System.Collections.Generic;

namespace Origin.Source.ECS.Render
{
    [MessagePackObject]
    public struct SpriteLocatorsConstruction
    {
        [IgnoreMember]
        public List<SpriteLocator> List { get; set; } = [];

        public SpriteLocatorsConstruction()
        {
        }

        public void Clear()
        {
            List = null;
        }
    }
}