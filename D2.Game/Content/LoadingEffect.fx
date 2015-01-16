﻿Texture2DArray<float4> Texture : register(t0);
sampler TextureSampler : register(s0);


cbuffer Parameters : register(b0) {

	float TileIndex;
	float3 EyePosition;

	float4x4 World;
	float3x3 WorldInverseTranspose;

}; 

cbuffer ProjectionMatrix : register(b1) {

	row_major float4x4 WorldViewProj    : packoffset(c0);

};

struct VSInputTx
{
	float4 Position : SV_Position;
	float2 TexCoord : TEXCOORD0;
};

struct VSOutputTx
{
    float2 TexCoord   : TEXCOORD0;
    float4 PositionPS : SV_Position;
};

struct PSInputTx
{
	float2 TexCoord : TEXCOORD0;
};

// Vertex shader: texture.
VSOutputTx VSBasicTx(VSInputTx vin)
{
	VSOutputTx vout;

	vout.PositionPS = mul(vin.Position, World);

	//vout.PositionPS = vin.Position;
	vout.TexCoord = vin.TexCoord;

	return vout;
}


// Pixel shader: texture.
float4 PSBasicTx(PSInputTx pin) : SV_Target
{
	float4 color = Texture.Sample(TextureSampler, float3(pin.TexCoord.x, pin.TexCoord.y, TileIndex));
	return color;
}

technique QuadEffect
{
	pass Pass1
	{
		Profile = 10.0;
		VertexShader = VSBasicTx;
		PixelShader = PSBasicTx;
	}
}

