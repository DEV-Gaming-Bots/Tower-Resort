global using Sandbox;
global using System;
global using System.Linq;
global using System.Threading.Tasks;
global using TowerResort.Achievements;
global using TowerResort.Entities.CondoItems;
global using TowerResort.Player;
global using TowerResort.UI;

namespace TowerResort;

public partial class TRGame : GameManager
{
	public static TRGame Instance => Current as TRGame;

	[ConVar.Replicated( "tr.devmode" )]
	public static bool DevMode { get; set; }
	public static bool IsDevMode { get; set; }

	public TRGame()
	{
		IsDevMode = DevMode;

		if (Game.IsServer)
		{
			
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

		if ( !AdminIDs.Contains( client.SteamId ) )
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
		HubChat.AddChatEntryStatic( To.Single( Game.Clients.First() ), "SERVER", 
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

		var pawn = new LobbyPawn();
		cl.Pawn = pawn;
		pawn.SetUpPlayerStats();

		/*if ( !LoadSave( cl ) )
		{
			pawn.NewStats();
			//DoSave( cl );
		}*/

		if(!Game.IsDedicatedServer && !DevMode)
			ForceShutdown();
	}

	public override void ClientDisconnect( IClient cl, NetworkDisconnectionReason reason )
	{
		DoSave( cl );

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
		HubChat.AddChatEntryStatic( To.Everyone, "SERVER", message );
	}

	[ClientRpc]
	public void PlaySoundOnClient(string path)
	{
		Sound.FromScreen( path );
	}
}
