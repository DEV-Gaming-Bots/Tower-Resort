using TowerResort;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TowerResort.Player;

public partial class NoclipControl : StandardController
{
	public NoclipControl()
	{
		
	}

	public NoclipControl( MainPawn newOwner ) : this()
	{
		Owner = newOwner;
	}

	public override void Simulate()
	{
		var fwd = Owner.InputDirection.x.Clamp( -1f, 1f );
		var left = Owner.InputDirection.y.Clamp( -1f, 1f );
		var rotation = Owner.ViewAngles.ToRotation();

		var vel = (rotation.Forward * fwd) + (rotation.Left * left);

		if ( Input.Down( InputButton.Jump ) )
		{
			vel += Vector3.Up * 1;
		}

		vel = vel.Normal * 2000;

		if ( Input.Down( InputButton.Run ) )
			vel *= 5.0f;

		if ( Input.Down( InputButton.Duck ) )
			vel *= 0.2f;

		Owner.Velocity += vel * Time.Delta;

		if ( Owner.Velocity.LengthSquared > 0.01f )
		{
			Owner.Position += Owner.Velocity * Time.Delta;
		}

		Owner.Velocity = Owner.Velocity.Approach( 0, Owner.Velocity.Length * Time.Delta * 5.0f );

		Owner.EyeRotation = rotation;
		WishVelocity = Owner.Velocity;
		Owner.GroundEntity = null;
		Owner.BaseVelocity = Vector3.Zero;
	}
}
