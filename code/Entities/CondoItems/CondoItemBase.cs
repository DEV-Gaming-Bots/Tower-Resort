using Components.NotificationManager;
using Sandbox;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TowerResort.Entities.Base;
using TowerResort.Player;
using TowerResort.GameComponents;

namespace TowerResort.Entities.CondoItems;

public partial class CondoItemBase : AnimatedEntity, IUse
{
	[Net] public CondoAssetBase Asset { get; private set; }
	Particles effects { get; set; }

	bool isToggled = false;
	Sound curSound;
	TimeUntil cooldown;
	int totalUses;

	[Net] bool IsPreviewing { get; set; } = false;

	public void SpawnFromAsset( CondoAssetBase asset, Entity owner = null )
	{
		Asset = asset;
		Owner = owner;

		SetModel( Asset.ModelPath );
		SetupPhysicsFromModel( PhysicsMotionType.Static );

		if ( Asset.Type == CondoAssetBase.ItemEnum.Playable )
		{	
			switch ( Asset.GameType )
			{
				case CondoAssetBase.GameEnum.Poker:
					Components.Create<PokerGame>();
					break;
			}
		}

		if(Asset.Type == CondoAssetBase.ItemEnum.Sittable)
		{
			CreateParticles( To.Everyone );
		}

		if(Asset.Type == CondoAssetBase.ItemEnum.Drinkable)
		{
			totalUses = asset.TotalDrinkUses;
		}

	}

	public void DisplayMessage( List<IClient> clients, string msg, float time = 5.0f )
	{
		ClientMessage( To.Multiple( clients ), msg, time );
	}

	public void DisplayMessage( LobbyPawn player, string msg, float time = 5.0f)
	{
		ClientMessage( To.Single( player ), msg, time );
	}

	[ClientRpc]
	public void ClientMessage(string message, float lifeTime)
	{
		BaseHud.Current.NotificationManager.AddNotification( message, NotificationType.Info, lifeTime );
	}

	[ClientRpc]
	public void CreateParticles()
	{
		effects = Particles.Create( "particles/direction_helper/direction_circle.vpcf", this );
	}

	[ClientRpc]
	public void ParticleDestruction()
	{
		effects?.Destroy();
	}

	[Event.Tick.Client]
	public void ParticleSimulation()
	{
		if ( Asset == null ) return;

		if(Asset.Type == CondoAssetBase.ItemEnum.Sittable && IsPreviewing )
		{
			Transform bone = GetBoneTransform( "lookdir" );
			Vector3 pos = bone.Position + bone.Rotation.Forward * 500;

			effects.SetPosition( 1, pos );
			effects.SetPosition( 2, Position );
		}
	}

	public override void Spawn()
	{
		base.Spawn();

		SetupPhysicsFromModel(PhysicsMotionType.Static);
	}

	protected override void OnDestroy()
	{
		if ( Game.IsServer )
		{
			if ( Asset == null ) return;

			curSound.Stop();

			if ( Asset.Type == CondoAssetBase.ItemEnum.Sittable )
			{
				if ( Sitter != null )
				{
					RemoveSittingPlayer( Sitter );
				}
			}

			if ( Asset.Type == CondoAssetBase.ItemEnum.Playable )
			{
				Components.Get<PokerGame>().RemovePlayers();
			}

			ParticleDestruction( To.Everyone );
		}
	}

	public bool IsUsable( Entity user )
	{
		if ( cooldown > 0.0f )
			return false;

		if ( Asset.Type == CondoAssetBase.ItemEnum.Sittable )
			return Sitter == null && Asset.IsInteractable;

		if ( Asset.OwnerOnly && Owner != user )
			return false;

		return Asset.IsInteractable;
	}

	public void CreateFromLoot(CondoAssetBase asset)
	{
		if ( asset == null )
			return;

		var lootedItem = new CondoItemBase();
		lootedItem.SpawnFromAsset( asset );
		lootedItem.Position = Position;

		if ( !string.IsNullOrEmpty( Asset.UnboxSound ) )
			lootedItem.PlaySound( Asset.UnboxSound );

		if ( !string.IsNullOrEmpty( Asset.ParticleName ) )
			Particles.Create( Asset.ParticleName, this, false );

		Delete();
	}

	public void DoUnboxing( Entity user )
	{
		CreateFromLoot( Asset.LootItems[Game.Random.Int(0, Asset.LootItems.Length-1)].Item );
	}

	public void DoSounds( Entity user )
	{
		if ( Asset.Toggable )
		{
			if ( !isToggled )
			{
				curSound.Stop();
				cooldown = (float)Asset.InteractCooldown;
				return;
			}

			curSound = PlaySound( Asset.InteractSound );
		}
		else
		{
			if ( !curSound.Finished )
				curSound.Stop();

			curSound = PlaySound( Asset.InteractSound );
		}
	}

	public void DoDrinking( Entity user )
	{
		if(user is LobbyPawn player)
		{
			Sound.FromEntity( Asset.DrinkSound, this );

			if ( Asset.IsAlcohol )
			{
				player.Drunkiness += 15.0f;
				player.TimeDrank = 0;
			}

			if ( totalUses == -1 )
				return;

			totalUses--;

			if ( totalUses <= 0 )
				Delete();
		}
	}

	public bool OnUse( Entity user )
	{
		if ( !IsUsable( user ) )
			return false;

		if ( Game.IsClient ) return false;

		if ( Asset.Toggable )
			isToggled = !isToggled;

		if ( Asset.Type == CondoAssetBase.ItemEnum.Drinkable )
			DoDrinking( user );

		if ( Asset.Type == CondoAssetBase.ItemEnum.Loot )
			DoUnboxing( user );

		if ( Asset.Type == CondoAssetBase.ItemEnum.Sound )
			DoSounds( user );

		if ( Asset.Type == CondoAssetBase.ItemEnum.Playable )
		{
			if ( Asset.GameType == CondoAssetBase.GameEnum.Unspecified )
			{
				Log.Warning( "TowerResort - This playable item has no specified game" );
				return false;
			}

			var player = user as LobbyPawn;

			switch (Asset.GameType)
			{
				case CondoAssetBase.GameEnum.Poker:
					var c = Components.GetOrCreate<PokerGame>();
					if ( !c.Players.Contains( player ) && player.FocusedEntity == null )
						c.JoinTable( player );
					else if ( player.FocusedEntity != null && player.FocusedEntity == this)
						player.FocusedEntity.Components.Get<PokerGame>().LeaveTable( player );
					break;
			}
		}

		if ( Asset.Type == CondoAssetBase.ItemEnum.Sittable )
			Sitdown( user );

		if( isToggled )
		{
			if ( Asset.InteractionBodyGroup != -1 )
				SetBodyGroup( Asset.InteractionBodyGroup, 1 );

			if ( !string.IsNullOrEmpty( Asset.InteractionMaterialOverride ) )
				SetMaterialOverride( Asset.InteractionMaterialOverride );
		} 
		else if( !isToggled )
		{
			if ( Asset.InteractionBodyGroup != -1 )
				SetBodyGroup( Asset.InteractionBodyGroup, 0 );

			if ( !string.IsNullOrEmpty( Asset.InteractionMaterial ) )
				SetMaterialOverride( Asset.InteractionMaterial );
		}
		
		cooldown = (float)Asset.InteractCooldown;
		var pawn = user as MainPawn;

		if ( pawn == null ) return false;

		if ( pawn.ActiveChild != null )
		{
			(pawn.ActiveChild as WeaponBase).ActiveEnd( pawn.ActiveChild, false );
			pawn.ActiveChild = null;
		}
		return false;
	}
}

