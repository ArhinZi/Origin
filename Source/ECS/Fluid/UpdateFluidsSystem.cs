using Arch.CommandBuffer;
using Arch.Core;
using Arch.Core.Extensions;

using MessagePack;

using Origin.Source.ECS.BaseComponents;
using Origin.Source.ECS.Construction;
using Origin.Source.ECS.Vegetation.Components;
using Origin.Source.Model.Site;
using Origin.Source.Model.Site.Light;
using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

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

            newEntity.Clear();
            var query = new QueryDescription().WithAll<IsTile, FluidParticle>().WithNone<IsFluidStatic>();
            _site.ArchWorld.Query(in query, (Entity ent, ref IsTile tile, ref FluidParticle fluid) =>
            {
                var pos = tile.Position;
                bool IsStatic = true;

                if (fluid.Volume > 0)
                {
                    // Checking down cell
                    bool step1 = false;
                    var posDown = pos + Point3.Down;
                    if (_site.Map.TryGet(posDown, out Entity entDown) && entDown != Entity.Null)
                    {
                        // If cell has some fluid
                        if (entDown.Has<FluidParticle>())
                        {
                            ref FluidParticle fluidDown = ref entDown.Get<FluidParticle>();
                            if (fluidDown.Volume < 64)
                            {
                                byte change = ((byte)(64 - fluidDown.Volume));
                                change = Math.Min(change, fluid.Volume);
                                fluidDown.Volume += change;
                                fluid.Volume -= change;

                                step1 = true;
                                IsStatic = false;
                                if (entDown.Has<IsFluidStatic>())
                                    commands.Remove<IsFluidStatic>(entDown);

                                commands.Add<UpdateTileRenderSelfRequest>(entDown);
                            }
                        }
                        // if cell not have fluid and not FluidBlocker
                        else if (!entDown.Has<IsFluidBlocker>())
                        {
                            commands.Add(entDown, fluid);
                            fluid.Volume = 0;
                            step1 = true;
                            IsStatic = false;
                            commands.Add<UpdateTileRenderSelfRequest>(entDown);
                        }
                    }
                    if (!step1)
                    {
                        byte max_change = Math.Min((byte)4, fluid.Volume);
                        if (max_change == 0 && fluid.Volume > 1) { max_change = 1; }
                        // Check plus neighbours
                        if (max_change > 0)
                            foreach (var n in WorldUtils.PLUS_NEIGHBOUR_PATTERN_1L())
                            {
                                if (fluid.Volume < max_change) break;
                                var posn = pos + n;
                                if (_site.Map.TryGet(posn, out Entity entn) && entn != Entity.Null)
                                {
                                    // If cell has some fluid
                                    if (entn.Has<FluidParticle>())
                                    {
                                        ref FluidParticle fluidn = ref entn.Get<FluidParticle>();
                                        if (fluidn.Volume < fluid.Volume)
                                        {
                                            byte change = ((byte)(fluidn.Volume - fluid.Volume));
                                            change = Math.Min(change, max_change);
                                            fluidn.Volume += change;
                                            fluid.Volume -= change;

                                            IsStatic = false;
                                            if (entn.Has<IsFluidStatic>())
                                                commands.Remove<IsFluidStatic>(entn);

                                            commands.Add<UpdateTileRenderSelfRequest>(entn);
                                        }
                                    }
                                    // if cell not have fluid and not FluidBlocker
                                    else if (!entn.Has<IsFluidBlocker>())
                                    {
                                        Debug.Assert(fluid.Volume > 0);
                                        Debug.Assert(!entn.Has<IsFluidStatic>());

                                        //commands.Add(entn, new FluidParticle()
                                        //{
                                        //    Type = fluid.Type,
                                        //    Volume = max_change
                                        //});
                                        byte change = max_change;
                                        if (!newEntity.ContainsKey(entn))
                                            newEntity[entn] = new FluidParticle()
                                            {
                                                Type = fluid.Type,
                                                Volume = change
                                            };
                                        else
                                        {
                                            change = (byte)Math.Min(max_change, fluid.Volume - newEntity[entn].Volume);
                                            var val = newEntity[entn];
                                            val.Volume += change;
                                            newEntity[entn] = val;
                                        }

                                        fluid.Volume -= change;
                                        IsStatic = false;
                                        commands.Add<UpdateTileRenderSelfRequest>(entn);
                                    }
                                }
                            }
                    }
                }

                if (IsStatic && !ent.Has<IsFluidStatic>())
                {
                    //commands.Add<IsFluidStatic>(ent);
                    //commands.Add<UpdateTileRenderSelfRequest>(ent);
                }
                if (!IsStatic)
                {
                    commands.Add<UpdateTileRenderSelfRequest>(ent);
                }

                if (fluid.Volume == 0)
                {
                    commands.Remove<FluidParticle>(ent);
                    //if (ent.Has<IsFluidStatic>())
                    //commands.Remove<IsFluidStatic>(ent);
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