using Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TowerResort.Entities.Hammer;

[Library("tr_trigger_teleport"), Category("Triggers")]
[Title("Trigger Teleporter"), Description("Basically just a simple teleport trigger but modified to set viewangles of players")]
[HammerEntity, SupportsSolid]
public class TRTeleporter : TeleportVolumeEntity
{
	public override void OnTouchStart( Entity other )
	{
		base.OnTouchStart( other );

		var Targetent = TargetEntity.GetTargets( null ).FirstOrDefault();

		if ( other is MainPawn player )
			player.SetViewAngles( Targetent.Rotation.Angles() );
	}
}

