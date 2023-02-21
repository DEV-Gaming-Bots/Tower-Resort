using TowerResort.Entities.Base;

namespace TowerResort.Player;

public partial class LobbyInventory : EntityComponent<MainPawn>, ISingletonComponent
{
	[Net, Predicted] public WeaponBase ActiveWeapon { get; set; }

	[Net] public IList<Entity> Items { get; set; }

	[Net] protected IList<CondoAssetBase> CondoItemAssets { get; set; }

	[Net] public int MaxSlots { get; set; } = 32;
	[Net] public int MaxHotarSlots { get; set; } = 9;

	public int GetSlotCount()
	{
		return CondoItemAssets.Count() + Items.Count();
	}

	public bool AddItem( Entity entity, bool makeActive = true )
	{
		if ( GetSlotCount() >= MaxSlots ) return false;

		if ( entity is WeaponBase wep )
		{
			if ( Items.Contains( wep ) ) return false;
			if ( makeActive )
				SetActiveWeapon( wep );
		} 
	
		Items.Add( entity );

		return true;
	}

	public bool AddItem(CondoAssetBase asset)
	{
		if ( GetSlotCount() >= MaxSlots ) return false;

		CondoItemAssets.Add( asset );

		return true;
	}

	public void ClearInventory()
	{
		foreach ( var ent in Items.ToArray() )
			ent?.Delete();

		Items.Clear();
		CondoItemAssets.Clear();

		ActiveWeapon = null;
	}

	public bool RemoveItem( Entity ent, bool drop = false )
	{
		var success = Items.Remove( ent );
		if ( success && drop )
		{
			// TODO - Drop the weapon on the ground
		}

		return success;
	}

	public bool RemoveItem( CondoAssetBase asset )
	{
		var success = CondoItemAssets.Remove( asset );

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
			Items.ToList()
				.ForEach( x => x.Delete() );
		}
	}

	public Entity GetSlot( int slot )
	{
		return Items.ElementAtOrDefault( slot ) ?? null;
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

	[ConCmd.Server("tr.inventory.select")]
	public static void SelectItemCMD(int index = 0)
	{
		if ( ConsoleSystem.Caller.Pawn == null || ConsoleSystem.Caller.Pawn is not LobbyPawn player )
			return;

		if ( player.Inventory.GetSlotCount() <= 0 ) return;

		player.StartPlacing( player.Inventory.CondoItemAssets[index] );
	}
}
