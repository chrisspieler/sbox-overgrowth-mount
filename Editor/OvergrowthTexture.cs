using S3TC;
using Sandbox.Mounting;

namespace Overgrowth;

public class OvergrowthTexture( MountAssetPath textureRef ) : ResourceLoader<OvergrowthMount>
{
	protected override object Load() => LoadTexture( textureRef );
	
	public static Texture LoadTexture( MountAssetPath texRef )
	{
		var ddsFile = DdsFile.Load( texRef.Absolute );
		// Log.Info( $"{ddsFile.Width}x{ddsFile.Height} {ddsFile.Format.ToString()} texture, {ddsFile.Header.dwMipMapCount} mips, compressed size: {ddsFile.CompressedSize}, mip0 size: {ddsFile.GetCompressedDataSize( 0 )}" );
		if ( ddsFile.Width <= 0 || ddsFile.Width > 8192 || ddsFile.Height <= 0 || ddsFile.Height > 8192 )
		{
			Log.Info( $"Unreasonable texture size: {ddsFile.Width}x{ddsFile.Height}" );
			return null;
		}
		
		var size = ddsFile.GetMipLevelSize( 0 );
		return Texture.Create( size.y, size.y, ddsFile.Format )
			.WithData( ddsFile.GetCompressedData( 0 ).ToArray() )
			.Finish();
	}
}
