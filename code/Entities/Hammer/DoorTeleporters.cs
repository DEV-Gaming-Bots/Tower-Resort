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

	[Property, ResourceType("sound")]
	public string OpenSound { get; set; }

	[Property, ResourceType( "sound" )]
	public string CloseSound { get; set; }

	public TimeSince TimeLastUse;

	[Property]
	public bool IsLocked { get; set; } = false;

	public override void Spawn()
	{
		base.Spawn();
		SetupPhysicsFromModel( PhysicsMotionType.Static );
	}

	public bool IsUsable( Entity user )
	{
		return CanUse(user);
	}

	public virtual bool CanUse( Entity user )
	{
		if ( TimeLastUse < 4.0f ) return false;

		if ( IsLocked ) return false;

		return true;
	}

	public virtual async void OpenDoor(LobbyPawn opener)
	{
		var dest = TargetDest.GetTarget( null );
		TimeLastUse = 0;

		if( dest == null )
		{
			Log.Error( Name + " has an invalid destination" );
			return;
		}

		opener.FreezeMovement = MainPawn.FreezeEnum.Movement;
		opener.StartFading( To.Single( opener ), 3.5f, 1.5f, 1.75f );
		opener.PlaySoundClientside( To.Single( opener ), OpenSound );

		await Task.DelayRealtimeSeconds( 2.25f );

		opener.PlaySoundClientside( To.Single( opener ), CloseSound );

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

