#include "BasicHeaders.fxh"

struct SpriteUnit
{
    float2 texturePosition;
    float2 textureSize;
    float4 color;
};

StructuredBuffer<int> flaggedSprites;
RWStructuredBuffer<SpriteUnit> spriteData;
RWByteAddressBuffer Vertices;

[numthreads(64, 1, 1)]
void VertexUpdater(uint3 localID : SV_GroupThreadID, uint3 groupID : SV_GroupID,
        uint localIndex : SV_GroupIndex, uint3 globalID : SV_DispatchThreadID)
{
    
}

technique UpdateVertices
{
    pass Pass0
    {
        ComputeShader = compile cs_5_0 VertexUpdater();
    }
}