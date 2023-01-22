using Sandbox;
using Sandbox.UI;
using System;
using TheHub.Entities.CondoItems;
using TheHub.Entities.Lobby;
using TheHub.GameComponents;
using static Components.BaseGamesUi.BaseGamesUi;

namespace TheHub.Player;

public class PokerCard
{
	public PokerCard() { }
	public PokerCard( int suit, int number )
	{
		Suit = (SuitEnum)suit;
		Number = number;
	}

	public enum SuitEnum
	{
		Diamonds,
		Hearts,
		Clubs,
		Spades
	}

	public SuitEnum Suit;
	public int Number;

	public string ConvertToDisplay()
	{
		string display = "";

		if ( Number == 14 )
		{
			switch ( Suit )
			{
				case SuitEnum.Diamonds: display = "Ace of Diamonds"; break;
				case SuitEnum.Hearts: display = "Ace of Hearts"; break;
				case SuitEnum.Clubs: display = "Ace of Clubs"; break;
				case SuitEnum.Spades: display = "Ace of Spades"; break;
			}

		}
		else if ( Number >= 11 )
		{
			switch ( Number )
			{
				case 11: display = "Jack of "; break;
				case 12: display = "Queen of "; break;
				case 13: display = "King of "; break;
			}

			switch ( Suit )
			{
				case SuitEnum.Diamonds: display += "Diamonds"; break;
				case SuitEnum.Hearts: display += "Hearts"; break;
				case SuitEnum.Clubs: display += "Clubs"; break;
				case SuitEnum.Spades: display += "Spades"; break;
			}
		}
		else
		{
			display = Number + " of ";

			switch ( Suit )
			{
				case SuitEnum.Diamonds: display += "Diamonds"; break;
				case SuitEnum.Hearts: display += "Hearts"; break;
				case SuitEnum.Clubs: display += "Clubs"; break;
				case SuitEnum.Spades: display += "Spades"; break;
			}
		}

		return display;
	}

	public void GenerateCard()
	{
		Suit = (SuitEnum)Enum.ToObject( typeof( SuitEnum ), Game.Random.Int( 0, 3 ) );
		Number = Game.Random.Int( 2, 14 );
	}
}

public partial class LobbyPawn : MainPawn
{
	[Net] public bool HasFolded { get; set; }
	[Net] public int CurChips { get; set; }
	[Net] public bool IsDealer { get; set; }

	public bool HasPlayed;

	public TimeUntil TimeBeforeForceSkip;
	[Net] public Entity CurPokerTable { get; private set; }
	public PokerCard CardOne { get; set; }
	public PokerCard CardTwo { get; set; }

	public int LastBet = 0;

	TimeSince timeLastAction;

	public PokerGame.HandWinType LastWin;

	public void SetUpPoker( Entity table )
	{
		CardOne = new PokerCard();
		CardTwo = new PokerCard();

		HasFolded = false;
		HasPlayed = false;
		
		timeLastAction = 0;

		if ( table is PokerTable casinoTable )
		{
			CurPokerTable = casinoTable;
			var c = CurPokerTable.Components.GetOrCreate<PokerGame>();

			if ( c.EntryFee > 0 )
				CurChips = c.EntryFee;
			else
				CurChips = c.StartingChips;
		} 
		else if (table is CondoItemBase condoTable)
		{
			CurPokerTable = condoTable;
			var c = CurPokerTable.Components.GetOrCreate<PokerGame>();

			CurChips = c.StartingChips;
		}


	}

	public class flexPanel : Panel { public flexPanel() { Style.Set( "gap: 24px;" ); }  }

	BaseTabletopLayout tableLayout;

	DeckCardsPanel cardPanel;
	ButtonActions pokerButtons;
	DeckCardsPanel deckCardPanel;
	PlayersPanel playersPanel;
	BaseGameValues GameStats;

	BaseGameValueEntry pot;
	BaseGameValueEntry currentturn;
	BaseGameValueEntry playerchips;

	public void SetUpPokerUI()
	{
		ClientSetUp( To.Single( this ) );
	}

	[ClientRpc]
	public void ClientSetUp()
	{
		ClientCleanUp();
		
		tableLayout = new BaseTabletopLayout();

		cardPanel = new DeckCardsPanel();
		pokerButtons = new ButtonActions();
		deckCardPanel = new DeckCardsPanel( "align-items: flex-end;" );
		playersPanel = new PlayersPanel();

		GameStats = new("1500px");
		pot = new( "POT", "--" );
		currentturn = new( "Turn", "--" );
		playerchips = new( "Chips", "--" );
		GameStats.AddValue( pot );
		GameStats.AddValue( currentturn );
		GameStats.AddValue( playerchips );

		var fold = pokerButtons.AddButton( "fold", "Fold" );
		fold.action = () => { ConsoleSystem.Run( "hub.game.poker.fold" ); };
		var check = pokerButtons.AddButton( "check", "Check" );
		check.action = () => { ConsoleSystem.Run( "hub.game.poker.check" ); };
		var betRaise = pokerButtons.AddButton( "betraise", "Bet | Raise" );
		betRaise.action = () => { ConsoleSystem.Run( "hub.game.poker.betraise" ); };
		var call = pokerButtons.AddButton( "call", "Call" );
		call.action = () => { ConsoleSystem.Run( "hub.game.poker.call" ); };
		var leave = pokerButtons.AddButton( "leave", "Leave" );
		leave.action = () => { ConsoleSystem.Run( "hub.game.poker.leave" ); };

		foreach ( var player in CurPokerTable.Components.Get<PokerGame>().Players )
			playersPanel.AddPlayer( new PlayerEntry( player.Client.Name ) );

		var cards = new flexPanel();

		tableLayout.top.AddClass( "column center" );
		tableLayout.top.AddChild( GameStats );
		tableLayout.top.AddChild( playersPanel );
		tableLayout.bottom.AddClass( "column reverse center" );
		tableLayout.bottom.AddChild( pokerButtons );
		tableLayout.bottom.AddChild( cards );

		cards.AddChild( deckCardPanel );
		cards.AddChild( cardPanel );

		cardPanel.AddCard( "C", "big debug" );

		BaseHud.Current.AddChild( tableLayout );

		/*		BaseHud.Current.AddChild( cardPanel );
				BaseHud.Current.AddChild( pokerButtons );
				BaseHud.Current.AddChild( deckCardPanel );
				BaseHud.Current.AddChild( playersPanel );
		*/
	}

	//Clears a specific player on that user's UI
	[ClientRpc]
	public void ClearPlayer( string playerName )
	{
		playersPanel?.RemovePlayer( playerName );
	}

	//Clears all players on that user's UI
	[ClientRpc]
	public void ClearAllPlayers()
	{
		playersPanel?.RemoveAllPlayers();
	}

	public void ClearCards()
	{
		CardOne = null;
		CardTwo = null;
	}

	public void ClearPokerPlayer()
	{
		FreezeMovement = FreezeEnum.None;

		CurPokerTable = null;

		CardOne = null;
		CardTwo = null;

		ClientCleanUp( To.Single( this ) );
	}

	[ClientRpc]
	public void ClientCleanUp()
	{
		cardPanel?.Delete();
		cardPanel = null;
		
		pokerButtons?.Delete();
		pokerButtons = null;

		deckCardPanel?.Delete();
		deckCardPanel = null;

		ClearAllPlayers();

		playersPanel?.Delete();
		playersPanel = null;

		tableLayout?.Delete();
		tableLayout = null;
	}

	public void GiveCards()
	{
		UpdateCardHandUI( To.Single( this ), CardOne.Number, CardOne.Suit, CardTwo.Number, CardTwo.Suit );

		CurPokerTable.Components.Get<PokerGame>().DisplayMessage( this, $"You have {CardOne.ConvertToDisplay()} and {CardTwo.ConvertToDisplay()}", 10.0f );
	}

	public void UpdateCardDeckUI( int number, PokerCard.SuitEnum suit )
	{
		UpdateUI( To.Single( this ), number, suit );
	}

	[ClientRpc]
	public void UpdateUI( int number, PokerCard.SuitEnum suit )
	{
		switch ( suit )
		{
			case PokerCard.SuitEnum.Diamonds: deckCardPanel.AddCard( "", "big medium", "ui/poker/diamond_" + number + ".png" ); break;
			case PokerCard.SuitEnum.Hearts: deckCardPanel.AddCard( "", "big medium", "ui/poker/heart_" + number + ".png" ); break;
			case PokerCard.SuitEnum.Clubs: deckCardPanel.AddCard( "", "big medium", "ui/poker/club_" + number + ".png" ); break;
			case PokerCard.SuitEnum.Spades: deckCardPanel.AddCard( "", "big medium", "ui/poker/spade_" + number + ".png" ); break;
		}
	}

	[ClientRpc]
	public void UpdateCardHandUI( int cardOneNumber, PokerCard.SuitEnum cardOneSuit, int cardTwoNumber, PokerCard.SuitEnum cardTwoSuit )
	{
		cardPanel.ClearCardsPanel();
		var classneames = "big debug";

		switch ( cardOneSuit )
		{
			case PokerCard.SuitEnum.Diamonds: cardPanel.AddCard( "", classneames, "ui/poker/diamond_" + cardOneNumber + ".png" ); break;
			case PokerCard.SuitEnum.Hearts: cardPanel.AddCard( "", classneames, "ui/poker/heart_" + cardOneNumber + ".png" ); break;
			case PokerCard.SuitEnum.Clubs: cardPanel.AddCard( "", classneames, "ui/poker/club_" + cardOneNumber + ".png" ); break;
			case PokerCard.SuitEnum.Spades: cardPanel.AddCard( "", classneames, "ui/poker/spade_" + cardOneNumber + ".png" ); break;
		}
		switch ( cardTwoSuit )
		{
			case PokerCard.SuitEnum.Diamonds: cardPanel.AddCard( "", classneames, "ui/poker/diamond_" + cardTwoNumber + ".png" ); break;
			case PokerCard.SuitEnum.Hearts: cardPanel.AddCard( "", classneames, "ui/poker/heart_" + cardTwoNumber + ".png" ); break;
			case PokerCard.SuitEnum.Clubs: cardPanel.AddCard( "", classneames, "ui/poker/club_" + cardTwoNumber + ".png" ); break;
			case PokerCard.SuitEnum.Spades: cardPanel.AddCard( "", classneames, "ui/poker/spade_" + cardTwoNumber + ".png" ); break;
		}
	}

	public void DoCleanUp()
	{
		ClearCardsUI( To.Single( this ) );
	}

	[ClientRpc]
	public void ClearCardsUI()
	{
		deckCardPanel?.ClearCardsPanel();
		cardPanel?.ClearCardsPanel();
	}

	[ConCmd.Server( "hub.game.poker.check" )]
	public static void PokerCheck()
	{
		var player = ConsoleSystem.Caller.Pawn as LobbyPawn;
		if ( player == null ) return;

		if ( player.CurPokerTable == null ) return;
		
		if ( player.timeLastAction < 0.25f ) return;

		player.timeLastAction = 0;

		var c = player.CurPokerTable.Components.Get<PokerGame>();
		c.DoPokerAction( player, PokerGame.PokerActionEnum.Check);
	}

	[ConCmd.Server( "hub.game.poker.leave" )]
	public static void PokerLeave()
	{
		var player = ConsoleSystem.Caller.Pawn as LobbyPawn;
		if ( player == null ) return;

		if ( player.CurPokerTable == null ) return;
		
		if ( player.timeLastAction < 0.25f ) return;

		player.timeLastAction = 0;

		var c = player.CurPokerTable.Components.Get<PokerGame>();
		c.DoPokerAction( player, PokerGame.PokerActionEnum.Leave );
	}

	[ConCmd.Server( "hub.game.poker.fold" )]
	public static void PokerFold()
	{
		var player = ConsoleSystem.Caller.Pawn as LobbyPawn;
		if ( player == null ) return;

		if ( player.CurPokerTable == null ) return;

		if ( player.timeLastAction < 0.25f ) return;

		player.timeLastAction = 0;

		var c = player.CurPokerTable.Components.Get<PokerGame>();
		c.DoPokerAction( player, PokerGame.PokerActionEnum.Fold );
	}

	[ConCmd.Server( "hub.game.poker.betraise" )]
	public static void PokerRaiseOrBet()
	{
		var player = ConsoleSystem.Caller.Pawn as LobbyPawn;
		if ( player == null ) return;

		if ( player.CurPokerTable == null ) return;

		var c = player.CurPokerTable.Components.Get<PokerGame>();
		if ( player.CurChips <= 0 || player.CurChips <= c.ChipPot ) return;

		if ( player.timeLastAction < 0.5f ) return;

		player.timeLastAction = 0;

		int bet = c.BetPot + 50;

		c.DoPokerAction( player, PokerGame.PokerActionEnum.BetRaise, bet );
	}

	[ConCmd.Server( "hub.game.poker.call" )]
	public static void PokerCall()
	{
		var player = ConsoleSystem.Caller.Pawn as LobbyPawn;
		if ( player == null ) return;

		if ( player.CurPokerTable == null ) return;

		if ( player.CurChips <= 0 ) return;

		if ( player.timeLastAction < 0.25f ) return;

		player.timeLastAction = 0;

		var c = player.CurPokerTable.Components.Get<PokerGame>();
		int callAmount = c.BetPot;

		if ( player.CurChips < callAmount )
			callAmount = c.BetPot - player.CurChips;

		player.CurChips -= callAmount;

		if ( player.CurChips < 0 )
			player.CurChips = 0;

		c.DoPokerAction( player, PokerGame.PokerActionEnum.Call, callAmount );
	}
}
