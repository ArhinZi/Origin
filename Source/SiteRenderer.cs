using Arch.Bus;
using Arch.Core;
using Arch.Core.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using MonoGame.Extended.Sprites;

using Origin.Source.ECS;
using Origin.Source.Events;
using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Linq;
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

        private SiteVertexBufferChunk[,,] _renderChunkArray;
        private RenderInstancer _renderInstancer;

        private HashSet<Point3> _reloadChunkList = new HashSet<Point3>();

        private Effect _customEffect;
        private AlphaTestEffect _alphaTestEffect;

        private GraphicsDevice _graphicsDevice;
        private SpriteBatch _spriteBatch;

        public bool HalfWallMode { get; private set; } = false;

        /// <summary>
        /// Z offset.
        /// Blocks in the far diagonal line are appearing behind the ones in the near line.
        /// </summary>
        public static readonly float Z_DIAGONAL_OFFSET = 0.01f;

        /// <summary>
        /// Z offset.
        /// Blocks will have different Z coordinate depending on level
        /// </summary>
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

            _renderInstancer = new RenderInstancer(ChunkSize, _graphicsDevice, _customEffect);
            RecalcHiddenInstances();
        }

        #region Events

        [Event]
        public void OnHalfWallModeChanged(HalfWallModeChanged modeChanged)
        {
            HalfWallMode = !HalfWallMode;
            if (HalfWallMode)
                FillLevel(Site.CurrentLevel, true);
            else FillLevel(Site.CurrentLevel, false);
        }

        #endregion Events

        #region Visibility

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

        private void CalcChunkCellsVisibility(Point3 chunkCoord)
        {
            if (chunkCoord.X >= 0 && chunkCoord.X < _chunksCount.X &&
                chunkCoord.Y >= 0 && chunkCoord.Y < _chunksCount.Y &&
                chunkCoord.Z >= 0 && chunkCoord.Z < _chunksCount.Z)
                for (int tileInChunkCoordX = 0; tileInChunkCoordX < ChunkSize.X; tileInChunkCoordX++)
                    for (int tileInChunkCoordY = 0; tileInChunkCoordY < ChunkSize.Y; tileInChunkCoordY++)
                    {
                        int tileCoordX = chunkCoord.X * ChunkSize.X + tileInChunkCoordX;
                        int tileCoordY = chunkCoord.Y * ChunkSize.Y + tileInChunkCoordY;

                        CalcTileVisibility(new Point3(tileCoordX, tileCoordY, chunkCoord.Z));
                    }
        }

        private void CalcTileVisibility(Point3 tilePos)
        {
            int tileCoordX = tilePos.X;
            int tileCoordY = tilePos.Y;
            int tileCoordZ = tilePos.Z;

            Entity tile = Site.Blocks[tileCoordX, tileCoordY, tileCoordZ];

            if (tile != Entity.Null)
            {
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
                            Site.Blocks[tileCoordX + 1, tileCoordY, tileCoordZ] == Entity.Null ||
                            Site.Blocks[tileCoordX + 1, tileCoordY, tileCoordZ].TryGet(out Nstructure) &&
                            Nstructure.WallMaterial != null)
                            &&
                        // Check BL
                        tileCoordY + 1 <= Site.Size.Y &&
                            (tileCoordY + 1 == Site.Size.Y ||
                            Site.Blocks[tileCoordX, tileCoordY + 1, tileCoordZ] == Entity.Null ||
                            Site.Blocks[tileCoordX, tileCoordY + 1, tileCoordZ].TryGet(out Nstructure) &&
                            Nstructure.WallMaterial != null)
                            &&
                        // Check TL
                        tileCoordX >= 0 &&
                            (tileCoordX == 0 ||
                            Site.Blocks[tileCoordX - 1, tileCoordY, tileCoordZ] == Entity.Null ||
                            Site.Blocks[tileCoordX - 1, tileCoordY, tileCoordZ].TryGet(out Nstructure) &&
                            Nstructure.WallMaterial != null)
                            &&
                        // Check TR
                        tileCoordY >= 0 &&
                            (tileCoordY == 0 ||
                            Site.Blocks[tileCoordX, tileCoordY - 1, tileCoordZ] == Entity.Null ||
                            Site.Blocks[tileCoordX, tileCoordY - 1, tileCoordZ].TryGet(out Nstructure) &&
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
                        if (tileCoordZ + 1 < Site.Size.Z &&
                            Site.Blocks[tileCoordX, tileCoordY, tileCoordZ + 1].Has<TileStructure>() &&
                            Site.Blocks[tileCoordX, tileCoordY, tileCoordZ + 1].Get<TileStructure>().WallMaterial != null)
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

                    /*if (!visibility.FloorDiscovered && tileCoordX + 1 == Site.Size.X || tileCoordY + 1 == Site.Size.Y)
                        visibility.FloorVisible = true;*/
                }
            }
        }

        #endregion Visibility

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

                        _renderChunkArray[x, y, level].FillStaticVertices(HalfWall);
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
                chunkPos.Y >= 0 && chunkPos.Y < _chunksCount.Y &&
                chunkPos.Z >= 0 && chunkPos.Z < _chunksCount.Z)
            {
                if (_renderChunkArray[chunkPos.X, chunkPos.Y, chunkPos.Z] == null)
                    _renderChunkArray[chunkPos.X, chunkPos.Y, chunkPos.Z] = new SiteVertexBufferChunk(this, new Point3(chunkPos.X, chunkPos.Y, chunkPos.Z));
                _renderChunkArray[chunkPos.X, chunkPos.Y, chunkPos.Z].Clear(VertexBufferType.Static);
                _renderChunkArray[chunkPos.X, chunkPos.Y, chunkPos.Z].FillStaticVertices();
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
        }

        public void Draw(GameTime gameTime)
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

                RecalcHiddenInstances();
            }

            if (Site.BlocksToReload.Count > 0)
                // Collect all ChunksToReload and redraw them
                foreach (var item in Site.BlocksToReload)
                {
                    //if (item.Z >= _drawLowest && item.Z <= _drawHighest)
                    {
                        List<Point3> neighbours = new List<Point3>()
                        {
                            new Point3(0, 0, 0),
                            new Point3(-1, 0, 0),new Point3(0, -1, 0),
                            new Point3(1, 0, 0),new Point3(0, 1, 0)
                        };
                        foreach (var n in neighbours)
                        {
                            Point3 chank = WorldUtils.GetChunkByCell(item + n, new Point3(ChunkSize, 1));
                            _reloadChunkList.Add(chank);
                            chank = WorldUtils.GetChunkByCell(item + n + new Point3(0, 0, -1), new Point3(ChunkSize, 1));
                            _reloadChunkList.Add(chank);
                        }
                        Site.BlocksToReload.Remove(item);
                    }
                }

            PrepareVertices(gameTime);

            DrawVertices(gameTime);
        }

        private void RecalcHiddenInstances()
        {
            _renderInstancer.ClearInstances();
            for (int z = _drawLowest; z <= _drawHighest; z++)
            {
                for (int x = 0; x < _chunksCount.X; x++)
                {
                    for (int y = 0; y < _chunksCount.Y; y++)
                    {
                        if (_renderChunkArray[x, y, z].UseHiddenInstancing)
                        {
                            Point spritePos = WorldUtils.GetSpritePositionByCellPosition(new Point3(x * ChunkSize.X, y * ChunkSize.Y, z));
                            float vertexZ = WorldUtils.GetSpriteZOffsetByCellPos(new Point3(x * ChunkSize.X, y * ChunkSize.Y, z));
                            if (z == _drawHighest)
                            {
                                _renderInstancer.AddInstance(RenderInstancer.InstanceDefs.HiddenWallFlatChank, new Vector3(spritePos.X, spritePos.Y, vertexZ), z);
                            }
                            else
                            {
                                if (x == _chunksCount.X - 1)
                                {
                                    _renderInstancer.AddInstance(RenderInstancer.InstanceDefs.HiddenRBorder, new Vector3(spritePos.X, spritePos.Y, vertexZ), z);
                                }
                                if (y == _chunksCount.Y - 1)
                                {
                                    _renderInstancer.AddInstance(RenderInstancer.InstanceDefs.HiddenLBorder, new Vector3(spritePos.X, spritePos.Y, vertexZ), z);
                                }
                            }
                        }
                    }
                }
                _renderInstancer.SetInstances();
            }
        }

        private void PrepareVertices(GameTime gameTime)
        {
            // Make chunk reload smoother
            if (_reloadChunkList.Count > 0 && gameTime.TotalGameTime.Ticks % 12 == 0)
            {
                /*List<Point3> neighbours = new List<Point3>()
                {
                    new Point3(0, 0, 0),
                    new Point3(-1, 0, 0),new Point3(0, -1, 0),
                    new Point3(1, 0, 0),new Point3(0, 1, 0)
                };

                HashSet<Point3> reload = new HashSet<Point3>();
                foreach (Point3 p in _reloadChunkList)
                {
                    foreach (var neighbor in neighbours)
                    {
                        reload.Add(p + neighbor);
                        reload.Add(p + neighbor + new Point3(0, 0, -1));
                    }
                }*/

                Parallel.ForEach(_reloadChunkList, rel =>
                {
                    _renderChunkArray[rel.X, rel.Y, rel.Z].CheckHidden();
                    CalcChunkCellsVisibility(rel);
                    FillChunk(rel);
                });
                foreach (var rel in _reloadChunkList)
                    SetChunk(rel);

                _reloadChunkList.Clear();
            }

            // Test drawing mouse selection on selectedBlock
            /*{
                Entity tile = Site.SelectedBlock;
                var onTile = Site.SelectedPosition;
                if (tile != Entity.Null || (tile == Entity.Null && onTile.Z == Site.CurrentLevel))
                {
                    *//*int blocksUnder = 0;
                    for (int i = 1; i < ONE_MOMENT_DRAW_LEVELS; i++)
                    {
                        if (onTile.position.Z - i >= 0 && !Site.Blocks[onTile.position.X, onTile.position.Y, onTile.position.Z - i].Has<TileStructure>())
                        {
                            Point3 chunkPosAbove = WorldUtils.GetChunkByCell(new Point3(onTile.position.X, onTile.position.Y, onTile.position.Z - i),
                                new Point3(ChunkSize.X, ChunkSize.Y, 1));
                            Sprite sprite2 = GlobalResources.GetSpriteByID("SelectionWall");
                            _renderChunkArray[chunkPosAbove.X,
                                            chunkPosAbove.Y,
                                            chunkPosAbove.Z].AddSprite(
                                VertexBufferType.Dynamic,
                                (int)VertexBufferLayer.Back,
                                sprite2, new Color(30, 0, 0, 200), onTile.position + new Point3(0, 0, -i), new Point(0, 0)
                                );
                            _renderChunkArray[chunkPosAbove.X,
                                            chunkPosAbove.Y,
                                            chunkPosAbove.Z].IsFullyHidded = false;
                        }
                        else break;
                    }*//*
                    Sprite sprite = GlobalResources.GetSpriteByID("SolidSelectionWall");
                    Point3 chunkPos = WorldUtils.GetChunkByCell(onTile,
                                new Point3(ChunkSize.X, ChunkSize.Y, 1));
                    _renderChunkArray[chunkPos.X, chunkPos.Y, chunkPos.Z].AddSprite(
                        VertexBufferType.Dynamic,
                        (int)VertexBufferLayer.Back,
                        sprite, new Color(30, 0, 0, 100), onTile, new Point(0, 0)
                        );
                    _renderChunkArray[chunkPos.X, chunkPos.Y, chunkPos.Z].IsFullyHidded = false;
                }
            }*/
            foreach (var sprite in Site.Tools.CurrentTool.sprites)
            {
                Point3 chunkPos = WorldUtils.GetChunkByCell(sprite.position,
                                new Point3(ChunkSize.X, ChunkSize.Y, 1));
                _renderChunkArray[chunkPos.X, chunkPos.Y, chunkPos.Z].AddSprite(
                            VertexBufferType.Dynamic,
                            (int)VertexBufferLayer.Front,
                            sprite.sprite, sprite.color, sprite.position, sprite.offset,
                            offsetZ: sprite.Zoffset
                            );
                _renderChunkArray[chunkPos.X, chunkPos.Y, chunkPos.Z].IsFullyHidded = false;
            }

            // Drawing Path
            {
                if (Site.currPath != null)
                {
                    foreach (var edge in Site.currPath.Edges)
                    {
                        Point3 pos = new Point3((int)edge.Start.Position.X, (int)edge.Start.Position.Y, (int)edge.Start.Position.Z);

                        if (pos.Z <= _drawHighest && pos.Z >= _drawLowest)
                        {
                            Sprite sprite = GlobalResources.GetSpriteByID("SolidSelectionWall");
                            Point3 chunkPos = WorldUtils.GetChunkByCell(pos,
                                        new Point3(ChunkSize.X, ChunkSize.Y, 1));
                            _renderChunkArray[chunkPos.X, chunkPos.Y, chunkPos.Z].AddSprite(
                                VertexBufferType.Dynamic,
                                (int)VertexBufferLayer.Back,
                                sprite, new Color(0, 0, 50, 255), pos, new Point(0, 0)
                                );
                            _renderChunkArray[chunkPos.X, chunkPos.Y, chunkPos.Z].IsFullyHidded = false;
                        }
                    }
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

        private bool IsChunkVisible(Point3 pos)
        {
            return true;
        }

        private void DrawVertices(GameTime gameTime)
        {
            Matrix WVP = Matrix.Multiply(Matrix.Multiply(Site.Camera.WorldMatrix, Site.Camera.Transformation),
                Site.Camera.Projection);

            _customEffect.Parameters["WorldViewProjection"].SetValue(WVP);
            _customEffect.Parameters["DayTime"].SetValue(Site.SiteTime);
            _customEffect.Parameters["MinMaxLevel"].SetValue(new Vector2(_drawLowest, _drawHighest));
            _graphicsDevice.DepthStencilState = DepthStencilState.Default;
            _graphicsDevice.BlendState = BlendState.AlphaBlend;
            //_graphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

            _customEffect.CurrentTechnique = _customEffect.Techniques["Instance"];
            _renderInstancer.DrawInstancedHidden();

            _customEffect.CurrentTechnique = _customEffect.Techniques["MainTech"];
            foreach (var key in SiteVertexBufferChunk.Texture2Ds)
            {
                _customEffect.Parameters["Texture"].SetValue(key);
                _customEffect.CurrentTechnique.Passes[0].Apply();

                for (int z = _drawLowest; z <= _drawHighest; z++)
                {
                    for (int x = 0; x < _chunksCount.X; x++)
                    {
                        for (int y = 0; y < _chunksCount.Y; y++)
                        {
                            if (IsChunkVisible(new Point3(x, y, z)))
                            {
                                if (!_renderChunkArray[x, y, z].IsSet)
                                    _renderChunkArray[x, y, z].SetStaticBuffer();

                                if (z == _drawHighest)
                                    _renderChunkArray[x, y, z].Draw(key,
                                        new List<int> { (int)VertexBufferLayer.HiddenBack, (int)VertexBufferLayer.Back });
                                else
                                    if (!_renderChunkArray[x, y, z].IsFullyHidded ||
                                    (_renderChunkArray[x, y, z].IsFullyHidded && (x == _chunksCount.X - 1 || y == _chunksCount.Y - 1)))
                                    _renderChunkArray[x, y, z].Draw(key,
                                        new List<int> { (int)VertexBufferLayer.Back, (int)VertexBufferLayer.Front });

                                _renderChunkArray[x, y, z].Clear(VertexBufferType.Dynamic);
                            }
                        }
                    }
                }
            }

            _spriteBatch.Begin(SpriteSortMode.Deferred);
            _spriteBatch.Draw(Site.hmt, new Vector2(0, 0), new Rectangle(0, 0, Site.hmt.Width, Site.hmt.Height), Color.White);
            _spriteBatch.End();
        }

        public void Dispose()
        {
        }
    }
}