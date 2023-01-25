using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using TowerResort.Player;

namespace TowerResort.UI;
public partial class PlayerEntry : Panel
{
	public IClient Client;

	public Label PlayerName;
	public Label Value;
	public Label Ping;
	public Image PlayerPicture;

	public PlayerEntry()
	{
		AddClass( "entry" );

		PlayerPicture = Add.Image( "", "image" );
		PlayerName = Add.Label( "" );
		Value = Add.Label( "", "location" );
		Ping = Add.Label( "", "ping" );
	}

	RealTimeSince TimeSinceUpdate = 0;

	public override void Tick()
	{
		base.Tick();

		if ( !IsVisible )
			return;

		if ( !Client.IsValid() )
			return;

		if ( TimeSinceUpdate < 0.1f )
			return;

		TimeSinceUpdate = 0;
		UpdateData();
	}

	public virtual void UpdateData()
	{
		LobbyPawn player = Client.Pawn as LobbyPawn;

		PlayerName.Text = Client.Name;
		Value.Text = player.CurZoneLocation;
		Ping.Text = Client.Ping.ToString();
		PlayerPicture.SetTexture( $"avatar:{Client.SteamId}" );
		SetClass( "me", Client == Game.LocalClient );
	}

	public virtual void UpdateFrom( IClient client )
	{
		Client = client;
		UpdateData();
	}
}
