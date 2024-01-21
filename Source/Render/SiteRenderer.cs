﻿using Arch.Bus;
using Arch.CommandBuffer;
using Arch.Core;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Origin.Source.ECS;
using Origin.Source.Events;
using Origin.Source.Render;
using Origin.Source.Render.GpuAcceleratedSpriteSystem;
using Origin.Source.Resources;
using Origin.Source.Utils;

using Microsoft.Extensions.Caching.Memory;

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
        BackInteractives,
        HiddenFront,
        Front,
        FrontInteractives,
    }

    public partial class SiteRenderer : IDisposable
    {
        private enum RenderTaskMarks
        {
            HalfWallUpdate,
            ChunkReloadUpdate
        }

        private class RenderTask
        {
            public Task task;
            public Action OnComplete;
        }

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

        private GraphicsDevice _graphicsDevice;
        private SpriteBatch _spriteBatch;

        public bool HalfWallMode { get; private set; } = false;

        private Queue<RenderTask> RenderTasks = new();

        private RenderTask CurrentRenderTask = null;

        public SiteRenderer(Site site, GraphicsDevice graphicDevice)
        {
            Site = site;
            //_visBuffer = new byte[_site.Size.X, _site.Size.Y, _site.Size.Z];

            ChunkSize = Global.BASE_CHUNK_SIZE;
            _graphicsDevice = graphicDevice;
            if (ChunkSize.X > Site.Size.X) ChunkSize.X = Site.Size.X;
            if (ChunkSize.Y > Site.Size.Y) ChunkSize.Y = Site.Size.Y;
            if (Site.Size.X % ChunkSize.X != 0 || Site.Size.Y % ChunkSize.Y != 0) throw new Exception("Site size is invalid!");

            _drawHighest = Site.CurrentLevel;
            _drawLowest = DiffUtils.GetOrBound(_drawHighest - Global.ONE_MOMENT_DRAW_LEVELS + 1, 0, _drawHighest);

            _chunksCount = new Point3(Site.Size.X / ChunkSize.X, Site.Size.Y / ChunkSize.Y, Site.Size.Z);

            _renderChunkArray = new SiteVertexBufferChunk[_chunksCount.X, _chunksCount.Y, _chunksCount.Z];

            _reloadChunkList = new HashSet<Point3>();

            _spriteBatch = new SpriteBatch(OriginGame.Instance.GraphicsDevice);

            _customEffect = OriginGame.Instance.Content.Load<Effect>("FX/MegaShader");

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

            Entity tile = Site.Map[tileCoordX, tileCoordY, tileCoordZ];

            /*if (tile != Entity.Null)
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
                        *//*if ((tileCoordZ + 1 < Site.Size.Z &&
                            Site.Blocks[tileCoordX, tileCoordY, tileCoordZ + 1] != Entity.Null &&
                            Site.Blocks[tileCoordX, tileCoordY, tileCoordZ + 1].Has<TileStructure>() &&
                            Site.Blocks[tileCoordX, tileCoordY, tileCoordZ + 1].Get<TileStructure>().WallMaterial != null) ||
                            (tileCoordZ - 1 >= 0 &&
                            Site.Blocks[tileCoordX, tileCoordY, tileCoordZ - 1] != Entity.Null &&
                            Site.Blocks[tileCoordX, tileCoordY, tileCoordZ - 1].Has<TileStructure>() &&
                            Site.Blocks[tileCoordX, tileCoordY, tileCoordZ - 1].Get<TileStructure>().WallMaterial != null)
                            )
                        {
                            // Then floor is invisible
                            visibility.FloorDiscovered = visibility.FloorVisible = false;
                        }
                        else*//*
                        {
                            // Else floor is visible
                            visibility.FloorVisible = visibility.FloorDiscovered = true;
                            visibility.WallVisible = visibility.WallDiscovered = true;
                        }
                    }
                    else
                    {
                        // Else both visible
                        visibility.WallVisible = visibility.WallDiscovered = true;
                        visibility.FloorVisible = visibility.FloorDiscovered = true;
                    }

                    *//*if (!visibility.FloorDiscovered && tileCoordX + 1 == Site.Size.X || tileCoordY + 1 == Site.Size.Y)
                        visibility.FloorVisible = true;*//*
                }
            }*/
        }

        #endregion Visibility

        private Task TaskChunkUpdate(Point3[] list)
        {
            return new Task(() =>
            {
                foreach (var rel in list)
                {
                    _renderChunkArray[rel.X, rel.Y, rel.Z].BlockSet = true;
                    _renderChunkArray[rel.X, rel.Y, rel.Z].CheckHidden();
                    CalcChunkCellsVisibility(rel);
                    FillChunk(rel);
                }
                foreach (var rel in list)
                {
                    _renderChunkArray[rel.X, rel.Y, rel.Z].BlockSet = false;
                }
            });
        }

        private Task TaskLevelHalfWallUpdate(int prev, int curr)
        {
            return new Task(() =>
            {
                FillLevel(prev, false);
                FillLevel(curr, true);
            });
        }

        private void FillAll()
        {
            Parallel.For(0, _chunksCount.Z, z =>
            {
                FillLevel(z);
            });
        }

        /// <summary>
        /// Fill whole level again
        /// </summary>
        /// <param name="level"></param>
        private void FillLevel(int level, bool HalfWall = false)
        {
            for (int x = 0; x < _chunksCount.X; x++)
            //Parallel.For(0, _chunksCount.X, x =>
            {
                for (int y = 0; y < _chunksCount.Y; y++)
                {
                    if (_renderChunkArray[x, y, level] == null)
                        _renderChunkArray[x, y, level] = new SiteVertexBufferChunk(this, new Point3(x, y, level));
                    if (!_renderChunkArray[x, y, level].IsFullyHidden)
                    {
                        _renderChunkArray[x, y, level].BlockSet = true;
                        _renderChunkArray[x, y, level].Clear(VertexBufferType.Static);

                        _renderChunkArray[x, y, level].FillStaticVertices(HalfWall);
                        _renderChunkArray[x, y, level].BlockSet = false;
                    }
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
            CheckCurrentLevelChanged();

            ControlChunkReloading();

            PrepareVertices(gameTime);

            DrawVertices(gameTime);

            if (CurrentRenderTask != null)
            {
                if (CurrentRenderTask.task.Status == TaskStatus.RanToCompletion)
                {
                    CurrentRenderTask.OnComplete.Invoke();
                    CurrentRenderTask = null;
                }
            }
            else if (RenderTasks.Count > 0)
            {
                CurrentRenderTask = RenderTasks.Dequeue();
                CurrentRenderTask.task.Start();
            }
        }

        private void CheckCurrentLevelChanged()
        {
            // Check if CurrentLevel changed and redraw what need to redraw
            if (_drawHighest != Site.CurrentLevel)
            {
                if (HalfWallMode)
                {
                    RenderTasks.Enqueue(new RenderTask()
                    {
                        task = TaskLevelHalfWallUpdate(Site.PreviousLevel, Site.CurrentLevel),
                        OnComplete = new Action(() => { })
                    });
                }
                _drawHighest = Site.CurrentLevel;
                _drawLowest = DiffUtils.GetOrBound(_drawHighest - Global.ONE_MOMENT_DRAW_LEVELS + 1, 0, _drawHighest);

                RecalcHiddenInstances();
            }
        }

        private void ControlChunkReloading()
        {
            if (Site.ArchWorld.CountEntities(new QueryDescription().WithAll<WaitingForUpdateTileRender>()) > 0)
            {
                // Collect all ChunksToReload and redraw them
                var query = new QueryDescription().WithAll<WaitingForUpdateTileRender, IsTile>();
                var commands = new CommandBuffer(Site.ArchWorld);
                Site.ArchWorld.Query(in query, (Entity entity, ref IsTile tile) =>
                {
                    var item = tile.Position;
                    List<Point3> neighbours = new List<Point3>()
                        {
                            new Point3(0, 0, 0),
                            new Point3(-1, 0, 0),new Point3(0, -1, 0),
                            new Point3(1, 0, 0),new Point3(0, 1, 0)
                        };
                    foreach (var n in neighbours)
                    {
                        if (!(item + n).LessOr(Point3.Zero) && !(item + n).GraterEqualOr(Site.Size))
                        {
                            Point3 chunk = WorldUtils.GetChunkByCell(item + n, new Point3(ChunkSize, 1));
                            _reloadChunkList.Add(chunk);
                        }
                        if (!(item + n + new Point3(0, 0, -1)).LessOr(Point3.Zero) && !(item + n + new Point3(0, 0, -1)).GraterEqualOr(Site.Size))
                        {
                            Point3 chunk = WorldUtils.GetChunkByCell(item + n + new Point3(0, 0, -1), new Point3(ChunkSize, 1));
                            _reloadChunkList.Add(chunk);
                        }
                        if (!(item + n + new Point3(0, 0, 1)).LessOr(Point3.Zero) && !(item + n + new Point3(0, 0, 1)).GraterEqualOr(Site.Size))
                        {
                            Point3 chunk = WorldUtils.GetChunkByCell(item + n + new Point3(0, 0, 1), new Point3(ChunkSize, 1));
                            _reloadChunkList.Add(chunk);
                        }
                    }
                    commands.Remove<WaitingForUpdateTileRender>(entity);
                });
                commands.Playback();
                RenderTasks.Enqueue(new RenderTask()
                {
                    task = TaskChunkUpdate(_reloadChunkList.ToArray()),
                    OnComplete = new Action(() =>
                    {
                        RecalcHiddenInstances();
                    })
                });
                _reloadChunkList.Clear();
            }
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
                                _renderInstancer.AddInstance(RenderInstancer.InstanceDefs.HiddenWallFlatChunk, new Vector3(spritePos.X, spritePos.Y, vertexZ), z);
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
            if (Site.Tools.CurrentTool != null)
                foreach (var sprite in Site.Tools.CurrentTool.sprites)
                {
                    Point3 chunkPos = WorldUtils.GetChunkByCell(sprite.position,
                                    new Point3(ChunkSize.X, ChunkSize.Y, 1));
                    _renderChunkArray[chunkPos.X, chunkPos.Y, chunkPos.Z].AddSprite(
                                VertexBufferType.Dynamic,
                                (int)Site.Tools.CurrentTool.RenderLayer,
                                sprite.sprite, sprite.color, sprite.position, sprite.offset,
                                offsetZ: sprite.Zoffset
                                );
                }
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
            _customEffect.Parameters["DayTime"].SetValue(0.5f);
            _customEffect.Parameters["MinMaxLevel"].SetValue(new Vector2(_drawLowest, _drawHighest));
            _graphicsDevice.DepthStencilState = DepthStencilState.Default;
            _graphicsDevice.BlendState = BlendState.AlphaBlend;
            //_graphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

            _customEffect.CurrentTechnique = _customEffect.Techniques["Instance"];
            _renderInstancer.DrawInstancedHidden();

            _customEffect.CurrentTechnique = _customEffect.Techniques["MainTech"];
            for (int z = _drawLowest; z <= _drawHighest; z++)
            {
                foreach (var key in SiteVertexBufferChunk.Texture2Ds)
                {
                    _customEffect.Parameters["Texture"].SetValue(key);
                    _customEffect.CurrentTechnique.Passes[0].Apply();

                    for (int x = 0; x < _chunksCount.X; x++)
                    {
                        for (int y = 0; y < _chunksCount.Y; y++)
                        {
                            if (IsChunkVisible(new Point3(x, y, z)))
                            {
                                if (!_renderChunkArray[x, y, z].IsSet && !_renderChunkArray[x, y, z].BlockSet)
                                    _renderChunkArray[x, y, z].SetStaticBuffer();

                                if (z == _drawHighest)
                                    _renderChunkArray[x, y, z].Draw(key,
                                        new List<int> { (int)VertexBufferLayer.HiddenBack, (int)VertexBufferLayer.Back });
                                else
                                    if (!_renderChunkArray[x, y, z].IsFullyHidden ||
                                    (_renderChunkArray[x, y, z].IsFullyHidden && (x == _chunksCount.X - 1 || y == _chunksCount.Y - 1)))
                                    _renderChunkArray[x, y, z].Draw(key,
                                        new List<int> { (int)VertexBufferLayer.Back, (int)VertexBufferLayer.Front });

                                _renderChunkArray[x, y, z].Draw(key,
                                        new List<int> { (int)VertexBufferLayer.BackInteractives, (int)VertexBufferLayer.FrontInteractives });
                                _renderChunkArray[x, y, z].Clear(VertexBufferType.Dynamic);
                            }
                        }
                    }
                }
            }

            /*_spriteBatch.Begin(SpriteSortMode.Deferred);
            _spriteBatch.Draw(Site.hmt, new Vector2(0, 0), new Rectangle(0, 0, Site.hmt.Width, Site.hmt.Height), Color.White);
            _spriteBatch.End();*/
        }

        public void Dispose()
        {
        }
    }
}