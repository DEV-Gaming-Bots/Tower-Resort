using Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TowerResort.Entities.Hammer;

[Library("tr_door_condo")]
[Title("Condo Doorway"), Description("A door that teleports players on use for condos"), Category("Condo")]
[HammerEntity, Model]
public class CondoDoorTeleporter : DoorTeleporter, IUse
{
	[Property]
	public EntityTarget CondoArea { get; set; }

	public override void Spawn()
	{
		base.Spawn();
		SetupPhysicsFromModel( PhysicsMotionType.Static );
	}

	public override bool CanUse( Entity user )
	{
		/*var targetCondo = CondoArea.GetTarget( null ) as CondoRoom;
		if ( !targetCondo.IsLoaded ) return false;
*/
		return base.CanUse( user );
	}

	public override async void OpenDoor(LobbyPawn opener)
	{
		var targetCondo = CondoArea.GetTarget( null ) as CondoRoom;

		TimeLastUse = 0;

		opener.FreezeMovement = MainPawn.FreezeEnum.Movement;
		opener.StartFading( To.Single( opener ), 3.5f, 1.5f, 1.75f );
		opener.PlaySoundClientside( To.Single( opener ), OpenSound );

		await Task.DelayRealtimeSeconds( 2.25f );

		opener.PlaySoundClientside( To.Single( opener ), CloseSound );

		var exit = (DoorTeleporter)targetCondo.Condo.Children.Where( x => x is DoorTeleporter ).FirstOrDefault();

		opener.Position = exit.Rotation.Forward * 25 + exit.Position;
		opener.ResetInterpolation();
		opener.SetViewAngles( exit.Rotation.Angles() );

		string condoLocation = targetCondo.Name.Replace( "_", " " );

		opener.CurZoneLocation = condoLocation;


		opener.FreezeMovement = MainPawn.FreezeEnum.None;
	}
}

