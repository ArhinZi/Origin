using Arch.Core;
using Arch.Core.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using MonoGame.Extended;

using Origin.Source.ECS;
using Origin.Source.ECS.Components;
using Origin.Source.GameComponentsServices;
using Origin.Source.GameStates;
using Origin.Source.IO;
using Origin.Source.Utils;

using SharpDX.Direct2D1.Effects;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;

using Point3 = Origin.Source.Utils.Point3;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace Origin.Source
{
    internal enum VisBufField : byte
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
    }

    public class SiteRenderer : IDisposable
    {
        private Site _Site;

        private Camera2D _Camera;

        public static bool ALL_DISCOVERED = false;

        public Point ChunkSize;

        private Point3 _chunksCount;
        private int _drawLowest;
        private int _drawHighest;

        private SiteVertexBufferChunk[,,] _renderChunkArray;

        private HashSet<Point3> _reloadChunkList = new HashSet<Point3>();

        private Effect _customEffect;
        private Effect _vertexAnimator;

        private GraphicsDevice _graphicsDevice;
        private SpriteBatch _spriteBatch;

        /// <summary>
        /// Z offset for every Left2Right diagonal block lines.
        /// Blocks in the far diagonal line are appearing behind the ones in the near line.
        /// </summary>
        public static readonly float Z_DIAGONAL_OFFSET = 0.01f;

        public static readonly float Z_LEVEL_OFFSET = 0.01f;

        public static readonly Point BASE_CHUNK_SIZE = new Point(32, 32);
        public static readonly int ONE_MOMENT_DRAW_LEVELS = 16;

        private VertexAnimationElement[] elements;
        private VertexAnimationIndex[] indexes;
        public StructuredBuffer animationElementsBuffer;
        public StructuredBuffer animationIndexesBuffer;

        private Point3 _SiteSize
        {
            get => _Site.StructureComponent.Size;
        }

        private int _CurrentLevel
        {
            get => _Site.CurrentLevel;
        }

        public SiteRenderer(Site site, GraphicsDevice graphicDevice)
        {
            _Site = site;

            _Camera = new Camera2D();
            _Camera.Move(new Vector2(0,
                -(_CurrentLevel * (Sprite.TILE_SIZE.Y + Sprite.FLOOR_YOFFSET)
                    - Sprite.TILE_SIZE.Y * (_SiteSize.X / 2)
                 )));

            ChunkSize = BASE_CHUNK_SIZE;
            if (_SiteSize.X % ChunkSize.X != 0 || _SiteSize.Y % ChunkSize.Y != 0) throw new Exception("Site size is invalid!");
            _chunksCount = new Point3(_SiteSize.X / ChunkSize.X, _SiteSize.Y / ChunkSize.Y, _SiteSize.Z);

            _drawHighest = _CurrentLevel;
            _drawLowest = DiffUtils.GetOrBound(_drawHighest + ONE_MOMENT_DRAW_LEVELS + 1, 0, _SiteSize.Z - 1);

            _renderChunkArray = new SiteVertexBufferChunk[_chunksCount.X, _chunksCount.Y, 100];

            _reloadChunkList = new HashSet<Point3>();

            _graphicsDevice = graphicDevice;
            _spriteBatch = new SpriteBatch(OriginGame.Instance.GraphicsDevice);

            _customEffect = OriginGame.Instance.Content.Load<Effect>("FX/MegaShader");
            _vertexAnimator = OriginGame.Instance.Content.Load<Effect>("FX/VertexAnimationCS");

            //CalcVisibility();
            Parallel.For(0, _SiteSize.Z, z =>
            //for (int z = 0; z < _chunksCount.Z; z++)
            {
                FillLevel(z);
            });
            /*for (int z = 0; z < _chunksCount.Z; z++)
            {
                SetLevel(z);
            }*/
            /*elements = new VertexAnimationElement[2 * 8];
            elements[0] = new VertexAnimationElement()
            {
                texturePosition = new Vector2(0, 64 / 96f)
            };
            elements[1] = new VertexAnimationElement()
            {
                texturePosition = new Vector2(32 / 128f, 64 / 96f)
            };
            animationElementsBuffer = new StructuredBuffer(_graphicsDevice, typeof(VertexAnimationElement), 2 * 8, BufferUsage.WriteOnly, ShaderAccess.Read);
            animationElementsBuffer.SetData(elements);

            indexes = new VertexAnimationIndex[1];
            indexes[0] = new VertexAnimationIndex()
            {
                currentIndex = 1,
                indexCount = 2
            };
            animationIndexesBuffer = new StructuredBuffer(_graphicsDevice, typeof(VertexAnimationIndex), 1, BufferUsage.WriteOnly, ShaderAccess.Read);
            animationIndexesBuffer.SetData(indexes);*/
        }

        private void CalcChunkCellsVisibility(Point3 chunkCoord)
        {
            /*if (chunkCoord.X >= 0 && chunkCoord.X < _chunksCount.X &&
                chunkCoord.Y >= 0 && chunkCoord.Y < _chunksCount.Y &&
                chunkCoord.Z >= 0 && chunkCoord.Z < _chunksCount.Z)
                for (int tileInChunkCoordX = 0; tileInChunkCoordX < ChunkSize.X; tileInChunkCoordX++)
                {
                    for (int tileInChunkCoordY = 0; tileInChunkCoordY < ChunkSize.Y; tileInChunkCoordY++)
                    {
                        int tileCoordX = chunkCoord.X * ChunkSize.X + tileInChunkCoordX;
                        int tileCoordY = chunkCoord.Y * ChunkSize.Y + tileInChunkCoordY;
                        SiteCell tile = Site.Blocks[(ushort)tileCoordX, (ushort)tileCoordY, (ushort)chunkCoord.Z];
                        if (tile.FloorID != TerrainMaterial.AIR_NULL_MAT_ID)
                        {
                            // Check if tile have neighbors in TL & TR & BL & BR borders
                            if (
                                // Check BR
                                tileCoordX + 1 <= Site.Size.X &&
                                    (tileCoordX + 1 == Site.Size.X ||
                                    Site.Blocks[(ushort)(tileCoordX + 1), (ushort)tileCoordY, (ushort)chunkCoord.Z].WallID != TerrainMaterial.AIR_NULL_MAT_ID)
                                    &&
                                // Check BL
                                tileCoordY + 1 <= Site.Size.Y &&
                                    (tileCoordY + 1 == Site.Size.Y ||
                                    Site.Blocks[(ushort)tileCoordX, (ushort)(tileCoordY + 1), (ushort)chunkCoord.Z].WallID != TerrainMaterial.AIR_NULL_MAT_ID)
                                    &&
                                // Check TL
                                tileCoordX >= 0 &&
                                    (tileCoordX == 0 ||
                                    Site.Blocks[(ushort)(tileCoordX - 1), (ushort)tileCoordY, (ushort)chunkCoord.Z].WallID != TerrainMaterial.AIR_NULL_MAT_ID)
                                    &&
                                // Check TR
                                tileCoordY >= 0 &&
                                    (tileCoordY == 0 ||
                                    Site.Blocks[(ushort)tileCoordX, (ushort)(tileCoordY - 1), (ushort)chunkCoord.Z].WallID != TerrainMaterial.AIR_NULL_MAT_ID)
                                    )
                            {
                                // Then at least wall is invisible
                                tile.WallVisual = CellVisual.None;

                                if (tileCoordX + 1 == Site.Size.X || tileCoordY + 1 == Site.Size.Y)
                                {
                                    tile.WallVisual |= CellVisual.Visible;
                                    tile.FloorVisual |= CellVisual.Visible;
                                }

                                // Check if tile have neighbor above
                                if (chunkCoord.Z + 1 < Site.Size.Z &&
                                    Site.Blocks[(ushort)tileCoordX, (ushort)tileCoordY, (ushort)(chunkCoord.Z + 1)].WallID != TerrainMaterial.AIR_NULL_MAT_ID)
                                {
                                    // Then floor is invisible
                                    tile.FloorVisual |= CellVisual.None;
                                }
                                else
                                {
                                    // Else floor is visible
                                    tile.FloorVisual |= CellVisual.Visible | CellVisual.Discovered;
                                }
                            }
                            else
                            {
                                // Else both visible
                                tile.WallVisual = CellVisual.Visible | CellVisual.Discovered;
                                tile.FloorVisual = CellVisual.Visible | CellVisual.Discovered;
                            }
                        }
                    }
                }*/
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
            Point3 chunkCoord)
        {
            if (chunkCoord.X >= 0 && chunkCoord.X < _chunksCount.X &&
                chunkCoord.Y >= 0 && chunkCoord.Y < _chunksCount.Y &&
                chunkCoord.Z >= 0 && chunkCoord.Z < _chunksCount.Z)
                // Loop through each tile block in the chunk
                for (int tileInChunkCoordX = 0; tileInChunkCoordX < ChunkSize.X; tileInChunkCoordX++)
                {
                    for (int tileInChunkCoordY = 0; tileInChunkCoordY < ChunkSize.Y; tileInChunkCoordY++)
                    {
                        int tileCoordX = chunkCoord.X * ChunkSize.X + tileInChunkCoordX;
                        int tileCoordY = chunkCoord.Y * ChunkSize.Y + tileInChunkCoordY;

                        var cell = _Site.StructureComponent._Blocks[(ushort)tileCoordX, (ushort)tileCoordY, (ushort)chunkCoord.Z];
                        Entity ewall = cell != null ? cell[CellStructure.Wall] : Entity.Null;
                        Entity efloor = cell != null ? cell[CellStructure.Floor] : Entity.Null;
                        if (cell == null)
                        {
                            Color hiddenColor = new Color(70, 70, 70, 255);
                            Sprite hiddenWallSprite = Sprite.SpriteSet["SolidSelectionWall"];
                            Sprite hiddenFloorSprite = Sprite.SpriteSet["SolidSelectionFloor"];

                            _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].AddSprite(
                                VertexBufferType.Static,
                                VertexBufferLayer.HiddenBack,
                                hiddenWallSprite, hiddenColor, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, 0));

                            _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].AddSprite(
                                VertexBufferType.Static,
                                VertexBufferLayer.HiddenFront,
                                hiddenFloorSprite, hiddenColor, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, -Sprite.FLOOR_YOFFSET));
                        }
                        else
                        {
                            if (ewall != Entity.Null)
                            {
                                Sprite sprite = Sprite.SpriteSet["SolidSelectionWall"];
                                Color c = Color.Purple;

                                GraphicSimple graphicComp;
                                if (ewall.TryGet<GraphicSimple>(out graphicComp))
                                {
                                    sprite = graphicComp.Sprite;
                                    c = Color.White;
                                }

                                HasMaterial hMaterial;
                                if (ewall.TryGet(out hMaterial))
                                    c = hMaterial.Material.Color;

                                if (hMaterial.Material.ID != "AIR")
                                    _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].AddSprite(
                                        VertexBufferType.Static,
                                        VertexBufferLayer.Back,
                                        sprite, c, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, 0));
                            }
                            if (efloor != Entity.Null)
                            {
                                Sprite sprite = Sprite.SpriteSet["SolidSelectionFloor"];
                                Color c = Color.Purple;

                                GraphicSimple graphicComp;
                                if (efloor.TryGet<GraphicSimple>(out graphicComp))
                                {
                                    sprite = graphicComp.Sprite;
                                    c = Color.White;
                                }

                                HasMaterial hMaterial;
                                if (efloor.TryGet(out hMaterial))
                                    c = hMaterial.Material.Color;

                                if (hMaterial.Material.ID != "AIR")
                                    _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].AddSprite(
                                    VertexBufferType.Static,
                                    VertexBufferLayer.Front,
                                    sprite, c, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, -Sprite.FLOOR_YOFFSET));
                            }
                        }
                        /*SiteCell tile = Site.Blocks[(ushort)tileCoordX, (ushort)tileCoordY, (ushort)chunkCoord.Z];

                        if (tile == null)
                        {
                            TerrainMaterial tm = TerrainMaterial.TerraMats[TerrainMaterial.HIDDEN_MAT_ID];
                            Sprite sprite;
                            Color c = Color.Wheat;
                            sprite = tm.Sprites["Wall"][0];
                            //sprite = tm.Sprites["Wall"][0];
                            c = tm.TerraColor;

                            _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].AddSprite(
                                VertexBufferType.Static,
                                VertexBufferLayer.HiddenBack,
                                sprite, c, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, 0));
                        }
                        else if (tile.WallID != TerrainMaterial.AIR_NULL_MAT_ID)
                        {
                            TerrainMaterial tm = TerrainMaterial.TerraMats[tile.WallID];
                            Sprite sprite;
                            Color c = Color.Wheat;
                            sprite = tm.Sprites["Wall"][tile.seed % tm.Sprites["Wall"].Count];
                            //sprite = tm.Sprites["Wall"][0];
                            c = tm.TerraColor;
                            _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].AddSprite(
                                VertexBufferType.Static,
                                VertexBufferLayer.Back,
                                sprite, c, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, 0));

                            if (tile.EmbeddedWallID != null)
                            {
                                tm = TerrainMaterial.TerraMats[tile.EmbeddedWallID];
                                sprite = tm.Sprites["Wall"][tile.seed % tm.Sprites["Wall"].Count];
                                c = tm.TerraColor;
                                _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].AddSprite(
                                    VertexBufferType.Static,
                                    VertexBufferLayer.Back,
                                    sprite, c, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, 0));
                            }
                        }

                        if (tile == null)
                        {
                            TerrainMaterial tm = TerrainMaterial.TerraMats[TerrainMaterial.HIDDEN_MAT_ID];
                            Sprite sprite;
                            Color c = Color.Wheat;
                            sprite = tm.Sprites["Floor"][0];
                            c = tm.TerraColor;

                            _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].AddSprite(
                            VertexBufferType.Static,
                            VertexBufferLayer.HiddenFront,
                            sprite, c, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, -Sprite.FLOOR_YOFFSET));
                        }
                        else if (tile.FloorID != TerrainMaterial.AIR_NULL_MAT_ID)
                        {
                            TerrainMaterial tm = TerrainMaterial.TerraMats[tile.FloorID];
                            Sprite sprite;
                            Color c = Color.Wheat;
                            sprite = tm.Sprites["Floor"][tile.seed % tm.Sprites["Floor"].Count];
                            //sprite = tm.Sprites["Floor"][0];
                            c = tm.TerraColor;
                            _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].AddSprite(
                                VertexBufferType.Static,
                                VertexBufferLayer.Front,
                                sprite, c, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, -Sprite.FLOOR_YOFFSET));

                            if (tile.EmbeddedFloorID != null)
                            {
                                tm = TerrainMaterial.TerraMats[tile.EmbeddedFloorID];
                                sprite = tm.Sprites["Floor"][tile.seed % tm.Sprites["Floor"].Count];
                                //sprite = tm.Sprites["Floor"][0];
                                c = tm.TerraColor;
                                _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].AddSprite(
                                    VertexBufferType.Static,
                                    VertexBufferLayer.Front,
                                    sprite, c, new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, -Sprite.FLOOR_YOFFSET));
                            }
                        }

                        if (tile != null && tile.WaterLevel > 0)
                        {
                            Sprite sprite = Sprite.SpriteSet["SolidSelectionFloor"];
                            for (int i = -1; i < 7; i++)
                            {
                                _renderChunkArray[chunkCoord.X, chunkCoord.Y, chunkCoord.Z].AddSprite(
                                VertexBufferType.Static,
                                VertexBufferLayer.Back,
                                sprite, new Color(0, 0, 250, 50), new Point3(tileCoordX, tileCoordY, chunkCoord.Z), new Point(0, i * 2));
                            }
                        }*/
                    }
                }
        }

        /// <summary>
        /// Fill whole level again
        /// </summary>
        /// <param name="level"></param>
        /// <param name="fillWalls"></param>
        /// <param name="fillFloors"></param>
        private void FillLevel(int level)
        {
            for (int x = 0; x < _chunksCount.X; x++)
            //Parallel.For(0, _chunksCount.X, x =>
            {
                for (int y = 0; y < _chunksCount.Y; y++)
                {
                    if (_renderChunkArray[x, y, level] == null)
                        _renderChunkArray[x, y, level] = new SiteVertexBufferChunk(this, new Point3(x, y, level));
                    _renderChunkArray[x, y, level].Clear(VertexBufferType.Static);

                    FillChunkVertices(new Point3(x, y, level));
                }
            };
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
            _Camera.Update(gameTime);
            IGameInfoMonitor debug = OriginGame.Instance.Services.GetService<IGameInfoMonitor>();
            debug.Set("Cam ZOOM", _Camera.Zoom.ToString(), 11);
            debug.Set("Cam POS", _Camera.Position.ToString(), 12);

            // Check if CurrentLevel changed and redraw what need to redraw
            if (_drawHighest != _CurrentLevel)
            {
                _drawHighest = _CurrentLevel;
                _drawLowest = DiffUtils.GetOrBound(_drawHighest + ONE_MOMENT_DRAW_LEVELS + 1, 0, _Site.StructureComponent.Size.Z - 1);
            }

            // Collect all ChunksToReload and redraw them
            foreach (var item in _Site.BlocksToReload)
            {
                if (item.Z <= _drawLowest && item.Z >= _drawHighest)
                {
                    int chunkX = (int)item.X / ChunkSize.X;
                    int chunkY = (int)item.Y / ChunkSize.Y;
                    _reloadChunkList.Add(new Point3(chunkX, chunkY, item.Z));
                }
            }
            _Site.BlocksToReload.Clear();
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

                List<Point3> neighbours1 = new List<Point3>()
                        {
                            new Point3(0, 0, -1),
                            new Point3(-1, 0, -1),new Point3(0, -1, -1),
                            new Point3(1, 0, -1),new Point3(0, 1, -1)
                        };
                foreach (var neighbor in neighbours1)
                {
                    //CalcChunkCellsVisibility(toReload + neighbor);
                    FillChunk(toReload + neighbor);
                    SetChunk(toReload + neighbor);
                }

                List<Point3> neighbours2 = new List<Point3>()
                        {
                            new Point3(0, 0, 0),
                            new Point3(-1, 0, 0),new Point3(0, -1, 0),
                            new Point3(1, 0, 0),new Point3(0, 1, 0)
                        };
                foreach (var neighbor in neighbours2)
                {
                    //CalcChunkCellsVisibility(toReload + neighbor);
                    FillChunk(toReload + neighbor);
                    SetChunk(toReload + neighbor);
                }

                _reloadChunkList.Remove(toReload);
            }
            // TODO: Implement Sprite animation
            /*if (gameTime.TotalGameTime.Ticks % 1 == 0)
            {
                _vertexAnimator.Parameters["Elements"].SetValue(animationElementsBuffer);
                _vertexAnimator.Parameters["ElementIndexes"].SetValue(animationIndexesBuffer);
                _vertexAnimator.Parameters["AnimationsCount"].SetValue(1);
                for (int z = _drawLowest; z <= _drawHighest; z++)
                {
                    for (int x = 0; x < _chunksCount.X; x++)
                    {
                        for (int y = 0; y < _chunksCount.Y; y++)
                        {
                            VertexBufferLayer layer = VertexBufferLayer.Front;
                            {
                                foreach (var key in SiteVertexBufferChunk._texture2Ds)
                                {
                                    if (_renderChunkArray[x, y, z]._staticVertexBuffer != null && _renderChunkArray[x, y, z]._staticVertexBuffer.ContainsKey(key))
                                    {
                                        List<VertexBuffer> listVB = _renderChunkArray[x, y, z]._staticVertexBuffer[key][(int)layer];
                                        for (int i = 0; i < listVB.Count; i++)
                                        {
                                            _vertexAnimator.Parameters["Vertices"].SetValue(listVB[i]);
                                            _vertexAnimator.CurrentTechnique.Passes[0].ApplyCompute();
                                            int count = listVB[i].VertexCount / 6 / 64;
                                            count = count < 1 ? 1 : count;
                                            _graphicsDevice.DispatchCompute(count, 1, 1);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                indexes[0].currentIndex = ((short)((short)(indexes[0].currentIndex + 1) % indexes[0].indexCount));
                //indexes[1].currentIndex = ((short)((short)(indexes[0].currentIndex + 1) % indexes[0].indexCount));
                animationIndexesBuffer.SetData(indexes);
            }*/

            // Test drawing mouse selection on selectedBlock
            {
                Point3 tile = _Site.SelectedBlock;
                if (tile != new Point3(-1, -1, -1))
                {
                    /*Sprite sprite = Sprite.SpriteSet["SolidSelectionWall"];
                    _renderChunkArray[tile.X / ChunkSize.X, tile.Y / ChunkSize.Y, tile.Z].AddSprite(
                        VertexBufferType.Dynamic,
                        VertexBufferLayer.Back,
                        sprite, new Color(30, 0, 0, 100), tile, new Point(0, 0)
                        );*/
                    /*sprite = Sprite.SpriteSet["SolidSelectionFloor"];
                    _renderChunkArray[tile.Position.Z][tile.Position.X / BASE_CHUNK_SIZE.X, tile.Position.Y / BASE_CHUNK_SIZE.Y].AddSprite(
                        VertexBufferType.Dynamic, sprite, new Color(30, 0, 0, 100), tile.Position, new Point(0, -Sprite.FLOOR_YOFFSET)
                        );*/
                }
            }

            // Draw Entities
            /*var query = new QueryDescription().WithAll<DrawComponent, SitePositionComponent>();
            _Site.OriginWorld.ECSworld.Query(in query, (in Entity entity) =>
            {
                var position = entity.Get<SitePositionComponent>();
                var draw = entity.Get<DrawComponent>();
                if (position.Position.Z <= _drawHighest && position.Position.Z > _drawLowest &&
                    position.Site == _Site)
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
                        VertexBufferLayer.Back,
                        sprite, Color.White, position.Position, new Point(0, 0),
                        //offsetZ: -SiteRenderer.Z_DIAGONAL_OFFSET / 2,
                        drawSize: Sprite.SPRITE_SIZE
                        );
                }
            });*/
        }

        private void DrawVertices(GameTime gameTime)
        {
            Matrix WVP = Matrix.Multiply(Matrix.Multiply(_Camera.WorldMatrix, _Camera.Transformation),
                _Camera.Projection);

            _customEffect.Parameters["WorldViewProjection"].SetValue(WVP);
            _customEffect.Parameters["DayTime"].SetValue(_Site.SiteTime);
            _customEffect.Parameters["LowHighLevel"].SetValue(new Vector2(_drawLowest, _drawHighest));

            _graphicsDevice.DepthStencilState = DepthStencilState.Default;
            _graphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
            _graphicsDevice.DepthStencilState = DepthStencilState.Default;
            _graphicsDevice.BlendState = BlendState.NonPremultiplied;

            for (int z = _drawLowest; z >= _drawHighest; z--)
            {
                for (int x = 0; x < _chunksCount.X; x++)
                {
                    for (int y = 0; y < _chunksCount.Y; y++)
                    {
                        if (!_renderChunkArray[x, y, z].IsSet)
                            _renderChunkArray[x, y, z].SetStaticBuffer();

                        if (z == _drawHighest)
                            _renderChunkArray[x, y, z].Draw(_customEffect,
                                new VertexBufferLayer[] { VertexBufferLayer.HiddenBack, VertexBufferLayer.Back }.ToArray());
                        else
                            _renderChunkArray[x, y, z].Draw(_customEffect);

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