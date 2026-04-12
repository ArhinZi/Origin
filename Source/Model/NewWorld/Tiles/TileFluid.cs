using Origin.Source.Resources;
using System.Diagnostics;

namespace Origin.Source.Model.NewWorld
{
    public struct TileFluid
    {
        public static byte MaxVolume = 64;

        public Global.FluidType Type;

        private byte _volume;
        public byte Volume
        {
            readonly get => _volume;
            set
            {
                Debug.Assert(value <= MaxVolume);
                _volume = value;
            }
        }

        public byte Direction;

        public TileFluid()
        {
        }

        public TileFluid(Global.FluidType type, byte volume)
        {
            Type = type;
            _volume = 0;
            Volume = volume;
        }
    }
}
