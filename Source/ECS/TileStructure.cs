using Arch.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.ECS
{
    public enum DrawSpriteTypes
    {
        DefOrRand,
        Dir
    }

    public struct TileStructure
    {
        public TerrainMaterial WallMaterial;
        public TerrainMaterial WallEmbeddedMaterial;
        public TerrainMaterial FloorMaterial;
        public TerrainMaterial FloorEmbeddedMaterial;

        public static TileStructure Null = new TileStructure()
        {
            WallMaterial = null,
            WallEmbeddedMaterial = null,
            FloorMaterial = null,
            FloorEmbeddedMaterial = null
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(TileStructure left, TileStructure right)
        {
            return left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(TileStructure left, TileStructure right)
        {
            return !left.Equals(right);
        }
    }
}