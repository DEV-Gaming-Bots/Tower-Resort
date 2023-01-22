using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;
using Components.SBOXTower;
using Components.BaseGamesUi;
using Components.NotificationManager;
using TheHub.UI;

public class BaseHud : RootPanel
{
	public static BaseHud Current;
	public NotificationManagerUI NotificationManager;
	public BaseGamesUi.PlayersPanel playersPanel;
	/*public BaseGamesUi.CardsPanel TestCardsPanel;*/

	public BaseHud()
	{
		if (Current != null)
		{
			Current.Delete();
			Current = null;
		}

		playersPanel = new();
		NotificationManager = new();
		AddChild( playersPanel );
		AddChild( NotificationManager );

		Current = this;
		StyleSheet.Load( "/UI/Styles/HubHud.scss" );

		/* TEST - REMOVE - Cards Panel
		 * TestCardsPanel = new();

		TestCardsPanel.AddCard( "path/to/texture", "spades" );
		TestCardsPanel.AddCard( "path/to/texture", "spades" );

		AddChild( TestCardsPanel );*/

		AddChild<HubChat>();
		AddChild<HudTracker>();
		//TEMPORARY
		AddChild<Scoreboard<ScoreboardEntry>>();
	}
}
