namespace TowerResort.Player;

public partial class BallPawn : MainPawn
{
	[Net] public ModelEntity PlayerBall { get; set; }

	TimeSince timeDied;

	public BallPawn()
	{
		//The ball shouldn't spawn but it does, just delete it
		if ( PlayerBall != null )
		{
			PlayerBall.Delete();
			PlayerBall = null;
		}
	}


	public override void MoveToSpawn()
	{
		var spawnpoint = All.OfType<SpawnPoint>().OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

		if ( spawnpoint != null )
		{
			var tx = spawnpoint.Transform;
			tx.Position = tx.Position + Vector3.Up * 50.0f;
			PlayerBall.Transform = tx;
			PlayerBall.ResetInterpolation();
		}
	}

	public void CreateBall()
	{
		PhysicsClear();

		PlayerBall = new Ball();

		PlayerBall.Owner = this;
		Controller = new BallController( this );

		FreezeMovement = FreezeEnum.MoveAndAnim;
	}

	protected override void OnDestroy()
	{
		if ( Game.IsServer )
		{
			PlayerBall.Delete();
			PlayerBall = null;
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


		if (Game.IsServer)
		{
			if(LifeState == LifeState.Dead)
			{
				if ( timeDied >= 3.5f )
					Respawn();

				return;
			}

			if(PlayerBall == null)
			{
				OnKilled();
				return;
			}

			Position = PlayerBall.Position;
			Velocity = Vector3.Zero;
			PlayerBall.Velocity += Controller.WishVelocity * Time.Delta * Controller.DefaultSpeed;
		}
	}

	public override void FrameSimulate( IClient cl )
	{
		Controller?.FrameSimulate();

		FrameCamera();
	}

	public override void OnKilled()
	{
		Particles.Create( "particles/confetti/confetti_splash.vpcf", Position );

		Controller = null;
		PlayerBall?.Delete();
		PlayerBall = null;

		timeDied = 0;
		LifeState = LifeState.Dead;
	}
}

