namespace TowerResort.Entities.CondoItems;

public partial class CondoItemBase
{
	public static VideoStreamPanel VideoPanel;
	public List<LobbyPawn> WatchingPlayers;
	[Net] public IList<string> CondoVideoQueue { get; set; }
	[Predicted] public float Timeline { get; set; }

	[Event.Tick.Server]
	protected void TickTimeline()
	{
		if ( Asset.Type != CondoAssetBase.ItemEnum.Visual ) return;

		foreach ( var player in WatchingPlayers.ToArray() )
		{
			if ( player.Position.Distance( Position ) >= 356.0f )
			{
				CondoVideoQueue.Clear();
				UpdateClient( To.Single( player ), false, "www.youtube.com" );
				WatchingPlayers.Remove( player );
			}
		}

		if( CondoVideoQueue.Count <= 0 || WatchingPlayers.Count <= 0 )
		{
			Timeline = 0;
			return;
		}

		Timeline += 30.0f * Time.Delta;
		Timeline = MathF.Round( Timeline );
	}

	public void SetUpVideoPlayer()
	{
		WatchingPlayers = new List<LobbyPawn>();
		CondoVideoQueue = new List<string>();
	}

	public void DoVideoActions(LobbyPawn user)
	{
		if( user.WatchingEntity != null )
		{
			user.WatchingEntity.UpdateClient( To.Single( user ), false );
		}

		if ( WatchingPlayers.Contains( user ) )
		{
			WatchingPlayers.Remove( user );
			user.WatchingEntity = null;

			if ( WatchingPlayers.Count <= 0 )
			{
				CondoVideoQueue.Clear();
				UpdateClient(To.Single(user), false, "www.youtube.com" );
			}
		}
		else
		{
			WatchingPlayers.Add( user );
			user.WatchingEntity = this;

			string curVideo = "www.youtube.com";

			if ( CondoVideoQueue.Count > 0 )
				curVideo = CondoVideoQueue[0];

			UpdateClient( To.Single( user ), true, curVideo, (int)Timeline );
		}

	}

	[ClientRpc]
	public void UpdateClient( bool start, string videoID = "", int time = 0 )
	{
		if( !start )
		{
			VideoPanel?.Delete();
			VideoPanel = null;
			VideoStreamPanel.Current?.Delete();
		} 
		else
		{
			if ( VideoPanel == null )
			{
				VideoPanel = new VideoStreamPanel( this, Asset.ScreenForwardPosition, Asset.ScreenHeightPosition );
			}

			if( !string.IsNullOrEmpty(videoID) )
			{
				if ( time > 0 )
					videoID += "&start=" + time / 30;

				VideoStreamPanel.Current.AddVideo( videoID );
			}
		}
	}

	[ConCmd.Server("tr.video.reset.timeline")]
	public static void ResetVideoTimeline()
	{
		if ( ConsoleSystem.Caller.Pawn is null || ConsoleSystem.Caller.Pawn is not LobbyPawn player )
			return;

		if ( player.WatchingEntity == null ) return;

		player.WatchingEntity.Timeline = 0;
	}

	[ConCmd.Server( "tr.video.request" )]
	public static void RequestVideo( string url )
	{
		if ( ConsoleSystem.Caller.Pawn is null || ConsoleSystem.Caller.Pawn is not LobbyPawn player )
			return;

		if ( player.WatchingEntity == null ) return;

		if ( !url.StartsWith( "https://www.youtube.com" ) || url.Length < 32 ) return;

		string videoID = "";

		videoID = "https://www.youtube.com/embed/" + url.Substring(32) + "?autoplay=1";

		player.WatchingEntity.CondoVideoQueue.Add( videoID );
		player.WatchingEntity.UpdateClient( To.Single( player ), true, videoID );
	}
}
