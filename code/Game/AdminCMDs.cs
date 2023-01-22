﻿using Sandbox;
using System;
using System.Linq;
using TowerResort.Entities.Base;
using TowerResort.Entities.CondoItems;
using TowerResort.Player;
using TowerResort.GameComponents;

namespace TowerResort;

public partial class TRGame
{
	//Hardcoded way of doing an admin check
	//We should figure out a better way of doing this
	public static long[] AdminIDs => new long[]
	{
		//ItsRifter
		76561197972285500,
		//AlexVeeBee
		76561198294665561,
		//Xenthio
		76561198231234036,
		//Baik
		76561197991940798,
		//trende2001
		76561198043979097,
	};

	[ConCmd.Server( "tr.dueling.admin.stop" )]
	public static void duelingForceStop()
	{
		if ( !AdminIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		Instance.ResetDuelToIdle();

	}
	[ConCmd.Server( "tr.entity.spawn.condo" )]
	public static void SpawnCondoItemCMD( string entName )
	{
		if ( !AdminIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		var player = ConsoleSystem.Caller.Pawn as MainPawn;
		if ( player == null ) return;

		if ( ResourceLibrary.TryGet( $"assets/condo/{entName}.citm", out CondoAssetBase asset ) )
		{
			var model = new CondoItemBase();
			model.SpawnFromAsset( asset );
			model.Position = player.GetEyeTrace( 999.0f, 0.1f ).EndPosition;
			model.Rotation = Rotation.LookAt( player.EyeRotation.Backward.WithZ( 0 ), Vector3.Up );
		}
	}

	[ConCmd.Server( "tr.entity.spawn.lobby" )]
	public static void SpawnLobbyItemCMD( string entName )
	{
		if ( !AdminIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		var player = ConsoleSystem.Caller.Pawn as MainPawn;
		if ( player == null ) return;

		var ent = CreateByName( entName );
		if ( ent == null ) return;

		ent.Position = player.GetEyeTrace( 999.0f, 0.1f ).EndPosition;
		ent.Rotation = Rotation.LookAt( player.EyeRotation.Backward.WithZ( 0 ), Vector3.Up );
	}

	[ConCmd.Server( "tr.weapon.spawn" )]
	public static void SpawnWeapon( string wepName, bool inInv = false )
	{
		var player = ConsoleSystem.Caller.Pawn as MainPawn;
		if ( player == null ) return;

		var wep = TypeLibrary.Create<WeaponBase>( wepName );
		if ( wep == null ) return;

		if ( inInv )
			player.Inventory.AddWeapon( wep, true );
		else
			wep.Position = player.GetEyeTrace( 999.0f ).EndPosition;
	}

	[ConCmd.Server( "tr.entity.delete" )]
	public static void DeleteEntityCMD()
	{
		if ( !AdminIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		var player = ConsoleSystem.Caller.Pawn as MainPawn;
		if ( player == null ) return;

		var ent = player.GetEyeTrace( 999.0f ).Entity;

		if ( ent is MainPawn || ent is WorldEntity ) return;

		ent.Delete();
	}

	[ConCmd.Server( "tr.player.give.credits" )]
	public static void GiveCreditsCMD( int amt, string targetName = "" )
	{
		if ( !AdminIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		var player = ConsoleSystem.Caller.Pawn as MainPawn;
		if ( player == null ) return;

		targetName = targetName.ToLower();

		if ( !string.IsNullOrEmpty( targetName ) )
		{
			int conflicts = Game.Clients.Where( x => x.Name.ToLower().Contains( targetName ) ).Count();

			if ( conflicts > 1 )
			{
				Log.Error( "There are multiple players with that target name, be more specific" );
				return;
			}

			var target = All.OfType<MainPawn>().Where( x => x.Client.Name.ToLower().Contains( targetName ) ).FirstOrDefault();

			if ( target != null )
				target.AddCredits( amt );
		}
		else
		{
			player.AddCredits( amt );
		}
	}

	[ConCmd.Server( "tr.game.poker.setcard" )]
	public static void SetCards( int card, int number, int suit )
	{
		if ( !AdminIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		var player = ConsoleSystem.Caller.Pawn as LobbyPawn;
		if ( player == null ) return;

		if ( player.CurPokerTable == null ) return;

		if ( suit > 3 || (number < 0 && number > 12) ) return;

		if ( card == 1 )
		{
			player.CardOne.Number = number;
			player.CardOne.Suit = (PokerCard.SuitEnum)Enum.ToObject( typeof( PokerCard.SuitEnum ), suit );
		}
		else if ( card == 2 )
		{
			player.CardTwo.Number = number;
			player.CardTwo.Suit = (PokerCard.SuitEnum)Enum.ToObject( typeof( PokerCard.SuitEnum ), suit );
		}

		player.UpdateCardHandUI( player.CardOne.Number, player.CardOne.Suit, player.CardTwo.Number, player.CardTwo.Suit );
	}

	[ConCmd.Server( "tr.game.reset" )]
	public static void ResetGame()
	{
		if ( !AdminIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		var ents = All.ToList();
		ents.RemoveAll( e => e is IClient );
		ents.RemoveAll( e => e is WorldEntity );
		foreach ( Entity ent in ents )
		{
			ent.Delete();
		}

		// Reset the map
		Game.ResetMap( Entity.All.Where( x => x is LobbyPawn || x is MainPawn ).ToArray() );

		// Tell our new game that all clients have just left.
		foreach ( IClient cl in Game.Clients )
		{
			cl.Components.RemoveAll();
			(TRGame.Current as TRGame).ClientDisconnect( cl, NetworkDisconnectionReason.DISCONNECT_BY_USER );
		}

		// Create a brand new game
		TRGame.Current = new TRGame();

		// Tell our new game that all clients have just joined to set them all back up.
		foreach ( IClient cl in Game.Clients )
		{
			cl.Components.RemoveAll();
			(TRGame.Current as TRGame).ClientJoined( cl );
		}
	}

	[ConCmd.Server( "tr.player.pawn.set" )]
	public static void SetPawnCMD( int type, string targetName = "" )
	{
		if ( !AdminIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		var player = ConsoleSystem.Caller.Pawn as MainPawn;
		if ( player == null ) return;

		MainPawn newPawn = null;

		switch ( type )
		{
			case 1: newPawn = new LobbyPawn(); break;
			case 2: newPawn = new BallPawn(); break;
		}

		if ( newPawn == null ) return;

		if ( !string.IsNullOrEmpty( targetName ) )
		{
			int conflicts = Game.Clients.Where( x => x.Name.ToLower().Contains( targetName ) ).Count();

			if ( conflicts > 1 )
			{
				Log.Error( "There are multiple players with that target name, be more specific" );
				return;
			}

			var target = All.OfType<MainPawn>().Where( x => x.Client.Name.ToLower().Contains( targetName ) ).FirstOrDefault();

			if ( target != null )
			{
				var oldPawn = target.Client.Pawn;
				target.Client.Pawn = newPawn;
				newPawn.Spawn();

				if ( newPawn is LobbyPawn )
					newPawn.SetUpPlayerStats();

				oldPawn.Delete();
			}
		}
		else
		{
			var oldPawn = player.Client.Pawn;
			player.Client.Pawn = newPawn;
			newPawn.Spawn();

			if ( newPawn is LobbyPawn )
				newPawn.SetUpPlayerStats();

			oldPawn.Delete();
		}
	}

	[ConCmd.Server( "tr.data.connect" )]
	public static void DataTest()
	{
		if ( !AdminIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		_ = Instance.StartSocket();
	}

	[ConCmd.Server( "tr.data.reset" )]
	public static void DataCheck()
	{
		if ( !AdminIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		Instance.DataSocket.Dispose();
	}

	[ConCmd.Server( "tr.data.save" )]
	public static void DataSaveCMD()
	{
		if ( !AdminIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		Instance.DoSave(ConsoleSystem.Caller);
	}

	[ConCmd.Admin( "tr.poker.admin.start" )]
	public static void PokerAdminStart()
	{
		if ( !AdminIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		var player = ConsoleSystem.Caller.Pawn as LobbyPawn;
		if ( player == null ) return;

		if ( player.CurPokerTable == null ) return;

		var c = player.CurPokerTable.Components.Get<PokerGame>();

		c.SetUpPlayers();
		c.UpdateState();
	}


	[ConCmd.Admin( "tr.poker.admin.update" )]
	public static void PokerAdminNextState()
	{
		if ( !AdminIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		var player = ConsoleSystem.Caller.Pawn as LobbyPawn;
		if ( player == null ) return;

		if ( player.CurPokerTable == null ) return;

		var c = player.CurPokerTable.Components.Get<PokerGame>();

		c.UpdateState();
	}
}
