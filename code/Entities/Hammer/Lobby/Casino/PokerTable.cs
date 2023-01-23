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

	public List<PokerChair> PokerChairs;

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
		return gameComponent.CurGameStatus == PokerGame.GameStatus.Idle
				|| gameComponent.CurGameStatus == PokerGame.GameStatus.Starting;
	}

	protected override void OnDestroy()
	{
		panel?.Delete();
		panel = null;

		if( PokerChairs != null )
		{
			foreach ( var chair in PokerChairs.ToArray() )
				chair.Delete();
		}

		base.OnDestroy();
	}

	public bool OnUse( Entity user )
	{
		if ( !IsUsable( user ) ) return false;

		if ( Game.IsClient ) return false;

		if ( user is LobbyPawn player )
		{
			if ( gameComponent.Players.Contains( player ) )
			{
				gameComponent.LeaveTable( player );
			}
			else
			{
				if ( !gameComponent.CanJoinTable( player ) ) return false;

				gameComponent.JoinTable( player );
			}
		}

		return false;
	}

	int yPos = 32;

	public PokerChair CreateChair(int index)
	{
		PokerChair chair = new PokerChair();
		chair.SetParent( this );

		switch (index)
		{
			case 0:
				chair.LocalPosition = new Vector3(-28, 62, 0);
				chair.LocalRotation = Rotation.FromYaw( -45 );
				break;

			case 1:
				chair.LocalPosition = new Vector3( -42, yPos, 0 );
				break;

			case 2:
				chair.LocalPosition = new Vector3( -42, yPos - 32, 0 );
				break;

			case 3:
				chair.LocalPosition = new Vector3( -42, yPos - 64, 0 );
				break;

			case 4:
				chair.LocalPosition = new Vector3( -28, yPos - 92, 0 );
				chair.LocalRotation = Rotation.FromYaw( 45 );
				break;
		}

		return chair;
	}

	public override void Spawn()
	{
		base.Spawn();

		if ( string.IsNullOrEmpty( WorldModel ) )
			SetModel( "models/gamemodes/poker/table/big/big_pokertable.vmdl" );
		else
			SetModel( WorldModel );

		SetupPhysicsFromModel(PhysicsMotionType.Keyframed);

		PokerChairs = new List<PokerChair>();

		for ( int i = 0; i < 5; i++ )
			PokerChairs.Add( CreateChair( i ) );

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

public class PokerChair : ModelEntity
{
	public LobbyPawn Sitter { get; private set; }

	public Vector3 OrgPos;

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/furniture/bar_stool/bar_stool.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
	}

	public void Sitdown( Entity user )
{
		var player = user as LobbyPawn;

		if ( player == null )
			return;

		OrgPos = player.Position;

		if ( Input.Down( InputButton.Duck ) )
			Input.SuppressButton( InputButton.Duck );

		Sitter = player;

		Sitter?.SetAnimParameter( "sit", 1 );

		player.Position = Position + Vector3.Up * 10;
		player.Rotation = Rotation;
		player.SetViewAngles( Rotation.Angles() );
		player.IsSitting = true;
	}

	public void RemoveSittingPlayer( LobbyPawn player )
	{
		if ( !Game.IsServer )
			return;

		player.SetAnimParameter( "sit", 0 );

		Sitter = null;

		if ( !player.IsValid() )
			return;

		player.FreezeMovement = MainPawn.FreezeEnum.None;
		player.IsSitting = false;
		player.ResetCamera();
		player.Rotation = Rotation.Identity;

		if ( Rotation.Roll() < 0.0f )
			player.Position += Rotation.Up * 24;
		else
			player.Position += player.Rotation.Up * 20;

	}

	[Event.Tick.Server]
	protected void SitTick()
	{
		if ( Sitter is LobbyPawn player && player.LifeState != LifeState.Alive )
		{
			RemoveSittingPlayer( player );
		}
	}
}
