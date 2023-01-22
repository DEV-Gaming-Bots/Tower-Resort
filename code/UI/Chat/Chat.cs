using Sandbox;

namespace TheHub.UI;

public partial class HubChat
{
	[ConCmd.Client( "hub.chat.add", CanBeCalledFromServer = true )]
	public static void AddChatEntry( string name, string message, string playerId = "0", bool isInfo = false )
	{
		Current?.AddEntry( name, message, long.Parse( playerId ), isInfo );

		// Only log clientside if we're not the listen server host
		if ( !Game.IsListenServer )
		{
			Log.Info( $"{name}: {message}" ); 
		}
	}

	public static void AddChatEntryStatic( To target, string name, string message, long playerId = 0, bool isInfo = false )
	{
		AddChatEntry( target, name, message, playerId.ToString(), isInfo );
	}

	[ConCmd.Client( "hub.addinfo", CanBeCalledFromServer = true )]
	public static void AddInformation( string message )
	{
		Current?.AddEntry( null, message );
	}

	[ConCmd.Server( "hub.say" )]
	public static void Say( string message )
	{
		// todo - reject more stuff
		if ( message.Contains( '\n' ) || message.Contains( '\r' ) )
			return;

		Log.Info( $"{ConsoleSystem.Caller}: {message}" );
		AddChatEntryStatic( To.Everyone, ConsoleSystem.Caller.Name, message, ConsoleSystem.Caller.SteamId );
	}
}
