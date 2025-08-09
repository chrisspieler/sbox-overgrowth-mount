using Duccsoft.Mounting;
using Sandbox.Mounting;

namespace Overgrowth;

public class MaterialLoader( MountAssetPath materialPath, MountAssetPath? colorPath, MountAssetPath? normalPath ) 
	: ResourceLoader<OvergrowthMount>
{
	protected override object Load() => OvergrowthMaterial.CreateDefault( materialPath.Mount, colorPath, normalPath );
}
