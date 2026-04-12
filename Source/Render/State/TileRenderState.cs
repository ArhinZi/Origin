using System.Collections.Generic;

namespace Origin.Source.Render.State
{
    public sealed class TileRenderState
    {
        public List<SpriteLocator> ConstructionLocators { get; } = [];
        public List<SpriteLocator> FluidLocators { get; } = [];
        public List<SpriteLocator> VegetationLocators { get; } = [];

        public bool HasAny => ConstructionLocators.Count > 0 || FluidLocators.Count > 0 || VegetationLocators.Count > 0;
    }
}
