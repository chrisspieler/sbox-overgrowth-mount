using Overgrowth.ObjModel;
using Sandbox.Diagnostics;
using Sandbox.Mounting;

namespace Overgrowth;

public class OvergrowthModel( MountAssetPath modelRef, MountAssetPath materialRef, MountAssetPath colorTexRef, MountAssetPath normalTexRef ) : ResourceLoader<OvergrowthMount>
{
	protected override object Load() => LoadModel( modelRef, materialRef, colorTexRef, normalTexRef );
	
	public static Model LoadModel( MountAssetPath modRef, MountAssetPath matRef, MountAssetPath colTexRef, MountAssetPath normTexRef )
	{
		var objData = ObjFile.Load( modRef.Absolute );

		var material = OvergrowthMaterial.LoadMaterial( matRef, colTexRef, normTexRef );
		Assert.NotNull( material, $"Given material was null: {matRef.Relative}" );
		
		var mesh = new Mesh( material );
		mesh.CreateVertexBuffer( objData.Vertices.Count, Vertex.Layout, objData.Vertices );
		mesh.CreateIndexBuffer( objData.Indices.Count, objData.Indices );
		mesh.SetIndexRange( 0, objData.Indices.Count );
		
		var points = objData.Vertices.Select( v => v.Position ).ToArray();
		mesh.Bounds = BBox.FromPoints( points );
		
		return new ModelBuilder()
			.AddMesh( mesh )
			.AddTraceMesh( points, objData.Indices.ToArray() )
			.AddCollisionHull( points )
			.WithName( modRef.Mount )
			.Create();
	}
}
