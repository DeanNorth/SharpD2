#include "Macros.fxh"

DECLARE_TEXTUREARRAY(Texture, 0);


BEGIN_CONSTANTS


float TileIndex _cb(c11);
float3 EyePosition              _vs(c13) _ps(c14) _cb(c12);



row_major float4x4 World                  _vs(c19)          _cb(c15);
row_major float3x3 WorldInverseTranspose  _vs(c23)          _cb(c19);

MATRIX_CONSTANTS

row_major float4x4 WorldViewProj          _vs(c15)          _cb(c0);

END_CONSTANTS


#include "Structures.fxh"
#include "Common.fxh"



// Vertex shader: texture.
VSOutputTx VSBasicTx(VSInputTx vin)
{
	VSOutputTx vout;

	CommonVSOutput cout = ComputeCommonVSOutput(vin.Position);
	SetCommonVSOutputParams;

	vout.TexCoord = vin.TexCoord;

	return vout;
}



// Pixel shader: texture.
float4 PSBasicTx(PSInputTx pin) : SV_Target
{
	float4 color = SAMPLE_TEXTURE(Texture, float3(pin.TexCoord, TileIndex));

	//float4 diffuseCol = pow(color, 1.0 / 2.2);

	//ApplyFog(color, pin.Specular.w);

	return float4(pow(color.rgb, 1.0 / 1.4), color.a);

	//return diffuseCol;
}


technique FloorEffect
{
    pass Pass1
    {
		Profile = 10.0;
        VertexShader = VSBasicTx;
        PixelShader = PSBasicTx;
    }
}

