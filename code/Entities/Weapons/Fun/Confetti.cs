using System;
using Sandbox;
using Sandbox.UI;
using TheHub.Player;
using TheHub.Entities.Base;

namespace TheHub.Weapons;

public partial class Confetti : WeaponBase
{
	public override string ViewModelPath => "";
	public override string WorldModelPath => "";
	public override CitizenAnimationHelper.HoldTypes HoldType => CitizenAnimationHelper.HoldTypes.None;
	public override float PrimaryRate => 0.35f;
	public override float SecondaryRate => 0.0f;
	public override bool PrimaryAuto => false;
	public override bool SecondaryAuto => false;
	public override float BaseDamage => 0.0f;
	public override float BaseRange => 0.0f;
	public override float TimeToReload => 0.0f;
	public override float TimeToDeploy => 0.55f;
	public override int ClipSize => 1;

	public override void Spawn()
	{
		base.Spawn();
	}

	public override void Simulate( IClient player )
	{
		base.Simulate( player );
	}

	public override void Reload()
	{

	}

	public override void FinishReload()
	{
		
	}

	public override void AttackPrimary()
	{
		PlaySound( "confetti" );
		Particles.Create( "particles/confetti/confetti_splash.vpcf", Owner.Position + Vector3.Up * 64 );
	}

	[ClientRpc]
	public void DoFiringEffects()
	{
		//ViewModelEntity?.SetAnimParameter( "fire", true );
		//Particles.Create( "particles/pistol_muzzleflash.vpcf", EffectEntity, "muzzle" );
	}

	[ClientRpc]
	public void DoReloadAnim(bool wasEmpty)
	{
		//ViewModelEntity?.SetAnimParameter( "reload", true );
		//ViewModelEntity?.SetAnimParameter( "b_empty", wasEmpty);
	}

	public override void AttackSecondary()
	{
		
	}
}

