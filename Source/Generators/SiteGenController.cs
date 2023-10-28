using Arch.Core;
using Arch.Core.Extensions;
using Arch.Relationships;

using Microsoft.Xna.Framework.Graphics;

using MonoGame.Extended;

using Origin.Source.ECS;
using Origin.Source.Resources;
using Origin.Source.Utils;

using Roy_T.AStar.Graphs;
using Roy_T.AStar.Paths;
using Roy_T.AStar.Primitives;

using System;
using System.Collections.Generic;
using System.ComponentModel;

using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;
using Point3 = Origin.Source.Utils.Point3;

namespace Origin.Source.Generators
{
    public class SiteGenController
    {
        public struct HeightTile
        {
            public float Height;
            public int WaterLevel;

            public override string ToString()
            {
                return Height.ToString() + (WaterLevel > 0 ? "W" + WaterLevel.ToString() : "");
            }
        }

        public enum MapBorder
        {
            TopRight,
            BottomRight,
            BottomLeft,
            TopLeft
        }

        public class RiverData
        {
            public MapBorder StartingBorder;
            public int StartingPos = 0;
            public MapBorder EndingBorder;
            public int EndingPos = 0;
            public int Strength;

            public RiverData(MapBorder startborder, MapBorder endBorder, int strength)
            {
                StartingBorder = startborder;
                EndingBorder = endBorder;
                Strength = strength;
            }
        }

        private HeightTile[,] heightMap;
        private RiverData river;
        private GraphicsDevice _device;

        private Site _site;
        private int _seed = 553;

        public Point3 Size { get; private set; }

        public SiteGenController(GraphicsDevice device, Site site, Point3 size)
        {
            _device = device;
            _site = site;
            Size = size;
            this.river = new RiverData(MapBorder.TopLeft, MapBorder.BottomLeft, 5);
        }

        public void Init()
        {
            GenerateHeightMap(10);
            GenerateRiverOnHeightMap();
            SmoothHeightMap();
        }

        public void Visit(Point3 startPos, bool visitStart = true)
        {
            Dictionary<int, bool[,]> visited = new Dictionary<int, bool[,]>();
            Stack<Point3> stack = new Stack<Point3>();
            if (visitStart)
            {
                stack.Push(startPos);
            }
            else
            {
                foreach (var p in WorldUtils.STAR_NEIGHBOUR_PATTERN_3L())
                {
                    stack.Push(startPos + p);
                }
            }

            while (stack.Count > 0)
            {
                Point3 pos = stack.Pop();

                if (!visited.ContainsKey(pos.Z)) visited.Add(pos.Z, new bool[Size.X, Size.Y]);
                if (!pos.InBounds(Point3.Zero, Size))
                    continue;
                if (visited[pos.Z][pos.X, pos.Y])
                    continue;
                if (_site.Blocks[pos.X, pos.Y, pos.Z] != Entity.Null)
                    continue;

                var dirtDepth = 5;
                var baseHeight = (int)(Size.Z * 0.7f);
                int height = (int)(heightMap[pos.X, pos.Y].Height + baseHeight);

                // Creating Root Entity
                Entity tileEnt = _site.ECSWorld.Create(new IsTile() { Position = pos });
                _site.Blocks[pos] = tileEnt;

                bool walkable = false;
                bool isAir = false;

                // Filling The Root
                if (pos.Z <= height - dirtDepth)
                {
                    tileEnt.Add(new BaseConstruction()
                    {
                        ConstructionID = "StoneWallFloor",
                        MaterialID = "Granite"
                    });
                }
                else if (pos.Z > height - dirtDepth && pos.Z <= height)
                {
                    tileEnt.Add(new BaseConstruction()
                    {
                        ConstructionID = "SoilWallFloor",
                        MaterialID = "Dirt"
                    });

                    if (pos.Z == height)
                    {
                        tileEnt.Add<HasVegetation>();
                    }
                }
                else
                {
                    isAir = true;
                    walkable = true;
                }

                //Checking Path Ability
                Entity tmp;
                if (walkable && _site.Blocks.TryGet(pos - new Point3(0, 0, 1), out tmp) && tmp != Entity.Null && tmp.Has<BaseConstruction>())
                {
                    tileEnt.Add<TilePathAble>();
                }
                if (tileEnt.Has<BaseConstruction>() &&
                    _site.Blocks.TryGet(pos + new Point3(0, 0, 1), out tmp) && tmp != Entity.Null && !tmp.Has<BaseConstruction>())
                {
                    tmp.Add<TilePathAble>();
                }

                // Visit neighbours
                visited[pos.Z][pos.X, pos.Y] = true;
                if (isAir)
                {
                    foreach (var p in WorldUtils.STAR_NEIGHBOUR_PATTERN_3L(false))
                    {
                        stack.Push(pos + p);
                    }
                }
            }
        }

        public void GenerateHeightMap(float scale, float freq = 0.003f)
        {
            heightMap = new HeightTile[Size.X, Size.Y];
            FastNoiseLite fnl = new FastNoiseLite(_seed);
            fnl.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            fnl.SetFractalType(FastNoiseLite.FractalType.FBm);
            fnl.SetFractalOctaves(8);
            fnl.SetFrequency(freq);
            fnl.SetFractalGain(0.3f);
            for (int i = 0; i < Size.X; i++)
            {
                for (int j = 0; j < Size.Y; j++)
                {
                    heightMap[i, j].Height = ((fnl.GetNoise(i, j) + 1) / 2) * scale;
                }
            }
        }

        public void SmoothHeightMap()
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);
            HeightTile[,] newHeightMap = new HeightTile[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float totalHeight = 0;
                    int neighborCount = 0;

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;

                            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                            {
                                totalHeight += heightMap[nx, ny].Height;
                                neighborCount++;
                            }
                        }
                    }

                    // Calculate the average height of the neighbors
                    newHeightMap[x, y].Height = totalHeight / neighborCount;
                    newHeightMap[x, y].WaterLevel = heightMap[x, y].WaterLevel;
                }
            }

            // Update the original height map with the smoothed values
            heightMap = newHeightMap;
        }

        private void GenerateRiverOnHeightMap()
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);
            Node[,] nodes = new Node[width, height];
            Node start;
            Node end;

            GenPathNodes();
            start = nodes[0, Random.Shared.Next() % height];
            end = nodes[width - 1, Random.Shared.Next() % height];

            PathFinder pf = new PathFinder();
            Path path = pf.FindPath(start, end, Velocity.FromMetersPerSecond(2));

            HashSet<Point> visited = new HashSet<Point>();
            foreach (var edge in path.Edges)
            {
                Point3 pos = new Point3((int)edge.Start.Position.X, (int)edge.Start.Position.Y, (int)edge.Start.Position.Z);
                // Calculate the boundaries of the square area
                int radius = river.Strength * 5;
                int minX = Math.Max(pos.X - radius, 0);
                int maxX = Math.Min(pos.X + radius, width - 1);
                int minY = Math.Max(pos.Y - radius, 0);
                int maxY = Math.Min(pos.Y + radius, height - 1);

                // Iterate through the square area

                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        int r = IsWithinRadius(x, y, pos.X, pos.Y, radius);
                        if (r != -1 && !visited.Contains(new Point(x, y)))
                        {
                            float erosionAmount = (float)Math.Pow(r, 0.9) - 3;
                            heightMap[x, y].Height = Math.Min(heightMap[x, y].Height, erosionAmount);
                            if (heightMap[x, y].Height < -5) heightMap[x, y].Height = -5;
                        }
                        if (r != -1 && r <= river.Strength)
                        {
                            heightMap[x, y].WaterLevel = 1;
                        }
                    }
                }
            }

            #region Functions

            void GenPathNodes()
            {
                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        Node node;
                        if (nodes[i, j] == null)
                            nodes[i, j] = new Node(new Position(i, j, 0));
                        node = nodes[i, j];
                        float currH = heightMap[i, j].Height;

                        for (int x = (int)node.Position.X - 1; x <= node.Position.X + 1; x++)
                        {
                            for (int y = (int)node.Position.Y - 1; y <= node.Position.Y + 1; y++)
                            {
                                if (x >= 0 && y >= 0 &&
                                    x < Size.X && y < Size.Y &&
                                    (x != node.Position.X || y != node.Position.Y))
                                {
                                    Velocity v;
                                    if (currH - heightMap[x, y].Height >= 0)
                                        v = Velocity.FromMetersPerSecond(2f);
                                    else if (currH - heightMap[x, y].Height > -0.005)
                                        v = Velocity.FromMetersPerSecond(1f);
                                    else
                                        v = Velocity.FromMetersPerSecond(0.5f);

                                    if (nodes[x, y] == null)
                                        nodes[x, y] = new Node(new Position(x, y, 0));

                                    node.Connect(nodes[x, y], v);
                                }
                            }
                        }
                    }
                }
            }

            void FillStrength(Point pos, int radius)
            {
                // Calculate the boundaries of the square area
                int minX = Math.Max(pos.X - radius, 0);
                int maxX = Math.Min(pos.X + radius, width - 1);
                int minY = Math.Max(pos.Y - radius, 0);
                int maxY = Math.Min(pos.Y + radius, height - 1);

                // Iterate through the square area
                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        // Check if the tile (x, y) is within the circular radius
                        if (IsWithinRadius(x, y, pos.X, pos.Y, radius) != -1)
                        {
                            heightMap[x, y].WaterLevel = 1;
                        }
                    }
                }
            }
            int IsWithinRadius(int x, int y, int centerX, int centerY, int radius)
            {
                // Calculate the squared distance from the center (x, y) to (centerX, centerY)
                int dx = x - centerX;
                int dy = y - centerY;
                int squaredDistance = dx * dx + dy * dy;

                // Check if the squared distance is less than or equal to the square of the radius
                int squaredRadius = radius * radius;
                if (squaredDistance <= squaredRadius) return (int)Math.Sqrt(squaredDistance);
                else return -1;
            }

            #endregion Functions
        }

        public Texture2D HeightMapToTexture2D(float scale)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);

            // Create a new Texture2D with the same dimensions as the height map.
            Texture2D texture = new Texture2D(_device, width, height);

            // Define colors for water (blue), land (gray scale from black to white).
            Color waterColor = Color.Blue;
            Color[] landColors = new Color[256]; // 256 shades of gray
            for (int i = 0; i < 256; i++)
            {
                byte shade = (byte)(i * 255 / 255); // Map 0-255 to 0-255
                landColors[i] = new Color(shade, shade, shade);
            }

            // Create a Color array to represent the pixels of the texture.
            Color[] colors = new Color[width * height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    HeightTile tile = heightMap[x, y];

                    float normalizedHeight = (tile.Height + scale) / (scale * 2);
                    if (normalizedHeight < 0 || normalizedHeight > 1)
                        throw new Exception("Height not normalized");
                    int colorIndex = (int)(normalizedHeight * 255);
                    Color color = landColors[colorIndex];

                    if (tile.WaterLevel > 0)
                    {
                        color.B = 255;
                    }

                    // Set the color in the Color array.
                    colors[x + y * width] = color;
                }
            }

            // Set the colors to the Texture2D and return it.
            texture.SetData(colors);
            return texture;
        }
    }
}