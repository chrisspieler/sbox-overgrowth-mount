using Overgrowth.Extensions;
using Sandbox.Diagnostics;
using Wavefront;

namespace Overgrowth;

public class OvergrowthModel
{
	public OvergrowthModel( OvergrowthObject objectFile )
	{
		Object = objectFile;
		if ( Object?.ModelPath is null )
			return;
		
		ObjFileData = ObjFile.Load( Object.ModelPath.Value.Absolute );
	}

	public OvergrowthObject Object { get; }
	public ObjFile ObjFileData { get; private set; }

	public Model CreateModel()
	{
		if ( Object?.ModelPath is null )
			return null;

		var modelRef = Object.ModelPath.Value;
		var materialPath = modelRef.Explorer.GetMaterialRef( modelRef.Relative );
		
		var material = Object.ColorMapPath is not null || Object.NormalMapPath is not null
			? OvergrowthMaterial.CreateDefault( materialPath.Mount, Object.ColorMapPath, Object.NormalMapPath )
			: OvergrowthMaterial.CreateDefault( materialPath.Mount ); 
		
		Assert.NotNull( material, $"Given material was null: {materialPath.Relative}" );
		
		var mesh = new Mesh( material );
		mesh.CreateVertexBuffer( ObjFileData.Vertices.Count, Vertex.Layout, ObjFileData.Vertices );
		mesh.CreateIndexBuffer( ObjFileData.Indices.Count, ObjFileData.Indices );
		mesh.SetIndexRange( 0, ObjFileData.Indices.Count );
		
		var points = ObjFileData.Vertices.Select( v => v.Position ).ToArray();
		mesh.Bounds = BBox.FromPoints( points );

		return new ModelBuilder()
			.AddMesh( mesh )
			.AddTraceMesh( points, ObjFileData.Indices.ToArray() )
			.AddCollisionHull( points )
			.WithName( modelRef.Mount )
			.Create();
	}
}
