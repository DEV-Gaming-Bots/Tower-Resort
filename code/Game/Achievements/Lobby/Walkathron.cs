using Sandbox;
using System;
using TowerResort.Player;

namespace TowerResort.Achievements;

public class Walkathron : AchBase
{
	public override string Name => "Walkathron";
	public override string Description => "Walk a total of 1000 steps";
	public override bool IsSecret => false;
	public override int PerUpdateNotify => 100;
	public override int Goal => 10;
	public override Type PawnType => typeof( LobbyPawn );
}

