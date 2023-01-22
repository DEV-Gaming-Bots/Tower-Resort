using Sandbox;
using Editor;
using System.Linq;
using TheHub.Player;
using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using TheHub.Entities.CondoItems;

namespace TheHub.Entities.Hammer;

[Library( "hub_condo_room" )]
[Title( "Condo Room" ), Description( "Defines a condo room" ), Category( "Condo" )]
[BoundsHelper( "MinRoomBox", "MaxRoomBox" )]
[HammerEntity]

public class CondoRoom : Entity
{
	public static CondoRoom StaticCondo;

	[Property]
	public Vector3 MinRoomBox { get; set; }

	[Property]
	public Vector3 MaxRoomBox { get; set; }

	CondoBoundaries Condo;

	BBox condoBox;

	List<CondoItemBase> test;
	List<(Vector3 Pos, Rotation Rot)> EntityInfo;
	public override void Spawn()
	{
		base.Spawn();

		StaticCondo = this;
		condoBox = new BBox( WorldSpaceBounds.Mins + MinRoomBox, WorldSpaceBounds.Maxs + MaxRoomBox );

		var ents = FindInBox( condoBox );

		test = new();
	}

	public bool FilterCheck(Entity ent)
	{
		if ( ent is WorldEntity )
			return false;

		if ( ent is MainPawn )
			return false;

		if ( ent is CondoRoom )
			return false;

		return true;
	}

	public void LoadCondo()
	{
		int index = 0;

		//Condo.Position = Position;

		Log.Info( Condo );


		foreach ( var entity in test )
		{
			var ent = CreateByName( entity.ClassName );

			if ( ent == null ) continue;

			ent.Position = EntityInfo[index].Pos;
			ent.Rotation = EntityInfo[index].Rot;

			ent.Spawn();
			Log.Info( $"Loaded {ent.Name}" );
			index++;
		}

		//test.Clear();
		//EntityInfo.Clear();
	}

	public void SaveCondoContents()
	{
		DebugOverlay.Box( condoBox, Color.Green, 5);
		EntityInfo = new();

		foreach ( var entity in FindInBox( condoBox ) )
		{
			if ( !FilterCheck( entity ) )
				continue;

			EntityInfo.Add( (entity.Position, entity.Rotation) );

			test.Add( entity as CondoItemBase );
			Log.Info($"Saving {entity.Name}");
		}

		Log.Info( "Saved Condo" );
	}
	public void ClearCondo(bool despawnCondo = false)
	{
		foreach ( var entity in FindInBox( condoBox ) )
		{
			if ( !FilterCheck( entity ) )
				continue;

			EntityInfo.Add( (entity.Position, entity.Rotation) );
			test.Add( entity as CondoItemBase );

			Log.Info( $"Deleting {entity.Name}" );
			entity.Delete();
		}
	}

	[ConCmd.Server( "hub.condo.assign" )]
	public static void CondoAssignCMD()
	{
		if ( !MainGame.AdminIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		var pawn = ConsoleSystem.Caller.Pawn as LobbyPawn;
		if ( pawn == null ) return;

		if ( pawn.AssignedCondo != null ) return;

		pawn.AssignedCondo = StaticCondo;
		Log.Info( $"{pawn.Name} assigned to {StaticCondo}" );
	}

	[ConCmd.Server("hub.condo.save")]
	public static void CondoSaveCMD()
	{
		if ( !MainGame.AdminIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		var pawn = ConsoleSystem.Caller.Pawn as LobbyPawn;
		if ( pawn == null ) return;

		if ( pawn.AssignedCondo == null ) return;

		pawn.AssignedCondo.SaveCondoContents();
	}

	[ConCmd.Server( "hub.condo.load" )]
	public static void CondoLoadCMD()
	{
		if ( !MainGame.AdminIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		var pawn = ConsoleSystem.Caller.Pawn as LobbyPawn;
		if ( pawn == null ) return;

		if ( pawn.AssignedCondo == null ) return;

		pawn.AssignedCondo.LoadCondo();
	}

	[ConCmd.Server( "hub.condo.clear" )]
	public static void CondoClearCMD(bool shouldRemove = false)
	{
		if ( !MainGame.AdminIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		var pawn = ConsoleSystem.Caller.Pawn as LobbyPawn;
		if ( pawn == null ) return;

		if ( pawn.AssignedCondo == null ) return;

		pawn.AssignedCondo.ClearCondo( shouldRemove );
	}
}

[Library( "hub_condo_boundaries" )]
[Title( "Condo Boundaries" ), Description( "Defines the condo's boundaries" ), Category( "Condo" )]
[HammerEntity, Solid]
public class CondoBoundaries : ModelEntity
{
	public override void Spawn()
	{
		base.Spawn();

		SetupPhysicsFromModel( PhysicsMotionType.Static );
	}
}

