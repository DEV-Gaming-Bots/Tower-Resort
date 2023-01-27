using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;
using Components.BaseGamesUi;
using Components.NotificationManager;

public class BaseHud : RootPanel
{
	public static BaseHud Current;
	public NotificationManagerUI NotificationManager;
	public TRBaseUIComponents.ThemePanel CurrentTheme; 
	public BaseHud()
	{
		if ( Current != null )
		{
			Current.Delete();
			Current = null;
		}

		NotificationManager = new();
		AddChild( NotificationManager );

		Current = this;
		StyleSheet.Load( "/UI/Styles/TRHud.scss" );
		
		AddChild<TRChat>();
		AddChild<HudTracker>();
		AddChild<Scoreboard>();
		AddChild<TRHudTest>();
	}
}
