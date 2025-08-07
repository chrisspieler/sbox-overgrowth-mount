using System.IO;
using System.Threading.Tasks;
using S3TC;
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
		var numTexturesFound = 0;
		foreach ( var ( absPath, relPath ) in FindFilesRecursive( TexturesDirectory, "*.dds" ) )
		{
			var loader = new OvergrowthTextureFile( absPath );
			numTexturesFound++;
			yield return new MountContextAddCommand( ResourceType.Texture, relPath, loader );
		}
		
		Log.Info( $"Mounted \"{Title}\" with {numTexturesFound} textures" );
	}

	private IEnumerable<(string absPath, string relPath)> FindFilesRecursive( string directory, string pattern )
	{
		var options = new EnumerationOptions() { RecurseSubdirectories = true };
		return Directory
			.EnumerateFiles( directory, pattern, options )
			.Select( absPath => ( absPath, Path.GetRelativePath( AppDirectory, absPath ) ) );
	}
}
