using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using TowerResort.Achievements;
using TowerResort.Entities.Hammer;
using TowerResort.UI;

namespace TowerResort.Player;

public partial class LobbyPawn : MainPawn
{
	[Net] public float Drunkiness { get; set; }

	public TimeSince TimeDrank;
	[Net] public Entity FocusedEntity { get; set; }
	public bool IsSitting;
	[Net] public string CurZoneLocation { get; set; } = "Somewhere";

	public LobbyPawn()
	{

	}

	public LobbyPawn(bool usingVR) : this()
	{
		PlayingInVR = usingVR;
	}

	public override void SetUpPlayerStats()
	{
		base.SetUpPlayerStats(); 
		AddCredits( 69420 );
	}

	TimeSince timeSinceLastFootstep = 0;

	public override void OnAnimEventFootstep( Vector3 pos, int foot, float volume )
	{
		if ( LifeState != LifeState.Alive )
			return;

		if( Game.IsServer )
			AchTracker.UpdateAchievement( typeof( Walkathron ), 1 );

		if ( !Game.IsClient )
			return;

		if ( timeSinceLastFootstep < 0.2f )
			return;

		volume *= FootstepVolume();

		timeSinceLastFootstep = 0;

		var tr = Trace.Ray( pos, pos + Vector3.Down * 20 )
			.Radius( 1 )
			.Ignore( this )
			.Run();

		if ( !tr.Hit ) return;

		tr.Surface.DoFootstep( this, tr, foot, volume );
	}

	public virtual float FootstepVolume()
	{
		return Velocity.WithZ( 0 ).Length.LerpInverse( 0.0f, 200.0f ) * 5.0f;
	}

	public override void Spawn()
	{
		if ( PlayingInVR )
		{
			Tags.Add( "trplayer" );
			SpawnVR();
		}
		else if ( !PlayingInVR )
		{
			base.Spawn();
		}

		Components.Create<LobbyInventory>();
		Drunkiness = 0;

		Tags.Add( "pve" );
		EnableSelfCollisions = false;
	}

	public override void BuildInput()
	{
		InputDirection = Input.AnalogMove;

		var look = Input.AnalogLook;

		var viewAngles = ViewAngles;
		viewAngles += look;
		viewAngles.pitch = viewAngles.pitch.Clamp( -89, 89 );
		ViewAngles = viewAngles.Normal;

		Inventory?.BuildInput();
		ActiveChild?.BuildInput();

		base.BuildInput();
	}

	public override void Respawn()
	{
		base.Respawn();
	}

	public override void Simulate( IClient cl )
	{
		if ( LifeState == LifeState.Dead && Game.IsServer )
		{
			SimulateDead();
			return;
		}

		TickPlayerUse();

		if (Drunkiness > 0.0 && Game.IsServer && LifeState == LifeState.Alive)
		{
			if( Drunkiness >= 100.0f )
			{
				OnKilled();
				Drunkiness = 0;
				return;
			}

			if ( TimeDrank >= 15.0f )
			{
				Drunkiness -= 2.5f * Time.Delta;
				Drunkiness = Drunkiness.Clamp( 0, 100 );
			}
		}

		DoCameraActions();

		if ( PlayingInVR )
		{
			SimulateVR( cl );
			return;
		}

		if ( SittingChair != null )
		{
			SimulateSittingOnObject();
		}

		if ( Input.Pressed( InputButton.Slot0 ) )
		{
			Inventory.ActiveWeapon = null;
		}

		if ( FocusedEntity != null && Game.IsServer )
			FocusedEntity.Simulate( cl );

		SimulateActiveChild( ActiveChild );
		SimulateActiveWeapon( Inventory.ActiveWeapon );
		SimulateAnimator();

		CondoSimulate();

		base.Simulate( cl );
	}
	public override void OnKilled()
	{
		if(AssignedCondo != null)
			UnclaimCondo();

		if ( PlayingInVR )
			OnKilledVR();

		base.OnKilled();
	}
	public override void FrameSimulate( IClient cl )
	{
		base.FrameSimulate( cl );

		FrameCamera();

		if ( PlayingInVR )
			FrameVR();
	}
}

