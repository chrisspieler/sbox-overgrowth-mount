using Duccsoft.Mounting;

namespace Overgrowth;

public static class OvergrowthMaterial
{
	public const string DefaultShaderName = "overgrowth_default";

	public static Material CreateDefault( string name )
	{
		var material = Material.Create( name, DefaultShaderName );
		material.Set( "Color", Texture.White );
		material.Set( "Normal", Texture.White );
		return material;
	}
	
	public static Material CreateDefault( string name, MountAssetPath? colorMapPath, MountAssetPath? normalMapPath )
	{
		if ( colorMapPath is null && normalMapPath is null ) 
			return CreateDefault( name );
		
		var material = Material.Create( name, DefaultShaderName );
		material.Set( "Color", Texture.Load( colorMapPath?.Mount ) ?? Texture.White );
		material.Set( "Normal", Texture.Load( normalMapPath?.Mount ) ?? Texture.White );
		return material;
	}
}
