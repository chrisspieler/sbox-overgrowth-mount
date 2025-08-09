using System.IO;

namespace Duccsoft.Mounting;

public readonly struct MountAssetPath
{
	public MountAssetPath( string mountIdent, string appDirectoryPath, string relativeAssetPath, string customExtension )
	{
		Absolute = Path.Combine( appDirectoryPath, relativeAssetPath );
		Relative = (relativeAssetPath + customExtension).ToLowerInvariant();
		Mount = Path.Combine( $"mount://{mountIdent}/", Relative );
		
		HashCode = Mount.GetHashCode();
	}

	public int HashCode { get; }
	public string Absolute { get; }
	public string Mount { get; }
	public string Relative { get; }

	public override int GetHashCode() => HashCode;
}
