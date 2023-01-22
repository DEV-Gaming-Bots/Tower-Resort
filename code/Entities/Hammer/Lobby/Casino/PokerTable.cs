using System.Collections.Generic;
using Sandbox;
using Editor;
using TowerResort.Player;
using Components.NotificationManager;
using TowerResort.UI;
using TowerResort.GameComponents;

namespace TowerResort.Entities.Lobby;

[Library( "tr_casino_pokertable" ), HammerEntity]
[Title( "Poker Table" ), Category( "Lobby" )]
[EditorModel( "models/gamemodes/poker/table/big/big_pokertable.vmdl" )]
public partial class PokerTable : ModelEntity, IUse
{
	[Property, ResourceType( "vmdl" )]
	public string WorldModel { get; set; } = "";

	[Property, Description( "How much does it cost for players to join" )]
	[Net] public int EntryFee { get; set; } = 0;

	[Property, Description( "The high blinds, used for dealer's bid" )]
	public int HighBlind { get; set; } = 0;

	[Property, Description( "The low blinds, used for dealer's bid" )]
	public int LowBlind { get; set; } = 0;

	PokerGame gameComponent;
	PokerTablePanel panel;

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

	public bool IsUsable( Entity user )
	{
		return gameComponent.CurGameStatus != PokerGame.GameStatus.Active;
	}

	protected override void OnDestroy()
	{
		panel?.Delete();
		panel = null;

		base.OnDestroy();
	}

	public bool OnUse( Entity user )
	{
		if ( !IsUsable( user ) ) return false;

		if ( user is LobbyPawn player )
		{
			if ( gameComponent.Players.Contains( player ) )
				gameComponent.LeaveTable( player );
			else
				gameComponent.JoinTable( player );
		}

		return false;
	}

	public override void Spawn()
	{
		base.Spawn();

		if ( string.IsNullOrEmpty( WorldModel ) )
			SetModel( "models/gamemodes/poker/table/big/big_pokertable.vmdl" );
		else
			SetModel( WorldModel );

		SetupPhysicsFromModel(PhysicsMotionType.Keyframed);

		gameComponent = Components.Create<PokerGame>();

		gameComponent.EntryFee = EntryFee;

		if( HighBlind != 0 )
			gameComponent.HighBlinds = HighBlind;
		
		if( LowBlind != 0 )
			gameComponent.LowBlinds = LowBlind;
	}

	public override void ClientSpawn()
	{
		base.ClientSpawn();

		ClientSpawnAsync();
	}

	async void ClientSpawnAsync()
	{
		await Task.DelaySeconds( 1.0f );

		panel = new PokerTablePanel( EntryFee );
	}

	[Event.Client.Frame]
	public void FrameTick()
	{
		if ( panel == null ) return;

		panel.Position = Position + Vector3.Up * 64;
		panel.Rotation = Rotation.LookAt( Camera.Rotation.Backward, Vector3.Up );
	}
}
