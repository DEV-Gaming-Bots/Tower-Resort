global using Sandbox;
global using System;
global using System.Linq;
global using System.Threading.Tasks;
global using System.Collections.Generic;
global using TowerResort.Achievements;
global using TowerResort.Entities.CondoItems;
global using TowerResort.Player;
global using TowerResort.UI;

namespace TowerResort;

public partial class TRGame : GameManager
{
	public static TRGame Instance => Current as TRGame;

	public List<WebSocket> VideoSockets { get; set; } = new();

	[Net] public bool RunningDedi { get; private set; }

	public TRGame()
	{
		if (Game.IsServer)
		{
			RunningDedi = Game.IsDedicatedServer;
		}

		if ( Game.IsClient )
		{
			_ = new BaseHud();
		}
	}

	[Event.Hotload]
	public void HotloadGame()
	{
		if ( Game.IsServer )
		{

		}

		if ( Game.IsClient )
		{
			_ = new BaseHud();
		}
	}

	public override void DoPlayerDevCam( IClient client )
	{
		Game.AssertServer();

		if ( !DevIDs.Contains( client.SteamId ) )
			return;

		var player = client.Pawn as MainPawn;

		var camera = client.Components.Get<DevCamera>( true );

		if ( camera == null )
		{
			camera = new DevCamera();
			client.Components.Add( camera );
			return;
		}

		camera.Enabled = !camera.Enabled;
	}

	public async Task WaitDelay(float seconds)
	{
		await Task.DelayRealtimeSeconds( seconds );
	}

	//Since games can't be set to dedicated servers only YET
	//we'll kick anyone who tries to host this unofficially or not in dev mode
	public async void ForceShutdown()
	{
		TRChat.AddChatEntryStatic( To.Single( Game.Clients.First() ), "SERVER", 
			"You are hosting this unofficially, please play the official servers" );

		await WaitDelay( 8.0f );

		if ( Game.IsServerHost )
			Game.Clients.First().Kick();
	}

	public override void PostLevelLoaded()
	{
		base.PostLevelLoaded();
	}

	public override void ClientJoined( IClient cl )
	{
		base.ClientJoined( cl );

		//Get all clients and remove the new client
		var clients = Game.Clients.ToList();
		clients.Remove( cl );

		//Display to current clients
		TRChat.AddChatEntryStatic( To.Multiple( clients ), "SERVER", $"{cl.Name} has connected" );

		var pawn = new LobbyPawn(cl.IsUsingVr);
		cl.Pawn = pawn;
		pawn.SetUpPlayerStats();

		if ( !LoadSave( cl ) )
			pawn.NewStats();

		if( !Game.IsDedicatedServer && !DevIDs.Contains(cl.SteamId) )
			ForceShutdown();

	}

	public override void ClientDisconnect( IClient cl, NetworkDisconnectionReason reason )
	{
		//If the pawn is a lobby type...
		if(cl.Pawn is LobbyPawn lobbyPawn)
		{
			//And they left while having a condo, unclaim it
			if( lobbyPawn.AssignedCondo != null )
				lobbyPawn.UnclaimCondo();
		}

		DoSave( cl );

		TRChat.AddChatEntryStatic( To.Everyone, "SERVER", $"{cl.Name} has disconnected: {reason}" );
		base.ClientDisconnect( cl, reason );
	}

	public override void Shutdown()
	{
		foreach ( IClient client in Game.Clients )
			DoSave( client );

		if( DataSocket != null && DataSocket.IsConnected )
			DataSocket.Dispose();

		base.Shutdown();
	}

	public static void ServerAnnouncement(string message)
	{
		TRChat.AddChatEntryStatic( To.Everyone, "SERVER", message );
	}

	[ClientRpc]
	public void PlaySoundOnClient(string path)
	{
		Sound.FromScreen( path );
	}

	[ConCmd.Server( "tr.audio.request" )]
	public static void RequestAudio( int playable, string url )
	{
		// var ent = FindByIndex( playable );
		// if ( ent != null && ent is TVEntity tvEntity )
		// {
		// 	tvEntity.RequestVideo(url);
		// }

		//TRGame.Instance?.ScreenPlayers[0].RequestVideo( url );
	}
}
