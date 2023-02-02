using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TowerResort.Entities.Hammer;

[Library("tr_trigger_vipwall"), Title("VIP Trigger"), Category( "Trigger" )]
[HammerEntity]
public class VIPWall : TeleportVolumeEntity
{
	Particles fogParticle;

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
			if (!player.IsVIP || !TRGame.DevIDs.Contains(player.Client.SteamId) )
				TeleportNonVIP( player );
		}
	}

	public void TeleportNonVIP( LobbyPawn other )
	{
		if ( !Enabled ) return;

		var Targetent = TargetEntity.GetTargets( null ).FirstOrDefault();

		if ( Targetent != null )
		{
			Vector3 offset = Vector3.Zero;
			if ( TeleportRelative )
			{
				offset = other.Position - Position;
			}

			if ( !KeepVelocity ) other.Velocity = Vector3.Zero;

			// Fire the output, before actual teleportation so entity IO can do things like disable a trigger_teleport we are teleporting this entity into
			OnTriggered.Fire( other );

			other.Transform = Targetent.Transform;
			other.Position += offset;
			other.SetViewAngles( Rotation.Angles() );
		}
	}
}

