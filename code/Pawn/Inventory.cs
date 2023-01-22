using TheHub.Entities.Base;
using Sandbox;
using System.Collections.Generic;
using System.Linq;

namespace TheHub.Player;

public partial class Inventory : EntityComponent<MainPawn>, ISingletonComponent
{
	[Net] protected IList<WeaponBase> Weapons { get; set; }
	[Net, Predicted] public WeaponBase ActiveWeapon { get; set; }

	public bool AddWeapon( WeaponBase weapon, bool makeActive = true )
	{
		if ( Weapons.Contains( weapon ) ) return false;

		Weapons.Add( weapon );

		if ( makeActive )
			SetActiveWeapon( weapon );

		return true;
	}

	public void RemoveAllWeapons()
	{
		foreach ( var wep in Weapons.ToArray() )
		{
			wep?.Delete();
		}

		Weapons.Clear();

		ActiveWeapon = null;
	}

	public bool RemoveWeapon( WeaponBase weapon, bool drop = false )
	{
		var success = Weapons.Remove( weapon );
		if ( success && drop )
		{
			// TODO - Drop the weapon on the ground
		}

		return success;
	}

	public void SetActiveWeapon( WeaponBase weapon )
	{
		var currentWeapon = ActiveWeapon;
		if ( currentWeapon.IsValid() )
		{
			currentWeapon.OnHolster( Entity );
			ActiveWeapon = null;
		}

		ActiveWeapon = weapon;

		if(weapon != null)
			weapon?.OnDeploy( Entity );
	}

	protected override void OnDeactivate()
	{
		if ( Game.IsServer )
		{
			Weapons.ToList()
				.ForEach( x => x.Delete() );
		}
	}

	public WeaponBase GetSlot( int slot )
	{
		return Weapons.ElementAtOrDefault( slot ) ?? null;
	}

	protected int GetSlotIndexFromInput( InputButton slot )
	{
		return slot switch
		{
			InputButton.Slot1 => 0,
			InputButton.Slot2 => 1,
			InputButton.Slot3 => 2,
			InputButton.Slot4 => 3,
			InputButton.Slot5 => 4,
			_ => -1
		};
	}

	protected void TrySlotFromInput( InputButton slot )
	{
		if ( Input.Pressed( slot ) )
		{
			Input.SuppressButton( slot );

			if ( GetSlot( GetSlotIndexFromInput( slot ) ) is WeaponBase weapon )
			{
				Entity.ActiveWeaponInput = weapon;
			}

		}
	}

	public void BuildInput()
	{
		TrySlotFromInput( InputButton.Slot1 );
		TrySlotFromInput( InputButton.Slot2 );
		TrySlotFromInput( InputButton.Slot3 );
		TrySlotFromInput( InputButton.Slot4 );
		TrySlotFromInput( InputButton.Slot5 );

		ActiveWeapon?.BuildInput();
	}

	public void Simulate( IClient cl )
	{
		if ( Entity.ActiveWeaponInput != null && ActiveWeapon != Entity.ActiveWeaponInput )
		{
			SetActiveWeapon( Entity.ActiveWeaponInput as WeaponBase );
			Entity.ActiveWeaponInput = null;
		}

		ActiveWeapon?.Simulate( cl );
	}

	public void FrameSimulate( IClient cl )
	{
		ActiveWeapon?.FrameSimulate( cl );
	}
}
