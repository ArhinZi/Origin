#define VS_SHADERMODEL vs_5_0
#define PS_SHADERMODEL ps_5_0
#define CS_SHADERMODEL cs_5_0 
	
#pragma enable_d3d11_debug_symbols


bool Unpack(float packedFlags, int bitOffset)
{
    return ((int) (packedFlags) & (1 << bitOffset)) != 0;
}

// 32 bits
struct SpriteMain
{
    float3 SpritePosition;
    float pud1;
    //float2 SpriteSize;
    //float3 CellPosition;
};
// 32 bits
struct SpriteExtra
{
    float4 Color;
    float4 TextureRect;
};
	

//==============================================================================
// Vertex shader
//==============================================================================

//Number of Textures inside of the Texture3D
//float NumberOf2DTextures;
matrix WorldViewProjection;

Texture2D SpriteTexture;
sampler SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
    Filter = Point;
    AddressU = CLAMP; // Clamp addressing mode for U coordinate
    AddressV = CLAMP; // Clamp addressing mode for V coordinate
    AddressW = CLAMP; // Clamp addressing mode for W coordinate (for 3D textures)
};

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

StructuredBuffer<SpriteMain> MainBuffer;
StructuredBuffer<SpriteExtra> ExtraBuffer;
float2 texSize;

InstancingVSoutput InstancingVS(in StaticVSinput input)
{
    uint spriteID = input.VertexID/6;
    SpriteMain main = MainBuffer[spriteID];
    SpriteExtra extra = ExtraBuffer[spriteID];
    
    InstancingVSoutput output;
    //uint3 atlasCoordinate = uint3((tile.AtlasCoord & 0x000000ff), (tile.AtlasCoord & 0x0000ff00) >> 8, tile.AtlasCoord >> 16);

	//actual Image Index
    //uint index = atlasCoordinate.z;
	//get texture Size in the atlas
    //float2 imageSize = ImageSizeArray[index];
	
	//how many Images are possible inside of the big texture
    //float2 NumberOfTextures = float2(2048, 2048) / float2(imageSize.x, imageSize.y); // all Images are 2048 x 2048 because 3DTexture doesnt support more and give blackscreen if bigger. Old Hardware cant support that much
	
    float2 vertPos = float2(input.Position.x * extra.TextureRect.z,
                        input.Position.y * extra.TextureRect.w);
    
	
	//calculate position with camera
    
    float4 pos = float4(main.SpritePosition.xy + vertPos, main.SpritePosition.z, 1);
    pos = mul(pos, WorldViewProjection);
    output.Position = pos;
	
    output.TexCoord = float2((extra.TextureRect.x + vertPos.x) / texSize.x,
                             (extra.TextureRect.y + vertPos.y) / texSize.y);
    
    output.ColorD = extra.Color;
    //output.ColorD.r = spriteID / 16;
	
    //output.TexCoord = float3((input.Position.x / NumberOfTextures.x) + (1.0f / NumberOfTextures.x * atlasCoordinate.x),
		//					 (input.Position.y / NumberOfTextures.y) + (1.0f / NumberOfTextures.y * atlasCoordinate.y), index / NumberOf2DTextures + 0.1f / NumberOf2DTextures); //+0.1f / NumberOf2DTextures because texture3d want some between value, in future use 2dTextureArray?
	
	
    return output;
}

//==============================================================================
// Pixel shader 
//==============================================================================

float4 InstancingPS(InstancingVSoutput input) : SV_TARGET
{
    //float4 color = SpriteTexture.Sample(SpriteTextureSampler, input.TexCoord) * input.Diffuse;
    float4 color = SpriteTexture.Sample(SpriteTextureSampler, input.TexCoord);
    float4 color_sh = color * input.ColorD;
    /*float h = 1 / texSize.x;
    float4 upcolor = SpriteTexture.Sample(SpriteTextureSampler, input.TexCoord.xy + float2(0,-h));
    
    if (color.a != 0 && upcolor.a == 0)
    {
        color_sh = float4(0, 0, 0, 1);

    }*/
    
    clip((color.a < 0.1) ? -1 : 1);
    return color_sh;
}


//===============================================================================
// Techniques
//===============================================================================

technique Instancing
{
    pass Main
    {
        //ComputeShader = compile CS_SHADERMODEL InstancingCS();
        VertexShader = compile VS_SHADERMODEL InstancingVS();
        PixelShader = compile PS_SHADERMODEL InstancingPS();
    }
};