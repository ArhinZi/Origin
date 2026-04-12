using MessagePack;
using Origin.Source.Model.Map;
using Origin.Source.Utils;

namespace Origin.Source.Save
{
    [MessagePackObject(true)]
    public struct SaveSiteDump
    {
        public int ID;
        public int CurrentLevel;
        public Point3 Size;
        public Camera2D Camera;
        public WorldRotation Rotation;
    }
}