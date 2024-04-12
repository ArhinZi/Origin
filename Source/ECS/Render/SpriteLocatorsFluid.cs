using MessagePack;

using Origin.Source.Render;

using System.Collections.Generic;

namespace Origin.Source.ECS.Render
{
    [MessagePackObject]
    public struct SpriteLocatorsFluid
    {
        [IgnoreMember]
        public List<SpriteLocator> List { get; set; } = [];

        public SpriteLocatorsFluid()
        {
        }

        public void Clear()
        {
            List = null;
        }
    }
}