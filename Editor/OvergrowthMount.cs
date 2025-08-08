using System.IO;
using System.Threading.Tasks;
using Sandbox.Mounting;
using Directory = System.IO.Directory;

namespace Overgrowth;

public class OvergrowthMount : BaseGameMount
{
	private record MountContextAddCommand( ResourceType Type, MountAssetPath Path, ResourceLoader Loader );
	
	public long AppId => 25000;
	public override string Ident => "overgrowth";
	public override string Title => "Overgrowth";
	public string AppDirectory { get; private set; }
	public string DataDirectory => Path.Combine( AppDirectory, "Data" );
	public string ModelsDirectory => Path.Combine( DataDirectory, "Models" );
	public string MusicDirectory => Path.Combine( DataDirectory, "Music" );
	public string ObjectsDirectory => Path.Combine( DataDirectory, "Objects" );
	public string SkeletonsDirectory => Path.Combine( DataDirectory, "Skeletons" );
	public string SoundsDirectory => Path.Combine( DataDirectory, "Sounds" );
	public string TexturesDirectory => Path.Combine( DataDirectory, "Textures" );

	public MountAssetPath RelativePathToAssetRef( string relativePath, string customExtension ) 
		=> new MountAssetPath( Ident, AppDirectory, relativePath, customExtension ); 
	
	protected override void Initialize( InitializeContext context )
	{
		if ( !context.IsAppInstalled( AppId ) )
			return;
		
		AppDirectory = context.GetAppDirectory( AppId );
		IsInstalled = Path.Exists( AppDirectory );
	}
	
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
	
	private IEnumerable<MountContextAddCommand> GetAllResources()
	{
		var objects = new List<OvergrowthObject>();
		foreach ( var xmlPath in FindFilesRecursive( AppDirectory, "*.xml" ) )
		{
			objects.Add( OvergrowthObject.Load( xmlPath ) );
		}
		
		var numTexturesFound = 0;
		foreach ( var ddsPath in FindFilesRecursive( AppDirectory, "*.dds" ) )
		{
			var loader = new OvergrowthTexture( ddsPath );
			numTexturesFound++;
			yield return new MountContextAddCommand( ResourceType.Texture, ddsPath, loader );
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
			
			var materialLoader = new OvergrowthMaterial(matRef, colorTexRef, normalTexRef );
			yield return new MountContextAddCommand( ResourceType.Material, matRef, materialLoader );
			
			var modelLoader = new OvergrowthModel( modelRef, matRef, colorTexRef, normalTexRef );
			numModelsFound++;

			yield return new MountContextAddCommand( ResourceType.Model, modelRef, modelLoader );
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
