using Components.NotificationManager;
using Sandbox;
using Sandbox.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using TowerResort.Achievements;
using TowerResort.Entities.Base;
using TowerResort.Entities.Hammer;
using TowerResort.Entities.Weapons;
using TowerResort.Weapons;

namespace TowerResort.Player;

public partial class MainPawn : AnimatedEntity, IPlayerData
{
	[Net, Predicted] public StandardController Controller { get; set; }
	[Net, Predicted] public Entity ActiveChild { get; set; }
	[Net, Predicted] public Entity LastActiveChild { get; set; }
	[ClientInput] public Entity ActiveWeaponInput { get; set; }
	[Net, Predicted] public Entity LastActiveWeapon { get; set; }

	public MainPawn DuelOpponent;

	ClothingContainer clothingContainer;

	public Vector3 EyePosition
	{
		get => Transform.PointToWorld( EyeLocalPosition );
		set => EyeLocalPosition = Transform.PointToLocal( value );
	}

	public Rotation EyeRotation
	{
		get => Transform.RotationToWorld( EyeLocalRotation );
		set => EyeLocalRotation = Transform.RotationToLocal( value );
	}

	[Net, Predicted] public Vector3 EyeLocalPosition { get; set; }
	[Net, Predicted] public Rotation EyeLocalRotation { get; set; }

	public override Ray AimRay => new Ray( EyePosition, EyeRotation.Forward );

	public enum FreezeEnum
	{
		None,
		Movement,
		Animation,
		MoveAndAnim,
		Mouse,
	}

	[Net] public FreezeEnum FreezeMovement { get; set; }
	[ClientInput] public Vector3 InputDirection { get; protected set; }
	[ClientInput] public Angles ViewAngles { get; set; }
	public Angles OriginalViewAngles { get; private set; }

	bool setView;
	Angles setAngles;
	[BindComponent] public LobbyInventory Inventory { get; }

	public AchTracker AchTracker;

	[ConCmd.Server( "noclip" )]
	public static void DoNoclip()
	{
		if ( !TRGame.DevIDs.Contains( ConsoleSystem.Caller.SteamId ) )
			return;

		var player = ConsoleSystem.Caller.Pawn as MainPawn;
		if ( player == null ) return;

		if ( player.Controller is NoclipControl )
		{
			Log.Info( $"{player.Client.Name} Noclip Mode Off" );
			
			if(player is BallPawn)
				player.Controller = new BallController( player );
			else
				player.Controller = new StandardController( player );
		}
		else if (player.Controller is not NoclipControl)
		{
			Log.Info( $"{player.Client.Name} Noclip Mode On" );
			player.Controller = new NoclipControl( player );
		}
		
	}

	[ConCmd.Server( "kill" )]
	public static void KillPawn()
	{
		var caller = ConsoleSystem.Caller.Pawn;

		if ( caller == null ) return;

		if ( caller is LobbyPawn lobby)
			lobby.TakeDamage( DamageInfo.Generic( 9999.0f ) );

		if ( caller is BallPawn ball )
			ball.OnKilled();
	}

	public MainPawn()
	{
		clothingContainer = new();
	}

	public void CreatePhysHull()
	{
		SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, new Vector3( -16, -16, 0 ), new Vector3( 16, 16, 72 ) );

		EnableHitboxes = true;
		EnableLagCompensation = true;
		EnableAllCollisions = true;
	}

	public virtual void SetUpAdmin()
	{
		Clothing glasses = ResourceLibrary.Get<Clothing>( "models/cloth/dealwithitglass/dealwithitglass.clothing" );
		if( !clothingContainer.Has(glasses))
			clothingContainer.Clothing.Add( glasses );

		clothingContainer.DressEntity( this );
	}

	[ClientRpc]
	public void PlaySoundClientside(string sound)
	{
		PlaySound( sound );
	}

	[ClientRpc]
	public void DisplayNotification( string message, float lifeTime )
	{
		BaseHud.Current.NotificationManager.AddNotification( message, NotificationType.Info, lifeTime );
	}

	public virtual void SetUpPlayerStats()
	{
		CreatePhysHull();

		LifeState = LifeState.Alive;
		Health = 100;
		Velocity = Vector3.Zero;

		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;

		FreezeMovement = FreezeEnum.None;

		if ( TRGame.DevIDs.Contains( Client.SteamId ) )
			SetUpAdmin();
	}

	public virtual void MoveToSpawn()
	{
		var spawnpoint = All.OfType<SpawnPoint>().OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

		if ( spawnpoint != null )
		{
			Transform = spawnpoint.Transform;
			ResetInterpolation();
		}
	}

	[ClientRpc]
	public void SetViewAngles(Angles angle)
	{
		setView = true;
		setAngles = angle;
	}

	public override void Spawn()
	{
		AchTracker = Components.Create<AchTracker>();

		Game.AssertServer();

		LifeState = LifeState.Alive;

		Components.Create<LobbyInventory>();

		SetModel( "models/citizen/citizen.vmdl" );

		Controller = new StandardController( this );

		Tags.Add( "trplayer" );
		MoveToSpawn();
		base.Spawn();
	}

	public virtual void Respawn()
	{
		Game.AssertServer();

		Controller = new StandardController( this );

		Inventory.ActiveWeapon = null;

		MoveToSpawn();
		SetUpPlayerStats();
	}

	public void SimulateDead()
	{
		if ( Input.Pressed( InputButton.PrimaryAttack ) )
			Respawn();
	}

	public override void Simulate( IClient cl )
	{
		AchTracker?.Simulate();
		Controller?.Simulate();
		Inventory?.Simulate(cl);
	}

	public override void OnKilled()
	{
		Controller = null;

		EnableHitboxes = false;
		EnableLagCompensation = false;
		EnableAllCollisions = false;

		LifeState = LifeState.Dead;
	}

	protected virtual void SimulateActiveWeapon( Entity child )
	{
		if ( Prediction.FirstTime )
		{
			if ( LastActiveWeapon != child )
			{
				OnActiveWeaponChanged( LastActiveWeapon, child );
				LastActiveWeapon = child;
			}
		}

		if ( !LastActiveWeapon.IsValid() ) return;

		if ( LastActiveWeapon.IsAuthority )
			LastActiveWeapon.Simulate( Client );
	}


	protected virtual void SimulateActiveChild( Entity child )
	{
		if ( Prediction.FirstTime )
		{
			if ( LastActiveChild != child )
			{
				OnActiveChildChanged( LastActiveChild, child );
				LastActiveChild = child;
			}
		}

		if ( !LastActiveChild.IsValid() ) return;

		if ( LastActiveChild.IsAuthority )
			LastActiveChild.Simulate( Client );
	}

	public virtual void OnActiveWeaponChanged( Entity previous, Entity next )
	{
		if ( previous is WeaponBase previousBc )
		{
			previousBc?.OnHolster( this );
		}

		if ( next is WeaponBase nextBc )
		{
			nextBc?.ActiveStart( this );
		}
	}

	public virtual void OnActiveChildChanged( Entity previous, Entity next )
	{
		if ( previous is CarriableEntityBase previousBc )
		{
			previousBc?.ActiveEnd( this, previousBc.Owner != this );
		}

		if ( next is CarriableEntityBase nextBc )
		{
			nextBc?.ActiveStart( this );
		}
	}

	public void SimulateAnimator()
	{
		if ( LifeState == LifeState.Dead ) return;

		var helper = new CitizenAnimationHelper( this );

		if( FreezeMovement == FreezeEnum.Animation || FreezeMovement == FreezeEnum.MoveAndAnim )
		{
			helper.IsGrounded = GroundEntity != null;
			if( Velocity.LengthSquared > 0.1f || Controller.WishVelocity.LengthSquared > 0.1f)
			{
				helper.WithVelocity( Velocity );
				helper.WithWishVelocity( Controller.WishVelocity );
				return;
			}

			helper.WithVelocity( Vector3.Zero );
			helper.WithWishVelocity( Vector3.Zero );
			return;
		}

		if( Client.Components.Get<DevCamera>() != null && !Client.Components.Get<DevCamera>().Enabled )
			helper.WithLookAt( AimRay.Position + AimRay.Forward * 200 );

		helper.WithVelocity( Velocity );
		helper.WithWishVelocity( Controller.WishVelocity );

		helper.DuckLevel = Input.Down( InputButton.Duck ) ? 0.75f : 0.0f;

		helper.IsGrounded = GroundEntity != null;

		Rotation rotation = ViewAngles.ToRotation();
		var idealRotation = Rotation.LookAt( rotation.Forward.WithZ( 0 ), Vector3.Up );
		Rotation = Rotation.Slerp( Rotation, idealRotation, Controller.WishVelocity.Length * Time.Delta * 0.05f );

		if ( LastActiveWeapon is WeaponBase wep )
			wep.SimulateAnimator( helper );
		else
			helper.HoldType = CitizenAnimationHelper.HoldTypes.None;

	}

	public override void BuildInput()
	{
		if ( setView )
		{
			ViewAngles = setAngles;
			setView = false;
		}

		OriginalViewAngles = ViewAngles;

		InputDirection = Input.AnalogMove;

		var look = Input.AnalogLook;

		var viewAngles = ViewAngles;
		viewAngles += look;
		viewAngles.pitch = viewAngles.pitch.Clamp( -89, 89 );
		ViewAngles = viewAngles.Normal;

		Inventory?.BuildInput();
		ActiveChild?.BuildInput();
	}

	public override void FrameSimulate( IClient cl )
	{
		Controller?.FrameSimulate();

		base.FrameSimulate( cl );
	}

	public TraceResult GetEyeTrace( float dist, float size = 1.0f, bool useHitbox = false )
	{
		var tr = Trace.Ray( EyePosition, EyePosition + EyeRotation.Forward * dist )
			.Ignore( this )
			.Size( size )
			.UseHitboxes( useHitbox )
			.Run();

		return tr;
	}
}
