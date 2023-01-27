using Sandbox;
using System;
using TowerResort.Player;

namespace TowerResort.Achievements;

public class Walkathron : AchBase
{
	public override string Name => "Walkathron";
	public override string Description => "Walk a total of 200,000 steps";
	public override bool IsSecret => false;
	public override int PerUpdateNotify => 50000;
	public override int Goal => 200000;
	public override Type PawnType => typeof( LobbyPawn );
}

