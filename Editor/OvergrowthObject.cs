using System;
using System.IO;
using System.Text;
using System.Xml;

namespace Overgrowth;

public class OvergrowthObject
{
	public string AbsoluteFilePath { get; init; }
	public string RelativeFilePath { get; init; }
	public string ModelPath { get; init; }
	public string ColorMapPath { get; init; }
	public string NormalMapPath { get; init; }

	public static OvergrowthObject Load( string absFilePath, string relFilePath )
	{
		var xml = new XmlDocument();
		
		string modelPath = null;
		string colorMapPath = null;
		string normalMapPath = null;
		
		try
		{
			xml.Load( absFilePath );
			modelPath = xml.SelectSingleNode( "Object/Model" )?.InnerText;
			colorMapPath = xml.SelectSingleNode( "Object/ColorMap" )?.InnerText;
			normalMapPath = xml.SelectSingleNode( "Object/NormalMap" )?.InnerText;
		}
		catch ( Exception ex )
		{
			// Prefabs and decals are going to have problems. Ignore these problems.
		}

		return new OvergrowthObject()
		{
			AbsoluteFilePath = absFilePath,
			RelativeFilePath = relFilePath,
			ModelPath = modelPath,
			ColorMapPath = colorMapPath,
			NormalMapPath = normalMapPath
		};
	}
}
