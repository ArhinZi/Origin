using Arch.Core;
using Arch.Core.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Origin.Source.ECS;
using Origin.Source.Resources;
using Origin.Source.Utils;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Origin.Source.Render
{
    public enum VertexBufferType
    {
        Static,
        Dynamic
    }

    public class SiteVertexBufferChunk : IDisposable
    {
        private static readonly object Lock = new object();

        /// <summary>
        /// Count of element in one vertex array
        /// There can be more then one vertex arrays in one chunk
        /// </summary>
        public static int MaxVertexCount = SiteRenderer.BASE_CHUNK_SIZE.X * SiteRenderer.BASE_CHUNK_SIZE.Y * 6;

        public static int CountOfLayers => Enum.GetNames(typeof(VertexBufferLayer)).Length;

        public Point3 SelfChunkPos { get; private set; }
        public Point SelfChunkSize { get; private set; }
        public SiteRenderer Renderer { get; private set; }

        private SiteTileContainer Blocks => Renderer.Site.Blocks;
        private int seed = 234223534;
        private Sprite lborderSprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", "LeftBorder");
        private Sprite rborderSprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID", "RightBorder");
        private Color borderColor = new Color(0, 0, 0, 150);

        /// <summary>
        /// Shows if chunk is uploaded to graphic device
        /// </summary>
        public bool IsSet { get; private set; }

        public bool IsFullyHidden { get; private set; } = false;
        private bool _isFullyHidden = false;

        public bool UseHiddenInstancing = true;

        private GraphicsDevice _graphicDevice;

        /// <summary>
        /// Set of used textures for all chunks
        /// </summary>
        public static HashSet<Texture2D> Texture2Ds = new HashSet<Texture2D>();

        /// <summary>
        /// Static vertex Indexes:<br/>
        ///     Dictionary of textures for batching;<br/>
        ///     Layers of map;<br/>
        ///     Vertex Index;
        /// </summary>
        private Dictionary<Texture2D, int[]> _staticVertexIndexes;

        /// <summary>
        /// Static vertices:<br/>
        ///     Dictionary of textures for batching;<br/>
        ///     Layers of map;<br/>
        ///     List of Vertex massives if it need more vertices than one array;<br/>
        ///     Vertex array;
        /// </summary>
        private Dictionary<Texture2D, List<VertexPositionColorTextureBlock[]>[]> _staticVertices;

        /// <summary>
        /// Static vertex buffers:<br/>
        ///     Dictionary of textures for batching;<br/>
        ///     Layers of map;<br/>
        ///     List of Vertex massives if it need more buffers than one array;<br/>
        ///     Vertex Buffer;
        /// </summary>
        private Dictionary<Texture2D, List<VertexBuffer>[]> _staticVertexBuffers;

        /// <summary>
        /// Dynamic vertex Indexes:<br/>
        ///     Dictionary of textures for batching;<br/>
        ///     Layers of map;<br/>
        ///     Vertex Index;
        /// </summary>
        private Dictionary<Texture2D, int[]> _dynamicVertexIndexes;

        /// <summary>
        /// Dynamic vertices:<br/>
        ///     Dictionary of textures for batching;<br/>
        ///     Layers of map;<br/>
        ///     List of Vertex massives if it need more vertices than one array;<br/>
        ///     Vertex array;
        /// </summary>
        private Dictionary<Texture2D, List<VertexPositionColorTextureBlock[]>[]> _dynamicVertices;

        public SiteVertexBufferChunk(SiteRenderer renderer, Point3 pos)
        {
            Renderer = renderer;
            SelfChunkPos = pos;
            SelfChunkSize = renderer.ChunkSize;
            _graphicDevice = OriginGame.Instance.GraphicsDevice;
            Init();
            CheckHidden();
        }

        private void Init()
        {
            if (_staticVertices == null)
                _staticVertices = new Dictionary<Texture2D, List<VertexPositionColorTextureBlock[]>[]>();
            if (_staticVertexBuffers == null)
                _staticVertexBuffers = new Dictionary<Texture2D, List<VertexBuffer>[]>();

            if (_dynamicVertices == null)
                _dynamicVertices = new Dictionary<Texture2D, List<VertexPositionColorTextureBlock[]>[]>();

            _staticVertexIndexes = new Dictionary<Texture2D, int[]>();
            _dynamicVertexIndexes = new Dictionary<Texture2D, int[]>();
        }

        public void CheckHidden()
        {
            UseHiddenInstancing = true;
            int tileCoordZ = SelfChunkPos.Z;
            for (int tileInChunkCoordX = 0; tileInChunkCoordX < SelfChunkSize.X; tileInChunkCoordX++)
            {
                for (int tileInChunkCoordY = 0; tileInChunkCoordY < SelfChunkSize.Y; tileInChunkCoordY++)
                {
                    int tileCoordX = SelfChunkPos.X * SelfChunkSize.X + tileInChunkCoordX;
                    int tileCoordY = SelfChunkPos.Y * SelfChunkSize.Y + tileInChunkCoordY;
                    Entity tile = Blocks[tileCoordX, tileCoordY, tileCoordZ];

                    if (tile != Entity.Null)
                    {
                        UseHiddenInstancing = false;
                        return;
                    }
                }
            }
        }

        public void Clear(VertexBufferType type, int clLayer = -1)
        {
            if (type == VertexBufferType.Static)
            {
                //DisposeStaticBuffer();
                foreach (var key in _staticVertexIndexes.Keys)
                {
                    for (int i = 0; i < _staticVertexIndexes[key].Length; i++)
                    {
                        _staticVertexIndexes[key][i] = 0;
                    }
                }
            }
            else if (type == VertexBufferType.Dynamic)
            {
                foreach (var key in _dynamicVertexIndexes.Keys)
                {
                    for (int i = 0; i < _dynamicVertexIndexes[key].Length; i++)
                    {
                        _dynamicVertexIndexes[key][i] = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Fill chunk vertices in already created _renderChunkArray
        /// </summary>
        public void FillStaticVertices(bool HalfWall = false, bool DrawBackBuffer = true, bool DrawFrontBuffer = true)
        {
            if (!UseHiddenInstancing)
            {
                // Loop through each tile block in the chunk
                _isFullyHidden = true;
                int tileCoordZ = SelfChunkPos.Z;
                for (int tileInChunkCoordX = 0; tileInChunkCoordX < SelfChunkSize.X; tileInChunkCoordX++)
                {
                    for (int tileInChunkCoordY = 0; tileInChunkCoordY < SelfChunkSize.Y; tileInChunkCoordY++)
                    {
                        int tileCoordX = SelfChunkPos.X * SelfChunkSize.X + tileInChunkCoordX;
                        int tileCoordY = SelfChunkPos.Y * SelfChunkSize.Y + tileInChunkCoordY;
                        Point3 tilePos = new Point3(tileCoordX, tileCoordY, tileCoordZ);
                        Entity tile = Blocks[tilePos];

                        int rand = seed + tileCoordX + tileCoordY + tileCoordZ;
                        Random random = new Random(rand);
                        rand = random.Next();

                        BaseConstruction bcc;
                        if (tile != Entity.Null && tile.TryGet(out bcc))
                        {
                            TileVisibility visibility = new TileVisibility();
                            Construction constr = GlobalResources.GetResourceBy(GlobalResources.Constructions, "ID", bcc.ConstructionID);
                            Material mat = GlobalResources.GetResourceBy(GlobalResources.Materials, "ID", bcc.MaterialID);

                            if (DrawBackBuffer)
                            {
                                if (visibility.WallVisible && visibility.WallDiscovered)
                                {
                                    // Draw Wall of HalfWall
                                    string spriteType = "Wall";
                                    Point spriteShift = new Point(0, 0);
                                    if (HalfWall && constr.Sprites.ContainsKey("Floor"))
                                    {
                                        spriteType = "Floor";
                                        spriteShift = new Point(0, GlobalResources.Settings.TileSize.Y - GlobalResources.Settings.FloorYoffset);
                                    }
                                    Sprite sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID",
                                            constr.Sprites[spriteType][rand % constr.Sprites[spriteType].Count]);
                                    Color col = constr.HasMaterialColor ? mat.Color : Color.White;
                                    AddSprite(
                                        VertexBufferType.Static,
                                        (int)VertexBufferLayer.Back,
                                        sprite, col, tilePos,
                                        new Point(0, 0) + spriteShift);

                                    // Draw borders on Wall
                                    Entity tmp;
                                    if (Blocks.TryGet(tilePos - new Point3(1, 0, 0), out tmp) && tmp != Entity.Null && (
                                            !tmp.Has<BaseConstruction>()))
                                        AddSprite(
                                                VertexBufferType.Static,
                                                (int)VertexBufferLayer.Back,
                                                lborderSprite, borderColor, tilePos,
                                                new Point(0, 0) + spriteShift);
                                    if (Blocks.TryGet(tilePos - new Point3(0, 1, 0), out tmp) && tmp != Entity.Null && (
                                            !tmp.Has<BaseConstruction>()))
                                        AddSprite(
                                                VertexBufferType.Static,
                                                (int)VertexBufferLayer.Back,
                                                rborderSprite, borderColor, tilePos,
                                                new Point(GlobalResources.Settings.TileSize.X / 2, 0) + spriteShift);

                                    //TODO Draw Embedded

                                    _isFullyHidden = false;
                                }
                            }
                            if (DrawFrontBuffer)
                            {
                                if (visibility.WallVisible && visibility.WallDiscovered)
                                {
                                    // Draw Floor
                                    string spriteType = "Floor";
                                    Sprite sprite = GlobalResources.GetResourceBy(GlobalResources.Sprites, "ID",
                                            constr.Sprites[spriteType][rand % constr.Sprites[spriteType].Count]);
                                    Color col = constr.HasMaterialColor ? mat.Color : Color.White;
                                    AddSprite(
                                        VertexBufferType.Static,
                                        (int)VertexBufferLayer.Front,
                                        sprite, col, tilePos,
                                        new Point(0, -GlobalResources.Settings.FloorYoffset));

                                    // Draw borders of Floor
                                    Entity tmp;
                                    if (Blocks.TryGet(tilePos - new Point3(1, 0, 0), out tmp) && tmp != Entity.Null && (
                                            !tmp.Has<BaseConstruction>()))
                                        AddSprite(
                                                VertexBufferType.Static,
                                                (int)VertexBufferLayer.Front,
                                                lborderSprite, borderColor, tilePos,
                                                new Point(0, -GlobalResources.Settings.FloorYoffset));
                                    if (Blocks.TryGet(tilePos - new Point3(0, 1, 0), out tmp) && tmp != Entity.Null && (
                                            !tmp.Has<BaseConstruction>()))
                                        AddSprite(
                                                VertexBufferType.Static,
                                                (int)VertexBufferLayer.Front,
                                                rborderSprite, borderColor, tilePos,
                                                new Point(GlobalResources.Settings.TileSize.X / 2, -GlobalResources.Settings.FloorYoffset - 1));

                                    //TODO Draw Vegetation

                                    _isFullyHidden = false;
                                }
                            }
                        }
                        else if (tile == Entity.Null)
                        {
                            // Draw hidden
                            Color c = GlobalResources.HIDDEN_COLOR;

                            Sprite sprite = GlobalResources.HIDDEN_WALL_SPRITE;
                            if (Renderer.Site.Size.X - 1 == tileCoordX || Renderer.Site.Size.Y - 1 == tileCoordY)
                                AddSprite(
                                    VertexBufferType.Static,
                                    (int)VertexBufferLayer.Back,
                                    sprite, c, tilePos, new Point(0, 0));
                            else
                                AddSprite(
                                    VertexBufferType.Static,
                                    (int)VertexBufferLayer.HiddenBack,
                                    sprite, c, tilePos, new Point(0, 0));

                            sprite = GlobalResources.HIDDEN_FLOOR_SPRITE;
                            if (Renderer.Site.Size.X - 1 == tileCoordX || Renderer.Site.Size.Y - 1 == tileCoordY)
                                AddSprite(
                                    VertexBufferType.Static,
                                    (int)VertexBufferLayer.Front,
                                    sprite, c, tilePos, new Point(0, -GlobalResources.Settings.FloorYoffset));
                            else
                                AddSprite(
                                VertexBufferType.Static,
                                (int)VertexBufferLayer.HiddenFront,
                                sprite, c, tilePos, new Point(0, -GlobalResources.Settings.FloorYoffset));
                        }
                    }
                }
            }
            IsSet = false;
        }

        public void AddSprite(VertexBufferType type, int vblayer,
            Sprite sprite, Color col, Point3 cellPos, Point offsetPosition,
            float offsetZ = 0,
            Point drawSize = default)
        {
            if (cellPos.X / SelfChunkSize.X != SelfChunkPos.X &&
               cellPos.Y / SelfChunkSize.Y != SelfChunkPos.Y)
            {
                throw new Exception("The Cell is not from this Chunk");
            }

            ref Dictionary<Texture2D, List<VertexPositionColorTextureBlock[]>[]> vertexBatches = ref _staticVertices;
            ref var indarr = ref _staticVertexIndexes;
            if (type == VertexBufferType.Static)
            {
                IsSet = false;
                vertexBatches = ref _staticVertices;
                indarr = ref _staticVertexIndexes;
            }
            else if (type == VertexBufferType.Dynamic)
            {
                vertexBatches = ref _dynamicVertices;
                indarr = ref _dynamicVertexIndexes;
            }
            else
            {
                throw new NotImplementedException();
            }

            lock (Lock)
            {
                if (!vertexBatches.ContainsKey(sprite.Texture))
                {
                    var tmpBatch = new List<VertexPositionColorTextureBlock[]>[CountOfLayers];
                    var tmpIndex = new int[CountOfLayers];
                    for (int i = 0; i < tmpBatch.Length; i++)
                    {
                        tmpBatch[i] = new List<VertexPositionColorTextureBlock[]>();
                        tmpIndex[i] = 0;
                    }
                    vertexBatches.Add(sprite.Texture, tmpBatch);
                    indarr.Add(sprite.Texture, tmpIndex);
                    Texture2Ds.Add(sprite.Texture);
                }
            }
            ref int rindex = ref indarr[sprite.Texture][vblayer];
            int VertexPack = rindex / MaxVertexCount;
            int index = rindex % MaxVertexCount;
            if (vertexBatches[sprite.Texture][vblayer].Count == 0)
            {
                vertexBatches[sprite.Texture][vblayer].Add(new VertexPositionColorTextureBlock[MaxVertexCount]);
            }
            else if (index == 0 && VertexPack >= vertexBatches[sprite.Texture][vblayer].Count)
            {
                vertexBatches[sprite.Texture][vblayer].Add(new VertexPositionColorTextureBlock[MaxVertexCount]);
            }

            VertexPositionColorTextureBlock[] vertices = vertexBatches[sprite.Texture][vblayer][VertexPack];

            Point spritePos = WorldUtils.GetSpritePositionByCellPosition(cellPos) + offsetPosition;
            float vertexZ = WorldUtils.GetSpriteZOffsetByCellPos(cellPos) + offsetZ;

            Rectangle textureRect = sprite.RectPos;

            if (drawSize == default) drawSize = new Point(textureRect.Width, textureRect.Height);

            // Calc the sprite corners positions
            Vector3 topLeft =
                new Vector3(spritePos.X, spritePos.Y, vertexZ);
            Vector3 topRight =
                new Vector3(spritePos.X + drawSize.X, spritePos.Y, vertexZ);
            Vector3 bottomLeft =
                new Vector3(spritePos.X, spritePos.Y + drawSize.Y, vertexZ);
            Vector3 bottomRight =
                new Vector3(spritePos.X + drawSize.X, spritePos.Y + drawSize.Y, vertexZ);

            // Calc the texture coordinates
            Vector2 textureTopLeft = new Vector2((float)textureRect.Left / sprite.Texture.Width, (float)textureRect.Top / sprite.Texture.Height);
            Vector2 textureTopRight = new Vector2((float)textureRect.Right / sprite.Texture.Width, (float)textureRect.Top / sprite.Texture.Height);
            Vector2 textureBottomLeft = new Vector2((float)textureRect.Left / sprite.Texture.Width, (float)textureRect.Bottom / sprite.Texture.Height);
            Vector2 textureBottomRight = new Vector2((float)textureRect.Right / sprite.Texture.Width, (float)textureRect.Bottom / sprite.Texture.Height);

            if (sprite.Effect == MySpriteEffect.FlipHorizontally)
            {
                (textureTopLeft, textureTopRight) = (textureTopRight, textureTopLeft);
                (textureBottomLeft, textureBottomRight) = (textureBottomRight, textureBottomLeft);
            }
            if (sprite.Effect == MySpriteEffect.FlipVertically)
            {
                (textureTopLeft, textureBottomLeft) = (textureBottomLeft, textureTopLeft);
                (textureTopRight, textureBottomRight) = (textureBottomRight, textureTopRight);
            }
            if (sprite.Effect == MySpriteEffect.FlipBLTR)
            {
                (textureBottomLeft, textureTopRight) = (textureTopRight, textureBottomLeft);
            }
            if (sprite.Effect == MySpriteEffect.FlipTLBR)
            {
                (textureTopLeft, textureBottomRight) = (textureBottomRight, textureTopLeft);
            }

            // Add the vertices for the tile to the vertex buffer
            vertices[index++] = new VertexPositionColorTextureBlock(topLeft, col, textureTopLeft, cellPos.ToVector3());
            vertices[index++] = new VertexPositionColorTextureBlock(topRight, col, textureTopRight, cellPos.ToVector3());
            vertices[index++] = new VertexPositionColorTextureBlock(bottomLeft, col, textureBottomLeft, cellPos.ToVector3());

            vertices[index++] = new VertexPositionColorTextureBlock(topRight, col, textureTopRight, cellPos.ToVector3());
            vertices[index++] = new VertexPositionColorTextureBlock(bottomRight, col, textureBottomRight, cellPos.ToVector3());
            vertices[index++] = new VertexPositionColorTextureBlock(bottomLeft, col, textureBottomLeft, cellPos.ToVector3());

            rindex += 6;

            _isFullyHidden = false;
        }

        /// <summary>
        /// Sends vertex buffers to videochip. Something like Update button.
        /// Caution! Dont run it in threads. It causes suspension;
        /// </summary>
        public void SetStaticBuffer()
        {
            DisposeStaticBuffer();
            _staticVertexBuffers = new Dictionary<Texture2D, List<VertexBuffer>[]>();
            foreach (var key in _staticVertices.Keys)
            {
                List<VertexBuffer>[] lvb = new List<VertexBuffer>[Enum.GetNames(typeof(VertexBufferLayer)).Length];
                for (int ilayer = 0; ilayer < _staticVertices[key].Length; ilayer++)
                {
                    ref List<VertexPositionColorTextureBlock[]> list = ref _staticVertices[key][ilayer];
                    lvb[ilayer] = new List<VertexBuffer>();
                    int index = _staticVertexIndexes[key][ilayer];
                    foreach (var item in list)
                    {
                        int count = index > item.Length ? MaxVertexCount : index;
                        VertexBuffer vb = new VertexBuffer(_graphicDevice,
                                     typeof(VertexPositionColorTextureBlock),
                                     count,
                                     BufferUsage.WriteOnly);
                        vb.SetData(item, 0, count);
                        lvb[ilayer].Add(vb);
                    }
                    list.Clear();
                }
                _staticVertexBuffers.Add(key, lvb);
            }
            IsFullyHidden = _isFullyHidden;
            IsSet = true;
        }

        public void Draw(Effect effect, List<int> typesToDraw = null, bool drawstatic = true, bool drawdynamic = true)
        {
            //if (typesToDraw == null) typesToDraw = Enum.GetValues(typeof(VertexBufferLayer));
            typesToDraw ??= Enumerable.Range(0, CountOfLayers).ToList();
            foreach (var key in Texture2Ds)
            {
                effect.Parameters["Texture"].SetValue(key);
                effect.CurrentTechnique.Passes[0].Apply();

                Draw(key, typesToDraw);
            }
        }

        public void Draw(Texture2D key, List<int> typesToDraw = null, bool drawstatic = true, bool drawdynamic = true)
        {
            foreach (var layer in typesToDraw)
            {
                if (_staticVertexBuffers.ContainsKey(key) && drawstatic)
                {
                    foreach (var item in _staticVertexBuffers[key][layer])
                    {
                        if (item.VertexCount > 0)
                        {
                            _graphicDevice.SetVertexBuffer(item);
                            _graphicDevice.DrawPrimitives(
                                   PrimitiveType.TriangleList, 0, item.VertexCount / 3);
                        }
                    }
                }
                if (_dynamicVertices.ContainsKey(key) && drawdynamic)
                {
                    ref List<VertexPositionColorTextureBlock[]> listVP = ref _dynamicVertices[key][layer];
                    int index = _dynamicVertexIndexes[key][layer];
                    int count = index;

                    int i = 0;
                    while (count > 0)
                    {
                        int verticesToDraw = Math.Min(MaxVertexCount, count);

                        // Draw a batch of vertices
                        _graphicDevice.DrawUserPrimitives(PrimitiveType.TriangleList, listVP[i], 0, verticesToDraw / 3);

                        count -= verticesToDraw;
                        i++;
                    }
                }
            }
        }

        public void Dispose()
        {
            DisposeStaticBuffer();
        }

        public void DisposeStaticBuffer(int dispLayer = -1)
        {
            if (_staticVertexBuffers != null)
                foreach (var key in _staticVertexBuffers.Keys)
                {
                    foreach (var layer in _staticVertexBuffers[key])
                    {
                        if (dispLayer >= 0)
                        {
                            if (layer[dispLayer] != null) layer[dispLayer].Dispose();
                            break;
                        }
                        foreach (var item in layer)
                        {
                            if (item != null) item.Dispose();
                        }
                    }
                }
        }
    }
}