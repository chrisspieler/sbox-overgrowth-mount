using System.IO;
using System.Threading.Tasks;
using Sandbox.Mounting;
using Directory = System.IO.Directory;

namespace Overgrowth;

public class OvergrowthMount : BaseGameMount
{
	private record MountContextAddCommand( ResourceType Type, string Path, ResourceLoader Loader );
	
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
			context.Add( resource.Type, resource.Path, resource.Loader );
		}
		
		Log.Info( $"Mounted \"{Title}\"" );
		
		IsMounted = true;
		return Task.CompletedTask;
	}
	
	private IEnumerable<MountContextAddCommand> GetAllResources()
	{
		var objects = new List<OvergrowthObject>();
		foreach ( var (absPath, relPath) in FindFilesRecursive( AppDirectory, "*.xml" ) )
		{
			objects.Add( OvergrowthObject.Load( absPath, relPath ) );
		}
		
		var numTexturesFound = 0;
		foreach ( var ( absPath, relPath ) in FindFilesRecursive( AppDirectory, "*.dds" ) )
		{
			var loader = new OvergrowthTextureFile( absPath, relPath );
			numTexturesFound++;
			yield return new MountContextAddCommand( ResourceType.Texture, relPath, loader );
		}
		
		var numModelsFound = 0;
		foreach ( var obj in objects )
		{
			if ( string.IsNullOrWhiteSpace( obj.ModelPath ) )
				continue;
			
			var colorTex = obj.ColorMapPath is null ? Texture.White : Texture.Load( obj.ColorMapPath + "_converted.dds" );
			var normalTex = obj.NormalMapPath is null ? Texture.White : Texture.Load( obj.NormalMapPath + "_converted.dds" );
			
			var absModelPath = Path.Combine( AppDirectory, obj.ModelPath );
			var relModelPath = obj.ModelPath + ".vmdl";
			var relMatPath =  Path.ChangeExtension( relModelPath, ".vmat" );

			var materialLoader = new OvergrowthMaterial("mount://overgrowth/" + relMatPath, colorTex, normalTex );
			yield return new MountContextAddCommand( ResourceType.Material, relMatPath, materialLoader );
			
			var modelLoader = new OvergrowthModel( absModelPath,"mount://overgrowth/" + relModelPath, materialLoader );
			numModelsFound++;

			yield return new MountContextAddCommand( ResourceType.Model, relModelPath, modelLoader );
		}
		Log.Info( $"Mounted \"{Title}\" with {objects.Count} objects, {numTexturesFound} textures, and {numModelsFound} models" );
	}

	private IEnumerable<(string absPath, string relPath)> FindFilesRecursive( string directory, string pattern )
	{
		var options = new EnumerationOptions() { RecurseSubdirectories = true };
		return Directory
			.EnumerateFiles( directory, pattern, options )
			.Select( absPath => ( absPath, Path.GetRelativePath( AppDirectory, absPath ) ) );
	}
}
