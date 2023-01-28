using Sandbox;

namespace TowerResort.Player;

public class VRController : StandardController
{
	public float Speed => 100.0f;
	public override float Acceleration => 10.0f;
	public override float AirAcceleration => 50.0f;
	public override float FallSoundZ => -30.0f;
	public override float GroundFriction => 4.0f;
	public override float StopSpeed => 100.0f;
	public override float Size => 20.0f;
	public override float DistEpsilon => 0.03125f;
	public override float GroundAngle => 46.0f;
	public override float Bounce => 0.0f;
	public override float MoveFriction => 1.0f;
	public override float StepSize => 18.0f;
	public override float MaxNonJumpVelocity => 140.0f;
	public override float BodyGirth => 6.0f;
	public override float BodyHeight => 72.0f;
	public override float EyeHeight => 64.0f;
	public override float Gravity => 800.0f;
	public override float AirControl => 30.0f;

/*	public override bool Swimming { get; set; } = false;
	public override bool AutoJump { get; set; } = false;*/

	public VRController()
	{
		Duck = new DuckController( this );
		Unstuck = new Unstucker( this );
	}

	public VRController(MainPawn pawn) : this()
	{
		Owner = pawn;
	}

	public virtual void SetBBox( Vector3 mins, Vector3 maxs )
	{
		if ( this.mins == mins && this.maxs == maxs )
			return;

		this.mins = mins;
		this.maxs = maxs;
	}

	/// <summary>
	/// Update the size of the bbox. We should really trigger some shit if this changes.
	/// </summary>
	public virtual void UpdateBBox()
	{
		Transform headLocal = Owner.Transform.ToLocal( Input.VR.Head );
		var girth = BodyGirth * 0.5f;

		var mins = (new Vector3( -girth, -girth, 0 ) + headLocal.Position.WithZ( 0 ) * Owner.Rotation) * Owner.Scale;
		var maxs = (new Vector3( +girth, +girth, BodyHeight ) + headLocal.Position.WithZ( 0 ) * Owner.Rotation) *
				   Owner.Scale;

		Duck.UpdateBBox( ref mins, ref maxs );
		SetBBox( mins, maxs );
	}

	public override void FrameSimulate()
	{
		base.FrameSimulate();

		Owner.EyeRotation = Input.VR.Head.Rotation;
	}

	public override void Simulate()
	{
		Owner.EyeLocalPosition = Vector3.Up * (EyeHeight * Owner.Scale);
		UpdateBBox();

		Owner.EyeLocalPosition += TraceOffset;
		Owner.EyeRotation = Input.VR.Head.Rotation;

		if ( Unstuck.TestAndFix() )
			return;

		// RunLadderMode

		CheckLadder();
		Swimming = Owner.GetWaterLevel() > 0.6f;

		//
		// Start Gravity
		//
		if ( !Swimming && !IsTouchingLadder )
		{
			Owner.Velocity -= new Vector3( 0, 0, Gravity * 0.5f ) * Time.Delta;
			Owner.Velocity += new Vector3( 0, 0, Owner.BaseVelocity.z ) * Time.Delta;

			Owner.BaseVelocity = Owner.BaseVelocity.WithZ( 0 );
		}

		if ( Input.VR.RightHand.JoystickPress.IsPressed )
		{
			CheckJumpButton();
		}

		// Fricion is handled before we add in any base velocity. That way, if we are on a conveyor,
		//  we don't slow when standing still, relative to the conveyor.
		bool bStartOnGround = Owner.GroundEntity != null;

		if ( bStartOnGround )
		{
			Owner.Velocity = Owner.Velocity.WithZ( 0 );

			if ( Owner.GroundEntity != null )
			{
				ApplyFriction( GroundFriction * SurfaceFriction );
			}
		}

		//
		// Work out wish velocity.. just take input, rotate it to view, clamp to -1, 1
		//

		WishVelocity = Vector3.Zero;
		WishVelocity += Input.VR.LeftHand.Joystick.Value.y * Input.VR.Head.Rotation.Forward;
		WishVelocity += Input.VR.LeftHand.Joystick.Value.x * Input.VR.Head.Rotation.Right;

		var inSpeed = WishVelocity.Length.Clamp( 0, 1 );

		if ( !Swimming && !IsTouchingLadder )
		{
			WishVelocity = WishVelocity.WithZ( 0 );
		}

		WishVelocity = WishVelocity.Normal * inSpeed;
		WishVelocity *= GetWishSpeed();

		Duck.PreTick();

		bool bStayOnGround = false;
		if ( Swimming )
		{
			ApplyFriction( 1 );
			WaterMove();
		}
		else if ( IsTouchingLadder )
		{
			LadderMove();
		}
		else if ( Owner.GroundEntity != null )
		{
			bStayOnGround = true;
			WalkMove();
		}
		else
		{
			AirMove();
		}

		CategorizePosition( bStayOnGround );

		if ( !Swimming && !IsTouchingLadder )
			Owner.Velocity -= new Vector3( 0, 0, Gravity * 0.5f ) * Time.Delta;

		if ( Owner.GroundEntity != null )
			Owner.Velocity = Owner.Velocity.WithZ( 0 );

		if ( Debug )
		{
			DebugOverlay.Box( Owner.Position + TraceOffset, mins, maxs, Color.Red );
			DebugOverlay.Box( Owner.Position, mins, maxs, Color.Blue );

			var lineOffset = 0;
			if ( Game.IsServer ) lineOffset = 10;

			DebugOverlay.ScreenText( $"        Position: {Owner.Position}", lineOffset + 0 );
			DebugOverlay.ScreenText( $"        Velocity: {Owner.Velocity}", lineOffset + 1 );
			DebugOverlay.ScreenText( $"    BaseVelocity: {Owner.BaseVelocity}", lineOffset + 2 );
			DebugOverlay.ScreenText( $"    GroundEntity: {Owner.GroundEntity} [{Owner.GroundEntity?.Velocity}]", lineOffset + 3 );
			DebugOverlay.ScreenText( $" SurfaceFriction: {SurfaceFriction}", lineOffset + 4 );
			DebugOverlay.ScreenText( $"    WishVelocity: {WishVelocity}", lineOffset + 5 );
		}
	}

	public override void WalkMove()
	{
		var wishdir = WishVelocity.Normal;
		var wishspeed = WishVelocity.Length;

		WishVelocity = WishVelocity.WithZ( 0 );
		WishVelocity = WishVelocity.Normal * wishspeed;

		Owner.Velocity = Owner.Velocity.WithZ( 0 );
		Accelerate( wishdir, wishspeed, 0, Acceleration );
		Owner.Velocity = Owner.Velocity.WithZ( 0 );

		// Add in any base velocity to the current velocity.
		Owner.Velocity += Owner.BaseVelocity;

		try
		{
			if ( Owner.Velocity.Length < 1.0f )
			{
				Owner.Velocity = Vector3.Zero;
				return;
			}

			// first try just moving to the destination
			var dest = (Owner.Position + Owner.Velocity * Time.Delta).WithZ( Owner.Position.z );

			var pm = TraceBBox( Owner.Position, dest );

			if ( pm.Fraction == 1 )
			{
				Owner.Position = pm.EndPosition;
				StayOnGround();
				return;
			}

			StepMove();
		}
		finally
		{
			// Now pull the base velocity back out.   Base velocity is set if you are on a moving object, like a conveyor (or maybe another monster?)
			Owner.Velocity -= Owner.BaseVelocity;
		}

		StayOnGround();
	}

	public override void StepMove()
	{
		MoveHelper mover = new MoveHelper( Owner.Position, Owner.Velocity );
		mover.Trace = mover.Trace.Size( mins, maxs ).Ignore( Owner );
		mover.MaxStandableAngle = GroundAngle;

		mover.TryMoveWithStep( Time.Delta, StepSize );

		Owner.Position = mover.Position;
		Owner.Velocity = mover.Velocity;
	}

	public override void Move()
	{
		MoveHelper mover = new MoveHelper( Owner.Position, Owner.Velocity );
		mover.Trace = mover.Trace.Size( mins, maxs ).Ignore( Owner );
		mover.MaxStandableAngle = GroundAngle;

		mover.TryMove( Time.Delta );

		Owner.Position = mover.Position;
		Owner.Velocity = mover.Velocity;
	}

	/// <summary>
	/// Add our wish direction and speed onto our velocity
	/// </summary>
	public override void Accelerate( Vector3 wishdir, float wishspeed, float speedLimit, float acceleration )
	{
		// This gets overridden because some games (CSPort) want to allow dead (observer) players
		// to be able to move around.

		if ( speedLimit > 0 && wishspeed > speedLimit )
			wishspeed = speedLimit;

		// See if we are changing direction a bit
		var currentspeed = Owner.Velocity.Dot( wishdir );

		// Reduce wishspeed by the amount of veer.
		var addspeed = wishspeed - currentspeed;

		// If not going to add any speed, done.
		if ( addspeed <= 0 )
			return;

		// Determine amount of acceleration.
		var accelspeed = acceleration * Time.Delta * wishspeed * SurfaceFriction;

		// Cap at addspeed
		if ( accelspeed > addspeed )
			accelspeed = addspeed;

		Owner.Velocity += wishdir * accelspeed;
	}

	/// <summary>
	/// Remove ground friction from velocity
	/// </summary>
	public override void ApplyFriction( float frictionAmount = 1.0f )
	{
		// Calculate speed
		var speed = Owner.Velocity.Length;
		if ( speed < 0.1f ) return;

		// Bleed off some speed, but if we have less than the bleed
		//  threshold, bleed the threshold amount.
		float control = (speed < StopSpeed) ? StopSpeed : speed;

		// Add the amount to the drop amount.
		var drop = control * Time.Delta * frictionAmount;

		// scale the velocity
		float newspeed = speed - drop;
		if ( newspeed < 0 ) newspeed = 0;

		if ( newspeed != speed )
		{
			newspeed /= speed;
			Owner.Velocity *= newspeed;
		}
	}

	public override void CheckJumpButton()
	{
		// If we are in the water most of the way...
		if ( Swimming )
		{
			ClearGroundEntity();

			Owner.Velocity = Owner.Velocity.WithZ( 100 );
			return;
		}

		if ( Owner.GroundEntity == null )
			return;

		ClearGroundEntity();

		float flGroundFactor = 1.0f;
		float flMul = 268.3281572999747f * 1.2f;
		float startz = Owner.Velocity.z;

		if ( Duck.IsActive )
			flMul *= 0.8f;

		Owner.Velocity = Owner.Velocity.WithZ( startz + flMul * flGroundFactor );

		Owner.Velocity -= new Vector3( 0, 0, Gravity * 0.5f ) * Time.Delta;

		AddEvent( "jump" );
	}

	bool IsTouchingLadder = false;
	Vector3 LadderNormal;
}
