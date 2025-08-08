using System;
using System.Threading.Tasks;
using Sandbox.Diagnostics;
using Sandbox.Mounting;

namespace Sandbox;

public class MountManager : Component
{
	[Property] public string MountIdent { get; set; } = "overgrowth";
	[Property] public int MaxResourceCount { get; set; } = 50;
	[Property] public bool MountOnStart { get; set; } = true;
	[Property] public int SpriteCount => Textures.Count;

	public bool IsLoading => _setUpMountTask is not null;
	
	public readonly Dictionary<string, Texture> Textures = [];
	private BaseGameMount _mount;
	private Task _setUpMountTask;

	protected override void OnStart()
	{
		if ( !MountOnStart )
			return;
		
		_setUpMountTask = SetUpMount();
	}

	[Button]
	public void Mount()
	{
		_setUpMountTask = SetUpMount();
	}

	public async Task SetUpMount()
	{
		TearDownMount();
		_mount = await Directory.Mount( MountIdent );

		int resourceCount = 0;
		var timer = FastTimer.StartNew();
		foreach ( var resource in _mount.Resources.Where( r => r.Type == ResourceType.Texture ) )
		{
			resourceCount++;
			if ( resourceCount > MaxResourceCount )
				break;
			
			var fileName = System.IO.Path.GetFileNameWithoutExtension( resource.Path );
			if ( string.IsNullOrWhiteSpace( fileName ) )
				continue;
			
			var extension = System.IO.Path.GetExtension( resource.Path );
			Log.Info( $"Loaded: {resource.Path}" );
			if ( extension == ".vtex" && await resource.GetOrCreate() is Texture tex )
			{
				Textures[fileName] = tex;
			}
		}

		var elapsed = timer.ElapsedMilliSeconds;
		Log.Info( $"Loaded {Textures.Count} textures in {elapsed:F3}ms" );

		_setUpMountTask = null;
	}

	private void TearDownMount()
	{
		Textures.Clear();
	}
}
