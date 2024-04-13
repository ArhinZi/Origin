using MessagePack;

namespace Origin.Source.Save
{
    [MessagePackObject(true)]
    public struct SaveSiteDump
    {
        public int ID;
        public int CurrentLevel;
        public Point3 Size;
    }
}