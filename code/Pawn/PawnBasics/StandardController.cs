
using System;
using System.Collections.Generic;
using TheHub;
using Sandbox;
using TheHub.Achievements;

namespace TheHub.Player;

public partial class StandardController : BaseNetworkable
{
	[ConVar.Replicated( "debug_controller" )]
	public static bool Debug { get; set; } = false;

	internal HashSet<string> Events;
	internal HashSet<string> Tags;
	public virtual bool EnableSprinting { get; set; } = true;
	public virtual float SprintSpeed { get; set; } = 320.0f;
	public virtual float WalkSpeed { get; set; } = 150.0f;
	public virtual float DefaultSpeed { get; set; } = 190.0f;
	public virtual float Acceleration { get; set; } = 10.0f;
	public virtual float AirAcceleration { get; set; } = 50.0f;
	public virtual float FallSoundZ { get; set; } = -30.0f;
	public virtual float GroundFriction { get; set; } = 4.0f;
	public virtual float StopSpeed { get; set; } = 100.0f;
	public virtual float Size { get; set; } = 20.0f;
	public virtual float DistEpsilon { get; set; } = 0.03125f;
	public virtual float GroundAngle { get; set; } = 46.0f;
	public virtual float Bounce { get; set; } = 0.0f;
	public virtual float MoveFriction { get; set; } = 1.0f;
	public virtual float StepSize { get; set; } = 18.0f;
	public virtual float MaxNonJumpVelocity { get; set; } = 140.0f;
	public virtual float BodyGirth { get; set; } = 32.0f;
	public virtual float BodyHeight { get; set; } = 72.0f;
	public virtual float EyeHeight { get; set; } = 64.0f;
	public virtual float Gravity { get; set; } = 800.0f;
	public virtual float AirControl { get; set; } = 30.0f;
	public bool Swimming { get; set; } = false;
	public bool AutoJump { get; set; } = false;
	[Net] public MainPawn Owner { get; protected set; }
	public Vector3 GroundNormal { get; set; }
	public Vector3 TraceOffset;
	public Vector3 WishVelocity { get; set; }

	public DuckController Duck;
	public Unstuck Unstuck;

	public StandardController()
	{
		Duck = new DuckController( this );
		Unstuck = new Unstuck( this );
	}

	public StandardController( MainPawn newOwner ) : this()
	{
		Owner = newOwner;
	}

	public BBox GetHull()
	{
		var girth = BodyGirth * 0.5f;
		var mins = new Vector3( -girth, -girth, 0 );
		var maxs = new Vector3( +girth, +girth, BodyHeight );

		return new BBox( mins, maxs );
	}

	protected Vector3 mins;
	protected Vector3 maxs;

	public virtual void SetCollisions( Vector3 mins, Vector3 maxs )
	{
		if ( this.mins == mins && this.maxs == maxs )
			return;

		this.mins = mins;
		this.maxs = maxs;
	}

	public virtual void UpdateCollisions()
	{
		var girth = BodyGirth * 0.5f;

		var mins = new Vector3( -girth, -girth, 0 ) * Owner.Scale;
		var maxs = new Vector3( +girth, +girth, BodyHeight ) * Owner.Scale;

		Duck.UpdateBBox( ref mins, ref maxs );

		SetCollisions( mins, maxs );
	}

	protected float SurfaceFriction;


	public virtual void FrameSimulate()
	{
		Owner.EyeRotation = Owner.ViewAngles.ToRotation();
	}

	public virtual void Simulate()
	{
		Owner.EyeLocalPosition = Vector3.Up * (EyeHeight * Owner.Scale);
		UpdateCollisions();

		Owner.EyeLocalPosition += TraceOffset;

		if ( Owner.Client.IsBot )
			Owner.EyeRotation = Owner.ViewAngles.WithYaw( Owner.ViewAngles.yaw + 180f ).ToRotation();
		else
			Owner.EyeRotation = Owner.ViewAngles.ToRotation();

		if ( Owner.FreezeMovement != MainPawn.FreezeEnum.Movement && Owner.FreezeMovement != MainPawn.FreezeEnum.MoveAndAnim )
		{
			if ( Unstuck.TestAndFix() )
				return;
		}

		CheckLadder();
		Swimming = Owner.GetWaterLevel() > 0.5f;

		if ( !Swimming && !IsTouchingLadder )
		{
			Owner.Velocity -= new Vector3( 0, 0, Gravity * 0.5f ) * Time.Delta;
			Owner.Velocity += new Vector3( 0, 0, Owner.BaseVelocity.z ) * Time.Delta;

			Owner.BaseVelocity = Owner.BaseVelocity.WithZ( 0 );
		}

		if ( AutoJump ? Input.Down( InputButton.Jump ) : Input.Pressed( InputButton.Jump ) )
		{
			CheckJumpButton();
		}

		bool bStartOnGround = Owner.GroundEntity != null;
		if ( bStartOnGround )
		{
			Owner.Velocity = Owner.Velocity.WithZ( 0 );

			if ( Owner.GroundEntity != null )
			{
				ApplyFriction( GroundFriction * SurfaceFriction );
			}
		}

		if( Owner.FreezeMovement != MainPawn.FreezeEnum.Movement && Owner.FreezeMovement != MainPawn.FreezeEnum.MoveAndAnim )
		{
			WishVelocity = new Vector3( Owner.InputDirection.x.Clamp( -1f, 1f ), Owner.InputDirection.y.Clamp( -1f, 1f ), 0 );
			var inSpeed = WishVelocity.Length.Clamp( 0, 1 );
			WishVelocity *= Owner.ViewAngles.WithPitch( 0 ).ToRotation();

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
				SetTag( "climbing" );
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
			{
				Owner.Velocity -= new Vector3( 0, 0, Gravity * 0.5f ) * Time.Delta;
			}

			if ( Owner.GroundEntity != null )
			{
				Owner.Velocity = Owner.Velocity.WithZ( 0 );
			}

		} 
		else
		{
			if ( Owner.GroundEntity == null )
			{
				AirMove();
				CategorizePosition( false );
			}
		}


		if ( Debug )
		{
			DebugOverlay.Box( Owner.Position + TraceOffset, mins, maxs, Color.Red );
			DebugOverlay.Box( Owner.Position, mins, maxs, Color.Blue );

			if ( Game.IsServer )
			{
				DebugOverlay.ScreenText( $"        pl.Position: {Owner.Position}", 0 );
				DebugOverlay.ScreenText( $"        pl.Velocity: {Owner.Velocity}", 1 );
				DebugOverlay.ScreenText( $"    pl.BaseVelocity: {Owner.BaseVelocity}", 2 );
				DebugOverlay.ScreenText( $"    pl.GroundEntity: {Owner.GroundEntity} [{Owner.GroundEntity?.Velocity}]", 3 );
				DebugOverlay.ScreenText( $" SurfaceFriction: {SurfaceFriction}", 4 );
				DebugOverlay.ScreenText( $"    WishVelocity: {WishVelocity}", 5 );
				DebugOverlay.ScreenText( $"    Speed: {Owner.Velocity.Length}", 6 );
			}
		}
	}

	public virtual float GetWishSpeed()
	{
		var ws = Duck.GetWishSpeed();
		if ( ws >= 0 ) return ws;

		if ( Input.Down( InputButton.Run ) && EnableSprinting ) return SprintSpeed;
		if ( Input.Down( InputButton.Walk ) ) return WalkSpeed;

		return DefaultSpeed;
	}

	public virtual void WalkMove()
	{
		var wishdir = WishVelocity.Normal;
		var wishspeed = WishVelocity.Length;

		WishVelocity = WishVelocity.WithZ( 0 );
		WishVelocity = WishVelocity.Normal * wishspeed;

		Owner.Velocity = Owner.Velocity.WithZ( 0 );
		Accelerate( wishdir, wishspeed, 0, Acceleration );
		Owner.Velocity = Owner.Velocity.WithZ( 0 );

		Owner.Velocity += Owner.BaseVelocity;

		try
		{
			if ( Owner.Velocity.Length < 1.0f )
			{
				Owner.Velocity = Vector3.Zero;
				return;
			}

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
			Owner.Velocity -= Owner.BaseVelocity;
		}

		StayOnGround();

		Owner.Velocity = Owner.Velocity.Normal * MathF.Min( Owner.Velocity.Length, GetWishSpeed() );
	}

	public virtual void StepMove()
	{
		MoveHelper mover = new MoveHelper( Owner.Position, Owner.Velocity );
		mover.Trace = mover.Trace.Size( mins, maxs ).Ignore( Owner );
		mover.MaxStandableAngle = GroundAngle;

		mover.TryMoveWithStep( Time.Delta, StepSize );

		Owner.Position = mover.Position;
		Owner.Velocity = mover.Velocity;
	}

	public virtual void Move()
	{
		MoveHelper mover = new MoveHelper( Owner.Position, Owner.Velocity );
		mover.Trace = mover.Trace.Size( mins, maxs ).Ignore( Owner );
		mover.MaxStandableAngle = GroundAngle;

		mover.TryMove( Time.Delta );

		Owner.Position = mover.Position;
		Owner.Velocity = mover.Velocity;
	}

	public virtual void Accelerate( Vector3 wishdir, float wishspeed, float speedLimit, float acceleration )
	{
		if ( speedLimit > 0 && wishspeed > speedLimit )
			wishspeed = speedLimit;

		var currentspeed = Owner.Velocity.Dot( wishdir );

		var addspeed = wishspeed - currentspeed;

		if ( addspeed <= 0 )
			return;

		var accelspeed = acceleration * Time.Delta * wishspeed * SurfaceFriction;

		if ( accelspeed > addspeed )
			accelspeed = addspeed;

		Owner.Velocity += wishdir * accelspeed;
	}

	public virtual void ApplyFriction( float frictionAmount = 1.0f )
	{
		var speed = Owner.Velocity.Length;
		if ( speed < 0.1f ) return;

		float control = (speed < StopSpeed) ? StopSpeed : speed;

		var drop = control * Time.Delta * frictionAmount;

		float newspeed = speed - drop;
		if ( newspeed < 0 ) newspeed = 0;

		if ( newspeed != speed )
		{
			newspeed /= speed;
			Owner.Velocity *= newspeed;
		}
	}

	public virtual void CheckJumpButton()
	{
		if ( Swimming )
		{
			ClearGroundEntity();

			Owner.Velocity = Owner.Velocity.WithZ( 100 );
			return;
		}

		if ( Owner.GroundEntity == null )
			return;

		ClearGroundEntity();

		if(Game.IsServer)
			Owner.AchTracker.UpdateAchievement( typeof( BunnyHopper ), 1 );

		float flGroundFactor = 1.0f;

		float flMul = 268.3281572999747f * 1.2f;

		float startz = Owner.Velocity.z;

		if ( Duck.IsActive )
			flMul *= 0.8f;

		Owner.Velocity = Owner.Velocity.WithZ( startz + flMul * flGroundFactor );

		Owner.Velocity -= new Vector3( 0, 0, Gravity * 0.5f ) * Time.Delta;

		AddEvent( "jump" );

	}

	public virtual void AirMove()
	{
		var wishdir = WishVelocity.Normal;
		var wishspeed = WishVelocity.Length;

		Accelerate( wishdir, wishspeed, AirControl, AirAcceleration );

		Owner.Velocity += Owner.BaseVelocity;

		Move();

		Owner.Velocity -= Owner.BaseVelocity;
	}

	public virtual void WaterMove()
	{
		var wishdir = WishVelocity.Normal;
		var wishspeed = WishVelocity.Length;

		wishspeed *= 0.8f;

		Accelerate( wishdir, wishspeed, 100, Acceleration );

		Owner.Velocity += Owner.BaseVelocity;

		Move();

		Owner.Velocity -= Owner.BaseVelocity;
	}

	bool IsTouchingLadder = false;
	Vector3 LadderNormal;

	public virtual void CheckLadder()
	{
		var wishvel = new Vector3( Owner.InputDirection.x.Clamp( -1f, 1f ), Owner.InputDirection.y.Clamp( -1f, 1f ), 0 );
		wishvel *= Owner.ViewAngles.WithPitch( 0 ).ToRotation();
		wishvel = wishvel.Normal;

		if ( IsTouchingLadder )
		{
			if ( Input.Pressed( InputButton.Jump ) )
			{
				Owner.Velocity = LadderNormal * 100.0f;
				IsTouchingLadder = false;

				return;

			}
			else if ( Owner.GroundEntity != null && LadderNormal.Dot( wishvel ) > 0 )
			{
				IsTouchingLadder = false;

				return;
			}
		}

		const float ladderDistance = 1.0f;
		var start = Owner.Position;
		Vector3 end = start + (IsTouchingLadder ? (LadderNormal * -1.0f) : wishvel) * ladderDistance;

		var pm = Trace.Ray( start, end )
					.Size( mins, maxs )
					.WithTag( "ladder" )
					.Ignore( Owner )
					.Run();

		IsTouchingLadder = false;

		if ( pm.Hit )
		{
			IsTouchingLadder = true;
			LadderNormal = pm.Normal;
		}
	}

	public virtual void LadderMove()
	{
		var velocity = WishVelocity;
		float normalDot = velocity.Dot( LadderNormal );
		var cross = LadderNormal * normalDot;
		Owner.Velocity = (velocity - cross) + (-normalDot * LadderNormal.Cross( Vector3.Up.Cross( LadderNormal ).Normal ));

		Move();
	}


	public virtual void CategorizePosition( bool bStayOnGround )
	{
		SurfaceFriction = 1.0f;

		var point = Owner.Position - Vector3.Up * 2;
		var vBumpOrigin = Owner.Position;

		bool bMovingUpRapidly = Owner.Velocity.z > MaxNonJumpVelocity;
		bool bMovingUp = Owner.Velocity.z > 0;

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

			if ( Owner.Velocity.z > 0 )
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
	public virtual void UpdateGroundEntity( TraceResult tr )
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
	public virtual void ClearGroundEntity()
	{
		if ( Owner.GroundEntity == null ) return;

		Owner.GroundEntity = null;
		GroundNormal = Vector3.Up;
		SurfaceFriction = 1.0f;
	}

	public TraceResult TraceBBox( Vector3 start, Vector3 end, float liftFeet = 0.0f )
	{
		return TraceBBox( start, end, mins, maxs, liftFeet );
	}

	public TraceResult TraceBBox( Vector3 start, Vector3 end, Vector3 mins, Vector3 maxs, float liftFeet = 0.0f )
	{
		if ( liftFeet > 0 )
		{
			start += Vector3.Up * liftFeet;
			maxs = maxs.WithZ( maxs.z - liftFeet );
		}

		var tr = Trace.Ray( start + TraceOffset, end + TraceOffset )
					.Size( mins, maxs )
					.WithAnyTags( "solid", "playerclip", "passbullets", "player" )
					.Ignore( Owner )
					.Run();

		tr.EndPosition -= TraceOffset;
		return tr;
	}

	public virtual void StayOnGround()
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

	public virtual void RunEvents( StandardController additionalController )
	{
		if ( Events == null ) return;

		foreach ( var e in Events )
		{
			OnEvent( e );
			additionalController?.OnEvent( e );
		}
	}

	public virtual void OnEvent( string name )
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
