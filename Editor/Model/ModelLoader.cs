using Sandbox.Mounting;

namespace Overgrowth;

public class ModelLoader( OvergrowthObject objectFile ) 
	: ResourceLoader<OvergrowthMount>
{
	protected override object Load() => new OvergrowthModel( objectFile ).CreateModel();
}
