using Sandbox.Mounting;

namespace Overgrowth;

public class OvergrowthMaterial( string matRelPath, Texture colorMap, Texture normalMap ) : ResourceLoader<OvergrowthMount>
{
	protected override object Load()
	{
		var material = Material.Create( matRelPath, "shaders/complex.shader" );
		if ( colorMap.IsValid() )
		{
			material.Set( "Color", colorMap );
		}
		if ( normalMap.IsValid() )
		{
			material.Set( "Normal", normalMap );
		}
		return material;
	}
}
