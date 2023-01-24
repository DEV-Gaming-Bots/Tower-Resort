using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TowerResort.Entities.Hammer;

namespace TowerResort.Player;

public partial class LobbyPawn
{
	public CondoRoom AssignedCondo { get; set; }

	//Assigns the player their random but available condo
	public void AssignCondo()
	{
		var condos = All.OfType<CondoRoom>().OrderBy( c => Guid.NewGuid() ).ToList();
		var newCondo = condos.FirstOrDefault();

		int attempts = 0;
		//If the first condo is unclaimable, find one until there is one
		//if there aren't any, just break the loop
		while (!newCondo.IsClaimable())
		{
			if( attempts >= All.OfType<CondoRoom>().Count() )
			{
				Log.Warning( "There are no condos available" );
				break;
			}

			condos.Remove( newCondo );
			newCondo = condos.FirstOrDefault();
			attempts++;
		}

		newCondo.Owner = this;
		newCondo.SpawnRoom();
		newCondo.Load();
		AssignedCondo = newCondo;
	}

	//Clears the condo
	//if its a wipe just wipe all the placed furniture inside
	//otherwise wipe, remove owner and delete the condo
	public void ClearCondo(bool isWipe = false)
	{
		if ( AssignedCondo == null ) return;

		if(!isWipe)
		{
			UnclaimCondo();
		} 
		else
		{
			AssignedCondo.Wipe();
		}

	}

	//Saves their assigned condo if any
	public void SaveCondo()
	{
		if ( AssignedCondo == null ) return;

		AssignedCondo.SaveContents();
	}

	//Unclaim the condo they have and clear any player placed furniture inside
	public void UnclaimCondo()
	{
		if ( AssignedCondo == null ) return;

		AssignedCondo.ClearRoom();
		AssignedCondo.Owner = null;
		AssignedCondo = null;
	}

	[ConCmd.Server( "tr.condo.assign" )]
	public static void CondoAssignCMD()
	{
		if ( !TRGame.AdminIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		var pawn = ConsoleSystem.Caller.Pawn as LobbyPawn;
		if ( pawn == null ) return;

		pawn.AssignCondo();
	}

	[ConCmd.Server( "tr.condo.save" )]
	public static void CondoSaveCMD()
	{
		if ( !TRGame.AdminIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		var pawn = ConsoleSystem.Caller.Pawn as LobbyPawn;
		if ( pawn == null ) return;

		pawn.SaveCondo();
	}

	[ConCmd.Server( "tr.condo.load" )]
	public static void CondoLoadCMD()
	{
		if ( !TRGame.AdminIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		var pawn = ConsoleSystem.Caller.Pawn as LobbyPawn;
		if ( pawn == null ) return;

		Log.Info( pawn.AssignedCondo );

		if ( pawn.AssignedCondo == null ) return;

		pawn.AssignedCondo.Load();
	}

	[ConCmd.Server( "tr.condo.spawn" )]
	public static void CondoSpawnCMD()
	{
		if ( !TRGame.AdminIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		var pawn = ConsoleSystem.Caller.Pawn as LobbyPawn;
		if ( pawn == null ) return;

		if ( pawn.AssignedCondo == null ) return;

		pawn.AssignedCondo.SpawnRoom();
	}

	[ConCmd.Server( "tr.condo.clear" )]
	public static void CondoClearCMD(bool wipe = false)
	{
		if ( !TRGame.AdminIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		var pawn = ConsoleSystem.Caller.Pawn as LobbyPawn;
		if ( pawn == null ) return;

		pawn.ClearCondo( wipe );
	}
}
