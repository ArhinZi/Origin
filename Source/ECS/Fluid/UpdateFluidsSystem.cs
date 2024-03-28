using Arch.CommandBuffer;
using Arch.Core;
using Arch.Core.Extensions;

using CommunityToolkit.HighPerformance;

using MessagePack;

using MonoGame.Extended.Collections;

using Origin.Source.ECS.BaseComponents;
using Origin.Source.ECS.Construction;
using Origin.Source.ECS.Vegetation.Components;
using Origin.Source.Model.Site;
using Origin.Source.Model.Site.Light;
using Origin.Source.Resources;
using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Origin.Source.ECS.Fluid
{
    internal class UpdateFluidsSystem : TickSystem
    {
        public UpdateFluidsSystem(Site site) : base(site)
        {
        }

        public override void Initialize()
        {
            var query = new QueryDescription().WithAll<IsTile, BaseConstruction>();
            _site.ArchWorld.Add(query, new IsFluidBlocker());
        }

        private Dictionary<Entity, FluidParticle> newEntity = new Dictionary<Entity, FluidParticle>();

        public override void Update(in ulong t)
        {
            if (t % 10 != 0) return;

            var commands = new CommandBuffer(_site.ArchWorld);
            var query = new QueryDescription().WithAll<IsTile, FluidParticle>();

            newEntity.Clear();
            query = new QueryDescription().WithAll<IsTile, FluidParticle>().WithNone<IsFluidStatic>();
            _site.ArchWorld.Query(in query, (Entity ent, ref IsTile tile, ref FluidParticle fluid) =>
            {
                var pos = tile.Position;
                bool IsStatic = true;

                // Checking down cell
                bool step1 = false;
                var posDown = pos + Point3.Down;
                bool waterDown = false;
                if (fluid.Volume > 0 && _site.Map.TryGet(posDown, out Entity entDown))
                {
                    Debug.Assert(entDown != Entity.Null);

                    // If cell has some fluid
                    ref FluidParticle fluidDown = ref entDown.TryGetRef<FluidParticle>(out bool exist);
                    if (exist)
                    {
                        waterDown = true;
                        if (fluidDown.Volume < FluidParticle.MaxVolume)
                        {
                            byte change = (byte)(FluidParticle.MaxVolume - fluidDown.Volume);
                            change = Math.Min(change, fluid.Volume);
                            fluidDown.Volume += change;
                            fluid.Volume -= change;

                            step1 = true;
                            IsStatic = false;
                            if (entDown.Has<IsFluidStatic>())
                                commands.Remove<IsFluidStatic>(entDown);

                            if (!entDown.Has<UpdateTileRenderSelfRequest>())
                                commands.Add<UpdateTileRenderSelfRequest>(entDown);
                        }
                    }
                    //if cell not have fluid and not FluidBlocker
                    else if (!entDown.Has<IsFluidBlocker>())
                    {
                        Debug.Assert(!entDown.Has<IsFluidStatic>());

                        if (!newEntity.ContainsKey(entDown))
                        {
                            newEntity.Add(entDown, fluid);
                            fluid.Volume = 0;
                        }
                        else
                        {
                            var part = newEntity[entDown];
                            byte change = (byte)(FluidParticle.MaxVolume - part.Volume);
                            change = Math.Min(change, fluid.Volume);
                            part.Volume += change;
                            newEntity[entDown] = part;
                            fluid.Volume -= change;
                        }

                        step1 = true;
                        IsStatic = false;
                        if (!entDown.Has<UpdateTileRenderSelfRequest>())
                            commands.Add<UpdateTileRenderSelfRequest>(entDown);
                    }
                }
                if (/*!step1 && */(fluid.Volume > 1 || waterDown))
                {
                    int max_change = Math.Max(fluid.Volume / 5, 1);
                    foreach (var n in WorldUtils.PLUS_NEIGHBOUR_PATTERN_1L(false)/*.Shuffle(Global.World.Random)*/)
                    {
                        var posn = pos + n;
                        if (fluid.Volume <= 1)
                        {
                            break;
                        }

                        if (_site.Map.TryGet(posn, out Entity entn) && entn != Entity.Null)
                        {
                            //Debug.Assert(!entn.Has<UpdateTileRenderSelfRequest>());
                            ref FluidParticle fluidn = ref entn.TryGetRef<FluidParticle>(out bool exist);
                            if (exist)
                            {
                                if (!IsStatic && entn.Has<IsFluidStatic>())
                                    commands.Remove<IsFluidStatic>(entn);
                                if (fluidn.Volume + 1 < fluid.Volume)
                                {
                                    byte change = (byte)Math.Min(Math.Max((fluid.Volume - fluidn.Volume) / 2, 1), max_change);
                                    fluidn.Volume += change;
                                    fluid.Volume -= change;

                                    IsStatic = false;

                                    if (!entn.Has<UpdateTileRenderSelfRequest>())
                                        commands.Add<UpdateTileRenderSelfRequest>(entn);
                                }
                            }
                            // if cell not have fluid and not FluidBlocker
                            else if (!entn.Has<IsFluidBlocker>())
                            {
                                Debug.Assert(!entn.Has<IsFluidStatic>());

                                IsStatic = false;
                                if (!entn.Has<UpdateTileRenderSelfRequest>())
                                    commands.Add<UpdateTileRenderSelfRequest>(entn);

                                if (!newEntity.ContainsKey(entn))
                                {
                                    newEntity.Add(entn, new FluidParticle(fluid.Type, (byte)max_change));
                                    fluid.Volume -= (byte)max_change;
                                }
                                else
                                {
                                    var part = newEntity[entn];
                                    if (part.Volume + 1 < fluid.Volume)
                                    {
                                        byte change = (byte)Math.Min((fluid.Volume - part.Volume) / 2, max_change);
                                        change = Math.Min(change, fluid.Volume);
                                        part.Volume += change;
                                        newEntity[entn] = part;
                                        fluid.Volume -= change;
                                    }
                                }
                            }
                            else
                            {
                            }
                        }
                    }
                }

                if (IsStatic && !ent.Has<IsFluidStatic>())
                {
                    commands.Add<IsFluidStatic>(ent);
                }
                // if not static -> remove static from neighbour cells
                if (!IsStatic)
                {
                    if (!ent.Has<UpdateTileRenderSelfRequest>())
                        commands.Add<UpdateTileRenderSelfRequest>(ent);
                    var posn = pos + Point3.Down;
                    if (_site.Map.TryGet(posn, out Entity entDown0) && entDown0 != Entity.Null && entDown0.Has<FluidParticle>() && entDown0.Has<IsFluidStatic>())
                    {
                        commands.Remove<IsFluidStatic>(entDown0);
                    }
                    foreach (var n in WorldUtils.PLUS_NEIGHBOUR_PATTERN_1L(false)/*.Shuffle(Global.World.Random)*/)
                    {
                        posn = pos + n;
                        if (_site.Map.TryGet(posn, out Entity entn) && entn != Entity.Null && entn.Has<FluidParticle>() && entn.Has<IsFluidStatic>())
                        {
                            commands.Remove<IsFluidStatic>(entn);
                        }
                    }
                }
                else
                {
                }

                if (fluid.Volume == 0)
                {
                    commands.Remove<FluidParticle>(ent);
                    if (ent.Has<IsFluidStatic>())
                        commands.Remove<IsFluidStatic>(ent);
                }
            });
            foreach (var item in newEntity)
            {
                item.Key.Add(item.Value);
            }
            commands.Playback();
        }
    }
}