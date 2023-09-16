using Microsoft.Xna.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.Utils
{
    [StructLayout(LayoutKind.Explicit)]
    public struct SpriteUnit
    {
        [FieldOffset(0)] public Vector2 texturePosition;
        [FieldOffset(8)] public Vector2 textureSize;
        [FieldOffset(16)] public Color Color;
    }
}