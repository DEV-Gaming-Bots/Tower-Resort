using Sandbox;
using Sandbox.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TowerResort.Entities.Base;
using TowerResort.Player;
using TowerResort.UI;
using TowerResort.Weapons;

namespace TowerResort;

public partial class TRGame
{
	public enum DuelEnum
	{
		Idle,
		Pre,
		Active,
		Post
	};

	public DuelEnum DuelStatus { get; set; }

	public static DuelEnum StaticDuelStatus => Instance?.DuelStatus ?? DuelEnum.Idle;

	MainPawn DuellerOne;
	MainPawn DuellerTwo;

	[Net]
	public RealTimeUntil DuelTimer { get; set; } = 0f;

	[Event.Tick.Server]
	public void DuelServerTick()
	{
		if ( DuelStatus == DuelEnum.Idle )
			return;

		if ( DuelStatus == DuelEnum.Active && (DuellerOne.LifeState == LifeState.Dead || DuellerTwo.LifeState == LifeState.Dead) )
			EndDuel();

		if ( DuelTimer <= 0.0f )
		{
			switch(DuelStatus)
			{
				case DuelEnum.Pre:
					StartDuel();
					break;
				case DuelEnum.Active:
					EndDuel();
					break;
				case DuelEnum.Post:
					ResetDuelToIdle();
					break;
			}
		}
	}

	public void SetUpdueling()
	{
		DuelStatus = DuelEnum.Idle;
	}

	public void InitiateDuel(MainPawn plOne, MainPawn plTwo)
	{
		DuellerOne = plOne;
		DuellerTwo = plTwo;

		DuellerOne.FreezeMovement = MainPawn.FreezeEnum.Movement;
		DuellerTwo.FreezeMovement = MainPawn.FreezeEnum.Movement;

		PlaySoundOnClient( To.Single( DuellerOne ), "dueling_wager" );
		PlaySoundOnClient( To.Single( DuellerTwo ), "dueling_wager" );

		DuelTimer = 8.0f;
		DuelStatus = DuelEnum.Pre;
	}

	public void StartDuel()
	{
		DuellerOne.Inventory.AddItem( new Pistol(), true );
		DuellerTwo.Inventory.AddItem( new Pistol(), true );

		DuellerOne.FreezeMovement = MainPawn.FreezeEnum.None;
		DuellerTwo.FreezeMovement = MainPawn.FreezeEnum.None;

		DuelTimer = 120.0f;
		DuelStatus = DuelEnum.Active;
	}

	public void EndDuel()
	{
		DuellerOne.Inventory.RemoveItem(DuellerTwo.ActiveChild as WeaponBase );
		DuellerTwo.Inventory.RemoveItem( DuellerTwo.ActiveChild as WeaponBase );

		DuellerOne.FreezeMovement = MainPawn.FreezeEnum.Movement;
		DuellerTwo.FreezeMovement = MainPawn.FreezeEnum.Movement;

		if ( DuellerOne.LifeState == LifeState.Dead )
		{
			PlaySoundOnClient( To.Single( DuellerOne ), "dueling_lose" );
			PlaySoundOnClient( To.Single( DuellerTwo ), "dueling_win" );

			Log.Info( $"{DuellerTwo.Client.Name} won" );
		} 
		else if ( DuellerTwo.LifeState == LifeState.Dead )
		{
			PlaySoundOnClient( To.Single( DuellerOne ), "dueling_win" );
			PlaySoundOnClient( To.Single( DuellerTwo ), "dueling_lose" );

			Log.Info( $"{DuellerOne.Client.Name} won" );
		} 
		else
		{
			Log.Info( "Its a draw" );
		}

		DuelTimer = 5.0f;
		DuelStatus = DuelEnum.Post;
	}

	public void ResetDuelToIdle()
	{
		DuellerOne?.Respawn();
		DuellerTwo?.Respawn();

		DuellerOne.FreezeMovement = MainPawn.FreezeEnum.None;
		DuellerTwo.FreezeMovement = MainPawn.FreezeEnum.None;

		DuellerOne = null;
		DuellerTwo = null;

		DuelStatus = DuelEnum.Idle;
	}

	[ConCmd.Server("tr.dueling.duel")]
	public static void SendDuel(string targetName)
	{
		var player = ConsoleSystem.Caller.Pawn as MainPawn;
		if ( player == null ) return;

		targetName = targetName.ToLower();

		var target = Game.Clients.FirstOrDefault( x => x.Name.ToLower().Contains(targetName) ).Pawn as MainPawn;
		if ( target == null ) return;

		target.DuelOpponent = player;
		player.DuelOpponent = target;

		TRChat.AddChatEntryStatic( To.Single( target ), "DUEL", $"{player.Client.Name} has challenged you to a duel" );
	}

	[ConCmd.Server("tr.dueling.respond")]
	public static void RespondToDuel(bool accepted)
	{
		var player = ConsoleSystem.Caller.Pawn as MainPawn;
		if ( player == null ) return;

		if ( player.DuelOpponent == null ) return;

		if ( accepted )
		{
			Instance.InitiateDuel( player.DuelOpponent, player );
			TRChat.AddChatEntryStatic( To.Single( player.DuelOpponent ), "DUEL", $"{player.Client.Name} has accepted your duel" );
		}
		else
		{
			player.DuelOpponent.DuelOpponent = null;
			player.DuelOpponent = null;

			TRChat.AddChatEntryStatic( To.Single( player.DuelOpponent ), "DUEL", $"{player.Client.Name} has denied your duel" );
		}

	}
}

