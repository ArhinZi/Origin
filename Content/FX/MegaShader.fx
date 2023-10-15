#define VS_SHADERMODEL vs_5_0
#define PS_SHADERMODEL ps_5_0

#pragma enable_d3d11_debug_symbols

float4x4 WorldViewProjection;

float2 MinMaxLevel;

Texture2D Texture;
sampler TextureSampler = sampler_state
{
    Texture = <Texture>;
    Filter = Point;
    AddressU = CLAMP; // Clamp addressing mode for U coordinate
    AddressV = CLAMP; // Clamp addressing mode for V coordinate
    AddressW = CLAMP; // Clamp addressing mode for W coordinate (for 3D textures)
};

// Daytime. 
//0     - 00:00 
//0.25  - 06:00
//0.5   - 12:00
//0.75  - 18:00
//1     - 24:00
float1 DayTime;
float4 SunRiseColor;
float4 SunSetColor;

float4 AmbientColor = float4(1, 1, 1, 1);

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
    float3 BlockPosition : TEXCOORD1;
};

struct InstanceShaderInput
{
    float4 Position : POSITION0;
    float2 TextureCoordinate : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_Position;
    float2 TextureCoordinates : TEXCOORD0;
    float4 Diffuse : COLOR0;
    float3 BlockPosition : TEXCOORD1;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;
    
    output.Position = mul(input.Position, WorldViewProjection);
    //if ((output.Position.x > 1024 || output.Position.y > 1024))
       //output.Position.w = 0;
    output.TextureCoordinates = input.TextureCoordinates;
    output.Diffuse = input.Color;
    output.BlockPosition = input.BlockPosition;

    return output;
}


VertexShaderOutput VertexInstanceShaderFunction(InstanceShaderInput input,

        uint vid : SV_VertexID,
        float3 position : POSITION1,
        float4 color : COLOR0,
        float4 texRect : TEXCOORD2,
        int layer : TEXCOORD3)
{
    VertexShaderOutput output;
    
    output.Position = mul(input.Position + float4(position, 0), WorldViewProjection);
    //output.Position = input.Position + float4(position, 1);
    
    output.TextureCoordinates = input.TextureCoordinate;
    output.Diffuse = color;
    output.BlockPosition.z = layer;

    return output;
}


float4 PixelShaderFunction(VertexShaderOutput input) : SV_Target
{
    //clip((input.Position.x > 1024 || input.Position.y > 1024) ? -1 : 1);
    // get pixel color
    float4 color = Texture.Sample(TextureSampler, input.TextureCoordinates) * input.Diffuse;
    // clip pixel if too opakue
    clip((color.a == 0) ? -1 : 1);
    
    // calc and apply light
    float1 light = 1 - abs(DayTime * 2 - 1);
    
    // light darken
    //color.rgb = color.rgb * light;
    
    // light desaturation
    float luminance = dot(color.rgb, float3(0.2126, 0.7152, 0.0722));
    float3 grayScale = float3(luminance, luminance, luminance)/2;
    color.rgb = lerp(grayScale, color.rgb, float3(light,light,light));
    
    // level shading
    float4 fogColor = float4(0.8, 0.8, 0.8, 1.0); // color of fog
    float hyperKS = 0.2;
    float shadeFactor = hyperKS / (hyperKS + (MinMaxLevel.y - input.BlockPosition.z) * 0.01);
    float hyperKF = 0.6;
    float fogFactor = hyperKF / (hyperKF + (MinMaxLevel.y - input.BlockPosition.z) * 0.01);
    //color.rgb *= hyperK / (hyperK + (MinMaxLevel.y - input.BlockPosition.z) * 0.01);
    color.rgb = lerp(color.rgb, fogColor.rgb, 1 - fogFactor) * shadeFactor;

    if (input.BlockPosition.z == 1)
        color = float4(1, 0, 0, 1);
    //if (input.BlockPosition.z == 0)
      //  color.rgb = float3(0, 0, 0);
    //color.rgb *= 1;
    //color.rgb = input.BlockPosition.rgb;
    //color = Texture.Sample(TextureSampler, input.TextureCoordinates) * input.Diffuse;
    return color;
}

technique MainTech
{
    pass MainPass
    {
        VertexShader = compile VS_SHADERMODEL VertexShaderFunction();
        PixelShader = compile PS_SHADERMODEL PixelShaderFunction();
    }
}
technique Instance
{
    pass InstancePass
    {
        VertexShader = compile VS_SHADERMODEL VertexInstanceShaderFunction();
        PixelShader = compile PS_SHADERMODEL PixelShaderFunction();
    }
}