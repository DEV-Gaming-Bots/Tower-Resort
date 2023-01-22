
using System;
using System.Collections.Generic;
using Sandbox;

namespace TheHub.Player;

public partial class BallController : StandardController
{
	/*
	public const float AirControl = 0.85f; // acceleration multiplier in air
	public const float MaxSpeed = 1100f; // this is the max speed the ball can accelerate to by itself
	*/

	public const float Friction = 0.15f; //0.15f; // resistance multiplier on ground
	public const float Viscosity = 3f; // resistance multiplier in water
	public const float Drag = 0.1f; // resistance multiplier in air
	public const float Bounciness = .35f; // elasticity of collisions, aka how much boing 
	public const float Buoyancy = 2.5f; // floatiness

	public enum GravityEnum
	{
		Default,
		Magnet,
		Manipulated
	}
	[Net, Predicted] public GravityEnum GravityType { get; private set; }

	public override bool EnableSprinting { get; set; } = false;
	public override float DefaultSpeed { get; set; } = 22.75f;
	public override float Acceleration { get; set; } = 1.5f;
	public override float AirAcceleration { get; set; } = 50.0f;
	public override float FallSoundZ { get; set; } = -30.0f;
	public override float GroundFriction { get; set; } = 0.30f;
	public override float StopSpeed { get; set; } = 100.0f;
	public override float Size { get; set; } = 20.0f;
	public override float DistEpsilon { get; set; } = 0.03125f;
	public override float GroundAngle { get; set; } = 46.0f;
	public override float Bounce { get; set; } = 0.0f;
	public override float MoveFriction { get; set; } = 0.30f;
	public override float StepSize { get; set; } = 18.0f;
	public override float MaxNonJumpVelocity { get; set; } = 140.0f;
	public override float BodyGirth { get; set; } = 32.0f;
	public override float BodyHeight { get; set; } = 72.0f;
	public override float EyeHeight { get; set; } = 64.0f;
	public override float Gravity { get; set; } = 800.0f;
	public override float AirControl { get; set; } = 0.85f;
	public float MaxSpeed { get; set; } = 550.0f;

	public Vector3 Velocity { get; set; }

	public BallController()
	{

	}

	public BallController( MainPawn newOwner ) : this()
	{
		Owner = newOwner;
	}

	public new BBox GetHull()
	{
		var girth = BodyGirth * 0.5f;
		var mins = new Vector3( -girth, -girth, 0 );
		var maxs = new Vector3( +girth, +girth, BodyHeight );

		return new BBox( mins, maxs );
	}

	public override void SetCollisions( Vector3 mins, Vector3 maxs )
	{
		if ( this.mins == mins && this.maxs == maxs )
			return;

		this.mins = mins;
		this.maxs = maxs;
	}

	public override void UpdateCollisions()
	{
		/*var girth = BodyGirth * 0.5f;

		var mins = new Vector3( -girth, -girth, 0 ) * Owner.Scale;
		var maxs = new Vector3( +girth, +girth, BodyHeight ) * Owner.Scale;

		SetCollisions( mins, maxs );*/
	}

	public override void FrameSimulate()
	{
		Owner.EyeRotation = Owner.ViewAngles.ToRotation();
	}

	public override void Simulate()
	{
		Owner.EyeLocalPosition = Vector3.Up * (EyeHeight * Owner.Scale);
		UpdateCollisions();

		Owner.EyeLocalPosition += TraceOffset;

		if ( Owner.Client.IsBot )
			Owner.EyeRotation = Owner.ViewAngles.WithYaw( Owner.ViewAngles.yaw + 180f ).ToRotation();
		else
			Owner.EyeRotation = Owner.ViewAngles.ToRotation();

		RestoreGroundPos();
		
		Velocity -= new Vector3( 0, 0, Gravity * 0.5f ) * Time.Delta;
		Velocity += new Vector3( 0, 0, Owner.BaseVelocity.z ) * Time.Delta;

		Owner.BaseVelocity = Owner.BaseVelocity.WithZ( 0 );
		

		bool bStartOnGround = Owner.GroundEntity != null;
		if ( bStartOnGround )
		{
			Velocity = Velocity.WithZ( 0 );

			if ( Owner.GroundEntity != null )
			{
				ApplyFriction( GroundFriction * SurfaceFriction );
			}
		}

		WishVelocity = new Vector3( Owner.InputDirection.x.Clamp( -1f, 1f ), Owner.InputDirection.y.Clamp( -1f, 1f ), 0 );
		var inSpeed = WishVelocity.Length.Clamp( 0, 1 );
		WishVelocity *= Owner.ViewAngles.WithPitch( 0 ).ToRotation();

		WishVelocity = WishVelocity.WithZ( 0 );

		WishVelocity = WishVelocity.Normal * inSpeed;
		WishVelocity *= GetWishSpeed();

		bool bStayOnGround = false;
			
		if ( Owner.GroundEntity != null )
		{
			bStayOnGround = true;
			WalkMove();
		}
		else
		{
			AirMove();
		}

		CategorizePosition( bStayOnGround );
			
		Velocity -= new Vector3( 0, 0, Gravity * 0.5f ) * Time.Delta;
			
		if ( Owner.GroundEntity != null )
		{
			Velocity = Velocity.WithZ( 0 );
		}

		SaveGroundPos();
		


		if ( Debug )
		{
			DebugOverlay.Sphere( Owner.Position + TraceOffset, 40.0f, Color.Red );
			DebugOverlay.Sphere( Owner.Position, 40.0f, Color.Blue );

			if ( Game.IsServer )
			{
				DebugOverlay.ScreenText( $"        pl.Position: {Owner.Position}", 0 );
				DebugOverlay.ScreenText( $"        pl.Velocity: {Velocity}", 1 );
				DebugOverlay.ScreenText( $"    pl.BaseVelocity: {Owner.BaseVelocity}", 2 );
				DebugOverlay.ScreenText( $"    pl.GroundEntity: {Owner.GroundEntity} [{Owner.GroundEntity?.Velocity}]", 3 );
				DebugOverlay.ScreenText( $" SurfaceFriction: {SurfaceFriction}", 4 );
				DebugOverlay.ScreenText( $"    WishVelocity: {WishVelocity}", 5 );
				DebugOverlay.ScreenText( $"    Speed: {Velocity.Length}", 6 );
			}
		}
	}

	public override float GetWishSpeed()
	{
		return DefaultSpeed;
	}

	/*public Vector3 GetGravity()
	{
		if ( GravityType == GravityEnum.Default )
			return PhysicsWorld
		else
			return GravityRotation.Forward * Map.Physics.Gravity.Length * 800f / 360f;
	}*/

	public override void Move()
	{
		BallMoveHelper mover = new BallMoveHelper( Owner.Position, Owner.Velocity, (Owner as BallPawn).Ball );
		mover.Trace = mover.Trace.Size( 40.0f ).Ignore( Owner );
		//mover.MaxStandableAngle = GroundAngle;

		mover.TryMove( Time.Delta );

		Velocity = mover.Velocity;
		Owner.Position = mover.Position;

		/*float dt = Time.Delta;

		MoveHelper mover = new MoveHelper( Owner.Position, Velocity );
		mover.Trace = mover.Trace.Size( mins, maxs ).Ignore( Owner );
		mover.MaxStandableAngle = GroundAngle;

		mover.TryMove( Time.Delta );

		Owner.Position = mover.Position;
		Velocity = mover.Velocity;*

		MoveHelper mover = new MoveHelper( Owner.Position, Velocity );
		//mover.Trace = mover.Trace.Size( mins, maxs ).Ignore( Owner );
		mover.MaxStandableAngle = GroundAngle;

		Vector3 gravityNormal = new Vector3( 0, 0, 800.0f ).Normal;
		Owner.GroundEntity = mover.TraceDirection( gravityNormal * 45 ).Entity;

		Vector3 flatVelocity = Velocity - gravityNormal * Velocity.Dot( gravityNormal );
		float speedFraction = flatVelocity.Length / MaxSpeed;
		if ( speedFraction > 1f )
			speedFraction = 1f;

		TraceResult groundTrace = mover.TraceDirection( gravityNormal * 16f + speedFraction * 24f );
		if ( groundTrace.Hit )
		{
			string surface = groundTrace.Surface.ResourceName;

			//Rotation want = Rotation.LookAt( -groundTrace.Normal, GravityRotation.Up );

			*//*switch ( surface )
			{
				case "magnet":
					GravityType = GravityEnum.Magnet;
					GravityRotation = Rotation.Slerp( GravityRotation, want, Time.Delta * 6f );
					break;
				case "gravity":
					GravityType = GravityEnum.Manipulated;
					GravityRotation = Rotation.Slerp( GravityRotation, want, Time.Delta * 6f );
					break;
				default:
					if ( GravityType != GravityEnum.Manipulated )
					{
						GravityRotation = Rotation.Slerp( GravityRotation, Rotation.LookAt( Vector3.Down ), Time.Delta * 10f );

						GravityType = GravityType.Default;
					}
					break;
			}*//*
		}
		else if ( GravityType == GravityEnum.Magnet )
			GravityType = GravityEnum.Default;

		mover.ApplyFriction( Friction, dt );

		mover.Velocity += 800.0f * dt;

		mover.TryMove( dt );
		//mover.TryUnstuck();

		TraceResult moveTrace = mover.Trace
			.FromTo( mover.Position, mover.Position + mover.Velocity * dt )
			.Run();

		Velocity = mover.Velocity;
		Owner.Position = mover.Position;
		Log.Info( Owner.Position );
*//*
		TraceTriggers( mover.Trace, out bool fallDamage );
		if ( fallDamage && (waterTrace.Hit || moveTrace.Hit) )
		{
			return;
		}*/
	}

	public override void WalkMove()
	{
		var wishdir = WishVelocity.Normal;
		var wishspeed = WishVelocity.Length;

		WishVelocity = WishVelocity.WithZ( 0 );
		WishVelocity = WishVelocity.Normal * wishspeed;

		Velocity = Velocity.WithZ( 0 );
		Accelerate( wishdir, wishspeed, 0, Acceleration );
		Velocity = Velocity.WithZ( 0 );

		Velocity += Owner.BaseVelocity;

		try
		{
			if ( Velocity.Length < 1.0f )
			{
				Velocity = Vector3.Zero;
				return;
			}

			var dest = (Owner.Position + Velocity * Time.Delta).WithZ( Owner.Position.z );

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
			Velocity -= Owner.BaseVelocity;
		}

		StayOnGround();

		Velocity = Velocity.Normal * MathF.Min( Velocity.Length, GetWishSpeed() );
	}

	public override void StepMove()
	{
		MoveHelper mover = new MoveHelper( Owner.Position, Velocity );
		mover.Trace = mover.Trace.Size( 40.0f ).Ignore( Owner );
		mover.MaxStandableAngle = GroundAngle;

		mover.TryMoveWithStep( Time.Delta, StepSize );

		Owner.Position = mover.Position;
		Velocity = mover.Velocity;
	}

/*	public override void Move()
	{
		MoveHelper mover = new MoveHelper( Owner.Position, Velocity );
		mover.Trace = mover.Trace.Size( mins, maxs ).Ignore( Owner );
		mover.MaxStandableAngle = GroundAngle;

		mover.TryMove( Time.Delta );

		Owner.Position = mover.Position;
		Velocity = mover.Velocity;
	}*/

	public override void Accelerate( Vector3 wishdir, float wishspeed, float speedLimit, float acceleration )
	{
		if ( speedLimit > 0 && wishspeed > speedLimit )
			wishspeed = speedLimit;

		var currentspeed = Velocity.Dot( wishdir );

		var addspeed = MaxSpeed - currentspeed;

		if ( addspeed <= 0 )
			return;

		var accelspeed = acceleration * Time.Delta * wishspeed * SurfaceFriction;

		Velocity += wishdir * accelspeed;
	}

	public override void ApplyFriction( float frictionAmount = 1.0f )
	{
		var speed = Velocity.Length;
		if ( speed < 0.1f ) return;

		float control = (speed < StopSpeed) ? StopSpeed : speed;

		var drop = control * Time.Delta * frictionAmount;

		float newspeed = speed - drop;
		if ( newspeed < 0 ) newspeed = 0;

		if ( newspeed != speed )
		{
			newspeed /= speed;
			Velocity *= newspeed;
		}
	}

	public override void AirMove()
	{
		var wishdir = WishVelocity.Normal;
		var wishspeed = WishVelocity.Length;

		Accelerate( wishdir, wishspeed, AirControl, AirAcceleration );

		Velocity += Owner.BaseVelocity;

		Move();

		Velocity -= Owner.BaseVelocity;
	}

	public override void CategorizePosition( bool bStayOnGround )
	{
		SurfaceFriction = 1.0f;

		var point = Owner.Position - Vector3.Up * 2;
		var vBumpOrigin = Owner.Position;

		bool bMovingUpRapidly = Velocity.z > MaxNonJumpVelocity;
		bool bMovingUp = Velocity.z > 0;

		bool bMoveToEndPos = false;

		if ( Owner.GroundEntity != null )
		{
			bMoveToEndPos = true;
			point.z -= StepSize;
		}
		else if ( bStayOnGround )
		{
			bMoveToEndPos = true;
			point.z -= StepSize;
		}

		if ( bMovingUpRapidly || Swimming )
		{
			ClearGroundEntity();
			return;
		}

		var pm = TraceBBox( vBumpOrigin, point, 4.0f );

		if ( pm.Entity == null || Vector3.GetAngle( Vector3.Up, pm.Normal ) > GroundAngle )
		{
			ClearGroundEntity();
			bMoveToEndPos = false;

			if ( Velocity.z > 0 )
				SurfaceFriction = 0.25f;
		}
		else
		{
			UpdateGroundEntity( pm );
		}

		if ( bMoveToEndPos && !pm.StartedSolid && pm.Fraction > 0.0f && pm.Fraction < 1.0f )
		{
			Owner.Position = pm.EndPosition;
		}

	}
	public override void UpdateGroundEntity( TraceResult tr )
	{
		GroundNormal = tr.Normal;

		SurfaceFriction = tr.Surface.Friction * 1.25f;
		if ( SurfaceFriction > 1 ) SurfaceFriction = 1;

		Vector3 oldGroundVelocity = default;
		if ( Owner.GroundEntity != null ) oldGroundVelocity = Owner.GroundEntity.Velocity;

		bool wasOffGround = Owner.GroundEntity == null;

		Owner.GroundEntity = tr.Entity;

		if ( Owner.GroundEntity != null )
		{
			Owner.BaseVelocity = Owner.GroundEntity.Velocity;
		}
	}
	public override void ClearGroundEntity()
	{
		if ( Owner.GroundEntity == null ) return;

		Owner.GroundEntity = null;
		GroundNormal = Vector3.Up;
		SurfaceFriction = 1.0f;
	}

	public override void StayOnGround()
	{
		var start = Owner.Position + Vector3.Up * 2;
		var end = Owner.Position + Vector3.Down * StepSize;

		var trace = TraceBBox( Owner.Position, start );
		start = trace.EndPosition;

		trace = TraceBBox( start, end );

		if ( trace.Fraction <= 0 ) return;
		if ( trace.Fraction >= 1 ) return;
		if ( trace.StartedSolid ) return;
		if ( Vector3.GetAngle( Vector3.Up, trace.Normal ) > GroundAngle ) return;

		Owner.Position = trace.EndPosition;
	}

	void RestoreGroundPos()
	{
		if ( Owner.GroundEntity == null || Owner.GroundEntity.IsWorld )
			return;
	}

	void SaveGroundPos()
	{
		if ( Owner.GroundEntity == null || Owner.GroundEntity.IsWorld )
			return;
	}

	public override void RunEvents( StandardController additionalController )
	{
		if ( Events == null ) return;

		foreach ( var e in Events )
		{
			OnEvent( e );
			additionalController?.OnEvent( e );
		}
	}
	public override void OnEvent( string name )
	{

	}
	public bool HasEvent( string eventName )
	{
		if ( Events == null ) return false;
		return Events.Contains( eventName );
	}

	public bool HasTag( string tagName )
	{
		if ( Tags == null ) return false;
		return Tags.Contains( tagName );
	}

	public void AddEvent( string eventName )
	{
		if ( Events == null ) Events = new HashSet<string>();

		if ( Events.Contains( eventName ) )
			return;

		Events.Add( eventName );
	}

	public void SetTag( string tagName )
	{
		Tags ??= new HashSet<string>();

		if ( Tags.Contains( tagName ) )
			return;

		Tags.Add( tagName );
	}
}
