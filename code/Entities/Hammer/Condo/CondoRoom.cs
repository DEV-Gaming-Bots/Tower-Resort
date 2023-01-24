using Sandbox;
using Editor;
using System.Collections.Generic;
using TowerResort.Entities.CondoItems;
using TowerResort.Entities.Condos;
using static TowerResort.Entities.Hammer.CondoRoom;

namespace TowerResort.Entities.Hammer;

[Library( "tr_condo_logic" )]
[Title( "Condo Logic" ), Description( "The condo's logic which can load and save condos" ) , Category( "Condo" )]
[HammerEntity]

public partial class CondoRoom : Entity
{
	CondoBase Condo;

	static CondoRoom testCondo;

	List<CondoAssetBase> entAssets;

	[Property]
	public EntityTarget LeaveDestination { get; set; }

	List<(Vector3 Pos, Rotation Rot)> EntityInfo;
	public override void Spawn()
	{
		base.Spawn();

		entAssets = new();
		testCondo = this;
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

		if ( ent is MainPawn )
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

	public void SpawnCondo( CondoType condoType = CondoType.Small )
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

	public void LoadCondo()
	{
		int index = 0;

		foreach ( var ent in entAssets )
		{
			Log.Info( ent.Name );

			var item = new CondoItemBase();
			item.SpawnFromAsset( ent );
			item.Position = EntityInfo[index].Pos;
			item.Rotation = EntityInfo[index].Rot;

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

	public void SaveCondoContents()
	{
		DebugOverlay.Box( Condo.WorldSpaceBounds, Color.Green, 5);
		EntityInfo = new();

		foreach ( var entity in FindInBox( Condo.WorldSpaceBounds ) )
		{
			if ( !FilterCheck( entity ) )
				continue;

			if(entity is CondoItemBase item)
			{
				Log.Info( item.Name );
			}

			entAssets.Add( (entity as CondoItemBase).Asset );
			EntityInfo.Add( (entity.Position, entity.Rotation) );
		}
	}

	public void ClearCondo()
	{
		foreach ( var entity in FindInBox( Condo.WorldSpaceBounds ) )
		{
			if ( !FilterCheck( entity ) )
				continue;

			//EntityInfo.Add( (entity.Position, entity.Rotation) );

			Log.Info( $"Deleting {entity.Name}" );
			entity.Delete();
		}

		DestroyLights( To.Everyone );
		Condo.Delete();
	}

	[ConCmd.Server( "tr.condo.assign" )]
	public static void CondoAssignCMD()
	{
		if ( !TRGame.AdminIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		var pawn = ConsoleSystem.Caller.Pawn as LobbyPawn;
		if ( pawn == null ) return;

		if ( pawn.AssignedCondo != null ) return;

		pawn.AssignedCondo = testCondo;
		pawn.AssignedCondo.Owner = pawn;
		Log.Info( $"{pawn.Name} assigned to {testCondo}" );
	}

	[ConCmd.Server("tr.condo.save")]
	public static void CondoSaveCMD()
	{
		if ( !TRGame.AdminIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		var pawn = ConsoleSystem.Caller.Pawn as LobbyPawn;
		if ( pawn == null ) return;

		if ( pawn.AssignedCondo == null ) return;

		pawn.AssignedCondo.SaveCondoContents();
	}

	[ConCmd.Server( "tr.condo.load" )]
	public static void CondoLoadCMD()
	{
		if ( !TRGame.AdminIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		var pawn = ConsoleSystem.Caller.Pawn as LobbyPawn;
		if ( pawn == null ) return;

		if ( pawn.AssignedCondo == null ) return;

		pawn.AssignedCondo.LoadCondo();
	}

	[ConCmd.Server( "tr.condo.spawn" )]
	public static void CondoSpawnCMD()
	{
		if ( !TRGame.AdminIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		var pawn = ConsoleSystem.Caller.Pawn as LobbyPawn;
		if ( pawn == null ) return;

		if ( pawn.AssignedCondo == null ) return;

		pawn.AssignedCondo.SpawnCondo();
	}

	[ConCmd.Server( "tr.condo.clear" )]
	public static void CondoClearCMD()
	{
		if ( !TRGame.AdminIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		var pawn = ConsoleSystem.Caller.Pawn as LobbyPawn;
		if ( pawn == null ) return;

		if ( pawn.AssignedCondo == null ) return;

		pawn.AssignedCondo.ClearCondo();
	}
}
