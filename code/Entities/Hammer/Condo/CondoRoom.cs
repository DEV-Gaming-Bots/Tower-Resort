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

	public bool IsLoaded = false;

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
		door.OpenSound = "door_open";
		door.CloseSound = "door_close";
	}

	public void Load()
	{
		var owner = Owner as LobbyPawn;
		int index = 0;

		IsLoaded = true;

		if ( owner.CondoInfoAsset == null )
			return;

		foreach ( var asset in owner.CondoInfoAsset )
		{
			var item = new CondoItemBase();
			item.SetParent( Condo );

			if ( ResourceLibrary.TryGet( $"assets/condo/{asset}.citm", out CondoAssetBase found ) )
				item.SpawnFromAsset( found );

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

		var condoPlayer = Owner as LobbyPawn;

		condoPlayer.CondoInfoAsset = condoPlayer.DataFile?.CondoInfoAsset ?? new();
		condoPlayer.CondoInfoPosition = condoPlayer.DataFile?.CondoInfoPosition ?? new();
		condoPlayer.CondoInfoRotation = condoPlayer.DataFile?.CondoInfoRotation ?? new();

		condoPlayer.CondoInfoAsset.Clear();
		condoPlayer.CondoInfoPosition.Clear();
		condoPlayer.CondoInfoRotation.Clear();

		int index = 0;
		foreach ( var item in entAssets )
		{
			condoPlayer.CondoInfoAsset.Add( item.ResourceName );
			condoPlayer.CondoInfoPosition.Add( entInfo[index].Item1 );
			condoPlayer.CondoInfoRotation.Add( entInfo[index].Item2 );
			index++;
		}
	}

	public void ClearRoom()
	{
		foreach ( var entity in FindInBox( Condo.WorldSpaceBounds ) )
		{
			if ( entity is LobbyPawn pawn )
			{
				pawn.StartFading( To.Single( pawn ), 0.0f, 2.5f );
				pawn.Position = LeaveDestination.GetTarget( null ).Position;
				pawn.SetViewAngles( LeaveDestination.GetTarget( null ).Rotation.Angles() );
			}

			if ( !FilterCheck( entity ) )
				continue;

			entity.Delete();
		}

		DestroyLights( To.Everyone );
		Condo.Delete();
		IsLoaded = false;
	}

	public void Wipe()
	{
		foreach ( var entity in FindInBox( Condo.WorldSpaceBounds ) )
		{
			if ( !FilterCheck( entity ) )
				continue;

			entity.Delete();
		}
	}
}
