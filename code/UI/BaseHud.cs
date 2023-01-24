using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;
using Components.BaseGamesUi;
using Components.NotificationManager;
using TowerResort.UI;

public class BaseHud : RootPanel
{
	public static BaseHud Current;
	public NotificationManagerUI NotificationManager;
	public BaseGamesUi.PlayersPanel playersPanel;

	public BaseHud()
	{
		if (Current != null)
		{
			Current.Delete();
			Current = null;
		}

		NotificationManager = new();
		AddChild( NotificationManager );

		Current = this;
		StyleSheet.Load( "/UI/Styles/HubHud.scss" );
		
		AddChild<HubChat>();
		AddChild<HudTracker>();
		//TEMPORARY
		AddChild<Scoreboard<ScoreboardEntry>>();
	}
}
