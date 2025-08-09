using Duccsoft.Mounting;

namespace Overgrowth.Extensions;

public static class MountExplorerExtensions
{
	public static MountAssetPath GetRefWithExtension( 
		this MountExplorer explorer, 
		string relativePath, 
		string extension, 
		string suffix = null 
	) => explorer.RelativePathToAssetRef( relativePath + suffix, extension );
	
	public static MountAssetPath GetTextureRef( this MountExplorer explorer, string relativePath ) 
		=> explorer.GetRefWithExtension( relativePath, ".vtex", "_converted.dds" );
	
	public static MountAssetPath GetModelRef( this MountExplorer explorer, string relativePath ) 
		=> explorer.GetRefWithExtension( relativePath, ".vmdl" );
	
	public static MountAssetPath GetMaterialRef( this MountExplorer explorer, string relativePath ) 
		=> explorer.GetRefWithExtension( relativePath, ".vmat" );
}
