using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TowerResort.Audio;

namespace TowerResort.Entities.CondoItems;

public partial class CondoItemBase : IPlayableAudio
{
	public AudioPlayer MusicPlayer { get; set; }
	private AudioReceiver MusicReceiver { get; set; }
	[Net] public bool IsMusicPlaying { get; set; }
	[Net] public string MusicId { get; set; }
	[Net] public TimeSince MusicStart { get; set; }

	public void SetUpMusicPlayer()
	{
		if ( Game.IsClient )
		{
			InitializeMusicPlayer();
			InitializeMusicReceiver();
			UpdateFrames();

			if ( IsVideoPlaying && !MusicPlayer.IsPlaying )
			{
				PlayVideo( VideoId );
			}
		}

		if ( Game.IsServer )
		{
			ServerInitializeMusicReceiver();
			Log.Info( "working" );
		}
	}

	private async void InitializeMusicPlayer()
	{
		MusicPlayer = new AudioPlayer( this );

		await Task.Delay( 1000 );

		SceneObject.Attributes.Set( "tint", Color.White );

		//AudioStreamPanel.Instance.Player = MusicPlayer;
		//VideoStreamPanel.Instance.Receiver = VidReciever;
	}

	private async void ServerInitializeMusicReceiver()
	{
		MusicReceiver = new AudioReceiver( async ( id ) =>
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
					Log.Warning( $"Failed to submit score to backend, error: {e.Message}" );
				}
			}

			PlayVideo( id );
		}, OnVideoProgress );

		await MusicReceiver.CreateSocket();
	}

	private async void InitializeMusicReceiver()
	{
		MusicReceiver = new AudioReceiver( MusicPlayer );
		await MusicReceiver.CreateSocket();
	}

	[Event.Client.Frame]
	private void AudioPlayback()
	{
		MusicPlayer?.Playback();
	}

	[Event.Client.Frame]
	private void UpdateAudio()
	{
		/*VideoPlayer?.UpdateFrames();
		VidReciever?.Update();*/
	}

	[ClientRpc]
	private void OnAudioProgress( string jsonProgress )
	{
		MusicPlayer.AudioProgress = JsonSerializer.Deserialize<AudioProgress>( jsonProgress );
	}



	[ClientRpc]
	public void PlayAudio( string id )
	{
		Log.Info( $"Playing the video! {id}" );
		MusicReceiver.JoinAudioStream( id );
	}

	public void RequestAudioURL( string url )
	{
		Game.AssertServer();
		MusicReceiver.RequestAudioURL( url );
	}
}
