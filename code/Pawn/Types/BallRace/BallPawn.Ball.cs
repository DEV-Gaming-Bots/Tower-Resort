
namespace TowerResort.Player;
public partial class Ball : ModelEntity
{
	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/gamemodes/ballrace/ball.vmdl" );
		SetupPhysicsFromSphere( PhysicsMotionType.Dynamic, Vector3.Zero, 40f );
		Tags.Add( "trball" );

		Health = 1;

		PhysicsBody.DragEnabled = false;
		PhysicsBody.AngularDamping = 0;
	}

	//Simple ground check
	public bool IsOnGround()
	{
		var tr = Trace.Ray( Position, Position + Vector3.Down * 45 )
			.WorldOnly()
			.Run();

		return tr.Hit;
	}

	protected override void OnPhysicsCollision( CollisionEventData eventData )
	{
		if ( eventData.Speed <= 120.0f ) return;

		//For some reason, if we're moving too fast and on ground the ball gets bumped at random
		//so we'll check if normal.z is -1 and on ground
		if ( eventData.Normal.z == -1 && IsOnGround() ) return;

		PlaySound( "ball_roll" ).SetPitch( MathX.Clamp( eventData.Speed / 150, 0, 1 ) );

		var LastSpeed = Math.Max( eventData.Other.PreVelocity.Length, eventData.Speed );
		var NewVelocity = PhysicsBody.Velocity;
		NewVelocity = NewVelocity.Normal;

		LastSpeed = Math.Max( NewVelocity.Length, LastSpeed );

		var TargetVelocity = NewVelocity * LastSpeed * 0.90f;
		Velocity = TargetVelocity;
	}
}
