using System.IO;
using Sandbox.Mounting;

namespace Duccsoft.Mounting;

public abstract class SteamGameMount : BaseGameMount
{
	public abstract long AppId { get; }
	public string AppDirectory { get; protected set; }

	protected override void Initialize( InitializeContext context )
	{
		if ( !context.IsAppInstalled( AppId ) )
			return;
		
		AppDirectory = context.GetAppDirectory( AppId );
		IsInstalled = Path.Exists( AppDirectory );
	}
	
	public MountAssetPath RelativePathToAssetRef( string relativePath, string customExtension ) 
		=> new MountAssetPath( Ident, AppDirectory, relativePath, customExtension ); 
}
