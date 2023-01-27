using Sandbox;
using System;
using TowerResort.Player;

namespace TowerResort.Achievements;

public class PartyAddict : AchBase
{
	public override string Name => "Party Addict";
	public override string Description => "Have a party of at least 4 people (excluding yourself) in your condo for 60 minutes";
	public override bool IsSecret => false;
	public override int PerUpdateNotify => 10;
	public override int Goal => 60;
	public override Type PawnType => typeof( LobbyPawn );
}

