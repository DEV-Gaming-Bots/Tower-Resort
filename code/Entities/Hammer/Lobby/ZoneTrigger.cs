using Editor;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheHub.Player;
using TheHub.UI;

namespace TheHub.Entities.Hammer;

[Library( "hub_trigger_zone" )]
[Title( "Zone" ), Description( "Defines the area for the hud tracker" ), Category( "Trigger" )]
[SupportsSolid]
[HammerEntity]
public partial class ZoneTrigger : BaseTrigger
{
	[Property, Description( "The name of this zone" )]
	public string ZoneName { get; set; } = "The Unknown";

	public override void Touch( Entity other )
	{
		base.Touch( other );

		if ( other is LobbyPawn player )
			player.ClientLocationUpdate( To.Single( player ), ZoneName );
	}
}

