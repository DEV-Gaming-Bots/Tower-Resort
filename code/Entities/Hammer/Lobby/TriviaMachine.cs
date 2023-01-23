using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TowerResort.GameComponents;

namespace TowerResort.Entities.Lobby;

public partial class TriviaMachine : ModelEntity, IUse
{
	TriviaMania triviaGame;

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/gamemodes/poker/table/small/small_pokertable.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

		triviaGame = Components.Create<TriviaMania>();
	}

	public bool IsUsable( Entity user )
	{
		return triviaGame.CurGameStatus == TriviaMania.GameStatus.Idle 
			|| triviaGame.CurGameStatus == TriviaMania.GameStatus.Starting;
	}

	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );
	}

	public bool OnUse( Entity user )
	{
		var player = user as LobbyPawn;
		if ( player == null ) return false;

		if ( triviaGame.Players.Contains( player ) )
			triviaGame.LeaveGame( player );
		else
			triviaGame.JoinGame( player );

		return false;
	}
}

