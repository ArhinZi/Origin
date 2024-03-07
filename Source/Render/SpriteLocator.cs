using System.Runtime.InteropServices;

namespace Origin.Source.Render
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SpriteLocator
    {
        public uint TextureMetaID;
        public uint Layer;
        public uint Index;
        public uint pud1;
    }
}