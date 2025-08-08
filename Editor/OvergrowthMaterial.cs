using Sandbox.Mounting;

namespace Overgrowth;

public class OvergrowthMaterial( MountAssetPath materialPath, MountAssetPath colorPath, MountAssetPath normalPath ) 
	: ResourceLoader<OvergrowthMount>
{
	protected override object Load() => LoadMaterial( materialPath, colorPath, normalPath );
	
	public static Material LoadMaterial( MountAssetPath matPath, MountAssetPath colPath, MountAssetPath normPath )
	{
		var material = Material.Create( matPath.Mount, "overgrowth_default" );
		material.Set( "Color", Texture.Load( colPath.Mount ) ?? Texture.White );
		material.Set( "Normal", Texture.Load( normPath.Mount ) ?? Texture.White );
		return material;
	}

}
