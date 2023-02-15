namespace TowerResort.Entities.CondoItems;

public partial class CondoItemBase
{
	public static VideoStreamPanel VideoPanel;

	public void SetUpVideoPlayer()
	{
		
	}

	public void DoVideoActions(LobbyPawn user)
	{
		UpdateClient();
	}

	[ClientRpc]
	public void UpdateClient()
	{
		if( VideoPanel != null )
		{
			VideoPanel?.Delete();
			VideoPanel = null;
			VideoStreamPanel.Current?.Delete();
		} 
		else
		{
			VideoPanel = new VideoStreamPanel( this, Asset.ScreenForwardPosition, Asset.ScreenHeightPosition );
		}
		
		
	}

	[ConCmd.Client( "tr.video.request", CanBeCalledFromServer = true )]
	public static void RequestVideo( string url )
	{
		if ( ConsoleSystem.Caller.Pawn is null || ConsoleSystem.Caller.Pawn is not LobbyPawn )
			return;

		if ( VideoStreamPanel.Current == null ) return;

		if ( !url.StartsWith( "https://www.youtube.com" ) ) return;

		string videoID = "";

		videoID = "https://www.youtube.com/embed/" + url.Substring(32) + "?autoplay=1";

		VideoStreamPanel.Current.AddVideo( videoID );
	}

	[ConCmd.Client("tr.video.timeline", CanBeCalledFromServer = true)]
	public static void RequestVideoTimeline(int seconds)
	{
		if ( ConsoleSystem.Caller.Pawn is null || ConsoleSystem.Caller.Pawn is not LobbyPawn )
			return;

		if ( VideoStreamPanel.Current == null ) return;

		//VideoStreamPanel.Current.SetFrame( seconds );
	}
}
