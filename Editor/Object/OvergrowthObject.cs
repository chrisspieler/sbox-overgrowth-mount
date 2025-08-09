using System;
using System.IO;
using System.Text;
using System.Xml;
using Duccsoft.Mounting;

namespace Overgrowth;

public class OvergrowthObject
{
	public MountAssetPath XmlPath { get; init; }
	public string ModelPath { get; init; }
	public string ColorMapPath { get; init; }
	public string NormalMapPath { get; init; }

	public static OvergrowthObject Load( MountAssetPath path )
	{
		var xml = new XmlDocument();
		
		string modelPath = null;
		string colorMapPath = null;
		string normalMapPath = null;
		
		try
		{
			xml.Load( path.Absolute );
			modelPath = xml.SelectSingleNode( "Object/Model" )?.InnerText?.ToLowerInvariant();
			colorMapPath = xml.SelectSingleNode( "Object/ColorMap" )?.InnerText?.ToLowerInvariant();;
			normalMapPath = xml.SelectSingleNode( "Object/NormalMap" )?.InnerText?.ToLowerInvariant();;
		}
		catch ( Exception ex )
		{
			// Prefabs and decals are going to have problems. Ignore these problems.
		}
		
		return new OvergrowthObject()
		{
			XmlPath = path,
			ModelPath = modelPath,
			ColorMapPath = colorMapPath,
			NormalMapPath = normalMapPath
		};
	}
}
