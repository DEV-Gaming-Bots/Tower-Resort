using Components.NotificationManager;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using TowerResort.Achievements;
using TowerResort.Entities.CondoItems;
using TowerResort.Entities.Lobby;
using TowerResort.Player;

namespace TowerResort.GameComponents;

public partial class PokerGame : EntityComponent, ISingletonComponent
{
	public enum GameStatus
	{
		Idle,
		Starting,
		Active,
		Pause,
	}

	public enum PokerStatus
	{
		Idle,
		PreFlop,
		Flop,
		Turn,
		River,
		Reveal
	}
	public GameStatus CurGameStatus { get; private set; }
	PokerStatus PokerStage;

	public int EntryFee = 0;

	public int HighBlinds = 50;
	public int LowBlinds = 25;

	public int PrizePot;

	public int ChipPot;

	[Net] public int BetPot { get; set; }

	public int StartingChips = 500;

	int curTurnIndex;
	int round;

	[Net] public IList<LobbyPawn> Players { get; set; }
	public List<PokerCard> DeckCards;

	const double TimeBeforeFold = 60.0;

	TimeSince TimeLastAction;

	protected override void OnActivate()
	{
		Players = new List<LobbyPawn>();

		CurGameStatus = GameStatus.Idle;
		PokerStage = PokerStatus.Idle;

		PrizePot = 0;

		DeckCards = new List<PokerCard>();
		CurrentDeck = StaticDeck.OrderBy( x => Game.Random.Int( 10000 ) ).ToList();

		round = 0;
		ChipPot = 0;
		curTurnIndex = 0;

		base.OnActivate();
	}

	protected override void OnDeactivate()
	{
		RemovePlayers();

		base.OnDeactivate();
	}

	public void RemovePlayers()
	{
		foreach ( var player in Players.ToArray() )
		{
			LeaveTable( player );
		}
	}
		public void DisplayMessage( LobbyPawn player, string message, float time = 5.0f )
	{
		if ( Entity is CondoItemBase condo )
			condo.DisplayMessage( player, message, time );
		else if ( Entity is PokerTable pokerTable )
			pokerTable.DisplayMessage( player, message, time );
	}

	public void DisplayMessage( List<IClient> clients, string message, float time = 5.0f )
	{
		if ( Entity is CondoItemBase condo )
			condo.DisplayMessage( clients, message, time );
		else if ( Entity is PokerTable pokerTable )
			pokerTable.DisplayMessage( clients, message, time );
	}

	public void DisplayToPlayers( string msg, float time )
	{
		foreach ( LobbyPawn player in Players )
			DisplayMessage( player, msg, time );
	}

	public bool CanJoinTable(LobbyPawn joiner)
	{
		if ( Game.IsClient ) return false;

		if ( Players.Count() >= 5 ) return false;

		if ( joiner is not LobbyPawn ) return false;

		if ( CurGameStatus != GameStatus.Idle )
		{
			DisplayMessage( joiner, "This table is currently playing", 3.0f );
			return false;
		}

		if ( EntryFee > 0 && joiner.GetCredits() < EntryFee )
		{
			DisplayMessage( joiner, "You have insufficient credits to play" );
			return false;
		}

		return true;
	}

	public void JoinTable(Entity entity)
	{
		var newPlayer = entity as LobbyPawn;

		if ( !CanJoinTable( newPlayer ) ) return;

		newPlayer.FocusedEntity = Entity;

		DisplayMessage( newPlayer, "You have joined the table", 5.0f );
		newPlayer.FreezeMovement = MainPawn.FreezeEnum.Movement;
		newPlayer.SetUpPoker( Entity );
		Players.Add( newPlayer );

		if ( Entity is PokerTable )
		{
			foreach ( var chair in (Entity as PokerTable).PokerChairs )
			{
				if ( chair.Sitter == null )
				{
					chair.Sitdown( entity );
					break;
				}
			}
		}
	}

	public void SetUpPlayers()
	{
		foreach ( LobbyPawn player in Players )
			player.SetUpPokerUI();
	}

	public void LeaveTable(LobbyPawn leaver)
	{
		DisplayMessage( leaver, "You have left the table", 5.0f );
		leaver.ClientCleanUp();

		leaver.FreezeMovement = MainPawn.FreezeEnum.None;
		leaver.FocusedEntity = null;

		Players.Remove( leaver );

		if(Entity is PokerTable)
		{
			foreach ( var chair in (Entity as PokerTable).PokerChairs )
			{
				if ( chair.Sitter == leaver )
				{
					chair.RemoveSittingPlayer( leaver );
					break;
				}
			}
		}
	}

	public void KickLosers()
	{
		foreach ( LobbyPawn player in Players.ToArray() )
		{
			if ( player.CurChips <= 0 )
				LeaveTable( player );
		}
	}

	public void SetUpRound()
	{
		DeckCards.Clear();
		CurrentDeck = StaticDeck.OrderBy( x => Game.Random.Int( 10000 ) ).ToList();

		HandOutCards();
		DoBlindBids();

		round++;
		PokerStage = PokerStatus.PreFlop;
	}

	public void DoBlindBids()
	{
		int leftToDealerIndex = dealerIndex + 1;

		if ( leftToDealerIndex >= Players.Count() )
			leftToDealerIndex = 0;

		LobbyPawn firstBidder = Players[leftToDealerIndex];

		leftToDealerIndex++;
		if ( leftToDealerIndex >= Players.Count() )
			leftToDealerIndex = 0;

		LobbyPawn secondBidder = Players[leftToDealerIndex];

		firstBidder.CurChips -= LowBlinds;
		firstBidder.LastBet = 25;
		secondBidder.CurChips -= HighBlinds;
		secondBidder.LastBet = 50;

		DisplayToPlayers( $"{firstBidder.Client.Name} Low Blind | {secondBidder.Client.Name} High Blind", 5.0f );

		ChipPot = HighBlinds;
		BetPot = 50;
	}

	int dealerIndex = 0;

	public void HandOutCards()
	{
		foreach ( LobbyPawn player in Players )
		{
			player.ClearCards();

			player.CardOne = CurrentDeck.First();
			CurrentDeck.RemoveAt( 0 );

			player.CardTwo = CurrentDeck.First();
			CurrentDeck.RemoveAt( 0 );

			player.GiveCards();

			if ( player.IsDealer )
				player.IsDealer = false;
		}

		if ( dealerIndex > Players.Count()-1)
			dealerIndex = 0;

		Players[dealerIndex].IsDealer = true;
		dealerIndex++;
	}

	public bool CheckDuplicateDeckCards( PokerCard card )
	{
		foreach ( LobbyPawn player in Players )
		{
			if ( (card.Suit == player.CardOne.Suit && card.Number == player.CardOne.Number)
				|| (card.Suit == player.CardTwo.Suit && card.Number == player.CardTwo.Number) )
				return true;
		}

		foreach ( PokerCard pastDeck in DeckCards )
		{
			if ( card.Suit == pastDeck.Suit && card.Number == pastDeck.Number ) return true;
		}

		return false;
	}

	public void DoGenerateCard()
	{
		PokerCard card = new PokerCard();
		card.GenerateCard();

		while ( CheckDuplicateDeckCards( card ) )
			card.GenerateCard();

		foreach ( var player in Players )
			player.UpdateCardDeckUI( card.Number, card.Suit );

		DeckCards.Add( card );
	}

	public Task DelayGame( float seconds )
	{
		return Task.Delay( (int)(seconds * 1000) );
	}

	public async void UpdateState( bool allFolded = false )
	{
		CurGameStatus = GameStatus.Pause;

		if ( allFolded || PokerStage == PokerStatus.River)
		{
			PokerStage = PokerStatus.Reveal;
			DoReveal();
			return;
		}

		BetPot = 0;

		foreach ( LobbyPawn player in Players )
		{
			player.HasPlayed = false;
		}

		switch ( PokerStage )
		{
			case PokerStatus.Idle:
				SetUpRound();
				break;
			case PokerStatus.PreFlop:
				await DelayGame( 3.0f );
				for ( int i = 0; i < 3; i++ )
					DoGenerateCard();

				PokerStage = PokerStatus.Flop;
				break;
			case PokerStatus.Flop:
				await DelayGame( 3.0f );
				DoGenerateCard();
				PokerStage = PokerStatus.Turn;
				break;
			case PokerStatus.Turn:
				await DelayGame( 3.0f );
				DoGenerateCard();
				PokerStage = PokerStatus.River;
				break;
		}

		CurGameStatus = GameStatus.Active;
	}

	public bool AllHadTurn()
	{
		int lastPlayer = -1;

		foreach ( LobbyPawn player in Players )
		{
			lastPlayer++;

			//If they folded, just continue
			if ( player.HasFolded ) continue;

			//If theres only one player who can properly play, just act like they played
			if ( lastPlayer == Players.Count() )
				return true;

			//If theres a player who hasn't had their turn, let them play
			if ( !player.HasPlayed )
				return false;
		}

		//Otherwise return that everyone has played
		return true;
	}
	public bool IsSequential( int[] array )
	{
		return array.Zip( array.Skip( 1 ), ( a, b ) => (a + 1) == b ).All( x => x );
	}

	public enum HandWinType
	{
		HighCard,
		OnePair,
		TwoPair,
		ThreeOfAKind,
		Straight,
		Flush,
		FullHouse,
		FourOfAKind,
		StraightFlush,
		RoyalFlush
	}

	public HandWinType CheckCardsWithDeck( PokerCard cardOne, PokerCard cardTwo )
	{
		List<PokerCard> hand = new List<PokerCard>();
		for ( int i = 0; i < 5; i++ )
			hand.Add( DeckCards[i] );

		hand.Add( cardOne );
		hand.Add( cardTwo );

		var suits = hand.Select( r => r.Suit ).DistinctBy(r => r).Count();

		int[] seq = hand.Select( r => r.Number ).ToArray().OrderBy( i => i ).ToArray();
		var fourKind = seq.GroupBy( f => f ).Where( n => n.Count() == 4 ).Select( p => p.First() ).ToArray();
		var threeKind = seq.GroupBy( f => f ).Where( n => n.Count() == 3 ).Select( p => p.First() ).ToArray();
		var pairs = seq.GroupBy( f => f ).Where( n => n.Count() == 2 ).Select( p => p.First() ).ToArray();

		if ( fourKind.Length > 0 ) return HandWinType.FourOfAKind;

		if ( threeKind.Length > 0 && pairs.Length > 0 ) return HandWinType.FullHouse;

		if ( suits == 1 && !IsSequential( seq ) ) return HandWinType.Flush;

		if ( IsSequential( seq ) ) return HandWinType.Straight;

		if ( threeKind.Length > 0 ) return HandWinType.ThreeOfAKind;

		if ( pairs.Length > 0 )
		{
			if ( pairs.Length == 2 ) return HandWinType.TwoPair;
			if ( pairs.Length == 1 ) return HandWinType.OnePair;
		}

		return HandWinType.HighCard;
	}
	public enum TieBreakerEnum
	{
		PlayerOne,
		PlayerTwo,
		Draw
	}
	public TieBreakerEnum SolvePokerTie( LobbyPawn plOne, LobbyPawn plTwo )
	{
		return TieBreakerEnum.PlayerTwo;
	}

	public async void DoReveal()
	{
		List<IClient> clients = new List<IClient>();
		List<LobbyPawn> plyList = new();

		foreach ( LobbyPawn player in Players )
		{
			if ( !player.HasFolded )
			{
				player.LastWin = CheckCardsWithDeck( player.CardOne, player.CardTwo );
				plyList.Add( player );
			}

			clients.Add( player.Client );
		}

		LobbyPawn winner = null;

		foreach ( LobbyPawn player in plyList )
		{
			DisplayMessage( clients, $"{player.Client.Name} got: {player.LastWin}", 12.5f );
			DisplayMessage( clients, $"{player.Client.Name} has: {player.CardOne.Suit} {player.CardOne.Number}, {player.CardTwo.Suit} {player.CardTwo.Number}", 12.5f );

			if ( player == Game.LocalPawn )
				continue;

			if ( winner == null )
			{
				winner = player;
				continue;
			}
			else
			{
				if ( (int)player.LastWin > (int)winner.LastWin )
					winner = player;

				if ( (int)player.LastWin == (int)winner.LastWin )
				{
					var tieBreak = SolvePokerTie( player, winner );

					if ( tieBreak == TieBreakerEnum.PlayerOne )
						winner = player;

					else if ( tieBreak == TieBreakerEnum.Draw )
					{
						DisplayToPlayers( "Tie detected, splitting pot", 5.0f );
						winner.CurChips += ChipPot / 2;
						player.CurChips += ChipPot / 2;
					}
				}
			}
		}

		await DelayGame( 10.0f );

		foreach ( LobbyPawn player in Players )
		{
			player.ClearCardsUI();
			player.HasFolded = false;
			player.HasPlayed = false;
		}

		await DelayGame( 2.0f );

		KickLosers();
		ChipPot = 0;

		if ( Players.Count() == 1 )
		{
			GiveWinnerRewards( Players[0] );
			return;
		}

		PokerStage = PokerStatus.Idle;
		UpdateState();
	}

	public void NextPlayerTurn()
	{
		curTurnIndex++;

		if ( curTurnIndex > Players.Count()-1 )
			curTurnIndex = 0;

		Log.Info( "Turn: " + curTurnIndex );
		Log.Info( "Players: " + (Players.Count() - 1) );

		if ( AllHadTurn() )
		{
			UpdateState();
			return;
		}

		int tries = 0;

		while ( Players[curTurnIndex].HasFolded || Players[curTurnIndex].CurChips <= 0 )
		{
			if ( curTurnIndex > (Players.Count() - 1) )
				curTurnIndex = 0;

			tries++;

			if ( tries >= Players.Count() )
			{
				UpdateState();
				break;
			}
			curTurnIndex++;
		}

		DisplayMessage( Players[curTurnIndex], "It is now your turn", 7.5f );
		DisplayMessage( Players[curTurnIndex], $"You have {Players[curTurnIndex].CurChips}", 7.5f );

		timeUntilFold = (TimeSince)TimeBeforeFold;
	}

	public enum PokerActionEnum
	{
		Check,
		BetRaise,
		Call,
		Fold,
		Leave
	}

	public void DoPokerAction( LobbyPawn player, PokerActionEnum action, int betAmount = 0 )
	{
		//We are in an active game or revealing poker stage
		if ( CurGameStatus != GameStatus.Active || PokerStage == PokerStatus.Reveal ) return;

		//It is not their turn to play
		if ( Players[curTurnIndex] != player ) return;

		//They already played their turn
		//if ( player.HasPlayed ) return;

		//They left, make them leave and move onto next player
		if ( action == PokerActionEnum.Leave )
		{
			LeaveTable( player );

			if ( Players.Count() > 1 )
				NextPlayerTurn();
			else
				GiveWinnerRewards( Players[0] );
		}

		//They folded
		if ( action == PokerActionEnum.Fold )
		{
			player.HasFolded = true;

			DisplayToPlayers( $"{player.Client.Name} has folded", 5.0f );
			NextPlayerTurn();
		}

		if ( action == PokerActionEnum.Check )
		{
			if( BetPot > 0 && player.LastBet < BetPot )
			{
				DisplayMessage( player, $"The bet is at {BetPot}, either call or bet higher" );
				return;
			}

			player.HasPlayed = true;
			DisplayToPlayers( $"{player.Client.Name} checked", 5.0f );
			NextPlayerTurn();
		}

		if ( action == PokerActionEnum.BetRaise )
		{
			if ( BetPot == 0 )
			{
				DisplayToPlayers( $"{player.Client.Name} bet {betAmount}", 6.5f );
			} 
			else
			{
				DisplayToPlayers( $"{player.Client.Name} raised to {betAmount}", 6.5f );
			}

			foreach ( LobbyPawn other in Players )
			{
				if ( other == player ) continue;

				other.HasPlayed = false;
			}

			player.LastBet = betAmount;
			player.HasPlayed = true;
			BetPot += betAmount;

			if ( Game.IsServer )
				player.AchTracker.UpdateAchievement( typeof( Gambler ), betAmount );

			NextPlayerTurn();

		}
		if ( action == PokerActionEnum.Call )
		{
			if ( BetPot == 0 )
			{
				player.HasPlayed = true;
				DisplayToPlayers( $"{player.Client.Name} checked", 5.0f );
				NextPlayerTurn();
				return;
			}

			player.HasPlayed = true;

			BetPot += betAmount - player.LastBet;
			ChipPot = BetPot;

			player.LastBet = BetPot;

			DisplayToPlayers( $"{player.Client.Name} called, the pot is at {ChipPot}", 5.0f );

			NextPlayerTurn();
		}
	}
	public void GiveWinnerRewards( LobbyPawn winner )
	{
		if( PrizePot > 0 )
			DisplayMessage( winner, $"YOU WON: ${PrizePot}", 5.0f );
		else
			DisplayMessage( winner, "YOU WON THE GAME", 5.0f );

		if ( EntryFee > 0 )
			winner.AddCredits( PrizePot );

		LeaveTable( winner );

		PrizePot = 0;
		Players.Clear();
	}

	TimeSince TimeBeforeStart;
	TimeSince timeUntilFold;

	[Event.Tick.Server]
	protected void PokerServerTick()
	{
		if ( Players.Count() <= 0 )
		{
			PokerStage = PokerStatus.Idle;
			CurGameStatus = GameStatus.Idle;
		}

		if ( PokerStage == PokerStatus.Idle )
		{
			foreach ( LobbyPawn player in Players.ToArray() )
			{
				if ( player.Position.Distance( Entity.Position ) > 92.0f )
					LeaveTable( player );
			}

			if ( Players.Count() >= 2 && CurGameStatus == GameStatus.Idle )
			{
				TimeBeforeStart = 0.0f;
				CurGameStatus = GameStatus.Starting;
			}
			else if ( Players.Count() < 2 && CurGameStatus == GameStatus.Starting )
				CurGameStatus = GameStatus.Idle;
		
			if ( TimeBeforeStart > 10.0f && CurGameStatus == GameStatus.Starting )
			{
				timeUntilFold = (TimeSince)TimeBeforeFold;
				CurGameStatus = GameStatus.Active;
				curTurnIndex = 0;
				SetUpPlayers();
				UpdateState();
			}
		}

		if ( timeUntilFold <= 0 && CurGameStatus == GameStatus.Active )
		{
			DoPokerAction( Players[curTurnIndex], PokerActionEnum.Fold );
		}

		foreach ( var player in Players.ToArray() )
		{
			if ( !player.IsValid )
				LeaveTable( player );
		}
	}

	public List<PokerCard> CurrentDeck = new();
	public static List<PokerCard> StaticDeck = new()
	{
		new PokerCard(0,2),
		new PokerCard(0,3),
		new PokerCard(0,4),
		new PokerCard(0,5),
		new PokerCard(0,6),
		new PokerCard(0,7),
		new PokerCard(0,8),
		new PokerCard(0,9),
		new PokerCard(0,10),
		new PokerCard(0,11),
		new PokerCard(0,12),
		new PokerCard(0,13),
		new PokerCard(0,14),

		new PokerCard(1,2),
		new PokerCard(1,3),
		new PokerCard(1,4),
		new PokerCard(1,5),
		new PokerCard(1,6),
		new PokerCard(1,7),
		new PokerCard(1,8),
		new PokerCard(1,9),
		new PokerCard(1,10),
		new PokerCard(1,11),
		new PokerCard(1,12),
		new PokerCard(1,13),
		new PokerCard(1,14),

		new PokerCard(2,2),
		new PokerCard(2,3),
		new PokerCard(2,4),
		new PokerCard(2,5),
		new PokerCard(2,6),
		new PokerCard(2,7),
		new PokerCard(2,8),
		new PokerCard(2,9),
		new PokerCard(2,10),
		new PokerCard(2,11),
		new PokerCard(2,12),
		new PokerCard(2,13),
		new PokerCard(2,14),

		new PokerCard(3,2),
		new PokerCard(3,3),
		new PokerCard(3,4),
		new PokerCard(3,5),
		new PokerCard(3,6),
		new PokerCard(3,7),
		new PokerCard(3,8),
		new PokerCard(3,9),
		new PokerCard(3,10),
		new PokerCard(3,11),
		new PokerCard(3,12),
		new PokerCard(3,13),
		new PokerCard(3,14),
	};
}
