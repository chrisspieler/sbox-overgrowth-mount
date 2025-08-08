FEATURES
{
    #include "common/features.hlsl"
}

MODES
{
    Forward();
    Depth();
}

COMMON
{
	#include "common/shared.hlsl"
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
};

VS
{
	#include "common/vertex.hlsl"

	PixelInput MainVs( VertexInput i )
	{
		PixelInput o = ProcessVertex( i );
		return FinalizeVertex( o );
	}
}

PS
{
	#define CUSTOM_MATERIAL_INPUTS
    #include "common/pixel.hlsl"

	CreateInputTexture2D( Color, Srgb, 8, "None", "_color", ",0/,0/0", Default4( 1.0, 1.0, 1.0, 1.0 ) );
	CreateInputTexture2D( Normal, Srgb, 8, "None", "_normal", ",0/,0/0", Default4( 1.0, 1.0, 1.0, 1.0 ) );
	Texture2D g_tColor < Channel( RGBA, Box( Color ), Srgb ); OutputFormat( DXT5 ); SrgbRead( true ); >;
	Texture2D g_tNormal < Channel( RGBA, Box( Normal ), Srgb ); OutputFormat( DXT5 ); SrgbRead( true ); >;

	Material CreateMaterial( PixelInput i, 
                            float4 vColor, 
                            float4 vNormalOs
							)
    {
        Material p = Material::Init();

        p.Albedo = vColor.rgb;
		// TODO: Convert object space normals to world space.
        p.Normal = normalize( i.vNormalWs.xyz + vNormalOs.xyz );
        p.Roughness = 1;
        p.Metalness = 0;
        p.AmbientOcclusion = 1;
        p.Opacity = vColor.a;       // Opacity is stored in the alpha channel of the color map
        p.Emission = 0;
        p.GeometricNormal = i.vNormalWs;
        p.WorldTangentU = i.vTangentUWs;
        p.WorldTangentV = i.vTangentVWs;
        
        // Seems backwards.. But it's what Valve were doing?
        p.WorldPosition = i.vPositionWithOffsetWs + g_vHighPrecisionLightingOffsetWs.xyz;
        p.WorldPositionWithOffset = i.vPositionWithOffsetWs;
        p.ScreenPosition = i.vPositionSs;

        return p;
    }

	float4 MainPs( PixelInput i ) : SV_Target0
	{
		float4 albedo = g_tColor.Sample( g_sAniso, i.vTextureCoords.xy );
		float4 normal = g_tNormal.Sample( g_sAniso, i.vTextureCoords.xy );
		Material m = CreateMaterial( i, albedo, normal );
		return ShadingModelStandard::Shade( i, m );
	}
}
