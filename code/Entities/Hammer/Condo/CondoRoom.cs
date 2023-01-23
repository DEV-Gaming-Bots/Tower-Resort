using Sandbox;
using Editor;
using System.Collections.Generic;
using TowerResort.Entities.CondoItems;
using TowerResort.Entities.Condos;

namespace TowerResort.Entities.Hammer;

[Library( "tr_condo_logic" )]
[Title( "Condo Logic" ), Description( "The condo's logic which can load and save condos" ) , Category( "Condo" )]
[HammerEntity]

public partial class CondoRoom : Entity
{
	public BasicCondo Condo;

	static CondoRoom testCondo;

	List<Entity> entities;

	BBox condoBox;

	List<(Vector3 Pos, Rotation Rot)> EntityInfo;
	public override void Spawn()
	{
		base.Spawn();

		entities = new();

		foreach ( var ent in FindInBox(WorldSpaceBounds) )
		{
			Log.Info( ent );
		}

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

		if ( ent is CondoRoom )
			return false;

		if ( ent == Condo ) 
			return false;

		return true;
	}

	public void LoadCondo()
	{
		Condo = new BasicCondo();
		Condo.SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
		Condo.Position = Position;
		Condo.Rotation = Rotation;

		int index = 0;

		foreach ( var lightPos in Condo.LightPositions )
		{
			PlaceLights( To.Everyone, Condo.Position + lightPos, Condo.LightRadius[index], Condo.LightColor[index] );
			index++;
		}

		//condoBox = new BBox( Condo.WorldSpaceBounds.Mins, Condo.WorldSpaceBounds.Maxs );

		//Log.Info( Condo.Model.HasData<LightComponent>() );
		/*if( Condo.Model.GetAllData<LightComponent>() != null)
		{
			var test = Condo.Model.TryGetData<LightComponent>( out LightComponent data );

			Log.Info( test );

			//Decorate(To.Everyone, Condo.Model.GetData<LightComponent>() );
		}*/

		//var ents = FindInBox( condoBox );

		/*foreach ( var entity in test )
		{
			var ent = CreateByName( entity.ClassName );

			if ( ent == null ) continue;

			ent.Position = EntityInfo[index].Pos;
			ent.Rotation = EntityInfo[index].Rot;

			ent.Spawn();
			Log.Info( $"Loaded {ent.Name}" );
			index++;
		}*/

		//test.Clear();
		//EntityInfo.Clear();
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
		//Log.Info( Condo.Model.GetData<LightComponent>());

		DebugOverlay.Box( Condo.WorldSpaceBounds, Color.Green, 5);
		EntityInfo = new();

		foreach ( var entity in FindInBox( Condo.WorldSpaceBounds ) )
		{
			if ( !FilterCheck( entity ) )
				continue;

			//EntityInfo.Add( (entity.Position, entity.Rotation) );

			Log.Info($"Saving {entity.Name}");
		}
	}

	public void ClearCondo()
	{
		foreach ( var entity in FindInBox( condoBox ) )
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
