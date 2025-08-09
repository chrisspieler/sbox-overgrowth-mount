using System.IO;
using System.Threading.Tasks;
using Duccsoft.Mounting;
using Sandbox.Mounting;
using Directory = System.IO.Directory;

namespace Overgrowth;

public class OvergrowthMount : SteamGameMount
{
	public override long AppId => 25000;
	public override string Ident => "overgrowth";
	public override string Title => "Overgrowth";
	
	protected override Task Mount( MountContext context )
	{
		foreach ( var resource in GetAllResources() )
		{
			context.Add( resource.Type, resource.Path.Relative, resource.Loader );
		}
		
		Log.Info( $"Mounted \"{Title}\"" );
		
		IsMounted = true;
		return Task.CompletedTask;
	}
	
	private IEnumerable<AddMountResourceCommand> GetAllResources()
	{
		var objects = new List<OvergrowthObject>();
		foreach ( var xmlPath in FindFilesRecursive( AppDirectory, "*.xml" ) )
		{
			objects.Add( OvergrowthObject.Load( xmlPath ) );
		}
		
		var numTexturesFound = 0;
		foreach ( var ddsPath in FindFilesRecursive( AppDirectory, "*.dds" ) )
		{
			var loader = new TextureLoader( ddsPath );
			numTexturesFound++;
			yield return new AddMountResourceCommand( ResourceType.Texture, ddsPath, loader );
		}
		
		var numModelsFound = 0;
		foreach ( var obj in objects )
		{
			if ( string.IsNullOrWhiteSpace( obj.ModelPath ) )
				continue;

			var colorTexRef = RelativePathToAssetRef( obj.ColorMapPath + "_converted.dds", ".vtex" );
			var normalTexRef = RelativePathToAssetRef( obj.NormalMapPath + "_converted.dds", ".vtex" );
			var modelRef = RelativePathToAssetRef( obj.ModelPath, ".vmdl" );
			var matRef = RelativePathToAssetRef( obj.ModelPath, ".vmat" );
			
			var materialLoader = new MaterialLoader(matRef, colorTexRef, normalTexRef );
			yield return new AddMountResourceCommand( ResourceType.Material, matRef, materialLoader );
			
			var modelLoader = new ModelLoader( modelRef, matRef, colorTexRef, normalTexRef );
			numModelsFound++;

			yield return new AddMountResourceCommand( ResourceType.Model, modelRef, modelLoader );
		}
		Log.Info( $"Mounted \"{Title}\" with {objects.Count} objects, {numTexturesFound} textures, and {numModelsFound} models" );
	}

	private IEnumerable<MountAssetPath> FindFilesRecursive( string directory, string pattern )
	{
		var options = new EnumerationOptions() { RecurseSubdirectories = true };
		return Directory
			.EnumerateFiles( directory, pattern, options )
			.Select( absPath => RelativePathToAssetRef( Path.GetRelativePath( AppDirectory, absPath ), string.Empty ) );
	}
}
