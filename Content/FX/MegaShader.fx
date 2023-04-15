#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float4x4 WorldViewProjection;

Texture2D Texture;
sampler2D TextureSampler = sampler_state
{
    Texture = <Texture>;
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
    float4 Position : SV_Position;
    float2 TextureCoordinates : TEXCOORD0;
    float4 Color : COLOR;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float2 TextureCoordinates : TEXCOORD1;
    float4 Diffuse : COLOR0;
    float4 LayerNNN : TEXCOORD2;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;
    
    output.Position = mul(input.Position, WorldViewProjection);
    output.TextureCoordinates = input.TextureCoordinates;
    output.Diffuse = input.Color;
    output.LayerNNN = float4(0, 0, 0, 0);

    return output;
}



float4 PixelShaderFunction(VertexShaderOutput input) : SV_Target0
{
    // get pixel color
    float4 color = tex2D(TextureSampler, input.TextureCoordinates) * input.Diffuse;
    // clip pixel if too opakue
    clip((color.a < 0.1) ? -1 : 1);
    
    // calc and apply light
    float1 light = 1 - abs(DayTime * 2 - 1);
    
    // light darken
    //color.rgb = color.rgb * light;
    
    // light desaturation
    float luminance = dot(color.rgb, float3(0.2126, 0.7152, 0.0722));
    float3 grayScale = float3(luminance, luminance, luminance)/2;
    color.rgb = lerp(grayScale, color.rgb, float3(light,light,light));
    
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