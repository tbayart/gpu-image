
uniform extern texture image;
uniform extern texture imageparm;
uniform extern float2 isize;
uniform extern float4x4 TexProj : MODELVIEWPROJECTION;
uniform extern float4x4 TexProj2 : MODELVIEWPROJECTION;
uniform extern float4x4 TexProj3 : MODELVIEWPROJECTION;
uniform extern float4x4 TexProj4 : MODELVIEWPROJECTION;
uniform extern float2 stepImageXy;
uniform extern float parms[100];
uniform extern float2 stepKernelXy; // == 0.5/XY; == 1/6


struct VS_INPUT   // all in Model space, without colour
{
    float3 modelPosition : POSITION; 
};

// standard structure for vs output, ps input
struct VS_OUTPUT_Image
{
    float4 Position : POSITION;
    float2 TexPos : TEXCOORD0;  // needed because hlsl rules do not let POSITION be read in pixel shader
};

// two transform structure for output, ps input
struct VS_OUTPUT_Image2
{
    float4 Position : POSITION;
    float2 TexPos : TEXCOORD0;  // needed because hlsl rules do not let POSITION be read in pixel shader
    float2 TexPos2 : TEXCOORD1;  // needed because hlsl rules do not let POSITION be read in pixel shader
};


struct PS_OUTPUT
{
    float4 pixClr : COLOR;
};

// point samplers for use in 'normal' image processing
sampler is = sampler_state {
    SRGBTexture = 1; Texture = <image>;
    mipfilter = POINT; minfilter = POINT; magfilter = POINT;
    AddressU = CLAMP; AddressV = CLAMP;
};

sampler isparmbil = sampler_state {
    SRGBTexture = 0; Texture = <imageparm>;
    mipfilter = LINEAR; minfilter = LINEAR; magfilter = LINEAR;
    AddressU = CLAMP; AddressV = CLAMP;
};

VS_OUTPUT_Image Image_VS(VS_INPUT Input)
{
    VS_OUTPUT_Image Output;
    Output.Position  = float4( Input.modelPosition, 1 );
    Output.TexPos = mul(Output.Position, TexProj).xy;
    return Output;
}

PS_OUTPUT Standard_PS(VS_OUTPUT_Image Input)
{
    PS_OUTPUT rcol;
    float4 col;
    col = float4(1,0,0,1);
    rcol.pixClr = col;
    return rcol;
}
technique Standard { pass P0 { SRGBWriteEnable = 1; VertexShader = compile vs_3_0 Image_VS();
        PixelShader = compile ps_3_0 Standard_PS(); } }
       
