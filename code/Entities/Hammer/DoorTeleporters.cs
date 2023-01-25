using Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TowerResort.Entities.Hammer;

[Library("tr_door")]
[Title("Doorway"), Description("A door that teleports players on use"), Category("Lobby")]
[HammerEntity, Model]
public class DoorTeleporter : ModelEntity, IUse
{
	[Property]
	public EntityTarget TargetDest { get; set; }

	TimeSince timeLastUse;

	[Property]
	public bool IsLocked { get; set; } = false;

	public override void Spawn()
	{
		base.Spawn();
		SetupPhysicsFromModel( PhysicsMotionType.Static );
	}

	public bool IsUsable( Entity user )
	{
		return timeLastUse > 1.5f || !IsLocked;
	}

	public async void OpenDoor(LobbyPawn opener)
	{
		var dest = TargetDest.GetTarget( null );
		timeLastUse = 0;

		if( dest == null )
		{
			Log.Error( Name + " has an invalid destination" );
			return;
		}

		opener.FreezeMovement = MainPawn.FreezeEnum.Movement;
		opener.StartFading( To.Single( opener ), 3.5f, 1.5f, 1.75f);

		await Task.DelayRealtimeSeconds( 2.0f );

		opener.Position = dest.Position;
		opener.ResetInterpolation();
		opener.SetViewAngles( dest.Rotation.Angles() );

		opener.FreezeMovement = MainPawn.FreezeEnum.None;
	}

	public bool OnUse( Entity user )
	{
		if ( !IsUsable( user ) ) return false;

		if(user is LobbyPawn player)
			OpenDoor( player );

		return false;
	}
}

