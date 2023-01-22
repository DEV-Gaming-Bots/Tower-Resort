using Sandbox;
using Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TowerResort.Player;
using TowerResort.GameComponents;
using Components.NotificationManager;

namespace TowerResort.Entities.Lobby;

[Library( "tr_bar_armwrestle" ), HammerEntity]
[Title( "Arm Wrestle Table" ), Category( "Lobby" )]
//TEMPORARY MODEL
[EditorModel( "models/gamemodes/poker/table/small/small_pokertable.vmdl" )]
public partial class ArmWrestleTable : ModelEntity, IUse
{
	ArmWrestle ArmWrestle;

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/gamemodes/poker/table/small/small_pokertable.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
		ArmWrestle = Components.Create<ArmWrestle>();
	}

	public bool IsUsable( Entity user )
	{
		return ArmWrestle.CurGameStatus != ArmWrestle.GameStatus.Active;
	}

	public override void Simulate( IClient cl )
	{
		if ( Game.IsClient ) return;

		var pawn = cl.Pawn as LobbyPawn;
		if ( pawn == null ) return;

		ArmWrestle.Simulate( pawn );
	}

	public bool OnUse( Entity user )
	{
		if ( !IsUsable( user ) ) return false;

		var pawn = user as LobbyPawn;

		if ( ArmWrestle.HasPlayer( pawn ) )
			ArmWrestle.LeaveWrestleTable( pawn );
		else
			ArmWrestle.JoinWrestleTable( pawn );

		return false;
	}

	public void DisplayMessage( List<IClient> clients, string msg, float time = 5.0f )
	{
		ClientMessage( To.Multiple( clients ), msg, time );
	}

	public void DisplayMessage( LobbyPawn player, string msg, float time = 5.0f )
	{
		ClientMessage( To.Single( player ), msg, time );
	}

	[ClientRpc]
	public void ClientMessage( string message, float lifeTime )
	{
		BaseHud.Current.NotificationManager.AddNotification( message, NotificationType.Info, lifeTime );
	}

}
