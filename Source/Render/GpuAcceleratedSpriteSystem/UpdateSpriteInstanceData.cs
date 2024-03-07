using System.Runtime.InteropServices;

namespace Origin.Source.Render.GpuAcceleratedSpriteSystem
{
    [StructLayout(LayoutKind.Sequential)]
    public struct UpdateSpriteInstanceData
    {
        public uint index;
        public float pud1;
        public float pud2;
        public float pud3;
        public SpriteMainData mainData;
        public SpriteExtraData extraData;
    }
}