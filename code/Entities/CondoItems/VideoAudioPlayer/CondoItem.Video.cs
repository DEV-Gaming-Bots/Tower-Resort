using System;
using System.Text.Json;
using TowerResort.Video;

namespace TowerResort.Entities.CondoItems;

public partial class CondoItemBase : IPlayableVideo
{
	public VideoPlayer Player { get; set; }
	private VideoReceiver Receiver { get; set; }
	[Net] public bool IsVideoPlaying { get; set; }
	[Net] public string VideoId { get; set; }
	[Net] public TimeSince VideoStart { get; set; }

	public void SetUpVideoPlayer()
	{
		if ( Game.IsClient )
		{
			InitializePlayer();
			InitializeReceiver();
			UpdateFrames();

			if ( IsVideoPlaying && !Player.IsPlaying )
			{
				PlayVideo( VideoId );
			}
		}

		if ( Game.IsServer )
		{
			InitializeServerReceiver();
			Log.Info( "working" );
		}
	}

	private async void InitializePlayer()
	{
		Player = new VideoPlayer( this );

		await Task.Delay( 1000 );
			
		SceneObject.Attributes.Set("tint", Color.White);

		VideoStreamPanel.Instance.Player = Player;
		VideoStreamPanel.Instance.Receiver = Receiver;
	}

	private async void InitializeServerReceiver()
	{
		Receiver = new VideoReceiver( async (id) =>
		{
			VideoId = id;
			VideoStart = 0;
			IsVideoPlaying = true;

			//var leaderboard = await Leaderboard.FindOrCreate("played_videos", false);
				
			foreach ( var player in Game.Clients )
			{
				try
				{
					/*if ( leaderboard == null )
					{
						continue;
					}

					var playerScore = await leaderboard.Value.GetScore( player.PlayerId );
					await leaderboard.Value.Submit( (playerScore?.Score ?? 0) + 1);*/
				}
				catch ( Exception e )
				{
					Log.Warning($"Failed to submit score to backend, error: {e.Message}");
				}
			}

			PlayVideo( id );
		}, OnVideoProgress );
			
        await Receiver.CreateSocket();
	}

	private async void InitializeReceiver()
	{
		Receiver = new VideoReceiver( Player );
		await Receiver.CreateSocket();
	}

	[Event.Client.Frame]
	private void Playback()
	{
		Player?.Playback();
			
		if ( Player?.ActiveTexture != null )
		{
			SceneObject?.Attributes.Set( "screen", Player.ActiveTexture );
		}
	}

	[Event.Client.Frame]
	private void UpdateFrames()
	{
		Player?.UpdateFrames();
		Receiver?.Update();
	}

	[ClientRpc]
	public void PlayVideo( string id )
	{
		Log.Info( $"Playing the video! {id}" );
		Receiver.JoinVideoStream( id );
	}
		
	public void RequestVideoURL(string url)
	{
		Game.AssertServer();
			
		Log.Info( $"Requested a video, URL: {url}" );
		Receiver.RequestVideo(url);
	}

	[ClientRpc]
	private void OnVideoProgress(string jsonProgress)
	{
		Player.VideoProgress = JsonSerializer.Deserialize<VideoProgress>(jsonProgress);
	}
}
