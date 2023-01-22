using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TowerResort.Player;

public partial class Ball : ModelEntity
{
	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/gamemodes/ballrace/ball.vmdl" );
		SetupPhysicsFromSphere( PhysicsMotionType.Dynamic, Vector3.Zero, 40f );

		Tags.Add( "ball" );

		PhysicsBody.DragEnabled = false;
		PhysicsBody.AngularDamping = 0;
	}

	protected override void OnPhysicsCollision( CollisionEventData eventData )
	{
		if ( eventData.Speed <= 120.0f ) return;

		PlaySound( "ball_roll" ).SetPitch( MathX.Clamp( eventData.Speed / 150, 0, 1 ) );

		var LastSpeed = Math.Max( eventData.Other.PreVelocity.Length, eventData.Speed );
		var NewVelocity = PhysicsBody.Velocity;
		NewVelocity = NewVelocity.Normal;

		LastSpeed = Math.Max( NewVelocity.Length, LastSpeed );

		var TargetVelocity = NewVelocity * LastSpeed * 0.85f;
		Velocity = TargetVelocity;
	}
}
