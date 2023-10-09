
	#define VS_SHADERMODEL vs_5_0
	#define PS_SHADERMODEL ps_5_0


float4x4 WorldViewProjection;

float2 MinMaxLevel;

Texture2D Texture;
sampler TextureSampler = sampler_state
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
    float4 Position : COLOR0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Diffuse : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
    float3 BlockPosition : TEXCOORD1;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input, 
        uint vid:SV_VertexID,
        float4 instanceTransform : POSITION1,
        float4 instanceColor: COLOR1,
        float4 instanceTexture: TEXCOORD2,
        float3 blockPos: TEXCOORD3)
{
    VertexShaderOutput output;
    
    output.Position = mul(input.Position * float4(32,32,1,1) + instanceTransform, WorldViewProjection);
    
    float2 textureCoordinates[4];
    textureCoordinates[0] = float2(instanceTexture.x / 128, instanceTexture.y / 96);
    textureCoordinates[1] = float2(instanceTexture.z / 128, instanceTexture.y / 96);
    textureCoordinates[2] = float2(instanceTexture.x / 128, instanceTexture.w / 96);
    textureCoordinates[3] = float2(instanceTexture.z / 128, instanceTexture.w / 96);
    
    output.TextureCoordinates = textureCoordinates[vid%4];
    output.Diffuse = instanceColor;
    output.BlockPosition = blockPos;

    return output;
}



float4 PixelShaderFunction(VertexShaderOutput input) : SV_Target0
{
    // get pixel color
    float4 color = Texture.Sample(TextureSampler, input.TextureCoordinates.xy) * input.Diffuse;
    //color = float4(0, 0, 0, 1);
    // clip pixel if too opakue
    /*clip((color.a < 0.1) ? -1 : 1);
    
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

    //if (input.BlockPosition.z == 0)
      //  color.rgb = float3(0, 0, 0);
    //color.rgb *= 1;
    //color.rgb = input.BlockPosition.rgb;*/
    return color;
}



technique MainTech
{
    pass MainPass
    {
        VertexShader = compile VS_SHADERMODEL VertexShaderFunction();
        PixelShader = compile PS_SHADERMODEL PixelShaderFunction();
    }
    pass InstancePass
    {
        VertexShader = compile VS_SHADERMODEL VertexShaderFunction();
        PixelShader = compile PS_SHADERMODEL PixelShaderFunction();
    }
}