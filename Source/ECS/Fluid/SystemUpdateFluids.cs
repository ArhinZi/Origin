using Arch.Core;
using Arch.Core.Extensions;
using CommunityToolkit.HighPerformance;
using Origin.Source.ECS.BaseComponents;
using Origin.Source.ECS.Construction;
using Origin.Source.ECS.Render;
using Origin.Source.Model.Map;
using Origin.Source.Model.NewWorld;
using Tile = Origin.Source.Model.NewWorld.Tile;
using Origin.Source.Utils;
using System;
using System.Collections.Generic;

namespace Origin.Source.ECS.Fluid
{
    internal class SystemUpdateFluids : TickSystem
    {
        public SystemUpdateFluids(Origin.Source.Model.Map.Site site) : base(site)
        {
        }

        public override void Initialize()
        {
        }

        private readonly Dictionary<Point3, TileFluid> newFluid = [];

        public override void Update(in ulong t)
        {
            if (t % 10 != 0) return;

            bool dirty = false;
            newFluid.Clear();

            //    for (int z = 0; z < _site.Size.Z; z++)
            //    {
            //        for (int x = 0; x < _site.Size.X; x++)
            //        {
            //            for (int y = 0; y < _site.Size.Y; y++)
            //            {
            //                Point3 pos = new(x, y, z);
            //                ref Tile tile = ref _site.Map.GetRef(pos);
            //                if (!tile.Exists || !tile.HasFluid || tile.IsFluidStatic)
            //                    continue;

            //                bool isStatic = true;
            //                bool waterDown = false;

            //                Point3 posDown = pos + Point3.Down;
            //                if (tile.Fluid.Volume > 0 && _site.Map.InBounds(posDown))
            //                {
            //                    ref Tile down = ref _site.Map.GetRef(posDown);
            //                    if (down.Exists)
            //                    {
            //                        if (down.HasFluid)
            //                        {
            //                            waterDown = true;
            //                            if (down.Fluid.Volume < FluidParticle.MaxVolume)
            //                            {
            //                                byte change = (byte)(FluidParticle.MaxVolume - down.Fluid.Volume);
            //                                change = Math.Min(change, tile.Fluid.Volume);
            //                                down.Fluid.Volume += change;
            //                                tile.Fluid.Volume -= change;
            //                                down.IsFluidStatic = false;
            //                                isStatic = false;
            //                                dirty = true;
            //                            }
            //                        }
            //                        else if (!down.IsFluidBlocker)
            //                        {
            //                            if (!newFluid.ContainsKey(posDown))
            //                            {
            //                                newFluid.Add(posDown, tile.Fluid);
            //                                tile.Fluid.Volume = 0;
            //                            }
            //                            else
            //                            {
            //                                var part = newFluid[posDown];
            //                                byte change = (byte)(FluidParticle.MaxVolume - part.Volume);
            //                                change = Math.Min(change, tile.Fluid.Volume);
            //                                part.Volume += change;
            //                                newFluid[posDown] = part;
            //                                tile.Fluid.Volume -= change;
            //                            }

            //                            isStatic = false;
            //                            dirty = true;
            //                        }
            //                    }
            //                }

            //                if (tile.Fluid.Volume > 1 || waterDown)
            //                {
            //                    int maxChange = Math.Max(tile.Fluid.Volume / 5, 1);
            //                    foreach (var offset in WorldUtils.PLUS_NEIGHBOUR_PATTERN_1L(false))
            //                    {
            //                        Point3 posn = pos + offset;
            //                        if (tile.Fluid.Volume <= 1 || !_site.Map.InBounds(posn))
            //                            break;

            //                        ref Tile neighbour = ref _site.Map.GetRef(posn);
            //                        if (!neighbour.Exists)
            //                            continue;

            //                        if (neighbour.HasFluid)
            //                        {
            //                            if (!isStatic)
            //                                neighbour.IsFluidStatic = false;
            //                            if (neighbour.Fluid.Volume + 1 < tile.Fluid.Volume)
            //                            {
            //                                byte change = (byte)Math.Min(Math.Max((tile.Fluid.Volume - neighbour.Fluid.Volume) / 2, 1), maxChange);
            //                                neighbour.Fluid.Volume += change;
            //                                tile.Fluid.Volume -= change;
            //                                isStatic = false;
            //                                dirty = true;
            //                            }
            //                        }
            //                        else if (!neighbour.IsFluidBlocker)
            //                        {
            //                            isStatic = false;
            //                            dirty = true;

            //                            if (!newFluid.ContainsKey(posn))
            //                            {
            //                                newFluid.Add(posn, new FluidParticle(tile.Fluid.Type, (byte)maxChange));
            //                                tile.Fluid.Volume -= (byte)maxChange;
            //                            }
            //                            else
            //                            {
            //                                var part = newFluid[posn];
            //                                if (part.Volume + 1 < tile.Fluid.Volume)
            //                                {
            //                                    byte change = (byte)Math.Min((tile.Fluid.Volume - part.Volume) / 2, maxChange);
            //                                    change = Math.Min(change, tile.Fluid.Volume);
            //                                    part.Volume += change;
            //                                    newFluid[posn] = part;
            //                                    tile.Fluid.Volume -= change;
            //                                }
            //                            }
            //                        }
            //                    }
            //                }

            //                tile.IsFluidStatic = isStatic;
            //                if (!isStatic)
            //                {
            //                    foreach (var offset in WorldUtils.PLUS_NEIGHBOUR_PATTERN_3L(false))
            //                    {
            //                        Point3 posn = pos + offset;
            //                        if (_site.Map.InBounds(posn))
            //                        {
            //                            ref Tile neighbour = ref _site.Map.GetRef(posn);
            //                            if (neighbour.Exists && neighbour.HasFluid)
            //                            {
            //                                neighbour.IsFluidStatic = false;
            //                            }
            //                        }
            //                    }
            //                }

            //                if (tile.Fluid.Volume == 0)
            //                {
            //                    tile.HasFluid = false;
            //                    tile.IsFluidStatic = false;
            //                    dirty = true;
            //                }
            //            }
            //        }
            //    }

            //    foreach (var item in newFluid)
            //    {
            //        ref Tile tile = ref _site.Map.GetRef(item.Key);
            //        if (!tile.Exists)
            //            continue;

            //        tile.HasFluid = true;
            //        tile.Fluid = item.Value;
            //        tile.IsFluidStatic = false;
            //        dirty = true;
            //    }

            //    if (dirty)
            //    {
            //        _site.InvalidateRender();
            //    }
        }
    }
}