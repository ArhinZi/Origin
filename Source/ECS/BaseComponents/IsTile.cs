using MessagePack;
using Origin.Source.Model.Map.Light;

namespace Origin.Source.ECS.BaseComponents
{
    [MessagePackObject]
    public struct IsTile
    {
        [Key(0)]
        public Point3 Position;

        public PackedLight GetLight(LightComponent lcomp)
        {
            lcomp.TryGetTile(Position, out PackedLight light);
            return light;
        }
    }
}