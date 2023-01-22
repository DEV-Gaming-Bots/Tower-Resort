using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TowerResort.Entities.Lobby;
using TowerResort.Player;

namespace TowerResort.GameComponents;

public class ArmWrestle : EntityComponent, ISingletonComponent
{
	public enum GameStatus
	{
		Idle,
		Starting,
		Active,
		Pause,
	}

	public GameStatus CurGameStatus { get; private set; }

	public LobbyPawn PlayerOne;
	public LobbyPawn PlayerTwo;

	TimeUntil timeUntilStart;
	const float timeToStart = 10.0f;

	protected override void OnActivate()
	{
		CurGameStatus = GameStatus.Idle;
		curWrestlePoints = 0;

		base.OnActivate();
	}

	//If either player one or two is the player, return true else false
	public bool HasPlayer(LobbyPawn joining)
	{
		if ( PlayerOne != null && joining == PlayerOne )
			return true;

		if ( PlayerTwo != null && joining == PlayerTwo )
			return true;

		return false;
	}

	//Player is joining the table
	public void JoinWrestleTable( LobbyPawn joiner )
	{
		joiner.FocusedEntity = Entity;

		(Entity as ArmWrestleTable).DisplayMessage( joiner, "You have joined the arm wrestle table" );

		if ( PlayerOne == null )
			PlayerOne = joiner;

		if ( PlayerTwo == null )
			PlayerTwo = joiner;

		joiner.FreezeMovement = MainPawn.FreezeEnum.Movement;
	}

	//Player has left the table
	public void LeaveWrestleTable( LobbyPawn leaver )
	{
		(Entity as ArmWrestleTable).DisplayMessage( leaver, "You have left the arm wrestle table" );

		leaver.FocusedEntity = null;

		if ( PlayerOne == leaver )
			PlayerOne = null;

		if(PlayerTwo == leaver)
			PlayerTwo = null;

		leaver.FreezeMovement = MainPawn.FreezeEnum.None;

		CurGameStatus = GameStatus.Idle;
	}

	public void KickPlayers()
	{
		PlayerOne.FocusedEntity = null;
		PlayerTwo.FocusedEntity = null;
	}

	int curWrestlePoints;

	public void Simulate(LobbyPawn player)
	{
		if( CurGameStatus == GameStatus.Active )
		{
			if(Input.Pressed(InputButton.PrimaryAttack))
			{
				if ( PlayerOne == player )
					curWrestlePoints++;
				else if ( PlayerTwo == player )
					curWrestlePoints--;

				if ( curWrestlePoints <= -15 )
					DeclareWinner( WinnerEnum.PlayerTwo );
				else if ( curWrestlePoints >= 15 )
					DeclareWinner( WinnerEnum.PlayerTwo);
			}
		}
	}

	public enum WinnerEnum
	{
		Draw,
		PlayerOne,
		PlayerTwo
	}

	public void DeclareWinner( WinnerEnum winner )
	{
		List<IClient> clients = new List<IClient>
		{
			PlayerOne.Client,
			PlayerTwo.Client
		};

		(Entity as ArmWrestleTable).DisplayMessage( clients, winner == WinnerEnum.PlayerOne ?
			$"{PlayerOne.Client.Name} won the arm wrestle!" : $"{PlayerTwo.Client.Name} has won the arm wrestle!");
	}

	const float dist = 64.0f;

	void DistanceCheck()
	{
		if ( PlayerOne != null && PlayerOne.Position.Distance( Entity.Position ) > dist )
			LeaveWrestleTable( PlayerOne );

		if ( PlayerTwo != null && PlayerTwo.Position.Distance( Entity.Position ) > dist )
			LeaveWrestleTable( PlayerTwo );
	}

	[Event.Tick.Server]
	public void TickGame()
	{
		DistanceCheck();

		if (CurGameStatus == GameStatus.Idle)
		{
			if( PlayerOne != null && PlayerTwo != null )
			{
				CurGameStatus = GameStatus.Starting;
				timeUntilStart = timeToStart;
			}
		}

		if ( CurGameStatus == GameStatus.Starting && timeUntilStart <= 0 )
		{
			CurGameStatus = GameStatus.Active;
			
			List<IClient> clients = new List<IClient>
			{
				PlayerOne.Client,
				PlayerTwo.Client
			};

			(Entity as ArmWrestleTable).DisplayMessage( clients, "Spam Primary Fire to wrestle!" );
		}
	}
}

