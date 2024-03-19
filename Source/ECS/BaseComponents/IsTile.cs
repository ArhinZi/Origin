using Origin.Source.Model.Site.Light;

namespace Origin.Source.ECS.BaseComponents
{
    public struct IsTile
    {
        public Point3 Position;

        public PackedLight GetLight(LightComponent lcomp)
        {
            lcomp.TryGetTile(Position, out PackedLight light);
            return light;
        }
    }
}