using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TowerResort.Entities.Hammer;

[Library("tr_trigger_checkout"), Title("Condo Auto Checkout"), Category( "Trigger" )]
[HammerEntity]
public class CondoAutoCheckout : BaseTrigger
{
	public override void Spawn()
	{
		base.Spawn();

		Transmit = TransmitType.Always;
	}

	public override void ClientSpawn()
	{
		base.ClientSpawn();
	}

	public override void OnTouchStart( Entity toucher )
	{
		if(toucher is LobbyPawn player)
		{
			if ( player.AssignedCondo != null )
				player.UnclaimCondo();
				
		}
	}
}

