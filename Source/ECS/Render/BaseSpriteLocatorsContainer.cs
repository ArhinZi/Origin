using MessagePack;

using Origin.Source.Render;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.ECS.Render
{
    [MessagePackObject]
    public abstract class BaseSpriteLocatorsContainer
    {
        [IgnoreMember]
        public List<SpriteLocator> List { get; } = new List<SpriteLocator>();
    }
}