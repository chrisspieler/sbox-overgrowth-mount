using System.IO;

namespace Duccsoft.Mounting;

public readonly struct MountAssetPath
{
	public MountAssetPath( SteamGameMount mount, string relativeAssetPath, string customExtension )
	{
		_sourceMount = mount;
		
		Absolute = Path.Combine( _sourceMount.AppDirectory, relativeAssetPath );
		Relative = (relativeAssetPath + customExtension).ToLowerInvariant();
		Mount = Path.Combine( $"mount://{_sourceMount.Ident}/", Relative );
		
		HashCode = Mount.GetHashCode();
	}
	
	public int HashCode { get; }
	public string Absolute { get; }
	public string Mount { get; }
	public string Relative { get; }
	
	private readonly SteamGameMount _sourceMount;
	public MountExplorer Explorer => _sourceMount.Explorer;

	public override int GetHashCode() => HashCode;
}
