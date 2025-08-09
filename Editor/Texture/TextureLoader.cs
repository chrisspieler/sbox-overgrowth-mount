using Duccsoft.Mounting;
using S3TC;
using Sandbox.Mounting;

namespace Overgrowth;

public class TextureLoader( MountAssetPath textureRef ) : ResourceLoader<OvergrowthMount>
{
	protected override object Load() => DdsFile.Load( textureRef.Absolute ).CreateTexture();
}
