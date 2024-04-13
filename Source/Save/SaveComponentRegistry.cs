using MessagePack;

namespace Origin.Source.Save
{
    [MessagePackObject(true)]
    public struct SaveComponentRegistry
    {
        public int Id;
        public int ByteSize;
        public string Type;
    }
}