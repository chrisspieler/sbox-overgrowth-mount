using System;
using System.IO;
using System.Runtime.InteropServices;
using Overgrowth.Extensions;
using Sandbox.Diagnostics;

namespace S3TC;

public class DdsFile
{
	public bool IsDxt10 => Dxt10Header is not null;
	public bool IsKnownFormat => (int)Format >= 0;
	
	public DDS_HEADER Header { get; init; }
	public DDS_HEADER_DXT10? Dxt10Header { get; init; }
	public byte[] Data { get; init; }
	public ImageFormat Format { get; init; }
	public int Width { get; init; }
	public int Height { get; init; }
	public int Pitch { get; init; }
	public int CompressedSize { get; init; }

	public DdsFile( DDS_HEADER header, DDS_HEADER_DXT10? dxt10, byte[] data )
	{
		Header = header;
		Dxt10Header = dxt10;
		Data = data;
		Width = header.dwWidth;
		Height = header.dwHeight;
		Format = GetImageFormat();
		Pitch = ComputePitch();
		CompressedSize = CalculateCompressedSize();
		if ( Format == ImageFormat.None )
		{
			Log.Info( $"Unrecognized FourCC:  0x{Header.ddspf.dwFourCc:X8}" );
		}
	}

	public static DdsFile Load( string filePath ) => Load( File.ReadAllBytes( filePath ) );
	public static DdsFile Load(byte[] bytes )
	{
		using var reader = new BinaryReader( new MemoryStream( bytes ) );
		
		var magic = reader.ReadInt32();
		Assert.AreEqual( magic, DdsMagic.HeaderMagic, $"Invalid DDS magic: 0x{magic:X8}");
		var header = reader.Read<DDS_HEADER>();
		
		DDS_HEADER_DXT10? dxt10 = null;
		if ( header.ddspf.dwFourCc == DdsMagic.FourCcDx10 )
		{
			dxt10 = reader.Read<DDS_HEADER_DXT10>();
		}

		reader.BaseStream.Position = dxt10 is null ? 128 : 148;
		var data = reader.ReadRemaining();
		return new DdsFile( header, dxt10, data );
	}

	private ImageFormat GetImageFormat()
	{
		if ( IsDxt10 )
			return ImageFormat.None;

		switch ( Header.ddspf.dwFourCc )
		{
			// TODO: Handle DXT1 with alpha?
			case DdsMagic.FourCcDxt1:
				return ImageFormat.DXT1;
			case DdsMagic.FourCcDxt3:
				return ImageFormat.DXT3;
			case DdsMagic.FourCcDxt5:
				return ImageFormat.DXT5;
			// TODO: Support DXT10
			case DdsMagic.FourCcDx10:
			default:
				return ImageFormat.None;
		}
	}

	private int ComputePitch()
	{
		if ( !IsKnownFormat )
			return 0;

		var blockSize = Format == ImageFormat.DXT1 ? 8 : 16;
		return Math.Max( 1, (Width + 3) / 4 ) * blockSize;
	}

	private int CalculateCompressedSize()
	{
		if ( !IsKnownFormat )
			return 0;
		
		var totalSize = 0;
		var mipCount = Math.Max( 1, Header.dwMipMapCount );
		for ( int mipLevel = 0; mipLevel < mipCount; mipLevel++ )
		{
			totalSize += GetCompressedDataSize( mipLevel );
		}
		return totalSize;
	}

	public int GetCompressedDataSize( int mipLevel )
	{
		var blockSize = Format == ImageFormat.DXT1 ? 8 : 16;
		var size = GetMipLevelSize( mipLevel );
		return Math.Max( 1, (size.x + 3) / 4 ) * Math.Max( 1, (size.y + 3) / 4 ) * blockSize;
	}

	public Vector2Int GetMipLevelSize( int mipLevel )
	{
		return new Vector2Int(
			x: Width / (1 << mipLevel),
			y: Height / (1 << mipLevel) 
		);
	}

	public Span<byte> GetCompressedData( int mipLevel )
	{
		var startIdx = 0;
		for ( int i = 0; i < mipLevel; i++ )
		{
			startIdx += GetCompressedDataSize( i );
		}

		var endIdx = startIdx + GetCompressedDataSize( mipLevel );
		return Data[startIdx..endIdx];
	}
}
