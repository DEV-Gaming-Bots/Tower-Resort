using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TowerResort.Entities.Condos;

public partial class BuyableDoor : DoorEntity, IUse
{
	[Net] public int UpgradeCost { get; set; } = 0;

	public Vector3 PositionA;
	public Vector3 PositionB;
	public Rotation RotationA;
	public Rotation RotationB;
	Rotation RotationB_Normal;
	Rotation RotationB_Opposite;

	public override void Spawn()
	{
		base.Spawn();

		Locked = true;

		if ( Model != null )
			SetupPhysicsFromModel( PhysicsMotionType.Static );
	}

	public override bool OnUse( Entity user )
	{
		if(user is LobbyPawn player && Owner == player)
		{
			if ( player.Credits >= UpgradeCost && Locked )
			{
				player.TakeCredits( UpgradeCost );
				Locked = false;
			}
		}

		if ( Locked )
		{
			PlaySound( LockedSound );
			SetAnimParameter( "locked", true );
			OnLockedUse.Fire( this );
			return false;
		}

		if ( SpawnSettings.HasFlag( Flags.UseOpens ) )
		{
			ToggleDoor( user );
		}

		return false;
	}

	public void ToggleDoor( Entity activator = null )
	{
		if ( State == DoorState.Open || State == DoorState.Opening ) CloseDoor( activator );
		else if ( State == DoorState.Closed || State == DoorState.Closing ) OpenDoor( activator );
	}

	public void OpenDoor( Entity activator = null )
	{
		if ( Locked )
		{
			PlaySound( LockedSound );
			SetAnimParameter( "locked", true );
			return;
		}

		if ( State == DoorState.Closed )
		{
			PlaySound( OpenSound );
		}

		if ( State == DoorState.Closed || State == DoorState.Closing ) State = DoorState.Opening;

		if ( activator != null && MoveDirType == DoorMoveType.Rotating && OpenAwayFromPlayer && State != DoorState.Open )
		{
			// TODO: In this case the door could be moving faster than given speed if we are trying to open the door while it is closing from the opposite side
			var axis = Rotation.From( MoveDir ).Up;
			if ( !MoveDirIsLocal ) axis = Transform.NormalToLocal( axis );

			// Generate the correct "inward" direction for the door since we can't assume RotationA.Forward is it
			// TODO: This does not handle non UP axis doors!
			var Dir = (WorldSpaceBounds.Center.WithZ( Position.z ) - Position).Normal;
			var Pos1 = Position + Rotation.FromAxis( Dir, 0 ).RotateAroundAxis( axis, Distance ) * Dir * 24.0f;
			var Pos2 = Position + Rotation.FromAxis( Dir, 0 ).RotateAroundAxis( axis, -Distance ) * Dir * 24.0f;

			var PlyPos = activator.Position;
			if ( PlyPos.Distance( Pos2 ) < PlyPos.Distance( Pos1 ) )
			{
				RotationB = RotationB_Normal;
			}
			else
			{
				RotationB = RotationB_Opposite;
			}
		}

		UpdateAnimGraph( true );

		UpdateState();

		OpenOtherDoors( true, activator );
	}

	public void CloseDoor( Entity activator = null )
	{
		if ( Locked )
		{
			PlaySound( LockedSound );

			SetAnimParameter( "locked", true );
			return;
		}

		if ( State == DoorState.Open )
		{
			PlaySound( CloseSound );
		}

		if ( State == DoorState.Open || State == DoorState.Opening ) State = DoorState.Closing;

		UpdateAnimGraph( false );
		UpdateState();

		OpenOtherDoors( false, activator );
	}

	internal bool ShouldPropagateState = true;

	void OpenOtherDoors( bool open, Entity activator )
	{
		if ( !ShouldPropagateState ) return;

		List<Entity> ents = new();

		if ( !string.IsNullOrEmpty( Name ) ) ents.AddRange( FindAllByName( Name ) );
		if ( OtherDoorsToOpen.TryGetTargets( out Entity[] doors ) ) ents.AddRange( doors );

		foreach ( var ent in ents )
		{
			if ( ent == this || ent is not BuyableDoor ) continue;

			BuyableDoor door = (BuyableDoor)ent;

			door.ShouldPropagateState = false;
			if ( open )
			{
				door.Open( activator );
			}
			else
			{
				door.Close( activator );
			}
			door.ShouldPropagateState = true;
		}
	}

	void UpdateState()
	{
		bool open = (State == DoorState.Opening) || (State == DoorState.Open);

		_ = DoMove( open );
	}

	int movement = 0;
	Sound? MoveSoundInstance = null;
	bool AnimGraphFinished = false;

	async Task DoMove( bool state )
	{
		if ( !MoveSoundInstance.HasValue && !string.IsNullOrEmpty( MovingSound ) )
		{
			MoveSoundInstance = PlaySound( MovingSound );
		}

		var moveId = ++movement;

/*		if ( State == DoorState.Opening )
		{
			_ = OnOpen.Fire( this );
		}
		else if ( State == DoorState.Closing )
		{
			_ = OnClose.Fire( this );
		}*/

		if ( MoveDirType == DoorMoveType.Moving )
		{
			var position = state ? PositionB : PositionA;
			var distance = Vector3.DistanceBetween( LocalPosition, position );
			var timeToTake = distance / Math.Max( Speed, 0.1f );

			var success = await LocalKeyframeTo( position, timeToTake, Ease );

			if ( !success )
				return;
		}
		else if ( MoveDirType == DoorMoveType.Rotating )
		{
			var target = state ? RotationB : RotationA;

			Rotation diff = LocalRotation * target.Inverse;
			var timeToTake = diff.Angle() / Math.Max( Speed, 0.1f );

			var success = await LocalRotateKeyframeTo( target, timeToTake, Ease );
			if ( !success )
				return;
		}
		else if ( MoveDirType == DoorMoveType.AnimatingOnly )
		{
			AnimGraphFinished = false;
			while ( !AnimGraphFinished )
			{
				await Task.Delay( 100 );
			}
		}
		else { Log.Warning( $"{this}: Unknown door move type {MoveDirType}!" ); }

		if ( moveId != movement || !this.IsValid() )
			return;

		if ( State == DoorState.Opening )
		{
			_ = OnFullyOpen.Fire( this );
			State = DoorState.Open;
			PlaySound( FullyOpenSound );
		}
		else if ( State == DoorState.Closing )
		{
			_ = OnFullyClosed.Fire( this );
			State = DoorState.Closed;
			PlaySound( FullyClosedSound );
		}

		if ( MoveSoundInstance.HasValue )
		{
			MoveSoundInstance.Value.Stop();
			MoveSoundInstance = null;
		}

		if ( state && TimeBeforeReset >= 0 )
		{
			await Task.DelaySeconds( TimeBeforeReset );

			if ( moveId != movement || !this.IsValid() )
				return;

			Toggle();
		}
	}

	void UpdateAnimGraph( bool open )
	{
		SetAnimParameter( "open", open );
	}

	protected override void OnAnimGraphTag( string tag, AnimGraphTagEvent fireMode )
	{
		if ( tag == "AnimationFinished" && fireMode != AnimGraphTagEvent.End )
		{
			AnimGraphFinished = true;
		}
	}
}
