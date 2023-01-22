using System;
using Sandbox;
using Sandbox.UI;
using TheHub.Player;
using TheHub.Entities.Base;

namespace TheHub.Weapons;

public partial class Pistol : WeaponBase
{
	public override string ViewModelPath => "weapons/rust_pistol/v_rust_pistol.vmdl";
	public override string WorldModelPath => "weapons/rust_pistol/rust_pistol.vmdl";
	public override CitizenAnimationHelper.HoldTypes HoldType => CitizenAnimationHelper.HoldTypes.Pistol;
	public override float PrimaryRate => 7.5f;
	public override float SecondaryRate => 0.0f;
	public override bool PrimaryAuto => false;
	public override bool SecondaryAuto => false;
	public override float BaseDamage => 15.0f;
	public override float BaseRange => 65.0f;
	public override float TimeToReload => 3.35f;
	public override float TimeToDeploy => 0.55f;
	public override int ClipSize => 18;

	public override void Spawn()
	{
		base.Spawn();
	}

	public override void Reload()
	{
		base.Reload();

		if ( Game.IsServer )
			DoReloadAnim( To.Single( Player ), false );
	}

	public override void FinishReload()
	{
		base.FinishReload();
	}

	public override void AttackPrimary()
	{
		base.AttackPrimary();

		if(AmmoClip <= 0)
		{
			DryFire();
			return;
		}

		ShootBullet( 0.05f, 25.0f, BaseDamage, 1.0f );
		AmmoClip--;

		if (Game.IsServer)
			DoFiringEffects( To.Single( Player ) );
	}

	[ClientRpc]
	public void DoFiringEffects()
	{
		ViewModelEntity?.SetAnimParameter( "fire", true );
		Particles.Create( "particles/pistol_muzzleflash.vpcf", EffectEntity, "muzzle" );
	}

	[ClientRpc]
	public void DoReloadAnim(bool wasEmpty)
	{
		ViewModelEntity?.SetAnimParameter( "reload", true );
		//ViewModelEntity?.SetAnimParameter( "b_empty", wasEmpty);
	}

	public override void AttackSecondary()
	{
		
	}
}

