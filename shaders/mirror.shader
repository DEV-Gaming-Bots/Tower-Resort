//=========================================================================================================================
// Optional
//=========================================================================================================================
HEADER
{
	CompileTargets = ( IS_SM_50 && ( PC || VULKAN ) );
	Description = "portalz";
}

//=========================================================================================================================
// Optional
//=========================================================================================================================
FEATURES
{
    #include "common/features.hlsl"
}

//=========================================================================================================================
COMMON
{
	#include "common/shared.hlsl"
}

//=========================================================================================================================

struct VertexInput
{
	#include "common/vertexinput.hlsl"
};

//=========================================================================================================================

struct PixelInput
{
	#include "common/pixelinput.hlsl"
};

//=========================================================================================================================

VS
{
	#include "common/vertex.hlsl"
	//
	// Main
	//
	PixelInput MainVs( INSTANCED_SHADER_PARAMS( VS_INPUT i ) )
	{
		PixelInput o = ProcessVertex( i );
		// Add your vertex manipulation functions here

		return FinalizeVertex( o );
	}
}

//=========================================================================================================================

PS
{
	#include "sbox_pixel.fxc"
	#include "common/pixel.config.hlsl"
	#include "common/pixel.material.structs.hlsl"
	#include "common/pixel.lighting.hlsl"
	#include "common/pixel.shading.hlsl"
	#include "common/pixel.material.helpers.hlsl"

	CreateInputTexture2D( Texture, Srgb, 8, "", "", "Color", Default3( 1.0, 1.0, 1.0 ) );
	// CreateTexture2DInRegister( g_tColor, 0 ) < Channel( RGBA, None( Texture ), Srgb ); OutputFormat( RGBA8888 ); SrgbRead( true ); >;
	CreateTexture2DInRegister( g_tColor, 0 ) < Channel( RGBA, None( Texture ), Srgb ); OutputFormat( RGBA16161616F ); SrgbRead( true ); >;
	//TextureAttribute( RepresentativeTexture, g_tColor );

	//CreateTexture2D( g_tDepthBuffer ) < Attribute( "DepthBufferCopyTexture" ); 	SrgbRead( false ); Filter( MIN_MAG_MIP_POINT ); AddressU( CLAMP ); AddressV( CLAMP ); >;

    // float GetDepth(float2 uv)
    // {
    //     return Tex2DLevel( g_tDepthBuffer, uv, 0 ).r + g_flViewportMinZ;
    // }

	float4 MainPs( PixelInput i ) : SV_Target0
	{
		float4 o;
		float2 psuv = i.vPositionSs.xy;
		//psuv.x = 1 - psuv.x;
		float2 vScreenUv = ScreenspaceCorrectionMultiview( CalculateViewportUv( psuv) );

		// mirror
		vScreenUv.x = 1 - vScreenUv.x;


        o = Tex2D( g_tColor, vScreenUv );

		return o;
	}
}