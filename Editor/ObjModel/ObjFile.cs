using System;
using Sandbox.Diagnostics;

namespace Overgrowth.ObjModel;

public class ObjFile
{
	private enum RecordType
	{
		Unknown,
		Position,
		Normal,
		TexCoord,
		Face
	}
	
	private struct FaceVertex
	{
		public int PositionIndex;
		public int TexCoordIndex;
		public int NormalIndex;
	}
	
	public const string LineVertexPosition = "v";
	public const string LineVertexNormal = "vn";
	public const string LineVertexTexCoord = "vt";
	public const string LineFace = "f";
	
	public ObjFile( List<Vertex> vertices, List<int> indices )
	{
		Vertices = vertices;
		Indices = indices;
	}

	public int VertexCount => Vertices.Count;
	public List<Vertex> Vertices { get; init; }
	public int IndexCount => Indices.Count;
	public List<int> Indices { get; init; }
	
	public static ObjFile Load( string filePath )
	{
		var lines = System.IO.File.ReadAllLines( filePath );


		var positions = new List<Vector3>();
		var normals = new List<Vector3>();
		var texCoords = new List<Vector2>();
		var faceData = new List<FaceVertex>();
		var vertices = new List<Vertex>();

		foreach ( var line in lines )
		{
			switch ( GetLineType( line, out var lineData ) )
			{
				case RecordType.Position:
					positions.Add( ParseVector3( lineData ) );
					break;
				case RecordType.TexCoord:
					texCoords.Add( ParseVector2( lineData ) );
					break;
				case RecordType.Face:
					var face = ParseFaceIndices( lineData );
					faceData.AddRange( face );
					break;
				default:
					continue;
			}
		}

		var newTexCoords = positions.Select( _ => Vector2.Zero ).ToList();
		foreach (var face in faceData)
		{
			newTexCoords[face.PositionIndex] = texCoords[face.TexCoordIndex];
		}
		texCoords = newTexCoords;


		ApplyImportTransform();

		if ( normals.Count < 1 )
		{
			RecalculateNormals();
		}

		var tangents = positions.Select( _ => Vector4.Zero ).ToList();
		var bitangents = positions.Select( _ => Vector3.Zero ).ToList();
		for ( int i = 0; i < faceData.Count; i += 3 )
		{
			RecalculateTangents( 
				new Vector3Int
				(
					x: faceData[i + 0].PositionIndex,
					y: faceData[i + 1].PositionIndex,
					z: faceData[i + 2].PositionIndex
				)
			);
		}

		for ( int i = 0; i < positions.Count; i++ )
		{
			OrthonormalizeTangent( i );
		}
		
		var indices = faceData.Select( faceVtx => faceVtx.PositionIndex ).ToList();

		for ( int i = 0; i < positions.Count; i++ )
		{
			var vtx = new Vertex()
			{
				Color = Color.White,
				TexCoord0 = new Vector4( new Vector3( texCoords[i], 0 ), 0 ),
				TexCoord1 = Vector4.Zero,
				Tangent = tangents[i],
				Normal = normals[i],
				Position = positions[i]
			};
			vertices.Add( vtx );
		}
		return new ObjFile( vertices, indices );

		void ApplyImportTransform()
		{
			var importTx = new Transform()
			{
				Position = Vector3.Zero, 
				Rotation = Rotation.FromYaw( 90f ) * Rotation.FromRoll( 90f ), 
				Scale = 39.3701f
			};
			for ( int i = 0; i < positions.Count; i++ )
			{
				positions[i] = importTx.PointToWorld( positions[i] );
			}

			// If we don't have any normals, we'll calculate them using the vertex positions, which were already transformed.
			if ( normals.Count < 1 )
				return;
			
			for ( int i = 0; i < normals.Count; i++ )
			{
				normals[i] = importTx.NormalToWorld( normals[i] );
			}
		}
		
		
		void RecalculateNormals()
		{
			normals.Clear();
			normals.AddRange( faceData.Select( _ => Vector3.Zero ) );

			for ( int i = 0; i < faceData.Count; i += 3 )
			{
				var f0 = faceData[i + 0];
				var f1 = faceData[i + 1];
				var f2 = faceData[i + 2];
				var i0 = f0.PositionIndex;
				var i1 = f1.PositionIndex;
				var i2 = f2.PositionIndex;
				var v0 = positions[i0];
				var v1 = positions[i1];
				var v2 = positions[i2];
				var triangle = new Triangle( v0, v1, v2 );
				var triNormal = triangle.Normal;
				normals[i0] += triNormal;
				normals[i1] += triNormal;
				normals[i2] += triNormal;
			}

			for ( int i = 0; i < normals.Count; i++ )
			{
				normals[i] = normals[i].Normal;
			}
		}
		
		void RecalculateTangents( Vector3Int triangle )
		{
			// Adapted from: https://www.opengl-tutorial.org/intermediate-tutorials/tutorial-13-normal-mapping/
			var v0 = positions[triangle.x];
			var v1 = positions[triangle.y];
			var v2 = positions[triangle.z];

			var uv0 = texCoords[triangle.x];
			var uv1 = texCoords[triangle.y];
			var uv2 = texCoords[triangle.z];

			var deltaPos1 = v1 - v0;
			var deltaPos2 = v2 - v0;

			var deltaUv1 = uv1 - uv0;
			var deltaUv2 = uv2 - uv0;
		
			var divisor = (deltaUv1.x * deltaUv2.y - deltaUv2.x * deltaUv1.y);
			if ( divisor == 0 )
				divisor = 0.01f;

			var r = 1.0f / divisor;
			var tangent = ( deltaPos1 * deltaUv2.y - deltaPos2 * deltaUv1.y ) * r;
			var bitangent = ( deltaPos2 * deltaUv1.x - deltaPos1 * deltaUv2.x ) * r;
		
			tangents[triangle.x] += new Vector4( tangent, 0 );
			tangents[triangle.y] += new Vector4( tangent, 0 );
			tangents[triangle.z] += new Vector4( tangent, 0 );
			bitangents[triangle.x] += bitangent;
			bitangents[triangle.y] += bitangent;
			bitangents[triangle.z] += bitangent;
			return;
		}
		
		void OrthonormalizeTangent( int index )
		{
			// Adapted from Listing 7.4 of Foundations of Game Engine Development Volume 2: Rendering

			var t = (Vector3)tangents[index];
			var b = bitangents[index];
			var n = normals[index];

			var xyz = Reject( t, n ).Normal;
			var w = MathF.Sign( t.Cross( b ).Dot( n ) );
			tangents[index] = new Vector4( xyz, w );
			return;
		
			Vector3 Reject( Vector3 v0, Vector3 v1 )
			{
				return v0 - v1 * (v0.Dot( v1 ) / v1.Dot( v1 ));
			}
		}

		RecordType GetLineType( string line, out string lineData )
		{
			lineData = line;
			var spaceIdx = line.IndexOf( (char)0x20 );
			// There was no space in this line.
			if ( spaceIdx < 0 )
				return RecordType.Unknown;

			var recordString = line[..spaceIdx];
			lineData = line[spaceIdx..].Trim();
			return recordString switch
			{
				LineVertexPosition => RecordType.Position,
				LineVertexNormal => RecordType.Normal,
				LineVertexTexCoord => RecordType.TexCoord,
				LineFace => RecordType.Face,
				_ => RecordType.Unknown
			};
		}

		Vector3 ParseVector3( string line )
		{
			var components = line.Split( (char)0x20 );
			Assert.True( components.Length == 3, "Vector3 must have 3 components" );
			return new Vector3(
				x: float.Parse( components[0] ),
				y: float.Parse( components[1] ),
				z: float.Parse( components[2] )
			);
		}

		Vector2 ParseVector2( string line )
		{
			var components = line.Split( (char)0x20 );
			Assert.True( components.Length >= 2, "Vector2 must have at least two components" );
			return new Vector2(
				x: float.Parse( components[0] ),
				y: float.Parse( components[1] )
			);
		}

		FaceVertex[] ParseFaceIndices( string line )
		{
			var faceStrings = line.Split( (char)0x20 );
			// If we find a quad, split it in to two triangles.
			if ( faceStrings.Length == 4 )
			{
				faceStrings = [faceStrings[0], faceStrings[1], faceStrings[2], faceStrings[0], faceStrings[2], faceStrings[3]];
			}
			Assert.True( faceStrings.Length % 3 == 0, "Number of face strings must be a multiple of 3" );
			var face = new FaceVertex[faceStrings.Length];
			for ( int i = 0; i < faceStrings.Length; i++ )
			{
				var fields = faceStrings[i].Split( '/' );
				FaceVertex faceIndices = new FaceVertex
				{
					PositionIndex = int.Parse( fields[0] ) - 1, 
					TexCoordIndex = int.Parse( fields[1] ) - 1
				};
				if ( fields.Length == 3 )
				{
					faceIndices.NormalIndex = int.Parse( fields[2] ) - 1;
				}
				else
				{
					faceIndices.NormalIndex = faceIndices.PositionIndex;
				}
				face[i] = faceIndices;
			}
			return face;
		}
	}
}
