using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheHub.Player;

namespace TheHub.Achievements;

public class Gambler : AchBase
{
	public override string Name => "Wise Gambler";
	public override string Description => "Bet/Raise over 1000 chips";
	public override bool IsSecret => false;
	public override int PerUpdateNotify => 250;
	public override int Goal => 1000;
	public override Type PawnType => typeof( LobbyPawn );
}
