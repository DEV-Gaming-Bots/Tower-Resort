using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TowerResort.Player.VR;

namespace TowerResort.Player;

public partial class LobbyPawn
{
	[Net, Local] public LeftHand LeftHand { get; set; }
	[Net, Local] public RightHand RightHand { get; set; }
	
	void CreateHands()
	{
		DeleteHands();

		LeftHand = new() { Owner = this };
		RightHand = new() { Owner = this };

		LeftHand.Other = RightHand;
		RightHand.Other = LeftHand;
	}

	void DeleteHands()
	{
		LeftHand?.Delete();
		RightHand?.Delete();
	}

	public void SpawnVR()
	{
		CreateHands();
		Controller = new VRController( this );
	}

	public void OnKilledVR()
	{
		EnableDrawing = false;
		DeleteHands();
	}

	public void SimulateVR(IClient cl)
	{
		CheckRotate();
		SetVRAnimProperties();

		LeftHand?.Simulate( cl );
		RightHand?.Simulate( cl );
		Controller?.Simulate();
	}

	public void FrameVR()
	{
		Controller?.FrameSimulate();
	}

	public void SetVRAnimProperties()
	{
		if ( LifeState != LifeState.Alive )
			return;

		if ( !Input.VR.IsActive )
			return;

		if ( LeftHand == null || RightHand == null )
			CreateHands();

		SetAnimParameter( "b_vr", true );
		var leftHandLocal = Transform.ToLocal( LeftHand.GetBoneTransform( 0 ) );
		var rightHandLocal = Transform.ToLocal( RightHand.GetBoneTransform( 0 ) );

		var handOffset = Vector3.Zero;
		SetAnimParameter( "left_hand_ik.position", leftHandLocal.Position + (handOffset * leftHandLocal.Rotation) );
		SetAnimParameter( "right_hand_ik.position", rightHandLocal.Position + (handOffset * rightHandLocal.Rotation) );

		SetAnimParameter( "left_hand_ik.rotation", leftHandLocal.Rotation * Rotation.From( 0, 0, 180 ) );
		SetAnimParameter( "right_hand_ik.rotation", rightHandLocal.Rotation );

		float height = Input.VR.Head.Position.z - Position.z;
		SetAnimParameter( "duck", 1.0f - ((height - 32f) / 32f) ); // This will probably need tweaking depending on height
	}

	TimeSince timeSinceLastRotation;

	const float deadzone = 0.2f;
	const float angle = 45f;
	const float delay = 0.25f;

	void CheckRotate()
	{
		if ( !Game.IsServer )
			return;

		float rotate = Input.VR.RightHand.Joystick.Value.x;

		if ( timeSinceLastRotation > delay )
		{
			if ( rotate > deadzone )
			{
				Transform = Transform.RotateAround(
					Input.VR.Head.Position.WithZ( Position.z ),
					Rotation.FromAxis( Vector3.Up, -angle )
				);

				timeSinceLastRotation = 0;
			}
			else if ( rotate < -deadzone )
			{
				Transform = Transform.RotateAround(
					Input.VR.Head.Position.WithZ( Position.z ),
					Rotation.FromAxis( Vector3.Up, angle )
				);

				timeSinceLastRotation = 0;
			}
		}

		if ( rotate > -deadzone && rotate < deadzone )
		{
			timeSinceLastRotation = 10;
		}
	}
}

