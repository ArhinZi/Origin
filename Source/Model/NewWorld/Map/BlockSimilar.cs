using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Model.NewWorld.Map
{
    /// <summary>
    /// Represents a block in which all tiles are identical, storing a single tile value for the entire block.
    /// </summary>
    /// <remarks>This class is intended for scenarios where a block's tile data does not vary by position,
    /// optimizing storage and access for uniform blocks. Inherits from BlockBase and overrides tile access methods to
    /// reflect the uniformity of the block.</remarks>
    internal sealed class BlockSimilar : BlockBase
    {
        public Tile UniformTile { get; private set; }
        public override Tile GetTile(Point3 tilePosition) => UniformTile;

        public override BlockBase SetTile(Point3 tilePosition, Tile tile)
        {
            if (tile.Equals(UniformTile)) return this;
            // Since all tiles in this block are the same, we can just set the TileData to the new tile.
            // If the new tile is different from the current TileData, we convert it to a BlockPartial to accommodate the change.
            var partial = new BlockPartial(this); // копіює UniformTile на всі 256 позицій
            return partial.SetTile(tilePosition, tile);
        }
    }
}
