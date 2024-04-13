using Microsoft.Xna.Framework;

using Origin.Source.Resources;

namespace Origin.Source.Render
{
    public struct RenderData
    {
        public byte nlayer;
        public Point3 tilePos;
        public Sprite sprite;
        public Color color;
        public Vector3 spriteOffset;

        public RenderData(byte nlayer, Point3 pos, Sprite sprite, Color col, Vector3 off)
        {
            this.nlayer = nlayer;
            tilePos = pos;
            this.sprite = sprite;
            this.color = col;
            this.spriteOffset = off;
        }
    }
}