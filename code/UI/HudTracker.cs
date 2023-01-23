using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TowerResort.Player;
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace TowerResort.UI;

public partial class HudTracker : Panel
{
	public Panel hud;
	public Panel keyboardActions;
	public Label CoinLbl;
	public Label Username;
	public Label CurZone;
	public Panel Avatar;

	public static HudTracker Current;

	public HudTracker()
	{
		StyleSheet.Load( "UI/Styles/Lobby/HudTracker.scss" );

		hud = Add.Panel( "hud" );
		Panel mainContainer = hud.Add.Panel( "main-cointracker" );
		mainContainer.Add.Panel( "logo" );

		Panel cointracker = mainContainer.Add.Panel( "coinTracker" );

		Panel coins = cointracker.Add.Panel( "coins" );
		coins.Add.Panel( "html_coin" );
		CoinLbl = coins.Add.Label( "???", "amount" );
		Panel panelUsername = cointracker.Add.Panel( "username" );
		Username = panelUsername.Add.Label( "username goes here", "name" );

		Panel zonePanel = cointracker.Add.Panel( "zone" );

		CurZone = zonePanel.Add.Label( "Somewhere", "name" );

		//mainContainer.Add.Panel("gradient_topright");
		//mainContainer.Add.Panel("gradient_bottomleft");

		Avatar = hud.Add.Panel( "logo" );

		Current = this;
	}

	public void UpdateLocation(string newLocation)
	{
		if ( newLocation == CurZone.Text ) return;

		CurZone.SetText( newLocation );
	}

	public override void Tick()
	{
		base.Tick();

		var player = Game.LocalPawn as LobbyPawn;

		SetClass( "active", player != null );

		if ( player == null )
			return;

		if( TRGame.Instance.RunningDedi )
			CoinLbl.Text = $"{player.Credits:C0}";
		else
		{
			if(TRGame.IsDevMode)
				CoinLbl.Text = $"DEV - {player.Credits:C0}";
			else
				CoinLbl.Text = "PLAY THE OFFICIAL SERVERS";
		}

		Username.SetText( Game.LocalClient.Name );

		Avatar.Style.SetBackgroundImage( $"avatarbig:{Game.LocalClient.SteamId}" );
	}
}


