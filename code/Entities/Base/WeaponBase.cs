using System.Collections.Generic;
using Sandbox;
using TowerResort.Player;

namespace TowerResort.Entities.Base;

public partial class WeaponBase : CarriableEntityBase, IUse
{
	public override string ViewModelPath => "";
	public virtual string WorldModelPath => "";
	public new WeaponViewModel ViewModelEntity { get; protected set; }
	public Model WorldModel => Model.Load( WorldModelPath );
	public virtual float PrimaryRate => 5.0f;
	public virtual float SecondaryRate => 15.0f;
	public virtual CitizenAnimationHelper.HoldTypes HoldType => CitizenAnimationHelper.HoldTypes.Pistol;
	public virtual bool PrimaryAuto => false;
	public virtual bool SecondaryAuto => false;
	public virtual float BaseDamage => 15.0f;
	public virtual float BaseRange => 65.0f;
	public virtual float TimeToReload => 1.45f;
	public virtual float TimeToDeploy => 0.95f;
	public MainPawn Player => Owner as MainPawn;
	[Net, Predicted] public bool IsReloading { get; set; }
	[Net, Predicted] public TimeSince TimeSinceDeploy { get; set; }
	[Net, Predicted] public TimeSince TimeSincePrimaryAttack { get; set; }
	[Net, Predicted] public TimeSince TimeSinceSecondaryAttack { get; set; }
	[Net, Predicted] public TimeSince TimeSinceReload { get; set; }
	[Net, Predicted] public int AmmoClip { get; set; }
	public virtual int ClipSize => 1;

	public override void Spawn()
	{
		base.Spawn();

		AmmoClip = ClipSize;

		Model = WorldModel;
		Tags.Add( "item" );
	}

	public virtual bool CanPrimaryAttack()
	{
		if ( !Player.IsValid() ) return false;
		if ( IsReloading ) return false;
		if ( TimeSinceDeploy <= TimeToDeploy ) return false;

		if ( Player is LobbyPawn lobbyPlayer && lobbyPlayer.FocusedEntity != null ) return false;

		if ( PrimaryAuto )
		{
			if ( !Input.Down( InputButton.PrimaryAttack ) ) return false;
		}
		else
		{
			if ( !Input.Pressed( InputButton.PrimaryAttack ) ) return false;
		}

		var rate = PrimaryRate;
		if ( rate <= 0 ) return true;

		return TimeSincePrimaryAttack > (1 / rate);
	}

	public virtual bool CanSecondaryAttack()
	{
		if ( !Player.IsValid() ) return false;
		if ( IsReloading ) return false;
		if ( TimeSinceDeploy <= TimeToDeploy ) return false;

		if ( Player is LobbyPawn lobbyPlayer && lobbyPlayer.FocusedEntity != null ) return false;

		if ( SecondaryAuto )
		{
			if ( !Input.Down( InputButton.SecondaryAttack ) ) return false;
		}
		else
		{
			if ( !Input.Pressed( InputButton.SecondaryAttack ) ) return false;
		}

		var rate = SecondaryRate;
		if ( rate <= 0 ) return true;

		return TimeSinceSecondaryAttack > (1 / rate);
	}

	public virtual bool CanReload()
	{
		if ( TimeSinceDeploy <= TimeToDeploy ) return false;
		if ( AmmoClip == ClipSize ) return false;
		if ( IsReloading ) return false;
		if ( !Player.IsValid() || !Input.Down( InputButton.Reload ) ) return false;

		return true;
	}

	public virtual void Reload()
	{
		IsReloading = true;
		TimeSinceReload = 0.0f;
	}

	public void DoReloading()
	{
		if ( TimeSinceReload >= TimeToReload )
			FinishReload();
	}

	public virtual void FinishReload()
	{
		IsReloading = false;
		AmmoClip = ClipSize;
	}

	public void OnDeploy( MainPawn player )
	{
		SetParent( player, true );
		Owner = player;

		TimeSinceDeploy = 0;
		EnableDrawing = true;
		EnableShadowInFirstPerson = false;

		if ( Game.IsServer )
			CreateViewModel( To.Single( player ), ViewModelPath );
	}

	public void OnHolster( MainPawn player )
	{
		EnableDrawing = false;

		if ( Game.IsServer )
		{
			DestroyViewmodel( To.Single( player ) );
		}
	}

	public override void ActiveStart( Entity ent )
	{
		EnableDrawing = true;
	}

	public override void Simulate( IClient player )
	{
		if ( IsReloading )
		{
			DoReloading();
			return;
		}

		if ( CanReload() )
		{
			Reload();
		}

		if ( !Owner.IsValid() )
			return;

		if ( CanPrimaryAttack() )
		{
			using ( LagCompensation() )
			{
				TimeSincePrimaryAttack = 0;
				AttackPrimary();
			}
		}

		if ( !Owner.IsValid() )
			return;

		if ( CanSecondaryAttack() )
		{
			using ( LagCompensation() )
			{
				TimeSinceSecondaryAttack = 0;
				AttackSecondary();
			}
		}
	}

	public override void SimulateAnimator( CitizenAnimationHelper anim )
	{
		anim.HoldType = HoldType;
		anim.Handedness = CitizenAnimationHelper.Hand.Both;
		anim.AimBodyWeight = 1.0f;
		anim.AimHeadWeight = 1.0f;
		anim.AimEyesWeight = 1.0f;
	}

	public virtual void DryFire()
	{

	}

	public virtual void AttackPrimary()
	{
	}

	public virtual void AttackSecondary()
	{
	}

	[ClientRpc]
	public void DestroyViewmodel()
	{
		ViewModelEntity?.Delete();
		ViewModelEntity = null;
	}

	[ClientRpc]
	public void CreateViewModel(string modelPath)
	{
		var vm = new WeaponViewModel( this );
		vm.Model = Model.Load( modelPath );
		ViewModelEntity = vm;

		ViewModelEntity.Owner = Owner;
		ViewModelEntity.Position = Position;
	}

	public override Sound PlaySound( string soundName, string attachment )
	{
		if ( Owner.IsValid() )
			return Owner.PlaySound( soundName, attachment );

		return base.PlaySound( soundName, attachment );
	}

	public virtual bool OnUse( Entity user )
	{
		if ( user is MainPawn pawn )
			pawn.Inventory.AddItem( this );

		return false;
	}

	public virtual IEnumerable<TraceResult> TraceBullet( Vector3 start, Vector3 end, float radius = 2.0f )
	{
		bool underWater = Trace.TestPoint( start, "water" );

		var trace = Trace.Ray( start, end )
				.UseHitboxes()
				.WithAnyTags( "solid", "player", "npc" )
				.Ignore( Owner )
				.Size( radius );

		if ( !underWater )
			trace = trace.WithAnyTags( "water" );

		var tr = trace.Run();

		if ( tr.Hit )
			yield return tr;
	}

	public virtual void ShootBullet( float spread, float force, float damage, float bulletSize, int bulletCount = 1 )
	{
		//
		// Seed rand using the tick, so bullet cones match on client and server
		//
		Game.SetRandomSeed( Time.Tick );
		var aim = Owner.AimRay;

		for ( int i = 0; i < bulletCount; i++ )
		{
			var forward = aim.Forward;
			forward += (Vector3.Random + Vector3.Random + Vector3.Random + Vector3.Random) * spread * 0.25f;
			forward = forward.Normal;

			//
			// ShootBullet is coded in a way where we can have bullets pass through shit
			// or bounce off shit, in which case it'll return multiple results
			//
			foreach ( var tr in TraceBullet( aim.Position, aim.Position + forward * 5000, bulletSize ) )
			{
				tr.Surface.DoBulletImpact( tr );

				if ( !Game.IsServer ) continue;
				if ( !tr.Entity.IsValid() ) continue;

				var damageInfo = DamageInfo.FromBullet( tr.EndPosition, forward * force, damage )
					.UsingTraceResult( tr )
					.WithAttacker( Owner )
					.WithWeapon( this );

				tr.Entity.TakeDamage( damageInfo );
			}
		}
	}

	public virtual bool IsUsable( Entity user )
	{
		return Owner == null;
	}
}

