using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace TowerResort.UI;
public partial class TowerScoreboardEntry : Panel
{
	public IClient Client;

	public Label PlayerName;
	public Label Value;
	public Label Ping;
	public Image PlayerPicture;

	public TowerScoreboardEntry()
	{
		AddClass( "entry" );

		PlayerPicture = Add.Image( "", "image" );
		PlayerName = Add.Label( "" );
		Value = Add.Label( "", "value" );
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
		PlayerName.Text = Client.Name;
		Value.Text = "50$";
		Ping.Text = Client.Ping.ToString();
		PlayerPicture.SetTexture( $"avatar:{Client.SteamId}" );
		SetClass( "me", Client == Sandbox.Game.LocalClient );
	}

	public virtual void UpdateFrom( IClient client )
	{
		Client = client;
		UpdateData();
	}
}
