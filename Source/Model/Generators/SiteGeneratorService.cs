using Arch.Core;
using Arch.Core.Extensions;

using MonoGame.Extended;

using Origin.Source.Model.NewWorld;
using Origin.Source.Resources;
using Origin.Source.Utils;

using System.Collections.Generic;

namespace Origin.Source.Model.Generators
{
    public class SiteGeneratorService
    {
        private List<AbstractPass> passes;

        private Site _site;
        private readonly int _seed;
        private readonly SiteGenerationSettings _settings;

        public Point3 Size { get; private set; }

        public SiteGeneratorService(Site site, Point3 size, int seed = 553, SiteGenerationSettings settings = null)
        {
            _site = site;
            Size = size;
            _seed = seed;
            _settings = settings ?? new SiteGenerationSettings();

            passes =
            [
                new SurfacePass(Size, _seed, _settings)
            ];
        }

        public void Visit(Point3 startPos, bool visitStart = true, bool upd = false)
        {
            Dictionary<int, bool[,]> visited = [];
            List<Point3> visitedTiles = [];
            Stack<Point3> stack = new();
            foreach (var p in WorldUtils.STAR_NEIGHBOUR_PATTERN_3L(visitStart))
            {
                stack.Push(startPos + p);
            }

            // Fill surface tiles
            while (stack.Count > 0)
            {
                Point3 pos = stack.Pop();

                if (!visited.ContainsKey(pos.Z)) visited.Add(pos.Z, new bool[Size.X, Size.Y]);
                if (!pos.InBounds(Point3.Zero, Size))
                    continue;
                if (visited[pos.Z][pos.X, pos.Y])
                    continue;
                if (_site.Map[pos].Exists)
                    continue;

                Tile tile = new()
                {
                    Exists = true
                };

                // Passes
                foreach (var pass in passes)
                {
                    tile = pass.Pass(tile, pos);
                }

                tile.IsFluidBlocker = tile.HasConstruction && !tile.IsRamp;
                _site.Map[pos] = tile;
                visitedTiles.Add(pos);

                if (tile.IsAir)
                {
                    if (_site.Map.TryGet(pos - Point3.Up, out Tile below) && below.Exists && below.HasConstruction)
                    {
                        var current = _site.Map[pos];
                        current.IsWalkable = true;
                        current.WalkableConstructionBelowMetaID = below.Construction.ConstructionMetaID;
                        _site.Map[pos] = current;
                    }
                }
                else if (_site.Map.TryGet(pos + Point3.Up, out Tile above) && above.Exists && !above.HasConstruction)
                {
                    var tileAbove = _site.Map[pos + Point3.Up];
                    tileAbove.IsWalkable = true;
                    tileAbove.WalkableConstructionBelowMetaID = tile.Construction.ConstructionMetaID;
                    _site.Map[pos + Point3.Up] = tileAbove;
                }

                // Visit neighbours
                visited[pos.Z][pos.X, pos.Y] = true;
                if (tile.IsAir)
                {
                    foreach (var p in WorldUtils.STAR_NEIGHBOUR_PATTERN_3L(false))
                    {
                        stack.Push(pos + p);
                    }
                }
            }

            // Fill ramps
            for (int i = 0; i < visitedTiles.Count; i++)
            {
                Point3 pos = visitedTiles[i];
                Tile current = _site.Map[pos];

                if (!current.Exists || current.HasConstruction) continue;
                if (!_site.Map.TryGet(pos + Point3.Down, out Tile below) || !below.Exists) continue;
                if (!_site.Map.TryGet(pos + Point3.Up, out Tile above) || !above.Exists) continue;

                if (above.HasConstruction) continue;
                if (!below.HasConstruction) continue;
                if (below.Construction.Construction.Type != "WallFloor") continue;

                Material rmat = below.Construction.Material;

                Tile north = _site.Map.TryGet(pos + Point3.North, out var northTile) ? northTile : default;
                Tile south = _site.Map.TryGet(pos + Point3.South, out var southTile) ? southTile : default;
                Tile west = _site.Map.TryGet(pos + Point3.West, out var westTile) ? westTile : default;
                Tile east = _site.Map.TryGet(pos + Point3.East, out var eastTile) ? eastTile : default;

                Point3 rdir = Point3.Zero;
                if (north.Exists && north.HasConstruction && north.Construction.Construction.Type == "WallFloor") rdir += Point3.North;
                if (south.Exists && south.HasConstruction && south.Construction.Construction.Type == "WallFloor") rdir += Point3.South;
                if (west.Exists && west.HasConstruction && west.Construction.Construction.Type == "WallFloor") rdir += Point3.West;
                if (east.Exists && east.HasConstruction && east.Construction.Construction.Type == "WallFloor") rdir += Point3.East;

                rdir *= -1;
                Global.Direction rddir = Point3.DirByPoint(rdir);

                if (rddir == Global.Direction.NONE)
                    continue;

                current.HasConstruction = true;
                current.IsRamp = true;
                current.IsFluidBlocker = false;
                current.IsWalkable = false;
                current.Construction = new TileConstruction()
                {
                    ConstructionID = "SoilRamp",
                    MaterialID = rmat.ID
                };
                current.HasConstructionShape = true;
                current.HasConstructionRotation = true;
                current.ConstructionRotation = new TileConstructionRotation()
                {
                    Direction = rddir
                };

                if (rddir == Global.Direction.NORTH || rddir == Global.Direction.SOUTH || rddir == Global.Direction.EAST || rddir == Global.Direction.WEST)
                {
                    current.ConstructionShape = new TileConstructionShape()
                    {
                        Name = "Slope"
                    };
                }
                else
                {
                    current.ConstructionShape = new TileConstructionShape()
                    {
                        Name = "CornerIn"
                    };
                }

                _site.Map[pos] = current;
            }

            if (visitedTiles.Count > 0 || upd)
            {
                _site.InvalidateRender(visitedTiles, true);
            }
        }
    }
}