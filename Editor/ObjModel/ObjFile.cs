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
	
	private struct FaceIndex
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

		var vData = new List<Vector3>();
		var vtData = new List<Vector2>();
		var vnData = new List<Vector3>();
		var fData = new List<FaceIndex>();

		var highestPosIndex = 0;
		var highestUvIndex = 0;
		var highestNormIndex = 0;

		foreach ( var line in lines )
		{
			switch ( GetLineType( line, out var lineData ) )
			{
				case RecordType.Position:
					vData.Add( ParseVector3( lineData ) );
					break;
				case RecordType.TexCoord:
					var texCoord = ParseVector2( lineData );
					texCoord = new Vector2( texCoord.x, 1.0f - texCoord.y );
					vtData.Add( texCoord );
					break;
				case RecordType.Normal:
					vnData.Add( ParseVector3( lineData ) );
					break;
				case RecordType.Face:
					var face = ParseFaceIndices( lineData );
					foreach ( var vtx in face )
					{
						highestPosIndex = Math.Max( vtx.PositionIndex, highestPosIndex );
						highestUvIndex = Math.Max( vtx.TexCoordIndex, highestUvIndex );
						highestNormIndex = Math.Max( vtx.NormalIndex, highestNormIndex );
					}
					fData.AddRange( face );
					break;
				default:
					continue;
			}
		}
		
		Log.Info( $"High v: {highestPosIndex}, v: {vData.Count}, high vt: {highestUvIndex}, vt: {vtData.Count}, high n: {highestNormIndex}, n: {vnData.Count}, f: {fData.Count}" );

		// Before we do anything to any vertices and normals we may have obtained, convert them to s&box coordinates.
		ApplyImportTransform();
		
		var vertices = new Vertex[highestPosIndex + 1];
		var indices = new int[fData.Count];

		for ( int i = 0; i < indices.Length; i += 3 )
		{
			var v0 = fData[i + 0];
			var v1 = fData[i + 1];
			var v2 = fData[i + 2];
			
			indices[i + 0] = v0.PositionIndex;
			indices[i + 1] = v1.PositionIndex;
			indices[i + 2] = v2.PositionIndex;

			for ( int j = i; j < i + 3; j++ )
			{
				var triData = fData[j];
				var posIdx = triData.PositionIndex;
				var texIdx = triData.TexCoordIndex;
				var normIdx = triData.NormalIndex;
				
				var position = vData[posIdx];
				var texCoord0 = vtData[texIdx];
				var normal = Vector3.Zero;
				if ( vnData.Count > 0 )
					normal = vnData[normIdx];

				vertices[posIdx] = new Vertex()
				{
					Position = position,
					Color = Color.White,
					TexCoord0 = new Vector4( texCoord0.x, texCoord0.y, 0, 0 ),
					TexCoord1 = new Vector4( 0, 0, 0, 0 ),
					Normal = normal
				};
			}
		}
		
		// If we didn't parse any normals, calculate them now.
		if ( vnData.Count < 1 )
		{
			RecalculateNormals();
		}
		
		var bitangents = new Vector3[vertices.Length];
		
		// Figure out what the tangents should be.
		for ( int i = 0; i < indices.Length; i += 3 )
		{
			RecalculateTangents( 
				new Vector3Int
				(
					x: indices[i + 0],
					y: indices[i + 1],
					z: indices[i + 2]
				)
			);
		}

		// Make the TBN basis vectors orthogonal and of length one.
		for ( int i = 0; i < vertices.Length; i++ )
		{
			OrthonormalizeTangent( i );
		}
		return new ObjFile( vertices.ToList(), indices.ToList() );

		void ApplyImportTransform()
		{
			var importTx = new Transform()
			{
				Position = Vector3.Zero, 
				Rotation = Rotation.FromYaw( 90f ) * Rotation.FromRoll( 90f ), 
				Scale = 39.3701f
			};
			for ( int i = 0; i < vData.Count; i++ )
			{
				vData[i] = importTx.PointToWorld( vData[i] );
			}

			// If we don't have any normals, we'll calculate them using the vertex positions, which were already transformed.
			if ( vnData.Count < 1 )
				return;
			
			for ( int i = 0; i < vnData.Count; i++ )
			{
				vnData[i] = importTx.NormalToWorld( vnData[i] );
			}
		}
		
		void RecalculateNormals()
		{
			for ( int i = 0; i < indices.Length; i += 3 )
			{
				var i0 = indices[i + 0];
				var i1 = indices[i + 1];
				var i2 = indices[i + 2];
				
				var v0 = vertices[i0];
				var v1 = vertices[i1];
				var v2 = vertices[i2];
				
				var triangle = new Triangle( v0.Position, v1.Position, v2.Position );
				var triNormal = triangle.Normal;
				v0.Normal += triNormal;
				v1.Normal += triNormal;
				v2.Normal += triNormal;
				
				vertices[i0] = v0;
				vertices[i1] = v0;
				vertices[i2] = v0;
			}

			for ( int i = 0; i < vertices.Length; i++ )
			{
				vertices[i] = vertices[i] with { Normal = vertices[i].Normal.Normal };
			}
		}
		
		void RecalculateTangents( Vector3Int triangle )
		{
			// Adapted from: https://www.opengl-tutorial.org/intermediate-tutorials/tutorial-13-normal-mapping/
			var v0 = vertices[triangle.x];
			var v1 = vertices[triangle.y];
			var v2 = vertices[triangle.z];

			var p0 = v0.Position;
			var p1 = v1.Position;
			var p2 = v2.Position;

			var uv0 = new Vector2( v0.TexCoord0.x, v0.TexCoord0.y );
			var uv1 = new Vector2( v1.TexCoord0.x, v1.TexCoord0.y );
			var uv2 = new Vector2( v2.TexCoord0.x, v2.TexCoord0.y );

			var deltaPos1 = p1 - p0;
			var deltaPos2 = p2 - p0;

			var deltaUv1 = uv1 - uv0;
			var deltaUv2 = uv2 - uv0;
		
			var divisor = (deltaUv1.x * deltaUv2.y - deltaUv2.x * deltaUv1.y);
			if ( divisor == 0 )
				divisor = 0.01f;

			var r = 1.0f / divisor;
			var tangent = ( deltaPos1 * deltaUv2.y - deltaPos2 * deltaUv1.y ) * r;
			var bitangent = ( deltaPos2 * deltaUv1.x - deltaPos1 * deltaUv2.x ) * r;
		
			v0.Tangent += new Vector4( tangent, 0 );
			v1.Tangent += new Vector4( tangent, 0 );
			v2.Tangent += new Vector4( tangent, 0 );
			vertices[triangle.x] = v0;
			vertices[triangle.y] = v1;
			vertices[triangle.z] = v2;
			bitangents[triangle.x] += bitangent;
			bitangents[triangle.y] += bitangent;
			bitangents[triangle.z] += bitangent;
		}
		
		void OrthonormalizeTangent( int index )
		{
			// Adapted from Listing 7.4 of Foundations of Game Engine Development Volume 2: Rendering

			var vtx = vertices[index];
			
			var t = (Vector3)vtx.Tangent;
			var b = bitangents[index];
			var n = vtx.Normal;

			var xyz = Reject( t, n ).Normal;
			var w = MathF.Sign( t.Cross( b ).Dot( n ) );
			vtx.Tangent = new Vector4( xyz, w );
			vertices[index] = vtx;
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
			lineData = line[spaceIdx..].Trim( (char)0x20 );
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
				x: (float)double.Parse( components[0] ),
				y: (float)double.Parse( components[1] )
			);
		}

		FaceIndex[] ParseFaceIndices( string line )
		{
			var faceStrings = line.Split( (char)0x20 );
			// If we find a quad, split it in to two triangles.
			if ( faceStrings.Length == 4 )
			{
				faceStrings = [faceStrings[0], faceStrings[1], faceStrings[2], faceStrings[0], faceStrings[2], faceStrings[3]];
			}
			Assert.True( faceStrings.Length % 3 == 0, "Number of face strings must be a multiple of 3" );
			var idx = new FaceIndex[faceStrings.Length];
			for ( int i = 0; i < faceStrings.Length; i++ )
			{
				var fields = faceStrings[i].Split( '/' );
				FaceIndex faceIndices = new FaceIndex
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
				idx[i] = faceIndices;
			}
			return idx;
		}
	}
}
