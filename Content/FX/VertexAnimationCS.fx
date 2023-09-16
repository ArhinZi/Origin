#include "BasicHeaders.fxh"

struct animationElement
{
    float2 texPosition;
};
struct animationIndex
{
    // 0:15     -indexCount
    // 16:32    -currentIndex
    uint packedData;
};

StructuredBuffer<animationElement> Elements;
StructuredBuffer<animationIndex> ElementIndexes;
uint AnimationsCount;

[numthreads(64, 1, 1)]
void AnimationUpdater(uint3 localID : SV_GroupThreadID, uint3 groupID : SV_GroupID,
        uint localIndex : SV_GroupIndex, uint3 globalID : SV_DispatchThreadID)
{
    uint vertexID = globalID.x*6;
    
    
    
    for (int i = 0; i < AnimationsCount; i++)
    {
        uint2 ei = uint2((ElementIndexes[i].packedData & 0x0000ffff), (ElementIndexes[i].packedData >> 16));
        animationElement pelem;
        if (ei.y == 0)
        {
            pelem = Elements[i * 8 + ei.x - 1];
        }
        else
        {
            pelem = Elements[i * 8 + ei.y - 1];

        }
        animationElement elem = Elements[i * 8 + ei.y];
        for (int e = 0; e < 6; e++)
        {
            uint posFloats = 3; // position is Vector3
            uint colFloats = 1; // color as Color
            uint texFloats = 2; // textureCoordinate is Vector2
            uint v3 = 3;
            uint totalFloats = posFloats + colFloats + texFloats + v3;
    
            uint bytesPerFloat = 4;
            uint vertexByteInd = (vertexID+e) * totalFloats * bytesPerFloat;
    
            uint posByteInd = vertexByteInd;
            uint colByteInd = posByteInd + posFloats * bytesPerFloat;
            uint texByteInd = colByteInd + colFloats * bytesPerFloat;
    
            float2 tex = asfloat(Vertices.Load2(texByteInd));
            
            if (tex.x == pelem.texPosition.x && tex.y == pelem.texPosition.y)
            {
                tex = elem.texPosition;
                Vertices.Store2(texByteInd, asuint(tex));
                break;
            }
        }   
    }
}

technique UpdateAnimations
{
    pass Pass0
    {
        ComputeShader = compile cs_5_0 AnimationUpdater();
    }
}