using Sandbox.Mounting;

namespace Duccsoft.Mounting;
public record AddMountResourceCommand( ResourceType Type, MountAssetPath Path, ResourceLoader Loader );
