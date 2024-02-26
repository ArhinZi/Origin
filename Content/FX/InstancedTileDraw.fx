#define VS_SHADERMODEL vs_5_0
#define PS_SHADERMODEL ps_5_0
#define CS_SHADERMODEL cs_5_0 
	
#pragma enable_d3d11_debug_symbols

// common
matrix WorldViewProjection;
float2 TextureSize;
float2 WorldSize;
float CurrentLevel;
float2 LowHighLevel;
float3 PositionOffset = float3(0,0,0);

float2 TileSize = float2(32,16);
float2 SpriteSize = float2(32, 32);

// hidden
float2 HiddenSpriteTexturePos;
float4 HiddenColor;

float FloorYoffset = 4;
float ZDiagOffset = 0.01;

bool Unpack(uint packedFlags, int bitOffset)
{
    return ((int) (packedFlags) & (1 << bitOffset)) != 0;
}
float2 GetSpritePositionByCellPosition(float3 cellPos)
{
    float VertexX = (cellPos.x - cellPos.y) * TileSize.x / 2;
    float VertexY = ((cellPos.x + cellPos.y) * TileSize.y / 2)
                  - cellPos.z * (TileSize.y + FloorYoffset);
    
    return float2(VertexX, VertexY);
}
float GetSpriteZOffsetByCellPos(float3 cellPos)
{
    float VertexZ = (cellPos.x + cellPos.y) * ZDiagOffset;
    return VertexZ;
}
float4 ShadeColor(float4 color, uint3 pos)
{
    float light = 1;
    // light desaturation
    float luminance = dot(color.rgb, float3(0.2126, 0.7152, 0.0722));
    float3 grayScale = float3(luminance, luminance, luminance) / 2;
    color.rgb = lerp(grayScale, color.rgb, float3(light, light, light));
    
    // level shading
    float4 fogColor = float4(0.8, 0.8, 0.8, 1.0); // color of fog
    float hyperKS = 0.7;
    float shadeFactor = hyperKS / (hyperKS + (LowHighLevel.y - pos.z) * 0.01);
    float hyperKF = 0.9;
    float fogFactor = hyperKF / (hyperKF + (LowHighLevel.y - pos.z) * 0.01);
    //color.rgb *= hyperK / (hyperK + (MinMaxLevel.y - input.BlockPosition.z) * 0.01);
    return float4(lerp(color.rgb, fogColor.rgb, 1 - fogFactor) * shadeFactor, color.a);
}





int RBIT_COUNT; // count of bitsreally using by hidden tit storage

Texture2D SpriteTexture;
sampler SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
    Filter = Point;
    AddressU = CLAMP; // Clamp addressing mode for U coordinate
    AddressV = CLAMP; // Clamp addressing mode for V coordinate
    AddressW = CLAMP; // Clamp addressing mode for W coordinate (for 3D textures)
};


// 32 bits
struct SpriteMain
{
    float3 SpritePosition;
    float pud1;
    //float2 SpriteSize;
    //float3 CellPosition;
};
RWStructuredBuffer<SpriteMain> RWMainBuffer;
StructuredBuffer<SpriteMain> MainBuffer;

// 32 bits
struct SpriteExtra
{
    float4 Color;
    float4 TextureRect;
};
RWStructuredBuffer<SpriteExtra> RWExtraBuffer;
StructuredBuffer<SpriteExtra> ExtraBuffer;


StructuredBuffer<uint4> HiddenLBuffer;
StructuredBuffer<uint4> HiddenSBuffer;


struct UpdateSpriteData
{
    uint index;
    float pud1;
    float pud2;
    float pud3;
    SpriteMain mainData;
    SpriteExtra extraData;
};
StructuredBuffer<UpdateSpriteData> UpdateData;

StructuredBuffer<uint> Remove;

uint count;
#define GroupSize 64
[numthreads(GroupSize, 1, 1)]
void RemoveSpriteCS(uint3 localID : SV_GroupThreadID, uint3 groupID : SV_GroupID,
        uint localIndex : SV_GroupIndex, uint3 globalID : SV_DispatchThreadID)
{
    if (globalID.x >= count)
    {
        return;
    }
    int index = Remove[globalID.x];
    RWMainBuffer[index].SpritePosition = float3(0, 0, 0);
    RWExtraBuffer[index].TextureRect = float4(0, 0, 0, 0);
}

[numthreads(GroupSize, 1, 1)]
void UpdateSpriteCS(uint3 localID : SV_GroupThreadID, uint3 groupID : SV_GroupID,
        uint localIndex : SV_GroupIndex, uint3 globalID : SV_DispatchThreadID)
{
    if (globalID.x >= count)
    {
        return;
    }
    int index = UpdateData[globalID.x].index;
    RWMainBuffer[index] = UpdateData[globalID.x].mainData;
    RWExtraBuffer[index] = UpdateData[globalID.x].extraData;

}


//8 bytes in total
struct StaticVSinput
{
    float4 Position : COLOR0; // only xyz are needed
    uint VertexID : SV_VertexID;
};
//16 byte + 12 byte = 28 bytes
struct InstancingVSoutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
    float4 ColorD : COLOR0; // only xyz are needed
};

InstancingVSoutput SpriteInstancingVS(in StaticVSinput input)
{
    InstancingVSoutput output;
    
    uint spriteID = input.VertexID/6;
    SpriteMain main = MainBuffer[spriteID];
    SpriteExtra extra = ExtraBuffer[spriteID];
    

    float2 vertPos = float2(input.Position.x * extra.TextureRect.z,
                        input.Position.y * extra.TextureRect.w);
    /*//text
    uint2 ppos = uint2(spriteID % worldSize.x,
                        spriteID / worldSize.x);
    float3 spritePos = float3(GetSpritePositionByCellPosition(uint3(ppos, CurrentLevel)), GetSpriteZOffsetByCellPos(uint3(ppos, CurrentLevel)));
    //endtest*/
	//calculate position with camera
    
    float4 pos = float4(main.SpritePosition.xy + vertPos, main.SpritePosition.z, 1);
    //float4 pos = float4(spritePos.xy + vertPos, spritePos.z, 1);
    pos = mul(pos, WorldViewProjection);
    
    output.Position = pos;
	
    output.TexCoord = float2((extra.TextureRect.x + vertPos.x) / TextureSize.x,
                             (extra.TextureRect.y + vertPos.y) / TextureSize.y);
    
    output.ColorD = extra.Color;
    output.ColorD = ShadeColor(extra.Color, uint3(uint2(0, 0), CurrentLevel));
    
    return output;
}

InstancingVSoutput HiddenLInstancingVS(in StaticVSinput input)
{
    InstancingVSoutput output;
    
    uint spriteID = input.VertexID / 6;
    
    uint3 pos = uint3(spriteID % WorldSize.x,
                        spriteID / WorldSize.x,
                        CurrentLevel);
    
    float3 spritePos = float3(GetSpritePositionByCellPosition(pos), GetSpriteZOffsetByCellPos(pos));
    float2 localVertexPos = float2(input.Position.x * SpriteSize.x, input.Position.y * SpriteSize.y);
    float4 globalVertexPos = float4(spritePos.xy + localVertexPos, spritePos.z, 1) + float4(PositionOffset, 0);
    float4 worldVertexPos = mul(globalVertexPos, WorldViewProjection);
    
    float xy = (WorldSize.x / RBIT_COUNT) * pos.y + pos.x / RBIT_COUNT;
    if (Unpack(HiddenLBuffer[xy][pos.x % RBIT_COUNT / 32], pos.x % RBIT_COUNT))
    {
        worldVertexPos.w = 1;
    }
    else
    {
        worldVertexPos.w = 0;
    }
        
    output.Position = worldVertexPos;
    output.TexCoord = float2((HiddenSpriteTexturePos.x + input.Position.x * SpriteSize.x) / TextureSize.x,
                             (HiddenSpriteTexturePos.y + input.Position.y * SpriteSize.y) / TextureSize.y);
    output.ColorD = ShadeColor(HiddenColor, uint3(uint2(0, 0), CurrentLevel));
    
    return output;
}

InstancingVSoutput HiddenSInstancingVS(in StaticVSinput input)
{
    InstancingVSoutput output;
    
    uint spriteID = input.VertexID / 6;
    
    uint3 pos = uint3(spriteID, WorldSize.y - 1, CurrentLevel);
    if (spriteID >= WorldSize.x)
        pos.xy = uint2(WorldSize.x-1, WorldSize.x+WorldSize.y-1-spriteID);
    
    float3 spritePos = float3(GetSpritePositionByCellPosition(pos), GetSpriteZOffsetByCellPos(pos));
    float2 localVertexPos = float2(input.Position.x * SpriteSize.x, input.Position.y * SpriteSize.y);
    float4 globalVertexPos = float4(spritePos.xy + localVertexPos, spritePos.z, 1) + float4(PositionOffset, 0);
    float4 worldVertexPos = mul(globalVertexPos, WorldViewProjection);
    
    float xy = spriteID/RBIT_COUNT;
    if (Unpack(HiddenSBuffer[xy][spriteID % RBIT_COUNT / 32], spriteID % RBIT_COUNT))
        worldVertexPos.w = 1;
    else
        worldVertexPos.w = 0;
    
        
    output.Position = worldVertexPos;
    output.TexCoord = float2((HiddenSpriteTexturePos.x + input.Position.x * SpriteSize.x) / TextureSize.x,
                             (HiddenSpriteTexturePos.y + input.Position.y * SpriteSize.y) / TextureSize.y);
    output.ColorD.rgba = ShadeColor(HiddenColor, uint3(uint2(0, 0), CurrentLevel));
    
    return output;
}

float4 InstancingPS(InstancingVSoutput input) : SV_TARGET
{
    float4 color = SpriteTexture.Sample(SpriteTextureSampler, input.TexCoord);
    float4 color_sh = color * input.ColorD;
    
    clip((color.a < 0.1) ? -1 : 1);
    return color_sh;
}


//===============================================================================
// Techniques
//===============================================================================

technique SpriteInstancing
{
    pass Main
    {
        VertexShader = compile VS_SHADERMODEL SpriteInstancingVS();
        PixelShader = compile PS_SHADERMODEL InstancingPS();
    }
};

technique HiddenLInstancing
{
    pass Main
    {
        //ComputeShader = compile CS_SHADERMODEL InstancingCS();
        VertexShader = compile VS_SHADERMODEL HiddenLInstancingVS();
        PixelShader = compile PS_SHADERMODEL InstancingPS();
    }
};

technique HiddenSInstancing
{
    pass Main
    {
        //ComputeShader = compile CS_SHADERMODEL InstancingCS();
        VertexShader = compile VS_SHADERMODEL HiddenSInstancingVS();
        PixelShader = compile PS_SHADERMODEL InstancingPS();
    }
};

technique BufferUpdating
{
    pass Remove
    {
        ComputeShader = compile CS_SHADERMODEL RemoveSpriteCS();
    }
    pass Update
    {
        ComputeShader = compile CS_SHADERMODEL UpdateSpriteCS();
    }
};