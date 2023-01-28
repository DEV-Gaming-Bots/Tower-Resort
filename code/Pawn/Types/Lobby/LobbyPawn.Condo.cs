using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using TowerResort.Entities.Condos;
using TowerResort.Entities.Hammer;

namespace TowerResort.Player;

public partial class LobbyPawn
{
	public CondoRoom AssignedCondo { get; set; }
	public bool PartyActive { get; set; } = false;

	public TimeSince TimePartyHappen;

	//When a condo owner wants to start a party
	public void StartCondoParty()
	{
		if ( AssignedCondo == null || PartyActive ) return;

		PartyActive = true;
		TimePartyHappen = 0;

		string condoRoom = AssignedCondo.Name.Replace( "_", " " );

		TRChat.AddChatEntryStatic(To.Everyone, "SERVER", $"{Client.Name} is hosting a party at {condoRoom}");
	}

	//Ends the party
	public void EndCondoParty()
	{
		if ( AssignedCondo == null || !PartyActive ) return;

		PartyActive = false;
		TRChat.AddChatEntryStatic( To.Everyone, "SERVER", $"{Client.Name}'s party has ended" );
	}

	int presentPlayers = 0;
	TimeUntil timeUpdate;

	//Condo simulation
	public void CondoSimulate()
	{
		if ( Game.IsClient || AssignedCondo == null ) return;

		if( PartyActive && TimePartyHappen > (60 * 5) )
		{
			EndCondoParty();
		}

		var players = FindInBox( AssignedCondo.Condo.WorldSpaceBounds ).Where(x => x is LobbyPawn && x != this );
		presentPlayers = players.Count();

		if ( presentPlayers < 4 )
			timeUpdate = 60.0f;

		if ( timeUpdate <= 0 )
		{
			AchTracker.UpdateAchievement( typeof( PartyAddict ) );
			Log.Info( $"A minute has passed - {AchTracker.GetAchievementProgress(typeof(PartyAddict))}" );
			timeUpdate = 60.0f;
		}
	}

	//Assigns the player their random but available condo
	public void AssignCondo()
	{
		var condos = All.OfType<CondoRoom>().OrderBy( c => Guid.NewGuid() ).ToList();
		var newCondo = condos.FirstOrDefault();

		int attempts = 0;

		//If the first condo is unclaimable, find one until there is one
		//if there aren't any, just break the loop
		while ( !newCondo.IsClaimable() )
		{
			if( attempts >= All.OfType<CondoRoom>().Count() )
			{
				DisplayNotification( To.Single( this ), "There are no condos available", 5.0f );
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

		string condoName = newCondo.Name.Replace("_", " ");

		DisplayNotification( To.Single( this ), $"You have been checked into {condoName}", 5.0f);
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

		SaveCondo();
		DisplayNotification( To.Single( this ), $"You have checked out of your condo", 5.0f );
		EndCondoParty();

		AssignedCondo.ClearRoom();
		AssignedCondo.Owner = null;
		AssignedCondo = null;

		TRGame.Instance.DoSave( Client );
	}


	//BELOW IS TEMPORARY, Used for condo testing, might have a use later

	[ConCmd.Server("tr.condo.party.start")]
	public static void CondoPartyStartCMD()
	{
		if ( !TRGame.DevIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		var pawn = ConsoleSystem.Caller.Pawn as LobbyPawn;
		if ( pawn == null ) return;

		pawn.StartCondoParty();
	}

	[ConCmd.Server( "tr.condo.party.end" )]
	public static void CondoPartyEndCMD()
	{
		if ( !TRGame.DevIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		var pawn = ConsoleSystem.Caller.Pawn as LobbyPawn;
		if ( pawn == null ) return;

		pawn.EndCondoParty();
	}

	[ConCmd.Server( "tr.condo.assign" )]
	public static void CondoAssignCMD()
	{
		if ( !TRGame.DevIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		var pawn = ConsoleSystem.Caller.Pawn as LobbyPawn;
		if ( pawn == null ) return;

		pawn.AssignCondo();
	}

	[ConCmd.Server( "tr.condo.save" )]
	public static void CondoSaveCMD()
	{
		if ( !TRGame.DevIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		var pawn = ConsoleSystem.Caller.Pawn as LobbyPawn;
		if ( pawn == null ) return;

		pawn.SaveCondo();
	}

	[ConCmd.Server( "tr.condo.load" )]
	public static void CondoLoadCMD()
	{
		if ( !TRGame.DevIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		var pawn = ConsoleSystem.Caller.Pawn as LobbyPawn;
		if ( pawn == null ) return;

		if ( pawn.AssignedCondo == null ) return;

		pawn.AssignedCondo.Load();
	}

	[ConCmd.Server( "tr.condo.spawn" )]
	public static void CondoSpawnCMD()
	{
		if ( !TRGame.DevIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		var pawn = ConsoleSystem.Caller.Pawn as LobbyPawn;
		if ( pawn == null ) return;

		if ( pawn.AssignedCondo == null ) return;

		pawn.AssignedCondo.SpawnRoom();
	}

	[ConCmd.Server( "tr.condo.clear" )]
	public static void CondoClearCMD(bool wipe = false)
	{
		if ( !TRGame.DevIDs.Contains( ConsoleSystem.Caller.SteamId ) ) return;

		var pawn = ConsoleSystem.Caller.Pawn as LobbyPawn;
		if ( pawn == null ) return;

		pawn.ClearCondo( wipe );
	}
}
