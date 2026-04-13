using Microsoft.Xna.Framework;
using Origin.Source.Model.NewWorld;
using Origin.Source.Resources;
using Roy_T.AStar.Paths;
using Roy_T.AStar.Primitives;
using System;
using System.Collections.Generic;
using Node = Roy_T.AStar.Graphs.Node;

namespace Origin.Source.Model.Generators
{
    public class SurfacePass : AbstractPass
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
        private Point3 Size;
        private int Seed;
        private readonly SiteGenerationSettings _settings;

        public SurfacePass(Point3 size, int seed, SiteGenerationSettings settings = null)
        {
            Size = size;
            Seed = seed;
            _settings = settings ?? new SiteGenerationSettings();
            _settings.HeightScale = Math.Clamp(_settings.HeightScale, 1f, 40f);
            _settings.NoiseFrequency = Math.Clamp(_settings.NoiseFrequency, 0.0001f, 0.05f);
            _settings.NoiseOctaves = Math.Clamp(_settings.NoiseOctaves, 1, 16);
            _settings.NoiseGain = Math.Clamp(_settings.NoiseGain, 0.01f, 1f);
            _settings.BaseHeightRatio = Math.Clamp(_settings.BaseHeightRatio, 0.05f, 0.95f);
            _settings.SoilDepth = Math.Clamp(_settings.SoilDepth, 1, 64);
            _settings.SmoothIterations = Math.Clamp(_settings.SmoothIterations, 0, 16);
            _settings.RiverStrength = Math.Clamp(_settings.RiverStrength, 1, 64);
            _settings.RiverRadiusMultiplier = Math.Clamp(_settings.RiverRadiusMultiplier, 0.5f, 20f);
            _settings.RiverErosionPower = Math.Clamp(_settings.RiverErosionPower, 0.1f, 3f);
            _settings.RiverMinHeight = Math.Clamp(_settings.RiverMinHeight, -64f, 10f);

            river = new RiverData(MapBorder.TopLeft, MapBorder.BottomLeft, _settings.RiverStrength);

            GenerateHeightMap(_settings.HeightScale, _settings.NoiseFrequency);
            if (_settings.EnableRiver)
                GenerateRiverOnHeightMap();
            for (int i = 0; i < _settings.SmoothIterations; i++)
                SmoothHeightMap();
        }

        public override Tile Pass(Tile tile, Point3 pos)
        {
            var dirtDepth = _settings.SoilDepth;
            var baseHeight = (int)(Size.Z * _settings.BaseHeightRatio);

            int GetH(Point3 hpos)
            {
                return (int)(heightMap[hpos.X, hpos.Y].Height + baseHeight);
            }

            int height = GetH(pos);

            if (pos.Z <= height - dirtDepth)
            {
                tile.HasConstruction = true;
                tile.Construction = new TileConstruction
                {
                    ConstructionID = "StoneWallFloor",
                    MaterialID = "GRANITE"
                };
            }
            else if (pos.Z > height - dirtDepth && pos.Z <= height)
            {
                tile.HasConstruction = true;
                tile.Construction = new TileConstruction
                {
                    ConstructionID = "SoilWallFloor",
                    MaterialID = "DIRT"
                };
            }
            else if (pos.Z - 1 == height - dirtDepth)
            {
                if (GetH(pos + Point3.PointByDir(Global.Direction.NORTH)) == height - dirtDepth)
                {
                    tile.HasConstruction = true;
                    tile.IsRamp = true;
                    tile.Construction = new TileConstruction
                    {
                        ConstructionID = "SoilRamp",
                        MaterialID = "DIRT"
                    };
                }
            }

            return tile;
        }

        public void GenerateHeightMap(float scale, float freq = 0.003f)
        {
            heightMap = new HeightTile[Size.X, Size.Y];
            FastNoiseLite fnl = new(Seed);
            fnl.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            fnl.SetFractalType(FastNoiseLite.FractalType.FBm);
            fnl.SetFractalOctaves(_settings.NoiseOctaves);
            fnl.SetFrequency(freq);
            fnl.SetFractalGain(_settings.NoiseGain);
            for (int i = 0; i < Size.X; i++)
            {
                for (int j = 0; j < Size.Y; j++)
                {
                    heightMap[i, j].Height = (fnl.GetNoise(i, j) + 1) / 2 * scale;
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
            var rnd = new Random(Seed);
            start = nodes[0, rnd.Next(0, height)];
            end = nodes[width - 1, rnd.Next(0, height)];

            PathFinder pf = new();
            Path path = pf.FindPath(start, end, Velocity.FromMetersPerSecond(2));

            HashSet<Point> visited = [];
            foreach (var edge in path.Edges)
            {
                Point3 pos = new((int)edge.Start.Position.X, (int)edge.Start.Position.Y, (int)edge.Start.Position.Z);
                int radius = (int)river.Strength * (int)_settings.RiverRadiusMultiplier;
                int minX = Math.Max(pos.X - radius, 0);
                int maxX = Math.Min(pos.X + radius, width - 1);
                int minY = Math.Max(pos.Y - radius, 0);
                int maxY = Math.Min(pos.Y + radius, height - 1);

                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        int r = IsWithinRadius(x, y, pos.X, pos.Y, radius);
                        if (r != -1 && !visited.Contains(new Point(x, y)))
                        {
                            float erosionAmount = (float)Math.Pow(r, _settings.RiverErosionPower) - 3;
                            heightMap[x, y].Height = Math.Min(heightMap[x, y].Height, erosionAmount);
                            if (heightMap[x, y].Height < _settings.RiverMinHeight)
                                heightMap[x, y].Height = _settings.RiverMinHeight;
                        }
                        if (r != -1 && r <= river.Strength)
                        {
                            heightMap[x, y].WaterLevel = 1;
                        }
                    }
                }
            }

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
        }

        private int IsWithinRadius(int x, int y, int centerX, int centerY, int radius)
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

        /*public Texture2D HeightMapToTexture2D(float scale)
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
        }*/
    }
}