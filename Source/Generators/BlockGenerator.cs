using Arch.Core;

using Origin.Source.ECS;
using Origin.Source.Utils;

using SharpDX.Direct2D1.Effects;
using SharpDX.Direct3D9;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Generators
{
    public class BlockGenerator
    {
        public Dictionary<string, float> Parameters { get; set; } = new Dictionary<string, float>();

        public (Entity, Entity) Generate(Point3 blockPosition, World eCSW)
        {
            FastNoiseLite fnl = new FastNoiseLite((int)Parameters["Seed"]);
            fnl.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            fnl.SetFractalType(FastNoiseLite.FractalType.FBm);
            fnl.SetFractalOctaves(8);
            fnl.SetFrequency(Parameters["Scale"]);
            fnl.SetFractalGain(0.2f);
            float h = fnl.GetNoise(blockPosition.X, blockPosition.Y) * 2 - 1;
            int height = (int)(Parameters["BaseHeight"] - h * 2);
            int localDirtDepth = (int)(Parameters["DirtDepth"] - (h * 0.5));
            localDirtDepth = localDirtDepth < 0 ? 0 : localDirtDepth;
            int z = blockPosition.Z;

            string wall, floor, embWall, embFloor;
            wall = floor = embWall = embFloor = null;
            int waterLevel = 0, lavaLevel = 0;

            if (z >= height)
            {
                /*if (z - height <= localDirtDepth)
                {
                    wall = floor = "Dirt";
                    if (z == height)
                    {
                        embFloor = "Grass";
                    }
                }*/
                //else
                {
                    wall = "GRANITE_WALL";
                    floor = "GRANITE_FLOOR";
                }
            }
            /*else if (z < 30)
            {
                wall = floor = TerrainMaterial.AIR_NULL_MAT_ID;
                waterLevel = 7;
            }*/
            else
            {
                wall = floor = null;
            }

            Entity ewall = Entity.Null, efloor = Entity.Null;
            if (wall != null)
            {
                ewall = EntityFactory.CreateEntityByID(eCSW, wall);
            }
            else
            {
                ewall = EntityFactory.CreateEntityByID(eCSW, "AIR_WALL");
            }
            if (floor != null)
            {
                efloor = EntityFactory.CreateEntityByID(eCSW, floor);
            }
            else
            {
                efloor = EntityFactory.CreateEntityByID(eCSW, "AIR_FLOOR");
            }
            return (ewall, efloor);

            /*return new Entity(site, blockPosition,
                wallMatID: wall, floorMatID: floor, embWallMatID: embWall, embFloorMatID: embFloor,
                waterLevel: waterLevel);*/
        }
    }
}