using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace TowerResort;

public interface IPlayerData
{
	string PlayerName { get; }
	long SteamID { get; }
	int Credits { get; set; }
}

public class PlayerData : IPlayerData
{
	public string PlayerName { get; }

	public long SteamID { get; }

	public int Credits { get; set; }
}

public partial class TRGame 
{
	public enum DataSaveEnum
	{
		Flatfile,
		SQL
	}

	public DataSaveEnum SavingType;

	public WebSocket DataSocket { get; private set; }

	string SocketURL => "ws://avbdns.duckdns.org:8080/";

	public void DoSave( IClient cl )
	{
		if( SavingType == DataSaveEnum.Flatfile)
		{
			FileSystem.Data.WriteJson( $"{cl.SteamId}.json", (IPlayerData)cl.Pawn );
			
		}
		else if ( SavingType == DataSaveEnum.SQL)
		{


		}

		JsonSerializerOptions options = new JsonSerializerOptions
		{
			ReadCommentHandling = JsonCommentHandling.Skip,
			PropertyNameCaseInsensitive = true,
			AllowTrailingCommas = true,
			WriteIndented = true
		};

		string contents = JsonSerializer.Serialize( (IPlayerData)cl.Pawn, options );

		Log.Info( contents );

		/*DataSocket.Send( 
			"{\"type\": \"jsonserver\", \"data\": \"users/\", \"method\": \"put\"}"
		);*/

		//DataSocket.OnMessageReceived += ( data ) => ReceiveSaveFile( data );
	}

	void ReceiveSaveFile(string json)
	{
		Log.Info( json );
	}

	public bool LoadSave( IClient cl )
	{
		var data = FileSystem.Data.ReadJson<PlayerData>( $"{cl.SteamId}.json" );

		if ( data == null )
			return false;

		return true;
	}

	void CreateSocket()
	{
		DataSocket = new WebSocket();
	}

	public async Task StartSocket()
	{
		if( DataSocket == null )
			CreateSocket();

		DataSocket.OnMessageReceived += ( data ) =>
		{
			Log.Info( data );
		};

		DataSocket.OnDisconnected += DataSocket_OnDisconnected;

		try
		{
			await DataSocket.Connect( SocketURL );
		}
		catch (Exception) 
		{
			Log.Error( "Failed to connect to Database, retrying" );
			//await RetrySocket();
		}
	}

	private void DataSocket_OnDisconnected( int status, string reason )
	{
		Log.Warning( "Lost connection to Hub Websocket with reason: " + reason );
		DataSocket.Dispose();

		_ = StartSocket();
	}

	/*async Task RetrySocket()
	{
		int retryNum = 1;

		DataSocket.OnMessageReceived += ( data ) =>
		{
			Log.Info( "Hub Database connected" );
			SavingType = DataSaveEnum.SQL;
		};

		DataSocket.OnDisconnected += DataSocket_OnDisconnected;

		while ( !DataSocket.IsConnected )
		{
			await Task.Delay( 5000 );
			
			Log.Info( $"Attempting to connect to database - Attempt {retryNum}" );

			try
			{
				await DataSocket.Connect( SocketURL );
			}
			catch ( Exception )
			{
				
			}

			retryNum++;
		}
	}*/

	public void GetDatabase()
	{
		if ( !DataSocket.IsConnected ) return;
	}
}
