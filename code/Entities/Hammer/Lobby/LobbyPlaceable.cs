using Editor;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TowerResort.Entities.CondoItems;

namespace TowerResort.Entities.Lobby;

[Library( "tr_lobby_object" ), HammerEntity]
[Title( "Lobby Object" ), Category( "Lobby" )]
[EditorModel( "models/editor/info_target.vmdl" )]
public class LobbyPlaceable : ModelEntity
{
	[Property, ResourceType( "citm" )]
	public static CondoAssetBase AssetToPlace { get; set; }

	public override void Spawn()
	{
		base.Spawn();

		var item = new CondoItemBase();
		item.SpawnFromAsset( AssetToPlace );
		item.Position = Position;
		item.Rotation = Rotation;
	}
}
