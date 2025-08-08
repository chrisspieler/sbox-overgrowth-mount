using Overgrowth.ObjModel;
using Sandbox.Mounting;

namespace Overgrowth;

public class OvergrowthModel( string objAbsPath, string objRelPath, OvergrowthMaterial material ) : ResourceLoader<OvergrowthMount>
{
	protected override object Load()
	{
		var objData = ObjFile.Load( objAbsPath );

		var mesh = new Mesh();
		mesh.CreateVertexBuffer( objData.Vertices.Count, Vertex.Layout, objData.Vertices );
		mesh.CreateIndexBuffer( objData.Indices.Count, objData.Indices );
		
		var result = material.GetOrCreate();
		if ( !result.IsCompleted )
		{
			result.RunSynchronously();
		}
		mesh.Material = (Material)result.Result;

		var points = objData.Vertices.Select( v => v.Position ).ToArray();
		mesh.Bounds = BBox.FromPoints( points );
		
		return new ModelBuilder()
			.AddMesh( mesh )
			.AddTraceMesh( points, objData.Indices.ToArray() )
			.AddCollisionHull( points )
			.WithName( objRelPath )
			.Create();
	}
}
