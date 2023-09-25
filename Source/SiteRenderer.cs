using Arch.Bus;
using Arch.Core;
using Arch.Core.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Origin.Source.ECS;
using Origin.Source.Events;
using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;

using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Origin.Source
{
    /*internal enum VisBufField : byte
    {
        FloorVisible = 1,
        WallVisible
    }

    internal enum RenderLayer : int
    {
        Wall,
        Floor,
        EmbeddedWall,
        EmbeddedFloor
    }*/

    public enum VertexBufferLayer
    {
        HiddenBack,
        Back,
        HiddenFront,
        Front
    }

    public partial class SiteRenderer : IDisposable
    {
        public Site Site;

        public static bool ALL_DISCOVERED = false;

        public Point ChunkSize;

        private Point3 _chunksCount;
        private int _drawLowest;
        private int _drawHighest;

        private int seed = 234223534;

        private SiteVertexBufferChunk[,,] _renderChunkArray;

        private HashSet<Point3> _reloadChunkList = new HashSet<Point3>();

        private Effect _customEffect;
        private AlphaTestEffect _alphaTestEffect;

        private GraphicsDevice _graphicsDevice;
        private SpriteBatch _spriteBatch;

        public bool HalfWallMode { get; private set; } = false;

        private Sprite lborderSprite = GlobalResources.GetSpriteByID("LeftBorder");
        private Sprite rborderSprite = GlobalResources.GetSpriteByID("RightBorder");
        private Color borderColor = new Color(0, 0, 0, 150);

        /// <summary>
        /// Z offset for every Left2Right diagonal block lines.
        /// Blocks in the far diagonal line are appearing behind the ones in the near line.
        /// </summary>
        public static readonly float Z_DIAGONAL_OFFSET = 0.01f;

        public static readonly float Z_LEVEL_OFFSET = 0.01f;

        public static readonly Point BASE_CHUNK_SIZE = new Point(64, 64);
        public static readonly int ONE_MOMENT_DRAW_LEVELS = 32;

        public SiteRenderer(Site site, GraphicsDevice graphicDevice)
        {
            Site = site;
            //_visBuffer = new byte[_site.Size.X, _site.Size.Y, _site.Size.Z];

            ChunkSize = BASE_CHUNK_SIZE;
            //if (ChunkSize.X < Site.Size.X) ChunkSize.X = Site.Size.X;
            //if (ChunkSize.Y < Site.Size.Y) ChunkSize.Y = Site.Size.Y;
            if (Site.Size.X % ChunkSize.X != 0 || Site.Size.Y % ChunkSize.Y != 0) throw new Exception("Site size is invalid!");

            _drawHighest = Site.CurrentLevel;
            _drawLowest = DiffUtils.GetOrBound(_drawHighest - ONE_MOMENT_DRAW_LEVELS + 1, 0, _drawHighest);

            _chunksCount = new Point3(Site.Size.X / ChunkSize.X, Site.Size.Y / ChunkSize.Y, Site.Size.Z);

            _renderChunkArray = new SiteVertexBufferChunk[_chunksCount.X, _chunksCount.Y, _chunksCount.Z];

            _reloadChunkList = new HashSet<Point3>();

            _graphicsDevice = graphicDevice;
            _spriteBatch = new SpriteBatch(OriginGame.Instance.GraphicsDevice);

            _customEffect = OriginGame.Instance.Content.Load<Effect>("FX/MegaShader");
            _alphaTestEffect = new AlphaTestEffect(_graphicsDevice);

            CalcVisibility();
            FillAll();

            Hook();
        }

        [Event]
        public void OnHalfWallModeChanged(HalfWallModeChanged modeChanged)
        {
            HalfWallMode = !HalfWallMode;
            /*for (int l = 0; l < _chunksCount.Z; l++)
            {
                for (int x = 0; x < _chunksCount.X; x++)
                {
                    for (int y = 0; y < _chunksCount.Y; y++)
                    {
                        _renderChunkArray[x, y, l].Clear(VertexBufferType.Static);
                    }
                }
            }*/
            //FillAll();
            if (HalfWallMode)
                FillLevel(Site.CurrentLevel, true);
            else FillLevel(Site.CurrentLevel, false);
        }

        private void CalcChunkCellsVisibility(Point3 chunkCoord)
        {
            if (chunkCoord.X >= 0 && chunkCoord.X < _chunksCount.X &&
                chunkCoord.Y >= 0 && chunkCoord.Y < _chunksCount.Y &&
                chunkCoord.Z >= 0 && chunkCoord.Z < _chunksCount.Z)
                for (int tileInChunkCoordX = 0; tileInChunkCoordX < ChunkSize.X; tileInChunkCoordX++)
                {
                    for (int tileInChunkCoordY = 0; tileInChunkCoordY < ChunkSize.Y; tileInChunkCoordY++)
                    {
                        int tileCoordX = chunkCoord.X * ChunkSize.X + tileInChunkCoordX;
                        int tileCoordY = chunkCoord.Y * ChunkSize.Y + tileInChunkCoordY;
                        Entity tile = Site.Blocks[tileCoordX, tileCoordY, chunkCoord.Z];
                        Point3 pos = new Point3(tileCoordX, tileCoordY, chunkCoord.Z);

                        TileStructure structure;
                        bool hasStructure = tile.TryGet<TileStructure>(out structure);
                        ref var visibility = ref tile.Get<TileVisibility>();

                        if (hasStructure && structure.FloorMaterial != null)
                        {
                            // Check if tile have neighbors in TL & TR & BL & BR borders
                            TileStructure Nstructure;
                            if (
                                // Check BR
                                tileCoordX + 1 <= Site.Size.X &&
                                    (tileCoordX + 1 == Site.Size.X ||
                                    Site.Blocks[tileCoordX + 1, tileCoordY, chunkCoord.Z].TryGet(out Nstructure) &&
                                    Nstructure.WallMaterial != null)
                                    &&
                                // Check BL
                                tileCoordY + 1 <= Site.Size.Y &&
                                    (tileCoordY + 1 == Site.Size.Y ||
                                    Site.Blocks[tileCoordX, tileCoordY + 1, chunkCoord.Z].TryGet(out Nstructure) &&
                                    Nstructure.WallMaterial != null)
                                    &&
                                // Check TL
                                tileCoordX >= 0 &&
                                    (tileCoordX == 0 ||
                                    Site.Blocks[tileCoordX - 1, tileCoordY, chunkCoord.Z].TryGet(out Nstructure) &&
                                    Nstructure.WallMaterial != null)
                                    &&
                                // Check TR
                                tileCoordY >= 0 &&
                                    (tileCoordY == 0 ||
                                    Site.Blocks[tileCoordX, tileCoordY - 1, chunkCoord.Z].TryGet(out Nstructure) &&
                                    Nstructure.WallMaterial != null)
                                    )
                            {
                                // Then at least wall is invisible
                                visibility.WallVisible = visibility.WallDiscovered = false;

                                if (tileCoordX + 1 == Site.Size.X || tileCoordY + 1 == Site.Size.Y)
                                {
                                    visibility.WallVisible = visibility.FloorVisible = true;
                                    visibility.WallDiscovered = visibility.FloorDiscovered = false;
                                }

                                // Check if tile have neighbor above
                                if (chunkCoord.Z + 1 < Site.Size.Z &&
                                    Site.Blocks[tileCoordX, tileCoordY, chunkCoord.Z + 1].Has<TileStructure>() &&
                                    Site.Blocks[tileCoordX, tileCoordY, chunkCoord.Z + 1].Get<TileStructure>().WallMaterial != null)
                                {
                                    // Then floor is invisible
                                    visibility.FloorDiscovered = visibility.FloorVisible = false;
                                }
                                else
                                {
                                    // Else floor is visible
                                    visibility.FloorVisible = visibility.FloorDiscovered = true;
                                }
                            }
                            else
                            {
                                // Else both visible
                                visibility.WallVisible = visibility.WallDiscovered = true;
                                visibility.FloorVisible = visibility.FloorDiscovered = true;
                            }

                            if (!visibility.FloorDiscovered && tileCoordX + 1 == Site.Size.X || tileCoordY + 1 == Site.Size.Y)
                                visibility.FloorVisible = true;
                        }
                    }
                }
        }

        private void CalcVisibility()
        {
            for (int l = 0; l < _chunksCount.Z; l++)
            {
                for (int x = 0; x < _chunksCount.X; x++)
                {
                    for (int y = 0; y < _chunksCount.Y; y++)
                    {
                        CalcChunkCellsVisibility(new Point3(x, y, l));
                    }
                }
            }
        }

        /// <summary>
        /// Fill chunk vertices in already created _renderChunkArray
        /// </summary>
        private void FillChunkVertices(
            Point3 chunkCoord, bool HalfWall = false)
        {
            if (chunkCoord.X >= 0 && chunkCoord.X < _chunksCount.X &&
                chunkCoord.Y >= 0 && chunkCoord.Y < _chunksCount.Y &&
                chunkCoord.Z >= 0 && chunkCoord.Z < _chunksCount.Z)
            {
                // Loop through each tile block in the chunk
                _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].IsFullyHidded = true;
                for (int tileInChunkCoordX = 0; tileInChunkCoordX < ChunkSize.X; tileInChunkCoordX++)
                {
                    for (int tileInChunkCoordY = 0; tileInChunkCoordY < ChunkSize.Y; tileInChunkCoordY++)
                    {
                        int tileCoordX = chunkCoord.X * ChunkSize.X + tileInChunkCoordX;
                        int tileCoordY = chunkCoord.Y * ChunkSize.Y + tileInChunkCoordY;
                        Entity tile = Site.Blocks[tileCoordX, tileCoordY, chunkCoord.Z];

                        int rand = seed + tileCoordX + tileCoordY + chunkCoord.Z;
                        Random random = new Random(rand);
                        rand = random.Next();

                        TileStructure structure;
                        bool hasStructure = tile.TryGet<TileStructure>(out structure);
                        ref var visibility = ref tile.Get<TileVisibility>();

                        if (hasStructure && structure.WallMaterial != null && visibility.WallVisible && visibility.WallDiscovered)
                        {
                            TerrainMaterial wall = structure.WallMaterial;
                            string spriteType = "Wall";
                            Point spriteShift = new Point(0, 0);
                            if ((HalfWall || HalfWallMode && chunkCoord.Z == Site.CurrentLevel) && wall.Sprites.ContainsKey("Floor"))
                            {
                                spriteType = "Floor";
                                spriteShift = new Point(0, (Sprite.TILE_SIZE.Y - Sprite.FLOOR_YOFFSET));
                            }
                            Sprite sprite = wall.Sprites[spriteType][rand % wall.Sprites[spriteType].Count];
                            Color c = structure.WallMaterial.Color;

                            _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].AddSprite(
                                VertexBufferType.Static,
                                (int)VertexBufferLayer.Back,
                                sprite, c, new Point3(tileCoordX, tileCoordY, chunkCoord.Z),
                                new Point(0, 0) + spriteShift);

                            if (!Site.Blocks[Math.Max(tileCoordX - 1, 0), tileCoordY, chunkCoord.Z].Has<TileStructure>() ||
                                Site.Blocks[Math.Max(tileCoordX - 1, 0), tileCoordY, chunkCoord.Z].Has<TileStructure>() &&
                                Site.Blocks[Math.Max(tileCoordX - 1, 0), tileCoordY, chunkCoord.Z].Get<TileStructure>().WallMaterial == null)
                                _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].AddSprite(
                                    VertexBufferType.Static,
                                    (int)VertexBufferLayer.Back,
                                    lborderSprite, borderColor, new Point3(tileCoordX, tileCoordY, chunkCoord.Z),
                                    new Point(0, 0) + spriteShift);

                            if (!Site.Blocks[tileCoordX, Math.Max(tileCoordY - 1, 0), chunkCoord.Z].Has<TileStructure>() ||
                                Site.Blocks[tileCoordX, Math.Max(tileCoordY - 1, 0), chunkCoord.Z].Has<TileStructure>() &&
                                Site.Blocks[tileCoordX, Math.Max(tileCoordY - 1, 0), chunkCoord.Z].Get<TileStructure>().WallMaterial == null)
                                _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].AddSprite(
                                    VertexBufferType.Static,
                                    (int)VertexBufferLayer.Back,
                                    rborderSprite, borderColor, new Point3(tileCoordX, tileCoordY, chunkCoord.Z),
                                    new Point(Sprite.TILE_SIZE.X / 2, 0) + spriteShift);

                            if ((structure.WallEmbeddedMaterial != null && visibility.WallVisible))
                            {
                                TerrainMaterial embfloor = structure.WallEmbeddedMaterial;
                                sprite = embfloor.Sprites["EmbeddedWall"][rand % embfloor.Sprites["EmbeddedWall"].Count];
                                c = structure.WallEmbeddedMaterial.Color;
                                _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].AddSprite(
                                VertexBufferType.Static,
                                (int)VertexBufferLayer.Back,
                                    sprite, c, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, 0));
                            }
                            _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].IsFullyHidded = false;
                        }
                        else if (hasStructure && !visibility.WallDiscovered)
                        {
                            TerrainMaterial tm = GlobalResources.GetTerrainMaterialByID(TerrainMaterial.HIDDEN_MAT_ID);
                            Sprite sprite;
                            Color c = Color.Wheat;
                            sprite = tm.Sprites["Wall"][0];
                            //sprite = tm.Sprites["Wall"][0];
                            c = tm.Color;
                            if (visibility.WallVisible)
                                _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].AddSprite(
                                    VertexBufferType.Static,
                                    (int)VertexBufferLayer.Back,
                                    sprite, c, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, 0));
                            else
                                _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].AddSprite(
                                    VertexBufferType.Static,
                                    (int)VertexBufferLayer.HiddenBack,
                                    sprite, c, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, 0));
                        }

                        if (hasStructure && structure.FloorMaterial != null && visibility.FloorVisible && visibility.FloorDiscovered)
                        {
                            TerrainMaterial floor = structure.FloorMaterial;
                            Sprite sprite = floor.Sprites["Floor"][rand % floor.Sprites["Floor"].Count];
                            Color c = structure.FloorMaterial.Color;
                            _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].AddSprite(
                                VertexBufferType.Static,
                                (int)VertexBufferLayer.Front,
                                sprite, c, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, -Sprite.FLOOR_YOFFSET));

                            if (!Site.Blocks[Math.Max(tileCoordX - 1, 0), tileCoordY, chunkCoord.Z].Has<TileStructure>() ||
                                Site.Blocks[Math.Max(tileCoordX - 1, 0), tileCoordY, chunkCoord.Z].Has<TileStructure>() &&
                                Site.Blocks[Math.Max(tileCoordX - 1, 0), tileCoordY, chunkCoord.Z].Get<TileStructure>().FloorMaterial == null)
                                _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].AddSprite(
                                    VertexBufferType.Static,
                                    (int)VertexBufferLayer.Front,
                                    lborderSprite, borderColor, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, -Sprite.FLOOR_YOFFSET - 1));
                            if (!Site.Blocks[tileCoordX, Math.Max(tileCoordY - 1, 0), chunkCoord.Z].Has<TileStructure>() ||
                                Site.Blocks[tileCoordX, Math.Max(tileCoordY - 1, 0), chunkCoord.Z].Has<TileStructure>() &&
                                Site.Blocks[tileCoordX, Math.Max(tileCoordY - 1, 0), chunkCoord.Z].Get<TileStructure>().FloorMaterial == null)
                                _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].AddSprite(
                                    VertexBufferType.Static,
                                    (int)VertexBufferLayer.Front,
                                    rborderSprite, borderColor, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(Sprite.TILE_SIZE.X / 2, -Sprite.FLOOR_YOFFSET - 1));

                            if (structure.FloorEmbeddedMaterial != null && visibility.FloorVisible)
                            {
                                TerrainMaterial embfloor = structure.FloorEmbeddedMaterial;
                                sprite = embfloor.Sprites["EmbeddedFloor"][rand % embfloor.Sprites["EmbeddedFloor"].Count];
                                c = structure.FloorEmbeddedMaterial.Color;
                                _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].AddSprite(
                                    VertexBufferType.Static,
                                    (int)VertexBufferLayer.Front,
                                    sprite, c, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, -Sprite.FLOOR_YOFFSET));
                            }
                            _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].IsFullyHidded = false;
                        }
                        else if (hasStructure && structure.FloorMaterial != null && !visibility.FloorDiscovered &&
                            (tileCoordX == Site.Size.X - 1 || tileCoordY == Site.Size.Y - 1))
                        {
                            TerrainMaterial tm = GlobalResources.GetTerrainMaterialByID(TerrainMaterial.HIDDEN_MAT_ID);
                            Sprite sprite;
                            Color c = Color.Wheat;
                            sprite = tm.Sprites["Floor"][0];
                            c = tm.Color;
                            if (visibility.FloorVisible)
                                _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].AddSprite(
                                    VertexBufferType.Static,
                                    (int)VertexBufferLayer.Front,
                                    sprite, c, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, -Sprite.FLOOR_YOFFSET));
                            else
                                _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].AddSprite(
                                VertexBufferType.Static,
                                (int)VertexBufferLayer.HiddenFront,
                                sprite, c, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, -Sprite.FLOOR_YOFFSET));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Fill whole level again
        /// </summary>
        /// <param name="level"></param>
        private void FillLevel(int level, bool HalfWall = false)
        {
            //for (int x = 0; x < _chunksCount.X; x++)
            Parallel.For(0, _chunksCount.X, x =>
            {
                for (int y = 0; y < _chunksCount.Y; y++)
                {
                    if (_renderChunkArray[x, y, level] == null)
                        _renderChunkArray[x, y, level] = new SiteVertexBufferChunk(this, new Point3(x, y, level));
                    if (!_renderChunkArray[x, y, level].IsFullyHidded)
                    {
                        _renderChunkArray[x, y, level].Clear(VertexBufferType.Static);

                        FillChunkVertices(new Point3(x, y, level), HalfWall);
                    }
                }
            });
        }

        private void FillAll()
        {
            Parallel.For(0, _chunksCount.Z, z =>
            {
                FillLevel(z);
            });
        }

        private void SetLevel(int level)
        {
            for (int x = 0; x < _chunksCount.X; x++)
            {
                for (int y = 0; y < _chunksCount.Y; y++)
                {
                    _renderChunkArray[x, y, level].SetStaticBuffer();
                }
            }
        }

        private void FillChunk(Point3 chunkPos)
        {
            if (chunkPos.X >= 0 && chunkPos.X < _chunksCount.X &&
                chunkPos.Y >= 0 && chunkPos.Y < _chunksCount.Y)
            {
                if (_renderChunkArray[chunkPos.X, chunkPos.Y, chunkPos.Z] == null)
                    _renderChunkArray[chunkPos.X, chunkPos.Y, chunkPos.Z] = new SiteVertexBufferChunk(this, new Point3(chunkPos.X, chunkPos.Y, chunkPos.Z));
                _renderChunkArray[chunkPos.X, chunkPos.Y, chunkPos.Z].Clear(VertexBufferType.Static);
                FillChunkVertices(chunkPos);
            }
        }

        private void SetChunk(Point3 chunkPos)
        {
            if (chunkPos.X >= 0 && chunkPos.X < _chunksCount.X &&
                chunkPos.Y >= 0 && chunkPos.Y < _chunksCount.Y)
                _renderChunkArray[chunkPos.X, chunkPos.Y, chunkPos.Z].SetStaticBuffer();
        }

        public void Update(GameTime gameTime)
        {
            // Check if CurrentLevel changed and redraw what need to redraw
            if (_drawHighest != Site.CurrentLevel)
            {
                if (HalfWallMode)
                {
                    FillLevel(Site.PreviousLevel, false);
                    FillLevel(Site.CurrentLevel, true);
                }
                _drawHighest = Site.CurrentLevel;
                _drawLowest = DiffUtils.GetOrBound(_drawHighest - ONE_MOMENT_DRAW_LEVELS + 1, 0, _drawHighest);
            }

            // Collect all ChunksToReload and redraw them
            foreach (var item in Site.BlocksToReload)
            {
                if (item.Z >= _drawLowest && item.Z <= _drawHighest)
                {
                    int chunkX = (int)item.X / ChunkSize.X;
                    int chunkY = (int)item.Y / ChunkSize.Y;
                    _reloadChunkList.Add(new Point3(chunkX, chunkY, item.Z));
                }
            }
            Site.BlocksToReload.Clear();
        }

        public void Draw(GameTime gameTime)
        {
            PrepareVertices(gameTime);

            DrawVertices(gameTime);
        }

        private void PrepareVertices(GameTime gameTime)
        {
            // Make chunk reload smoother
            if (_reloadChunkList.Count > 0 && gameTime.TotalGameTime.Ticks % 12 == 0)
            {
                Point3 toReload = _reloadChunkList.ToList()[0];

                List<Point3> neighbours = new List<Point3>()
                {
                    new Point3(0, 0, 0),
                    new Point3(-1, 0, 0),new Point3(0, -1, 0),
                    new Point3(1, 0, 0),new Point3(0, 1, 0)
                };
                Parallel.ForEach(neighbours, neighbour =>
                {
                    Point3 p = toReload + neighbour;
                    CalcChunkCellsVisibility(p);
                    FillChunk(p);
                });
                Parallel.ForEach(neighbours, neighbour =>
                {
                    Point3 p = toReload + neighbour + new Point3(0, 0, -1);
                    CalcChunkCellsVisibility(p);
                    FillChunk(p);
                });
                foreach (var neighbor in neighbours)
                    SetChunk(toReload + neighbor);

                foreach (var neighbor in neighbours)
                    SetChunk(toReload + neighbor + new Point3(0, 0, -1));

                _reloadChunkList.Remove(toReload);
            }

            // Test drawing mouse selection on selectedBlock
            {
                Entity tile = Site.SelectedBlock;

                if (tile != Entity.Null)
                {
                    ref var onTile = ref tile.Get<OnSitePosition>();
                    int blocksUnder = 0;
                    for (int i = 1; i < ONE_MOMENT_DRAW_LEVELS; i++)
                    {
                        if (!Site.Blocks[onTile.position.X, onTile.position.Y, onTile.position.Z - i].Has<TileStructure>())
                        {
                            Point3 pos = new Point3(onTile.position.X, onTile.position.Y, onTile.position.Z - i);
                            Sprite sprite2 = GlobalResources.GetSpriteByID("SelectionWall");
                            _renderChunkArray[pos.X / ChunkSize.X,
                                            pos.Y / ChunkSize.Y,
                                            pos.Z].AddSprite(
                                VertexBufferType.Dynamic,
                                (int)VertexBufferLayer.Back,
                                sprite2, new Color(30, 0, 0, 200), pos, new Point(0, 0)
                                );
                            _renderChunkArray[pos.X / ChunkSize.X,
                                            pos.Y / ChunkSize.Y,
                                            pos.Z].IsFullyHidded = false;
                        }
                        else break;
                    }
                    Sprite sprite = GlobalResources.GetSpriteByID("SolidSelectionWall");
                    _renderChunkArray[onTile.position.X / ChunkSize.X, onTile.position.Y / ChunkSize.Y, onTile.position.Z].AddSprite(
                        VertexBufferType.Dynamic,
                        (int)VertexBufferLayer.Back,
                        sprite, new Color(30, 0, 0, 100), onTile.position, new Point(0, 0)
                        );
                    _renderChunkArray[onTile.position.X / ChunkSize.X, onTile.position.Y / ChunkSize.Y, onTile.position.Z].IsFullyHidded = false;
                }
            }

            // Draw Entities

            /*var query = new QueryDescription().WithAll<DrawComponent, OnSitePosition>();

            Site.World.ECSworld.Query(in query, (in Entity entity) =>
            {
                var position = entity.Get<OnSitePosition>();
                var draw = entity.Get<DrawComponent>();
                if (position.Position.Z <= _drawHighest && position.Position.Z > _drawLowest &&
                    position.Site == Site)
                {
                    Sprite sprite;
                    if (position.DirectionOfView != IsometricDirection.NONE)
                    {
                        sprite = draw.Sprites[(int)position.DirectionOfView] != null ?
                        draw.Sprites[(int)position.DirectionOfView] :
                        draw.Sprites[draw.Sprites[0] != null ? 0 : 1];
                    }
                    else
                    {
                        sprite = draw.Sprites[draw.Sprites[0] != null ? 0 : 1];
                    }
                    _renderChunkArray[position.Position.X / ChunkSize.X, position.Position.Y / ChunkSize.Y, position.Position.Z].AddSprite(
                        VertexBufferType.Dynamic,
                        (int)VertexBufferLayer.Back,
                        sprite, Color.White, position.Position, new Point(0, 0),
                        //offsetZ: -SiteRenderer.Z_DIAGONAL_OFFSET / 2,
                        drawSize: Sprite.SPRITE_SIZE
                        );
                    _renderChunkArray[position.Position.X / ChunkSize.X, position.Position.Y / ChunkSize.Y, position.Position.Z].IsFullyHidded = false;
                }
            });*/
        }

        private void DrawVertices(GameTime gameTime)
        {
            Matrix WVP = Matrix.Multiply(Matrix.Multiply(Site.Camera.WorldMatrix, Site.Camera.Transformation),
                Site.Camera.Projection);

            _customEffect.Parameters["WorldViewProjection"].SetValue(WVP);
            _customEffect.Parameters["DayTime"].SetValue(Site.SiteTime);
            _customEffect.Parameters["MinMaxLevel"].SetValue(new Vector2(_drawLowest, _drawHighest));

            _graphicsDevice.DepthStencilState = DepthStencilState.Default;
            _graphicsDevice.SamplerStates[0] = SamplerState.PointClamp;

            for (int z = _drawLowest; z <= _drawHighest; z++)
            {
                for (int x = 0; x < _chunksCount.X; x++)
                {
                    for (int y = 0; y < _chunksCount.Y; y++)
                    {
                        if (!_renderChunkArray[x, y, z].IsSet)
                            _renderChunkArray[x, y, z].SetStaticBuffer();

                        if (z == _drawHighest)
                            _renderChunkArray[x, y, z].Draw(_customEffect,
                                new List<int> { (int)VertexBufferLayer.HiddenBack, (int)VertexBufferLayer.Back });
                        else
                            if (!_renderChunkArray[x, y, z].IsFullyHidded ||
                            (_renderChunkArray[x, y, z].IsFullyHidded && (x == _chunksCount.X - 1 || y == _chunksCount.Y - 1)))
                            _renderChunkArray[x, y, z].Draw(_customEffect,
                                new List<int> { (int)VertexBufferLayer.Back, (int)VertexBufferLayer.Front });

                        _renderChunkArray[x, y, z].Clear(VertexBufferType.Dynamic);
                    }
                }
            }
        }

        public void Dispose()
        {
        }
    }
}