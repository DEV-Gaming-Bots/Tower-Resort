using TowerResort;
using TowerResort.Player;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TowerResort.Entities.CondoItems;

public struct SitSlots
{
	public int Seats { get; set; }
	public Vector3 StartOffset { get; set; }
	public Vector3 LocalOffset { get; set; }
}

public struct SitTest
{
	public Vector3 Position;
	public Rotation Rotation;
}

public struct LootItem
{
	public CondoAssetBase Item { get; set; }
}

[GameResource( "Condo Item", "citm", "Creates a new Condo Item" )]
public class CondoAssetBase : GameResource
{
	[Category( "Meta Info" )]
	public string Name { get; set; }

	[Category( "Meta Info" )]
	public string Description { get; set; }

	[Category( "Meta Info" ), ResourceType( "png" )]
	public string Icon { get; set; }

	[Category( "Meta Info" )]
	public int BaseCost { get; set; } = 1;

	public enum ShopperType
	{
		Generic,
		Bartender,
		Electronic,
		Furniture,
		FunGames,
	}

	[Category( "Meta Info" ), Description("Which shop keeper should store this item")]
	public ShopperType ShopKeeper { get; set; } = ShopperType.Generic;

	public enum ItemEnum
	{
		Standard,
		Drinkable,
		Sound,
		Light,
		Animated,
		Visual,
		Loot,
		Playable,
		Sittable,
	}

	[Category( "Meta Info" )]
	public ItemEnum Type { get; set; } = ItemEnum.Standard;

	[Category( "Model" ), ResourceType( "vmdl" )]
	public string ModelPath { get; set; }

	[Category("Interaction")]
	public bool IsInteractable { get; set; } = false;

	[Category( "Interaction" )]
	public bool Toggable { get; set; } = false;

	[Category( "Interaction" ), ShowIf("IsInteractable", true)]
	public double InteractCooldown { get; set; } = 1.0f;

	[Category( "Interaction" ), ShowIf( "Toggable", true )]
	public int InteractionBodyGroup { get; set; } = -1;

	[Category( "Interaction" ), Description("Used to revert back to original material after use"), ShowIf( "Toggable", true ), ResourceType( "vmat" )]
	public string InteractionMaterial { get; set; } = "";

	[Category( "Interaction" ), Description( "Used to change material when used" ), ShowIf( "Toggable", true ), ResourceType("vmat")]
	public string InteractionMaterialOverride { get; set; } = "";

	[Category( "Sounds" ), ResourceType( "sound" ), ShowIf("Type", ItemEnum.Sound)]
	public string InteractSound { get; set; }

	[Category( "Animated Functionality" ), ResourceType( "sound" ), ShowIf( "Type", ItemEnum.Animated )]
	public string AnimatingSound { get; set; }

	[Category( "Unboxing Functionality" ), ResourceType( "sound" ), ShowIf( "Type", ItemEnum.Loot )]
	public string UnboxSound { get; set; }

	[Category( "Animated Functionality" ), ShowIf( "Type", ItemEnum.Animated )]
	public string InteractAnimBool { get; set; }

	[Category( "Visual Functionality" ), ShowIf( "Type", ItemEnum.Visual )]
	public float ScreenScale { get; set; } = 1.0f;

	[Category( "Visual Functionality" ), ShowIf( "Type", ItemEnum.Visual )]
	public Vector3 ScreenCenterPoint { get; set; }

	[Category( "Visual Functionality" ), ShowIf( "Type", ItemEnum.Visual )]
	public Rotation ScreenRotation { get; set; }

	[Category( "Unboxing Functionality" ), ShowIf( "Type", ItemEnum.Loot )]
	public LootItem[] LootItems { get; set; }

	[Category( "Unboxing Functionality" ), ResourceType("vpcf"), ShowIf( "Type", ItemEnum.Loot )]
	public string ParticleName { get; set; }
	public enum GameEnum
	{
		Unspecified,
		Poker,
		TicTacToe,
		Checkers
	}

	[Category( "Playable Functionality" ), ShowIf( "Type", ItemEnum.Playable )]
	public GameEnum GameType { get; set; } = GameEnum.Unspecified;

	[Category( "Sitting Functionality" ), ShowIf( "Type", ItemEnum.Sittable )]
	public float SitHeight { get; set; } = 0;
	
	[Category( "Sitting Functionality" ), ShowIf( "Type", ItemEnum.Sittable )]
	public SitTest[] Seats { get; set; }

	[Category( "Drinking Functionality" ), ShowIf( "Type", ItemEnum.Drinkable )]
	public bool IsAlcohol { get; set; } = false;
	
	[Category( "Drinking Functionality" ), ShowIf( "Type", ItemEnum.Drinkable )]
	public int TotalDrinkUses { get; set; } = 1;

	[Category( "Drinking Functionality" ), ShowIf( "Type", ItemEnum.Drinkable ), ResourceType("sound")]
	public string DrinkSound { get; set; } = "";

	public static IReadOnlyList<CondoAssetBase> CondoItems => _CondoItems;
	internal static List<CondoAssetBase> _CondoItems = new();

	public IReadOnlyList<CondoAssetBase> ReadAllCondoItems()
	{
		return _CondoItems;
	}

	protected override void PostLoad()
	{
		base.PostLoad();

		if ( !_CondoItems.Contains( this ) )
			_CondoItems.Add( this );
	}

	[ConCmd.Server("tr.item.buy")]
	public static void BuyItem(string itemName, long id = -1)
	{
		var caller = ConsoleSystem.Caller.Pawn as MainPawn;
		if ( caller == null ) return;

		if ( id != ConsoleSystem.Caller.SteamId )
			return;

		if ( ResourceLibrary.TryGet( $"assets/condo/{itemName}.citm", out CondoAssetBase asset ) )
		{
			var model = new CondoItemBase();
			model.SpawnFromAsset( asset );
			model.Position = caller.GetEyeTrace( 999.0f ).EndPosition;
			caller.Credits -= asset.BaseCost;
		}
	}
}

