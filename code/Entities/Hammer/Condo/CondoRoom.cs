using Sandbox;
using Editor;
using System.Collections.Generic;
using TowerResort.Entities.CondoItems;
using TowerResort.Entities.Condos;
using static TowerResort.Entities.Hammer.CondoRoom;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace TowerResort.Entities.Hammer;

[Library( "tr_condo_logic" )]
[Title( "Condo Logic" ), Description( "The condo's logic which can load and save condos" ) , Category( "Condo" )]
[HammerEntity]

public partial class CondoRoom : Entity
{
	public CondoBase Condo { get; protected set; }

	[Property]
	public EntityTarget LeaveDestination { get; set; }

	public override void Spawn()
	{
		base.Spawn();
	}

	public bool IsClaimable()
	{
		return Owner == null;
	}

	public override void ClientSpawn()
	{
		base.ClientSpawn();

		condoLights = new();
	}

	public bool FilterCheck(Entity ent)
	{
		if ( ent is WorldEntity )
			return false;

		if ( ent is LobbyPawn )
			return false;

		if ( ent == Condo ) 
			return false;

		return true;
	}

	public enum CondoType
	{
		Small,
		Classic,
		//TODO, get more condo styles
	}

	public void SpawnRoom( CondoType condoType = CondoType.Small )
	{
		switch ( condoType )
		{
			case CondoType.Small: Condo = new CondoBase(); break;
				//case CondoType.Classic: Condo = new ClassicCondo(); break;
		}

		if ( Condo == null )
		{
			return;
		}

		Condo.Position = Position;
		Condo.Rotation = Rotation;

		int index = 0;

		foreach ( var lightPos in Condo.LightPositions )
		{
			PlaceLights( To.Everyone, Condo.Position + lightPos, Condo.LightRadius[index], Condo.LightColor[index] );
			index++;
		}

		DoorTeleporter door = new DoorTeleporter();
		door.SetModel( "models/citizen_props/crate01.vmdl" );
		door.Position = Position + Condo.DoorPos;
		door.TargetDest = LeaveDestination;
		door.SetParent( Condo );
	}

	public void Load()
	{
		var owner = Owner as LobbyPawn;
		int index = 0;

		foreach ( var asset in owner.CondoInfoAsset )
		{
			Log.Info( owner.CondoInfoPosition[index] );

			var item = new CondoItemBase();
			item.SetParent( Condo );

			if ( ResourceLibrary.TryGet( $"assets/condo/{asset}.citm", out CondoAssetBase found ) )
			{
				item.SpawnFromAsset( found );
				Log.Info( found );
			}

			item.Position = Position + owner.CondoInfoPosition[index];
			item.Rotation = owner.CondoInfoRotation[index];
			index++;
		}
	}

	List<SceneLight> condoLights;

	[ClientRpc]
	public void PlaceLights( Vector3 pos, float radius, Color colour )
	{
		SceneLight light = new SceneLight( Scene, pos, radius, colour );
		condoLights.Add( light );
	}

	[ClientRpc]
	public void DestroyLights()
	{
		foreach ( var light in condoLights.ToArray() )
		{
			light?.Delete();
		}

		condoLights.Clear();
	}

	public void SaveContents()
	{
		DebugOverlay.Box( Condo.WorldSpaceBounds, Color.Green, 5);
		List<(Vector3, Rotation)> entInfo = new();
		List<CondoAssetBase> entAssets = new();

		foreach ( var entity in FindInBox( Condo.WorldSpaceBounds ) )
		{
			if ( !FilterCheck( entity ) )
				continue;

			if( entity is CondoItemBase item )
			{
				entAssets.Add( item.Asset );
				entInfo.Add( (entity.Position - Position, entity.Rotation) );
			}
		}

		int index = 0;
		foreach ( var item in entAssets )
		{
			(Owner as LobbyPawn).CondoInfoAsset.Add( item.ResourceName );
			(Owner as LobbyPawn).CondoInfoPosition.Add( entInfo[index].Item1 );
			(Owner as LobbyPawn).CondoInfoRotation.Add( entInfo[index].Item2 );
			index++;
		}
	}

	public void ClearRoom()
	{
		foreach ( var entity in FindInBox( Condo.WorldSpaceBounds ) )
		{
			if ( !FilterCheck( entity ) )
				continue;

			Log.Info( $"Deleting {entity.Name}" );
			entity.Delete();
		}

		DestroyLights( To.Everyone );
		Condo.Delete();
	}

	public void Wipe()
	{
		foreach ( var entity in FindInBox( Condo.WorldSpaceBounds ) )
		{
			if ( !FilterCheck( entity ) )
				continue;

			Log.Info( $"Deleting {entity.Name}" );
			entity.Delete();
		}
	}
}
