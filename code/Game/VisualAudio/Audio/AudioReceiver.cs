using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;
using TowerResort.Video;

namespace TowerResort.Audio;

public class AudioReceiver
{
	public string WebSocketURL { get; } = "ws://avbdns.duckdns.org:8080/";
	//public string WebSocketURL { get; } = "ws://m0uka.dev:8880";
	public Action<string[]> MessageReceived { get; set; }
	public Action<string> StreamSuccess { get; set; }
	public Action<string> AudioProgress { get; set; }
		
	public AudioPlayer MusicPlayer { get; set; }
		
	private WebSocket WebSocket { get; set; }

	private Dictionary<DateTime, int> ThroughputData { get; set; } = new();

	public int Throughput
	{
		get
		{
			return ThroughputData.Sum( x => x.Value );
		}
	}

	public AudioReceiver( AudioPlayer player )
	{
		MusicPlayer = player;
	}
		
	public AudioReceiver(Action<string> onStreamSuccess, Action<string> onAudioProgress )
	{
		StreamSuccess = onStreamSuccess;
		AudioProgress = onAudioProgress;
	}

	private byte[] MergeBytes(List<byte[]> fragments)
	{
		var output = new byte[fragments.Sum(arr=>arr.Length)];
		int writeIdx=0;
		foreach(var byteArr in fragments) {
			byteArr.CopyTo(output, writeIdx);
			writeIdx += byteArr.Length;
		}

		return output;
	}

	private void UpdateThroughput()
	{
		foreach ( var i in ThroughputData.Where( x => x.Key < DateTime.Now - TimeSpan.FromSeconds( 1 ) )
				        .ToList() )
		{
			ThroughputData.Remove( i.Key );
		}
	}

	private void CalculateThroughput(int length)
	{
		var now = DateTime.Now;
		if ( ThroughputData.ContainsKey( now ) )
		{
			ThroughputData[now] += length;
		}
				
		ThroughputData[now] = length;
	}
		
	private short[] ConvertBitsToShorts(byte[] buffer)
	{
		List<short> samples = new List<short>(buffer.Length);
		for (int n = 0; n < buffer.Length; n+=2)
		{
			short sample = (short)(buffer[n] | buffer[n+1] << 8);
			samples.Add( sample );
		}

		return samples.ToArray();
	}
		
	public void Update()
	{
		UpdateThroughput();
	}
		
	/// <summary>
	/// Inits a websocket connection
	/// </summary>
	public async Task CreateSocket()
	{
		WebSocket = new WebSocket();
		TRGame.Instance.VideoSockets.Add( WebSocket );

		try
		{
			await WebSocket.Connect( WebSocketURL );
		}
		catch ( Exception )
		{
			Log.Error( "Couldn't connect to AudioPlayer Backend! Retrying connection in 5 seconds..." );
			await RetryConnection();
		}

		if ( WebSocket.IsConnected )
		{
			Log.Info( "Connected to AudioPlayer Backend" );
		}
		else return;

		bool activeFragment = false;
		List<byte[]> frameFragments = new List<byte[]>();
			
		WebSocket.OnDataReceived += (data) =>
		{
			if ( activeFragment )
			{
				if ( data.Length > 0 && data[0] == 0xFE )
				{
					activeFragment = false;

					byte[] fragment = MergeBytes( frameFragments );

					MusicPlayer.AddFrame(fragment);
					frameFragments.Clear();
				}
				else
				{
					frameFragments.Add( data.ToArray() );
				}
			}
			else
			{
				// Check if data is JPEG
				if ( data.Length > 1 && data[0] == 0xFF && data[1] == 0xD8 )
				{
					MusicPlayer.AddFrame( data.ToArray() );
				}
				else if ( data.Length > 0 && data[0] == 0xAA )
				{
					// start of fragment
					activeFragment = true;
				} else if ( data.Length > 0 && data[0] == 0x01 )
				{
					// sound frame!
					short[] shorts = ConvertBitsToShorts( data.Slice( 1 ).ToArray() );
					MusicPlayer.AddSoundSample( shorts );
				}
			}
				
			CalculateThroughput( data.Length );
		};

		WebSocket.OnDisconnected += async (_, _1) =>
		{
			Log.Info( "Lost connection with the backend. Retrying connection in 5 seconds..." );
			await RetryConnection();
		};

		WebSocket.OnMessageReceived += ( msg ) =>
		{
			string[] messageSplit = msg.Split( " " );
			string msgId = messageSplit[0];

			MessageReceived?.Invoke( messageSplit );

			if ( msgId == "convert_success" )
			{
				string id = messageSplit[1];
				StreamSuccess( id );
			}

			if ( msgId == "error" )
			{
				string error = string.Join( " ", messageSplit.Skip( 1 ) );
				Log.Error( error );
			}
				
			if ( msgId == "audio_progress" )
			{
				string rest = string.Join( " ", messageSplit.Skip( 1 ));
				AudioProgress( rest );
			}
			
			if ( msgId == "stream_end" )
			{
				MusicPlayer.IsStreaming = false;
			}

			if ( msgId == "join_success" )
			{
				string rest = string.Join( " ", messageSplit.Skip( 1 ));
				var video = JsonSerializer.Deserialize<AudioData>(rest);
					
				Log.Info( "Successfully joined, starting!" );
				MusicPlayer.AudioData = video;
				MusicPlayer.Ready();
			}
		};
	}

	private async Task RetryConnection()
	{
		int retryNum = 1;

		while ( !WebSocket.IsConnected )
		{
			await Task.Delay(5000);
				
			Log.Info($"Trying to reconnect to Audio Backend... Attempt number {retryNum}");
			try
			{
				WebSocket = new WebSocket();
				await WebSocket.Connect( WebSocketURL );
			}
			catch ( Exception )
			{
				// ignored
			}

			retryNum++;
		}
		
		Log.Info( "Successfully reconnected to Audio Backend!" );
	}

	/// <summary>
	/// Request a video (serverside)
	/// </summary>
	public void RequestAudioURL(string url)
	{
		if ( !WebSocket.IsConnected ) return;
		WebSocket.Send( $"stream_request_audio {url}" );
	}
		
	/// <summary>
	/// Join video stream (clientside)
	/// </summary>
	public void JoinAudioStream(string id)
	{
		if ( !WebSocket.IsConnected ) return;
		WebSocket.Send( $"stream_join_audio {id}" );
	}
}
