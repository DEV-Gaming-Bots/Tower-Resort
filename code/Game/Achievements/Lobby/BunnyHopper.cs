using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TowerResort.Player;

namespace TowerResort.Achievements;

public class BunnyHopper : AchBase
{
	public override string Name => "Bunny Hopper";
	public override string Description => "Jump a total of 1000 times";
	public override bool IsSecret => false;
	public override int PerUpdateNotify => 50;
	public override int Goal => 10;
	public override Type PawnType => typeof( LobbyPawn );
}

