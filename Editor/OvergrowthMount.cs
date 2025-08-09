using Duccsoft.Mounting;
using Overgrowth.Extensions;
using Sandbox.Mounting;

namespace Overgrowth;

public class OvergrowthMount : SteamGameMount
{
	public override long AppId => 25000;
	public override string Ident => "overgrowth";
	public override string Title => "Overgrowth";

	public int ObjectFileCount { get; private set; }
	public int TextureCount { get; private set; }
	public int MaterialCount { get; private set; }
	public int ModelCount { get; private set; }

	protected override IEnumerable<AddMountResourceCommand> GetAllResources()
	{
		// For each .dds file:
		// - Create a loader for the Texture within that file.
		foreach ( var addTexCommand in CreateTextureLoaders() )
		{
			yield return addTexCommand;
		}
		
		// For each XML file that references models and textures:
		// - Create a loader for a Material that uses the ColorMap and NormalMap textures.
		// - Create a loader for a Model that uses this Material.
		foreach ( var addLoaderCommand in CreateModelLoaders( DeserializeAllObjectFiles() ) )
		{
			yield return addLoaderCommand;
		} 

		Log.Info( $"Mounted \"{Title}\" with {ObjectFileCount} objects, {TextureCount} textures, {MaterialCount} materials, and {ModelCount} models" );
	}

	private IEnumerable<OvergrowthObject> DeserializeAllObjectFiles()
	{
		ObjectFileCount = 0;
		foreach ( var xmlPath in Explorer.FindFilesRecursive( AppDirectory, "*.xml" ) )
		{
			var objData = OvergrowthObject.Load( xmlPath );
			ObjectFileCount++;
			yield return objData;
		}
	}

	private IEnumerable<AddMountResourceCommand> CreateTextureLoaders()
	{
		TextureCount = 0;
		foreach ( var ddsPath in Explorer.FindFilesRecursive( AppDirectory, "*.dds" ) )
		{
			var texLoader = new TextureLoader( ddsPath );
			TextureCount++;
			yield return new AddMountResourceCommand( ResourceType.Texture, ddsPath, texLoader );
		}
	}

	private IEnumerable<AddMountResourceCommand> CreateModelLoaders( IEnumerable<OvergrowthObject> objectFiles )
	{
		MaterialCount = 0;
		ModelCount = 0;
		
		foreach ( var obj in objectFiles )
		{
			var modelRelativePath = obj?.ModelPath?.Relative;
			if ( string.IsNullOrWhiteSpace( modelRelativePath ) )
				continue;

			// Define a Material that uses the ColorMap and NormalMap textures.
			var matRef = Explorer.GetMaterialRef( modelRelativePath );
			var materialLoader = new MaterialLoader(matRef, obj.ColorMapPath, obj.NormalMapPath );
			MaterialCount++;
			yield return new AddMountResourceCommand( ResourceType.Material, matRef, materialLoader );

			// Define a Model that uses the aforementioned Material.
			var modelLoader = new ModelLoader( obj );
			ModelCount++;
			yield return new AddMountResourceCommand( ResourceType.Model, obj.ModelPath.Value, modelLoader );
		}
	}
}
