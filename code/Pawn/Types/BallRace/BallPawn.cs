using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using TheHub.Entities.Hammer;

namespace TheHub.Player;

public partial class BallPawn : MainPawn
{
	[Net] public ModelEntity Ball { get; set; }

	TimeSince timeDied;

	[ConCmd.Server("kill")]
	public static void KillBall()
	{
		var player = ConsoleSystem.Caller.Pawn as BallPawn;

		if ( player == null ) return;
		if ( player == null || player.LifeState == LifeState.Dead ) return;

		player.OnKilled();
	}

	public BallPawn()
	{
		//The ball shouldn't spawn but it does, just delete it
		if ( Ball != null )
		{
			Ball.Delete();
			Ball = null;
		}
	}


	public override void MoveToSpawn()
	{
		var spawnpoint = All.OfType<SpawnPoint>().OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

		if ( spawnpoint != null )
		{
			var tx = spawnpoint.Transform;
			tx.Position = tx.Position + Vector3.Up * 50.0f;
			Ball.Transform = tx;
			Ball.ResetInterpolation();
		}
	}

	public void CreateBall()
	{
		PhysicsClear();

		Ball = new Ball();

		Ball.Owner = this;
		Controller = new BallController( this );

		FreezeMovement = FreezeEnum.MoveAndAnim;
	}

	protected override void OnDestroy()
	{
		if ( Game.IsServer )
		{
			Ball.Delete();
			Ball = null;
		}

		base.OnDestroy();
	}

	public override void Spawn()
	{
		Game.AssertServer();

		LifeState = LifeState.Alive;
		CreateBall();
		MoveToSpawn();
	}

	public override void Respawn()
	{
		Game.AssertServer();

		LifeState = LifeState.Alive;
		CreateBall();
		MoveToSpawn();
	}

	public override void Simulate( IClient cl )
	{
		Controller?.Simulate();

		Position = Vector3.Zero;
		Velocity = Vector3.Zero;

		if (Game.IsServer)
		{
			if(LifeState == LifeState.Dead)
			{
				if ( timeDied >= 3.5f )
					Respawn();

				return;
			}

			Ball.Velocity += Controller.WishVelocity * Time.Delta * Controller.DefaultSpeed;
		}
	}

	public override void FrameSimulate( IClient cl )
	{
		Controller?.FrameSimulate();

		FrameCamera();
	}

	public override void OnKilled()
	{
		Particles.Create( "particles/confetti/confetti_splash.vpcf", Ball.Position );

		Controller = null;
		Ball.Delete();
		Ball = null;

		timeDied = 0;
		LifeState = LifeState.Dead;
	}
}

